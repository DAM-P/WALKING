using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Authoring
{
	[Serializable]
	public class BGMStepFadeEntry
	{
		[Tooltip("关卡索引（LevelSequence.stages 的 0-based）")] public int stageIndex = 0;
		[Tooltip("是否按坐标匹配")] public bool useCoord = true;
		[Tooltip("网格坐标（与 PlayerFootTracker 一致）")] public Vector3Int coord;
		[Tooltip("淡出时长（秒）")] public float fadeSeconds = 0.8f;
	}

	/// 配置：踩到指定关卡方块（坐标）时淡出 BGM
	public class BGMStepFadeConfigAuthoring : MonoBehaviour
	{
		public List<BGMStepFadeEntry> entries = new List<BGMStepFadeEntry>();
	}
}




