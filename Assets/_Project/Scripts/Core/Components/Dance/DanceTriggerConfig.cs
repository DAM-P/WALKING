using Unity.Entities;
using Unity.Mathematics;

namespace Project.Core.Components
{
	public struct DanceTriggerEntry : IBufferElementData
	{
		public int StageIndex;
		public int TypeId;      // 依据 CubeVariantId
		public int3 Coord;      // 可选：精确到格坐标
		public byte UseCoord;   // 0/1 是否按坐标匹配
	}
}




