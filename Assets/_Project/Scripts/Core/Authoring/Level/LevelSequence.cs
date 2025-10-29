using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Authoring
{
	[CreateAssetMenu(menuName = "Level/Level Sequence", fileName = "LevelSequence")]
	public class LevelSequence : ScriptableObject
	{
		[Tooltip("按顺序引用的关卡 Stage 配置（作为最小单元）")]
		public List<StageConfig> stages = new List<StageConfig>();

		[Tooltip("从该索引开始播放（默认 0）")]
		public int startIndex = 0;
	}
}



