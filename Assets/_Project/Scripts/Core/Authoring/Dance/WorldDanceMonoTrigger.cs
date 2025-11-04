using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Project.Core.Components;

namespace Project.Core.Authoring
{
	/// Mono侧基于 PlayerFootTracker 的世界之舞触发器：避免等待 ECS 事件帧序
	public class WorldDanceMonoTrigger : MonoBehaviour
	{
		[Tooltip("玩家脚步网格跟踪器（可自动查找）")] public PlayerFootTracker footTracker;
		[Tooltip("触发配置（可自动查找场景中的第一个）")] public DanceTriggerConfigAuthoring triggerConfig;
		[Tooltip("音乐控制器（可选，若存在则直接触发播放）")] public WorldDanceAudioController audioController;

		EntityManager _em;
		int3 _lastCell = new int3(int.MinValue, int.MinValue, int.MinValue);
		HashSet<long> _fired = new HashSet<long>(); // 防止同格重复触发（stage<<40 | x<<26 | y<<13 | z）

		void Awake()
		{
			if (footTracker == null) footTracker = FindObjectOfType<PlayerFootTracker>();
			if (triggerConfig == null) triggerConfig = FindObjectOfType<DanceTriggerConfigAuthoring>();
			if (audioController == null) audioController = FindObjectOfType<WorldDanceAudioController>();
			if (World.DefaultGameObjectInjectionWorld != null)
				_em = World.DefaultGameObjectInjectionWorld.EntityManager;
		}

		void Update()
		{
			if (footTracker == null || triggerConfig == null) return;
			// 读取当前格（沿用 PlayerFootTracker 的逻辑）
			float3 p = footTracker.player != null ? (float3)footTracker.player.position : float3.zero;
			float3 o = (float3)footTracker.origin;
			float size = Mathf.Max(0.0001f, footTracker.cellSize);
			int3 cell = new int3(Mathf.RoundToInt((p.x - o.x) / size), Mathf.RoundToInt((p.y - o.y) / size), Mathf.RoundToInt((p.z - o.z) / size));
			if (cell.Equals(_lastCell)) return;
			_lastCell = cell;

			// 匹配配置（优先用按坐标的条目）。此处不依赖 ECS 变体/类型，直接用坐标触发，或按配置 typeId==-1 代表忽略类型
			var entries = triggerConfig.entries;
			if (entries == null || entries.Count == 0) return;
			for (int i = 0; i < entries.Count; i++)
			{
				var e = entries[i];
				if (e.useCoord)
				{
					if (e.coord.x != cell.x || e.coord.y != cell.y || e.coord.z != cell.z) continue;
				}
				// 构造去重键
				long key = (((long)e.stageIndex & 0x7FFF) << 48) | (((long)cell.x & 0x1FFF) << 35) | (((long)cell.y & 0x1FFF) << 22) | (((long)cell.z & 0x1FFF) << 9) | (long)i;
				if (_fired.Contains(key)) continue;
				_fired.Add(key);

				// 读取设置的序列间隔
				float interval = 0.5f;
				if (_em != default && _em.CreateEntityQuery(typeof(WorldDanceSettings)).CalculateEntityCount() > 0)
				{
					var se = _em.CreateEntityQuery(typeof(WorldDanceSettings)).GetSingletonEntity();
					var ws = _em.GetComponentData<WorldDanceSettings>(se);
					interval = Mathf.Max(0.01f, ws.SequenceIntervalSeconds);
				}

				// 直接发起从第0关开始的序列
				if (_em != default)
				{
					var evt = _em.CreateEntity(typeof(WorldDanceSequenceStart));
					_em.SetComponentData(evt, new WorldDanceSequenceStart { StartIndex = 0, EndIndex = -1, IntervalSeconds = interval });
				}

				// 立即起声（避免等待 ECS 帧）
				if (audioController != null)
				{
					audioController.BeginPlay();
				}

				break; // 命中一条后即触发
			}
		}
	}
}




