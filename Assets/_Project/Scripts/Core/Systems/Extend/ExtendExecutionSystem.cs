using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
#if HAS_URP_MATERIAL_PROPERTY
using Unity.Rendering;
#endif
using UnityEngine;
using Project.Core.Components;
using Project.Core.Authoring;

namespace Project.Core.Systems
{
    /// <summary>
    /// 拉伸执行系统，处理 Cube 链的实际生成
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ExtendPreviewSystem))]
    public partial class ExtendExecutionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // 若本帧没有任何执行请求，直接返回，避免无意义开销
            var reqQuery = SystemAPI.QueryBuilder().WithAll<ExtendExecutionRequest>().Build();
            if (reqQuery.CalculateEntityCount() == 0)
            {
                return;
            }

            // 获取设置
            if (!SystemAPI.TryGetSingleton<ExtendSettings>(out var settings))
            {
                Debug.LogWarning("[ExtendExecutionSystem] ExtendSettings 未找到！请在场景中添加 ExtendSettingsAuthoring。");
                return;
            }

            if (settings.CubePrefab == Entity.Null)
            {
                Debug.LogError("[ExtendExecutionSystem] Cube Prefab 未设置！");
                return;
            }

            // 获取空间哈希表
            if (!SystemAPI.TryGetSingleton<OccupiedCubeMap>(out var cubeMap))
            {
                Debug.LogWarning("[ExtendExecutionSystem] OccupiedCubeMap 未找到！");
                return;
            }

            // 获取玩家当前脚下栅格（用于限制：不在玩家所在位置生成方块）
            bool hasPlayerFoot = SystemAPI.TryGetSingleton<PlayerGridFoot>(out var playerFoot);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // 处理所有拉伸请求
            Entities
                .WithAll<InteractableCubeTag, ExtendableTag>()
                .WithoutBurst()
                .ForEach((Entity rootEntity, ref ExtendableTag extendable, in ExtendExecutionRequest request, in LocalTransform rootTransform, in StageCubeTag stageTag) =>
                {
                    // 计算起点坐标
                    int3 startPos = new int3(math.round(rootTransform.Position));

                    // 生成 Cube 链（支持从 StartIndex 继续增量生成）
                    int successCount = 0;
                    int startIndex = math.max(0, request.StartIndex);
                    int endIndex = startIndex + math.max(0, request.Length);
                    for (int i = startIndex + 1; i <= endIndex; i++)
                    {
                        int3 cubeGridPos = startPos + request.Direction * i;

                        // 限制：不在玩家“所在位置”（脚下栅格的上方一格）生成方块
                        if (hasPlayerFoot &&(cubeGridPos.Equals(playerFoot.CurrentCell + new int3(0, 1, 0)) || cubeGridPos.Equals(playerFoot.CurrentCell + new int3(0, 2, 0))))
                        {
                            // 跳过该格子，继续后续位置
                            continue;
                        }

                        // 再次检查碰撞（防止并发问题）
                        var key = new int4(cubeGridPos.x, cubeGridPos.y, cubeGridPos.z, stageTag.StageIndex);
                        if (cubeMap.Map.ContainsKey(key))
                        {
                            Debug.LogWarning($"[ExtendExecutionSystem] 位置 {cubeGridPos} 已被占用，停止拉伸。");
                            break;
                        }

                        // 实例化 Cube
                        var newCube = ecb.Instantiate(settings.CubePrefab);

                        // 设置位置
                        float3 worldPos = (float3)cubeGridPos;
                        ecb.SetComponent(newCube, LocalTransform.FromPositionRotationScale(worldPos, quaternion.identity, 1f));

                        // 添加网格坐标
                        ecb.AddComponent(newCube, new CubeGridPosition
                        {
                            GridPosition = cubeGridPos,
                            IsRegistered = false
                        });

                        // 添加 Cube 类型标记（拉伸类型）
                        ecb.AddComponent(newCube, new CubeTypeTag { Type = CubeType.Extended });

                        // 添加拉伸链数据
                        ecb.AddComponent(newCube, new ExtendChainData
                        {
                            RootEntity = rootEntity,
                            Direction = request.Direction,
                            IndexInChain = i,
                            ChainID = request.ChainID
                        });

                        // 添加寿命（若启用）
                        if (settings.ExtendedLifetimeSeconds > 0f)
                        {
                            float originalAlpha = 1f;
#if HAS_URP_MATERIAL_PROPERTY
                            if (settings.ApplyInstanceColor)
                            {
                                // 若我们写入了实例 BaseColor，则以其 alpha 作为初值
                                // 注意：此时组件尚未真正存在于实体，渲染属性通常在生成后由系统写入
                                // 因此这里采用 DefaultColor.a 作为初值，足够用于计算渐隐
                                originalAlpha = settings.DefaultColor.w;
                            }
#endif
                            ecb.AddComponent(newCube, new ExtendedLifetime { RemainingSeconds = settings.ExtendedLifetimeSeconds, TotalSeconds = settings.ExtendedLifetimeSeconds, OriginalAlpha = originalAlpha });
                        }

                        // 添加关卡标记（与静态 Cube 相同的 StageIndex）
                        ecb.AddComponent(newCube, new StageCubeTag { StageIndex = stageTag.StageIndex });

                        // 添加 Collider 标记（如果启用）
                        if (settings.AutoAddCollider)
                        {
                            ecb.AddComponent(newCube, new NeedsCollider { Size = settings.CubeSize });
                            ecb.SetComponentEnabled<NeedsCollider>(newCube, true);
                        }

#if HAS_URP_MATERIAL_PROPERTY
                        // 添加实例颜色（URP）
                        if (settings.ApplyInstanceColor)
                        {
                            // 在默认颜色基础上做轻微亮度扰动（±3%），提升可读性且不破坏合批
                            const float jitterStrength = 0.03f; // 可按需调小/调大

                            // 生成确定性随机（基于 ChainID 与索引 i），确保可复现
                            uint seed = (uint)(request.ChainID * 73856093 ^ i * 19349663);
                            var rand = Unity.Mathematics.Random.CreateFromIndex(seed);
                            float jitter = rand.NextFloat(-jitterStrength, jitterStrength);

                            float4 baseColor = settings.DefaultColor;
                            float3 rgb = baseColor.xyz * (1f + jitter);
                            // 夹紧到 [0,1]
                            rgb = math.saturate(rgb);

                            ecb.AddComponent(newCube, new URPMaterialPropertyBaseColor
                            {
                                Value = new float4(rgb, baseColor.w)
                            });
                        }
#endif

                        successCount++;
                    }

                    // 更新可拉伸标记：仅当从0开始生成时，认为新开一条链
                    if (startIndex == 0 && successCount > 0)
                    {
                        extendable.CurrentExtensions++;
                    }

                    // 移除执行请求和预览
                    ecb.RemoveComponent<ExtendExecutionRequest>(rootEntity);
                    ecb.RemoveComponent<ExtendPreview>(rootEntity);

                    // 可选日志：注释以避免频繁打印
                    // Debug.Log($"[ExtendExecution] +{successCount} cubes axis={request.Direction} range=({startIndex+1}..{endIndex}) chain={request.ChainID}");

                }).Run();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}

