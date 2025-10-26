using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Project.Core.Components;

namespace Project.Core.Systems
{
    /// <summary>
    /// 收缩系统，处理cube链的收缩操作
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class RetractSystem : SystemBase
    {
        private EntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // 处理收缩请求
            Entities
                .WithAll<RetractRequest>()
                .ForEach((Entity requestEntity, int entityInQueryIndex, in RetractRequest request) =>
                {
                    // 移除收缩请求标记
                    ecb.RemoveComponent<RetractRequest>(entityInQueryIndex, requestEntity);

                }).WithoutBurst().ScheduleParallel();

            // 查询并收缩指定ChainID的所有cube
            var retractRequestQuery = GetEntityQuery(typeof(RetractRequest));
            var retractRequests = retractRequestQuery.ToEntityArray(Allocator.Temp);

            foreach (var requestEntity in retractRequests)
            {
                var request = EntityManager.GetComponentData<RetractRequest>(requestEntity);
                RetractChain(request.ChainID, request.RetractWholeChain);
                EntityManager.RemoveComponent<RetractRequest>(requestEntity);
            }

            retractRequests.Dispose();

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }

        /// <summary>
        /// 收缩指定Chain ID的cube链
        /// </summary>
        private void RetractChain(int chainID, bool wholeChain)
        {
            // 查询该链的所有cube
            var query = EntityManager.CreateEntityQuery(typeof(ExtendChainData));
            var entities = query.ToEntityArray(Allocator.Temp);
            var chainDataArray = query.ToComponentDataArray<ExtendChainData>(Allocator.Temp);

            var toDestroy = new NativeList<Entity>(Allocator.Temp);
            Entity rootEntityForUpdate = Entity.Null;

            for (int i = 0; i < entities.Length; i++)
            {
                if (chainDataArray[i].ChainID == chainID)
                {
                    if (rootEntityForUpdate == Entity.Null)
                    {
                        rootEntityForUpdate = chainDataArray[i].RootEntity;
                    }
                    if (wholeChain)
                    {
                        // 收缩整条链
                        toDestroy.Add(entities[i]);
                    }
                    else
                    {
                        // 只收缩链的末端（索引最大的）
                        // TODO: 实现逐个收缩逻辑
                        toDestroy.Add(entities[i]);
                    }
                }
            }

            // 销毁cube（支持部分收缩）
            foreach (var entity in toDestroy)
            {
                // 先清理碰撞体
                if (EntityManager.HasComponent<ColliderReference>(entity))
                {
                    var colliderRef = EntityManager.GetComponentData<ColliderReference>(entity);
                    var colliderGO = Resources.InstanceIDToObject(colliderRef.GameObjectInstanceID) as GameObject;
                    if (colliderGO != null)
                        Object.Destroy(colliderGO);
                }

                // 销毁entity
                EntityManager.DestroyEntity(entity);
            }

            // 更新根cube的ExtendableTag（仅当找到对应链的根时）
            if (toDestroy.Length > 0 && rootEntityForUpdate != Entity.Null)
            {
                var rootEntity = rootEntityForUpdate;
                if (EntityManager.Exists(rootEntity) && EntityManager.HasComponent<ExtendableTag>(rootEntity))
                {
                    var extendable = EntityManager.GetComponentData<ExtendableTag>(rootEntity);
                    extendable.CurrentExtensions = math.max(0, extendable.CurrentExtensions - 1);
                    EntityManager.SetComponentData(rootEntity, extendable);
                }
            }

            Debug.Log($"<color=yellow>[RetractSystem]</color> 收缩Chain {chainID}，销毁 {toDestroy.Length} 个cube");

            toDestroy.Dispose();
            entities.Dispose();
            chainDataArray.Dispose();
        }
    }

    /// <summary>
    /// 收缩请求组件
    /// </summary>
    public struct RetractRequest : IComponentData
    {
        public int ChainID;
        public bool RetractWholeChain; // true=收缩整条链，false=按 TargetLength 收缩
        public int TargetLength;       // 目标保留长度（仅在部分收缩时使用）
    }
}

