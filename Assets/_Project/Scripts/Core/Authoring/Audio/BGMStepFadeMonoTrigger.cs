using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Project.Core.Authoring
{
	/// 基于 PlayerFootTracker 的 BGM 淡出触发器：踩到指定关卡方块坐标则淡出
	public class BGMStepFadeMonoTrigger : MonoBehaviour
	{
		[Tooltip("玩家脚步网格跟踪器（可自动查找）")] public PlayerFootTracker footTracker;
		[Tooltip("BGM 控制器（淡入/淡出/切换）")] public BackgroundMusicController bgmController;
		[Tooltip("触发配置（可自动查找场景中的第一个）")] public BGMStepFadeConfigAuthoring config;

		int3 _lastCell = new int3(int.MinValue, int.MinValue, int.MinValue);
		HashSet<long> _fired = new HashSet<long>();

		void Awake()
		{
			if (footTracker == null) footTracker = FindObjectOfType<PlayerFootTracker>();
			if (bgmController == null) bgmController = FindObjectOfType<BackgroundMusicController>();
			if (config == null) config = FindObjectOfType<BGMStepFadeConfigAuthoring>();
		}

		void Update()
		{
			if (footTracker == null || bgmController == null || config == null) return;
			float3 p = footTracker.player != null ? (float3)footTracker.player.position : float3.zero;
			float3 o = (float3)footTracker.origin;
			float size = Mathf.Max(0.0001f, footTracker.cellSize);
			int3 cell = new int3(Mathf.RoundToInt((p.x - o.x) / size), Mathf.RoundToInt((p.y - o.y) / size), Mathf.RoundToInt((p.z - o.z) / size));
			if (cell.Equals(_lastCell)) return;
			_lastCell = cell;

			var entries = config.entries;
			if (entries == null || entries.Count == 0) return;
			for (int i = 0; i < entries.Count; i++)
			{
				var e = entries[i];
				// 只按坐标判断（stageIndex 仅用于区分/去重，不做额外查询）
				if (e.useCoord)
				{
					if (e.coord.x != cell.x || e.coord.y != cell.y || e.coord.z != cell.z) continue;
				}
				long key = (((long)e.stageIndex & 0x7FFF) << 48) | (((long)cell.x & 0x1FFF) << 35) | (((long)cell.y & 0x1FFF) << 22) | (((long)cell.z & 0x1FFF) << 9) | (long)i;
				if (_fired.Contains(key)) continue;
				_fired.Add(key);
				bgmController.Stop(e.fadeSeconds);
				break;
			}
		}
	}
}




