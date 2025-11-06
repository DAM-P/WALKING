using Unity.Entities;
using Unity.Mathematics;

namespace Project.Core.Components
{
	public struct CubeRiseIn : IComponentData
	{
		public float3 StartPos;
		public float3 TargetPos;
		public float StartTime;
		public float Duration;
		public float Delay;
	}
}






