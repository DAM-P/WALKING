using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Project.Core.Components;

namespace Project.Core.Systems
{
	[BurstCompile]
	public partial struct PlanetSpinSystem : ISystem
	{
		public void OnUpdate(ref SystemState state)
		{
			if (!SystemAPI.TryGetSingleton<WorldDanceState>(out var dance)) return;
			if (!SystemAPI.TryGetSingleton<WorldDanceSettings>(out var settings)) return;
			float degPerSec = settings.PlanetSpinDegPerSec;
			if (math.abs(degPerSec) < 1e-4f) return;
			float dt = SystemAPI.Time.DeltaTime;
			float rad = math.radians(degPerSec) * dt;
			float3 center = new float3(0f, settings.GatherHeight, 0f);
			quaternion dq = quaternion.AxisAngle(new float3(0f,1f,0f), rad);

			// rotate all dance participants that are not in flight/dismantle
			foreach (var (lt, e) in SystemAPI.Query<RefRW<LocalTransform>>().WithEntityAccess())
			{
				if (!state.EntityManager.HasComponent<DanceIndex>(e)) continue;
				if (state.EntityManager.HasComponent<DanceFlight>(e)) continue;
				if (state.EntityManager.HasComponent<DanceDismantle>(e)) continue;
				float3 rel = lt.ValueRO.Position - center;
				rel = math.mul(dq, rel);
				lt.ValueRW.Position = center + rel;
			}
		}
	}
}




