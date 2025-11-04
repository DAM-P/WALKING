using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Project.Core.Components;

namespace Project.Core.Systems
{
	[BurstCompile]
	public partial struct CubeRiseInSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<CubeRiseIn>();
		}

		public void OnUpdate(ref SystemState state)
		{
			float elapsed = (float)SystemAPI.Time.ElapsedTime;
			float dt = SystemAPI.Time.DeltaTime;
			var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

			foreach (var (lt, rise, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<CubeRiseIn>>().WithEntityAccess())
			{
				float t = (elapsed - rise.ValueRO.StartTime - rise.ValueRO.Delay) / math.max(0.0001f, rise.ValueRO.Duration);
				if (t <= 0f)
				{
					// 尚未开始
					continue;
				}
				t = math.saturate(t);
				float s = t * t * (3f - 2f * t); // smoothstep
				float3 p = math.lerp(rise.ValueRO.StartPos, rise.ValueRO.TargetPos, s);
				lt.ValueRW.Position = p;

				if (t >= 1f)
				{
					ecb.RemoveComponent<CubeRiseIn>(entity);
				}
			}

			ecb.Playback(state.EntityManager);
		}
	}
}


