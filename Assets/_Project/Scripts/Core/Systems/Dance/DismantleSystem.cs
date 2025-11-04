using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Project.Core.Components;

namespace Project.Core.Systems
{
	[BurstCompile]
	public partial struct DismantleSystem : ISystem
	{
		public void OnUpdate(ref SystemState state)
		{
			float time = (float)SystemAPI.Time.ElapsedTime;
			var ecb = new EntityCommandBuffer(Allocator.Temp);

			foreach (var (dis, lt, entity) in SystemAPI.Query<RefRW<DanceDismantle>, RefRW<LocalTransform>>().WithEntityAccess())
			{
				float duration = dis.ValueRO.Duration;
				if (SystemAPI.TryGetSingleton<WorldDanceSettings>(out var settings))
				{
					if (settings.ScatterDuration > 0f) duration = settings.ScatterDuration;
				}
				float t = math.saturate((time - dis.ValueRO.StartTime) / math.max(0.0001f, duration));
				// easeOutCubic
				float ease = 1f - math.pow(1f - t, 3f);
				// pseudo-random direction
				uint h = dis.ValueRO.Seed;
				float3 dir = math.normalize(new float3(hash11(h), hash11(h * 1664525u + 1013904223u), hash11(h * 22695477u + 1u)) + new float3(0.001f, 0.002f, 0.003f));
				float noiseAmp = dis.ValueRO.NoiseAmplitude;
				if (SystemAPI.TryGetSingleton<WorldDanceSettings>(out var settings2))
				{
					if (settings2.ScatterPower > 0f) dir *= settings2.ScatterPower;
					if (settings2.ScatterNoise >= 0f) noiseAmp = settings2.ScatterNoise;
				}
				float wobble = (math.sin(time * 6f + hash11(h) * 10f) * 0.5f + 0.5f) * noiseAmp;
				float3 offset = dir * ease + new float3(0f, wobble, 0f);
				lt.ValueRW.Position = dis.ValueRO.BasePosition + offset;

				if (t >= 1f)
				{
					// Transition to flight; WorldDanceState holds settings
					if (SystemAPI.TryGetSingleton<WorldDanceState>(out var danceState))
					{
						float3 gatherPoint = new float3(0f, danceState.GatherHeight, 0f);
						// compute target on sphere by DanceIndex via Fibonacci sphere
						int idx = SystemAPI.GetComponent<DanceIndex>(entity).Value;
						int n = math.max(1, danceState.TotalParticipants);
						float3 target;
						var sset = SystemAPI.TryGetSingleton<WorldDanceSettings>(out var wd) ? wd : default;
						bool useRing = sset.RingEnabled != 0 && sset.RingShare > 0f && hash11((uint)(idx + 7)) < sset.RingShare;
						if (useRing)
						{
							// ring position in tilted plane
							float tRand = hash11((uint)(idx * 1103515245u + 12345u));
							float ang = tRand * math.PI * 2f;
							float rad = math.lerp(sset.RingInnerRadius, sset.RingOuterRadius, hash11((uint)(idx * 22695477u + 1u)));
							float3 p = new float3(math.cos(ang) * rad, 0f, math.sin(ang) * rad);
							quaternion tilt = quaternion.AxisAngle(new float3(1f,0f,0f), math.radians(sset.RingTiltDeg));
							p = math.mul(tilt, p);
							target = p + gatherPoint;
						}
						else
						{
							float3 sphere = FibonacciOnSphere(idx, n) * danceState.PlanetRadius + gatherPoint;
							target = sphere;
						}
						ecb.RemoveComponent<DanceDismantle>(entity);
						ecb.AddComponent(entity, new DanceFlight
						{
							StartTime = time,
							DurationGather = 0f,
							DurationToSphere = SystemAPI.TryGetSingleton<WorldDanceSettings>(out var s3) ? s3.FlightDuration : 2.0f,
							Delay = (SystemAPI.TryGetSingleton<WorldDanceSettings>(out var s4) ? s4.DelayStep : 0.03f) * (idx % 16),
							StartPos = lt.ValueRO.Position,
							GatherPoint = gatherPoint,
							TargetPos = target
						});
					}
				}
			}

			ecb.Playback(state.EntityManager);
		}

		static float hash11(uint x)
		{
			// 0..1
			x ^= 2747636419u; x *= 2654435769u; x ^= x >> 16; x *= 2654435769u; x ^= x >> 16; x *= 2654435769u;
			return (x & 0x00FFFFFFu) / 16777215f;
		}

		static float3 FibonacciOnSphere(int i, int n)
		{
			float k = i + 0.5f;
			float phi = math.acos(1f - 2f * k / n);
			float theta = math.PI * (1f + math.sqrt(5f)) * k;
			float x = math.sin(phi) * math.cos(theta);
			float y = math.cos(phi);
			float z = math.sin(phi) * math.sin(theta);
			return new float3(x, y, z);
		}
	}
}



