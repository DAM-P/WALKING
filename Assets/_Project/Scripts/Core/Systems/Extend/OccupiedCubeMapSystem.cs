using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Project.Core.Components;

namespace Project.Core.Systems
{
    /// <summary>
    /// 管理空间哈希表，维护 Cube 坐标注册
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct OccupiedCubeMapSystem : ISystem
    {
        private EntityQuery _cubeQuery;

        public void OnCreate(ref SystemState state)
        {
            // 创建查询所有 Cube 的 Query
            _cubeQuery = SystemAPI.QueryBuilder()
                .WithAll<CubeGridPosition, LocalTransform>()
                .Build();

            // 初始化 Singleton
            var estimatedCount = _cubeQuery.CalculateEntityCount();
            int initialCapacity = math.max(1024, estimatedCount * 2 + 256);
            var singleton = new OccupiedCubeMap
            {
                // 提高初始容量，避免并行写入时容量不足
                Map = new NativeParallelHashMap<int3, Entity>(math.max(initialCapacity, 4096), Allocator.Persistent),
                IsInitialized = true
            };
            var singletonEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(singletonEntity, singleton);

            state.RequireForUpdate<OccupiedCubeMap>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var mapSingleton = SystemAPI.GetSingletonRW<OccupiedCubeMap>();

            // 动态扩容：当实体数量接近容量时，重建更大的哈希表，避免 "HashMap is full"
            int currentCount = _cubeQuery.CalculateEntityCount();
            if (mapSingleton.ValueRW.Map.IsCreated)
            {
                int cap = mapSingleton.ValueRW.Map.Capacity;
                // 需要容量：当前数量的 1.5 倍 + 1024 缓冲
                int needed = math.max(4096, currentCount + currentCount / 2 + 1024);
                if (cap < needed)
                {
                    var newMap = new NativeParallelHashMap<int3, Entity>(needed, Allocator.Persistent);
                    // 拷贝旧内容
                    foreach (var kv in mapSingleton.ValueRW.Map)
                    {
                        newMap.TryAdd(kv.Key, kv.Value);
                    }
                    mapSingleton.ValueRW.Map.Dispose();
                    mapSingleton.ValueRW.Map = newMap;
                }
            }

            // 注册新 Cube（使用 ParallelWriter 支持并行写入）
            var registerJob = new RegisterCubeJob
            {
                MapWriter = mapSingleton.ValueRW.Map.AsParallelWriter()
            };
            registerJob.ScheduleParallel();
            state.Dependency.Complete();

            // 清理已销毁的 Cube
            CleanupDestroyedCubes(ref state, ref mapSingleton.ValueRW);
        }

        public void OnDestroy(ref SystemState state)
        {
            // 释放 NativeHashMap
            if (SystemAPI.TryGetSingleton<OccupiedCubeMap>(out var mapSingleton))
            {
                if (mapSingleton.Map.IsCreated)
                {
                    mapSingleton.Map.Dispose();
                }
            }
        }

        /// <summary>
        /// 清理已销毁 Entity 的坐标
        /// </summary>
        private void CleanupDestroyedCubes(ref SystemState state, ref OccupiedCubeMap mapData)
        {
            var entitiesToRemove = new NativeList<int3>(Allocator.Temp);

            // 查找已销毁的 Entity
            foreach (var kvp in mapData.Map)
            {
                if (!state.EntityManager.Exists(kvp.Value))
                {
                    entitiesToRemove.Add(kvp.Key);
                }
            }

            // 从哈希表移除
            foreach (var pos in entitiesToRemove)
            {
                mapData.Map.Remove(pos);
            }

            entitiesToRemove.Dispose();
        }
    }

    /// <summary>
    /// 注册 Cube 到空间哈希表
    /// </summary>
    [BurstCompile]
    public partial struct RegisterCubeJob : IJobEntity
    {
        public NativeParallelHashMap<int3, Entity>.ParallelWriter MapWriter;

        public void Execute(Entity entity, ref CubeGridPosition gridPos, in LocalTransform transform)
        {
            if (gridPos.IsRegistered)
                return;

            // 计算网格坐标（四舍五入到整数）
            var calculatedPos = new int3(math.round(transform.Position));

            // 更新网格位置（如果变化了）
            if (!gridPos.GridPosition.Equals(calculatedPos))
            {
                gridPos.GridPosition = calculatedPos;
                gridPos.IsRegistered = false; // 重新注册
            }

            // 注册到哈希表（ParallelWriter 的 TryAdd 是线程安全的）
            if (!gridPos.IsRegistered)
            {
                if (MapWriter.TryAdd(gridPos.GridPosition, entity))
                {
                    gridPos.IsRegistered = true;
                }
            }
        }
    }
}

