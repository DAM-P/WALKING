using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
#if HAS_URP_MATERIAL_PROPERTY
using Unity.Rendering;
#endif
using Project.Core.Authoring;
using Project.Core.Components;

namespace Project.Core.Systems
{
    [BurstCompile]
    public partial struct CubeLayoutSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CubeLayoutSpawner>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (spawner, cells, entity) in SystemAPI.Query<RefRW<CubeLayoutSpawner>, DynamicBuffer<CubeCell>>().WithEntityAccess())
            {
                int total = cells.Length;
                int spawned = spawner.ValueRO.SpawnedCount;
                if (spawned >= total) continue;

                int batch = math.min(spawner.ValueRO.SpawnPerFrame, total - spawned);
                
                for (int i = 0; i < batch; i++)
                {
                    var cell = cells[spawned + i];
                    var e = ecb.Instantiate(spawner.ValueRO.Prefab);
                    float3 pos = spawner.ValueRO.Origin + (float3)cell.Coord * spawner.ValueRO.CellSize;
                    ecb.SetComponent(e, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f));
                    
                    // 添加关卡标记，便于统一清理
                    ecb.AddComponent(e, new StageCubeTag { StageIndex = 0 });
                    
                    // 添加网格坐标（用于空间哈希表）
                    ecb.AddComponent(e, new CubeGridPosition 
                    { 
                        GridPosition = cell.Coord,
                        IsRegistered = false
                    });
                    
                    // 添加 Cube 类型标记（静态地形）
                    ecb.AddComponent(e, new CubeTypeTag { Type = CubeType.Static });
                    
                    // 标记可交互方块（根据规则）
                    bool isInteractable = ShouldBeInteractable(cell);
                    if (isInteractable)
                    {
                        ecb.AddComponent(e, new InteractableCubeTag { InteractionType = 0 });
                        ecb.AddComponent(e, new SelectionState { IsSelected = 0, SelectTime = 0f });
                        ecb.AddComponent(e, new HighlightState 
                        { 
                            Intensity = 0f, 
                            Color = new float4(1f, 1f, 0f, 1f),
                            AnimTime = 0f
                        });
                        
                        // 添加可拉伸标记
                        ecb.AddComponent(e, new ExtendableTag 
                        {
                            MaxExtendLength = 10,  // 默认最大拉伸 10 个 Cube
                            CurrentExtensions = 0,
                            AllowMultipleChains = true
                        });
                    }

                    // 写入每实例颜色（URP 属性组件）（需要定义 HAS_URP_MATERIAL_PROPERTY 并导入相应包）
#if HAS_URP_MATERIAL_PROPERTY
                    if (spawner.ValueRO.ApplyInstanceColor == 1)
                    {
                        var baseColor = new Unity.Mathematics.float4(cell.Color.x, cell.Color.y, cell.Color.z, cell.Color.w);
                        if (math.all(baseColor == float4.zero)) baseColor = new float4(0.75f, 0.75f, 0.75f, 1f);
                        ecb.AddComponent(e, new URPMaterialPropertyBaseColor { Value = baseColor });

                        // Emission 逻辑：仅对可交互方块添加，且初始为 0，由高亮系统控制
                        if (isInteractable)
                        {
                            ecb.AddComponent(e, new URPMaterialPropertyEmissionColor { Value = float4.zero });
                        }
                    }
#endif
                }

                spawner.ValueRW.SpawnedCount += batch;

                if (spawner.ValueRW.SpawnedCount >= total)
                {
                    if (spawner.ValueRO.RemoveOnComplete == 1)
                    {
                        ecb.RemoveComponent<CubeLayoutSpawner>(entity);
                        // 也可去掉存放坐标的缓冲（若不再需要）
                        // ecb.RemoveComponent<CubeCell>(entity);
                    }
                }
            }

            ecb.Playback(state.EntityManager);
        }

        /// <summary>
        /// 判断方块是否应该可交互（根据规则）
        /// </summary>
        private static bool ShouldBeInteractable(CubeCell cell)
        {
            // 规则1：所有方块都可交互（测试用）
            // return true;

            // 规则2：特定 TypeId 可交互
            // return cell.TypeId == 1;

            // 规则3：特定颜色可交互（例如黄色/高亮色）
            // return cell.Color.x > 0.8f && cell.Color.y > 0.8f && cell.Color.z < 0.3f;

            // 规则4：特定坐标范围可交互
            // return cell.Coord.y == 0; // 仅地面层

            // 规则5：边界方块可交互
            // return math.abs(cell.Coord.x) > 5 || math.abs(cell.Coord.z) > 5;

            // 规则6：随机部分方块可交互（例如 10%）
            // uint hash = math.hash(cell.Coord);
            // return (hash % 10) == 0;

            // 默认规则：特定 TypeId 或特定颜色
            return cell.TypeId == 1;
        }
    }
}


