using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Project.Core.Authoring;
using Project.Core.Components;
using UnityEngine;

namespace Project.Core.Systems
{
	/// 最小骨架：如果存在 LevelSequenceRuntime 且未启动，则把当前 index 的布局写入 CubeLayoutSpawner 体系生成
	[BurstCompile]
	public partial struct LevelProgressionSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			Debug.Log("[LevelProgression] System created. Waiting for LevelSequenceRuntime...");
		}

		public void OnUpdate(ref SystemState state)
		{
			var ecb = new EntityCommandBuffer(Allocator.Temp);
			foreach (var (seq, entity) in SystemAPI.Query<RefRW<LevelSequenceRuntime>>().WithEntityAccess())
			{
				if (seq.ValueRO.Started != 0) continue;

				var blobs = state.EntityManager.GetBuffer<LevelBlobRef>(entity);
				if (blobs.Length == 0) continue;
				int idx = math.clamp(seq.ValueRO.CurrentIndex, 0, blobs.Length - 1);
				var entry = blobs[idx];
				var layout = entry.Layout;

				// 创建一个 holder 实体，附加 CubeLayoutSpawner 与 CubeCell 缓冲
				var holder = ecb.CreateEntity();
				ecb.AddComponent(holder, new CubeLayoutSpawner
				{
					Prefab = entry.Prefab,
					Origin = layout.Value.origin,
					CellSize = layout.Value.cellSize,
					SpawnPerFrame = entry.SpawnPerFrame,
					SpawnedCount = 0,
					ApplyInstanceColor = entry.ApplyInstanceColor,
					EmissionIntensity = entry.EmissionIntensity,
					RemoveOnComplete = entry.RemoveOnComplete,
					StageIndex = idx,
					// Per-stage rise-in
					RiseEnabled = entry.RiseEnabled,
					RiseHeight = math.max(0f, layout.Value.cellSize * entry.RiseHeightMultiplier),
					RiseDuration = entry.RiseDuration,
					RisePerCubeDelay = entry.RisePerCubeDelay
				});
				var buffer = ecb.AddBuffer<CubeCell>(holder);
					ref var cells = ref layout.Value.cells;
				for (int i = 0; i < cells.Length; i++)
				{
					var c = cells[i];
					buffer.Add(new CubeCell
					{
						Coord = c.coord,
						TypeId = c.typeId,
						Color = c.color
					});
				}

					// 写入 StepTrigger 目标计数
					int totalToTrigger = 0;
					if (state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<StepTriggerColor>()).CalculateEntityCount() > 0)
					{
						var cfg = state.EntityManager.GetBuffer<StepTriggerColor>(SystemAPI.GetSingletonEntity<StepTriggerColor>());
						for (int i = 0; i < cells.Length; i++)
						{
							int typeId = cells[i].typeId;
							for (int k = 0; k < cfg.Length; k++)
							{
								if (cfg[k].TypeId == typeId) { totalToTrigger++; break; }
							}
						}
					}
					var progEntity = ecb.CreateEntity();
					ecb.AddComponent(progEntity, new StageStepProgress { StageIndex = idx, TotalToTrigger = totalToTrigger, Triggered = 0 });

					// 标记已启动
				seq.ValueRW.Started = 1;
					Debug.Log($"[LevelProgression] 初始关加载完成 index={idx}, cells={cells.Length}");

				// 生成 PhysX 碰撞体的请求（交给 Mono 桥接执行）
					var req = ecb.CreateEntity();
					ecb.AddComponent(req, new GenerateCollidersRequest
				{
					Layout = entry.Layout,
					ColliderType = entry.ColliderType,
						MergeMode = entry.MergeMode,
						StageIndex = idx
				});
			}

			// 处理“下一关”请求
			var nextReqQuery = SystemAPI.QueryBuilder().WithAll<NextLevelRequest>().Build();
			if (!nextReqQuery.IsEmpty)
			{
				// 清除请求
				state.EntityManager.DestroyEntity(nextReqQuery);

				// 处理所有序列（通常只有一个）
				foreach (var (seq, seqEntity) in SystemAPI.Query<RefRW<LevelSequenceRuntime>>().WithEntityAccess())
				{
					int oldIndex = seq.ValueRO.CurrentIndex;
					seq.ValueRW.CurrentIndex = math.min(oldIndex + 1, int.MaxValue);
					var blobs = state.EntityManager.GetBuffer<LevelBlobRef>(seqEntity);
					if (blobs.Length == 0)
					{
						Debug.LogWarning("[LevelProgression] 无关卡数据（blobs.Length==0）");
						continue;
					}
					int idx = math.clamp(seq.ValueRW.CurrentIndex, 0, blobs.Length - 1);
					var entry = blobs[idx];
					var layout = entry.Layout;
					Debug.Log($"[LevelProgression] NextLevel 请求：oldIndex={oldIndex} -> newIndex={seq.ValueRW.CurrentIndex} / clamped={idx}, cells={layout.Value.cells.Length}");

					// 不清理旧关卡实体：保留已有内容，直接叠加生成下一关

					// 生成新关
					var holder = ecb.CreateEntity();
					ecb.AddComponent(holder, new CubeLayoutSpawner
					{
						Prefab = entry.Prefab,
						Origin = layout.Value.origin,
						CellSize = layout.Value.cellSize,
						SpawnPerFrame = entry.SpawnPerFrame,
						SpawnedCount = 0,
						ApplyInstanceColor = entry.ApplyInstanceColor,
						EmissionIntensity = entry.EmissionIntensity,
						RemoveOnComplete = entry.RemoveOnComplete,
						StageIndex = idx,
						RiseEnabled = entry.RiseEnabled,
						RiseHeight = math.max(0f, layout.Value.cellSize * entry.RiseHeightMultiplier),
						RiseDuration = entry.RiseDuration,
						RisePerCubeDelay = entry.RisePerCubeDelay
					});
					ref var cells = ref layout.Value.cells;
					// 写入 StepTrigger 目标计数
					int totalToTrigger = 0;
					if (state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<StepTriggerColor>()).CalculateEntityCount() > 0)
					{
						var cfg = state.EntityManager.GetBuffer<StepTriggerColor>(SystemAPI.GetSingletonEntity<StepTriggerColor>());
						for (int i = 0; i < cells.Length; i++)
						{
							int typeId = cells[i].typeId;
							for (int k = 0; k < cfg.Length; k++)
							{
								if (cfg[k].TypeId == typeId) { totalToTrigger++; break; }
							}
						}
					}
					// 重置/更新进度单例
					var qProg = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<StageStepProgress>());
					if (!qProg.IsEmpty)
					{
						var ent = qProg.GetSingletonEntity();
						state.EntityManager.SetComponentData(ent, new StageStepProgress { StageIndex = idx, TotalToTrigger = totalToTrigger, Triggered = 0 });
					}
					else
					{
						var ent = ecb.CreateEntity();
						ecb.AddComponent(ent, new StageStepProgress { StageIndex = idx, TotalToTrigger = totalToTrigger, Triggered = 0 });
					}

					var buffer = ecb.AddBuffer<CubeCell>(holder);
					for (int i = 0; i < cells.Length; i++)
					{
						var c = cells[i];
						buffer.Add(new CubeCell { Coord = c.coord, TypeId = c.typeId, Color = c.color });
					}
					Debug.Log($"[LevelProgression] 已创建新关 spawner，待生成 cells={cells.Length}");

					// 请求生成碰撞体
					var req = ecb.CreateEntity();
					ecb.AddComponent(req, new GenerateCollidersRequest { Layout = entry.Layout, ColliderType = entry.ColliderType, MergeMode = entry.MergeMode, StageIndex = idx });
					Debug.Log($"[LevelProgression] 已请求生成碰撞体 type={entry.ColliderType}, merge={entry.MergeMode}");
				}
			}

			ecb.Playback(state.EntityManager);
		}
	}
}



