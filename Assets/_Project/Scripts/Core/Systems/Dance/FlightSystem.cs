using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Project.Core.Components;

namespace Project.Core.Systems
{
	[BurstCompile]
	public partial struct FlightSystem : ISystem
	{
		public void OnUpdate(ref SystemState state)
		{
			float time = (float)SystemAPI.Time.ElapsedTime;
			var ecb = new EntityCommandBuffer(Allocator.Temp);

			foreach (var (flight, lt, entity) in SystemAPI.Query<RefRW<DanceFlight>, RefRW<LocalTransform>>().WithEntityAccess())
			{
				float tLocal = math.max(0f, time - flight.ValueRO.StartTime - flight.ValueRO.Delay);
				// Directly fly from start to target (no gather phase)
				float t = math.saturate(tLocal / math.max(0.0001f, flight.ValueRO.DurationToSphere));
				float3 p = math.lerp(flight.ValueRO.StartPos, flight.ValueRO.TargetPos, Smooth01(t));
				lt.ValueRW.Position = p;
				if (t >= 1f)
				{
					ecb.RemoveComponent<DanceFlight>(entity);
					// add cube spin once
					if (!state.EntityManager.HasComponent<CubeSpin>(entity))
					{
						float3 axis = math.normalize(new float3(hash11((uint)(entity.Index*3+1u))*2f-1f, hash11((uint)(entity.Index*7+3u))*2f-1f, hash11((uint)(entity.Index*11+5u))*2f-1f));
						if (math.lengthsq(axis) < 1e-6f) axis = new float3(0f,1f,0f);
						float minS = 20f, maxS = 60f;
						if (SystemAPI.TryGetSingleton<WorldDanceSettings>(out var s)) { minS = s.CubeSpinMinDegPerSec; maxS = s.CubeSpinMaxDegPerSec; }
						float speed = math.lerp(minS, maxS, hash11((uint)(entity.Index*13+9u)));
						ecb.AddComponent(entity, new CubeSpin { Axis = axis, SpeedDegPerSec = speed });
					}
				}
			}

			ecb.Playback(state.EntityManager);
		}

		static float Smooth01(float t)
		{
			return t * t * (3f - 2f * t);
		}

		static float hash11(uint x)
		{
			// 0..1 hash
			x ^= 2747636419u; x *= 2654435769u; x ^= x >> 16; x *= 2654435769u; x ^= x >> 16; x *= 2654435769u;
			return (x & 0x00FFFFFFu) / 16777215f;
		}
	}
}



