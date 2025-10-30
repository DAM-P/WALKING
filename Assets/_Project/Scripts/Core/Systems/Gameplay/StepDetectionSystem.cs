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
                Debug.Log($"[StepDetection] cell {last} -> {current}, found cube={cube.Index}:{cube.Version}, variant={variant}, hasConfig={hasConfig}, alreadyTriggered={state.EntityManager.HasComponent<StepTriggeredOnce>(cube)}");
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
                    // 音效事件（由 Mono 桥接播放）
                    float3 pos = float3.zero;
                    if (state.EntityManager.HasComponent<LocalTransform>(cube))
                    {
                        pos = state.EntityManager.GetComponentData<LocalTransform>(cube).Position;
                    }
                    var evtEntity = ecb.CreateEntity();
                    ecb.AddComponent(evtEntity, new StepAudioEvent { Position = pos, TypeId = variant });

#if UNITY_EDITOR
                    Debug.Log($"[StepDetection] TRIGGERED at {current}");
#endif
                }
            }
#if UNITY_EDITOR
            else
            {
                Debug.Log($"[StepDetection] cell {last} -> {current}, no cube or map not ready (IsInitialized={map.IsInitialized}, IsCreated={map.Map.IsCreated})");
            }
#endif

            // 更新 last
            foot.ValueRW.LastCell = current;
        }
    }
}


