using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Project.Core.Components;
using UnityEngine;
#if HAS_URP_MATERIAL_PROPERTY
using Unity.Rendering;
#endif

namespace Project.Core.Systems
{
    /// <summary>
    /// 当玩家脚下格子发生变化时，检测是否踩到目标 TypeId 的 Cube，触发一次性事件（音效 + 变色）。
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct StepDetectionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerGridFoot>();
            state.RequireForUpdate<StepTriggerColor>();
            state.RequireForUpdate<OccupiedCubeMap>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var foot = SystemAPI.GetSingletonRW<PlayerGridFoot>();
            var colorBuffer = SystemAPI.GetSingletonBuffer<StepTriggerColor>(true);
            var map = SystemAPI.GetSingleton<OccupiedCubeMap>();

            int3 current = foot.ValueRO.CurrentCell;
            int3 last = foot.ValueRO.LastCell;
            if (current.Equals(last))
                return;

            if (map.IsInitialized && map.Map.IsCreated && map.Map.TryGetValue(current, out Entity cube))
            {
                var ecb = SystemAPI
                    .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged);
                int variant = -1;
                if (state.EntityManager.HasComponent<CubeVariantId>(cube))
                    variant = state.EntityManager.GetComponentData<CubeVariantId>(cube).Value;

                bool hasConfig = false;
                float4 cfgColor = new float4(1, 1, 1, 1);
                float cfgEmission = 2f;
                if (variant != -1)
                {
                    for (int i = 0; i < colorBuffer.Length; i++)
                    {
                        if (colorBuffer[i].TypeId == variant)
                        {
                            hasConfig = true;
                            cfgColor = colorBuffer[i].Color;
                            cfgEmission = colorBuffer[i].EmissionIntensity;
                            break;
                        }
                    }
                }

                bool shouldTrigger = hasConfig && !state.EntityManager.HasComponent<StepTriggeredOnce>(cube);

#if UNITY_EDITOR
                // Debug disabled per request
#endif

                if (shouldTrigger)
                {
                    // 标记只触发一次
                    ecb.AddComponent<StepTriggeredOnce>(cube);

                    // 变色（URP 实例属性）并开启发光
#if HAS_URP_MATERIAL_PROPERTY
                    float4 color = cfgColor;
                    float emissionIntensity = cfgEmission;
                    if (state.EntityManager.HasComponent<URPMaterialPropertyBaseColor>(cube))
                    {
                        ecb.SetComponent(cube, new URPMaterialPropertyBaseColor { Value = color });
                    }
                    else
                    {
                        ecb.AddComponent(cube, new URPMaterialPropertyBaseColor { Value = color });
                    }
                    // Emission 颜色（与目标色一致，可按需调整强度）
                    if (state.EntityManager.HasComponent<URPMaterialPropertyEmissionColor>(cube))
                    {
                        ecb.SetComponent(cube, new URPMaterialPropertyEmissionColor { Value = color * emissionIntensity });
                    }
                    else
                    {
                        ecb.AddComponent(cube, new URPMaterialPropertyEmissionColor { Value = color * emissionIntensity });
                    }
#endif
                    // 若配置：踩中该 cube 触发世界之舞（按 StageIndex/TypeId/可选坐标）
                    if (state.EntityManager.HasComponent<StageCubeTag>(cube))
                    {
                        var stageTag = state.EntityManager.GetComponentData<StageCubeTag>(cube);
                        int stageIdx = stageTag.StageIndex;
                        int type = variant;
                        int3 coord = int3.zero; bool hasCoord = false;
                        if (state.EntityManager.HasComponent<CubeGridPosition>(cube))
                        {
                            coord = state.EntityManager.GetComponentData<CubeGridPosition>(cube).GridPosition;
                            hasCoord = true;
                        }
                        var cfgQ = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<DanceTriggerEntry>());
                        if (!cfgQ.IsEmpty)
                        {
                            var buf = state.EntityManager.GetBuffer<DanceTriggerEntry>(cfgQ.GetSingletonEntity());
                            for (int i = 0; i < buf.Length; i++)
                            {
                                var entry = buf[i];
                                if (entry.StageIndex != stageIdx) continue;
                                if (entry.TypeId != type) continue;
                                if (entry.UseCoord != 0 && (!hasCoord || !coord.Equals(entry.Coord))) continue;
                                // 改为从第0关开始的序列触发
                                float interval = 0.5f;
                                if (SystemAPI.TryGetSingleton<WorldDanceSettings>(out var ws)) interval = math.max(0.01f, ws.SequenceIntervalSeconds);
                                var we = ecb.CreateEntity();
                                ecb.AddComponent(we, new WorldDanceSequenceStart { StartIndex = 0, EndIndex = -1, IntervalSeconds = interval });
                                break;
                            }
                        }
                    }

                    // 音效事件（由 Mono 桥接播放）
                    float3 pos = float3.zero;
                    if (state.EntityManager.HasComponent<LocalTransform>(cube))
                    {
                        pos = state.EntityManager.GetComponentData<LocalTransform>(cube).Position;
                    }
                    var evtEntity = ecb.CreateEntity();
                    ecb.AddComponent(evtEntity, new StepAudioEvent { Position = pos, TypeId = variant });

#if UNITY_EDITOR
                    // Debug disabled per request
#endif

                    // 统计当前关卡进度（仅统计当前 StageIndex 的方块）
                    if (state.EntityManager.HasComponent<StageCubeTag>(cube))
                    {
                        var tag = state.EntityManager.GetComponentData<StageCubeTag>(cube);
                        var qProg = state.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<StageStepProgress>());
                        if (!qProg.IsEmpty)
                        {
                            var progEnt = qProg.GetSingletonEntity();
                            var prog = state.EntityManager.GetComponentData<StageStepProgress>(progEnt);
                            if (prog.StageIndex == tag.StageIndex)
                            {
                                prog.Triggered++;
                                state.EntityManager.SetComponentData(progEnt, prog);
                                // 完成则请求下一关
                                if (prog.TotalToTrigger > 0 && prog.Triggered >= prog.TotalToTrigger)
                                {
                                    var req = ecb.CreateEntity();
                                    ecb.AddComponent<NextLevelRequest>(req);
                                }
                            }
                        }
                    }
                }
            }
#if UNITY_EDITOR
            else
            {
                // Debug disabled per request
            }
#endif

            // 更新 last
            foot.ValueRW.LastCell = current;
        }
    }
}


