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

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // 处理所有拉伸请求
            Entities
                .WithAll<InteractableCubeTag, ExtendableTag>()
                .WithoutBurst()
                .ForEach((Entity rootEntity, ref ExtendableTag extendable, in ExtendExecutionRequest request, in LocalTransform rootTransform) =>
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

                        // 再次检查碰撞（防止并发问题）
                        if (cubeMap.Map.ContainsKey(cubeGridPos))
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

                        // 添加关卡标记（与静态 Cube 相同的 StageIndex）
                        ecb.AddComponent(newCube, new StageCubeTag { StageIndex = 0 });

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
                            ecb.AddComponent(newCube, new URPMaterialPropertyBaseColor
                            {
                                Value = settings.DefaultColor
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

                    Debug.Log($"<color=green>[ExtendExecutionSystem]</color> 成功增量生成 {successCount} 个 Cube，范围=({startIndex+1}..{endIndex})，方向={request.Direction}，ChainID={request.ChainID}");

                }).Run();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}

