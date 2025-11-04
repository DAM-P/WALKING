using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Project.Core.Components;

namespace Project.Core.Systems
{
	[BurstCompile]
	public partial struct CubeSpinSystem : ISystem
	{
		public void OnUpdate(ref SystemState state)
		{
			float dt = SystemAPI.Time.DeltaTime;
			foreach (var (spin, lt) in SystemAPI.Query<RefRO<CubeSpin>, RefRW<LocalTransform>>())
			{
				float rad = math.radians(spin.ValueRO.SpeedDegPerSec) * dt;
				quaternion dq = quaternion.AxisAngle(math.normalize(spin.ValueRO.Axis), rad);
				lt.ValueRW.Rotation = math.mul(dq, lt.ValueRO.Rotation);
			}
		}
	}
}




