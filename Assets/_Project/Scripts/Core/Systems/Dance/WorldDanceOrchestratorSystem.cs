using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Project.Core.Components;
using UnityEngine;
using Project.Core.Authoring;

namespace Project.Core.Systems
{
	[BurstCompile]
	public partial struct WorldDanceOrchestratorSystem : ISystem
	{
		public void OnCreate(ref SystemState state) {}

		public void OnUpdate(ref SystemState state)
		{
			var ecb = new EntityCommandBuffer(Allocator.Temp);

			// Single-stage trigger
			foreach (var (start, startEntity) in SystemAPI.Query<RefRO<WorldDanceStart>>().WithEntityAccess())
			{
				int targetStage = start.ValueRO.StageIndex;
				// Remove start event
				ecb.DestroyEntity(startEntity);

				// Collect participants: all cubes of this stage (filter by component value)
				var entities = new NativeList<Entity>(Allocator.Temp);
				foreach (var (tag, e) in SystemAPI.Query<RefRO<StageCubeTag>>().WithEntityAccess())
				{
					if (tag.ValueRO.StageIndex == targetStage)
					{
						entities.Add(e);
					}
				}

				int count = entities.Length;
				if (count == 0)
				{
					Debug.LogWarning($"[WorldDance] No participants for stage {targetStage}");
					continue;
				}

				// Ensure single WorldDanceState
				ClearDanceState(ref state, ref ecb);
				// State
				float now = (float)SystemAPI.Time.ElapsedTime;
				var stateEntity = ecb.CreateEntity();
				float gatherH = 50f; float planetR = 20f;
				if (SystemAPI.TryGetSingleton<WorldDanceSettings>(out var s)) { gatherH = s.GatherHeight; planetR = s.PlanetRadius; }
				ecb.AddComponent(stateEntity, new WorldDanceState
				{
					StageIndex = targetStage,
					Phase = 1,
					PhaseStartTime = now,
					GatherHeight = gatherH,
					PlanetRadius = planetR,
					TotalParticipants = count
				});

				// Assign dismantle
				for (int i = 0; i < count; i++)
				{
					var e = entities[i];
					var lt = state.EntityManager.GetComponentData<LocalTransform>(e);
					ecb.AddComponent(e, new DanceIndex { Value = i });
					ecb.AddComponent(e, new DanceDismantle
					{
						StartTime = now,
						Duration = 1.0f,
						OutwardPower = 1.0f,
						Seed = (uint)(0x9E3779B1u * (uint)(i + 1)),
						BasePosition = lt.Position
					});
					// Disable collider if any
					if (state.EntityManager.HasComponent<ColliderReference>(e))
					{
						ecb.RemoveComponent<ColliderReference>(e);
					}
				}

				entities.Dispose();

				// if single-stage trigger is for the last stage, also enable free fly
				int totalStages2 = 0;
				if (SystemAPI.TryGetSingletonEntity<LevelSequenceRuntime>(out var seqEnt2))
				{
					var blobs2 = state.EntityManager.GetBuffer<LevelBlobRef>(seqEnt2);
					totalStages2 = blobs2.Length;
				}
				if (totalStages2 > 0 && targetStage == totalStages2 - 1)
				{
					var fe = ecb.CreateEntity();
					ecb.AddComponent(fe, new EnableFreeFlyRequest { Enable = 1 });
				}
			}

			// Multi-stage sequence trigger (create state)
			foreach (var (seqStart, seqStartEnt) in SystemAPI.Query<RefRO<WorldDanceSequenceStart>>().WithEntityAccess())
			{
				int totalStages = 0;
				if (SystemAPI.TryGetSingletonEntity<LevelSequenceRuntime>(out var seqEnt))
				{
					var blobs = state.EntityManager.GetBuffer<LevelBlobRef>(seqEnt);
					totalStages = blobs.Length;
				}
				int end = seqStart.ValueRO.EndIndex >= 0 ? seqStart.ValueRO.EndIndex : math.max(0, totalStages - 1);
				var sEnt = ecb.CreateEntity();
				ecb.AddComponent(sEnt, new WorldDanceSequenceState
				{
					Current = math.max(0, seqStart.ValueRO.StartIndex),
					End = end,
					Interval = math.max(0.01f, seqStart.ValueRO.IntervalSeconds),
					NextTime = (float)SystemAPI.Time.ElapsedTime,
					TotalStages = totalStages
				});
				// consume event
				ecb.DestroyEntity(seqStartEnt);
			}

				// Sequence progression
			foreach (var (seq, seqEnt) in SystemAPI.Query<RefRW<WorldDanceSequenceState>>().WithEntityAccess())
			{
				float now = (float)SystemAPI.Time.ElapsedTime;
				if (now < seq.ValueRO.NextTime) continue;
				if (seq.ValueRO.Current > seq.ValueRO.End)
				{
					ecb.DestroyEntity(seqEnt);
					continue;
				}
                // trigger current stage
                TriggerStage(ref state, ref ecb, seq.ValueRO.Current);
                // if this is the last stage, request free-fly enable immediately
                if (seq.ValueRO.Current == seq.ValueRO.End)
                {
                    var fe = ecb.CreateEntity();
                    ecb.AddComponent(fe, new EnableFreeFlyRequest { Enable = 1 });
                }
				seq.ValueRW.Current++;
				seq.ValueRW.NextTime = now + seq.ValueRO.Interval;
			}

			ecb.Playback(state.EntityManager);
		}

		private void TriggerStage(ref SystemState state, ref EntityCommandBuffer ecb, int targetStage)
		{
			// Collect participants by StageIndex
			var list = new NativeList<Entity>(Allocator.Temp);
			foreach (var (tag, e) in SystemAPI.Query<RefRO<StageCubeTag>>().WithEntityAccess())
			{
				if (tag.ValueRO.StageIndex == targetStage) list.Add(e);
			}
			int count = list.Length;
			if (count == 0) { list.Dispose(); return; }
			float now = (float)SystemAPI.Time.ElapsedTime;
			// ensure single state
			ClearDanceState(ref state, ref ecb);
			// update state singleton
			var stateEnt = ecb.CreateEntity();
			float gatherH = 50f; float planetR = 20f;
			if (SystemAPI.TryGetSingleton<WorldDanceSettings>(out var s)) { gatherH = s.GatherHeight; planetR = s.PlanetRadius; }
			ecb.AddComponent(stateEnt, new WorldDanceState
			{
				StageIndex = targetStage,
				Phase = 1,
				PhaseStartTime = now,
				GatherHeight = gatherH,
				PlanetRadius = planetR,
				TotalParticipants = count
			});
				for (int i = 0; i < count; i++)
			{
				var e = list[i];
				var lt = state.EntityManager.GetComponentData<LocalTransform>(e);
				ecb.AddComponent(e, new DanceIndex { Value = i });
				ecb.AddComponent(e, new DanceDismantle
				{
					StartTime = now,
					Duration = 1.0f,
					OutwardPower = 1.0f,
					Seed = (uint)(0x9E3779B1u * (uint)(i + 1)),
					BasePosition = lt.Position
				});
					if (state.EntityManager.HasComponent<ColliderReference>(e))
					{
						// best-effort destroy dynamic collider go
						var cref = state.EntityManager.GetComponentData<ColliderReference>(e);
						var go = Resources.InstanceIDToObject(cref.GameObjectInstanceID) as UnityEngine.GameObject;
						if (go != null) UnityEngine.Object.Destroy(go);
						ecb.RemoveComponent<ColliderReference>(e);
					}
			}
				// also request static collider container destroy
				var dr = ecb.CreateEntity();
				ecb.AddComponent(dr, new DestroyStageCollidersRequest { StageIndex = targetStage });
			list.Dispose();
		}

		private static void ClearDanceState(ref SystemState state, ref EntityCommandBuffer ecb)
		{
			var q = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WorldDanceState>());
			if (!q.IsEmpty)
			{
				ecb.DestroyEntity(q);
			}
		}
	}
}



