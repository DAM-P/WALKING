using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Project.Core.Components;

namespace Project.Core.Systems
{
    /// <summary>
    /// 动态 Collider 系统
    /// 为标记了 NeedsCollider 的 Entity 创建 GameObject BoxCollider
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class DynamicColliderSystem : SystemBase
    {
        private GameObject _colliderContainer;

        protected override void OnCreate()
        {
            // 创建容器 GameObject
            _colliderContainer = new GameObject("DynamicColliders");
            _colliderContainer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        protected override void OnDestroy()
        {
            if (_colliderContainer != null)
            {
                Object.Destroy(_colliderContainer);
            }
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            // 创建新 Collider
            Entities
                .WithAll<NeedsCollider>()
                .WithNone<ColliderReference>()
                .WithoutBurst()
                .ForEach((Entity entity, in LocalTransform transform, in NeedsCollider needsCollider) =>
                {
                    // 创建 Collider GameObject
                    var colliderGO = new GameObject($"Collider_Entity_{entity.Index}_{entity.Version}");
                    colliderGO.transform.SetParent(_colliderContainer.transform);
                    colliderGO.transform.SetPositionAndRotation(transform.Position, transform.Rotation);
                    colliderGO.transform.localScale = Vector3.one * transform.Scale;

                    // 添加 BoxCollider
                    var boxCollider = colliderGO.AddComponent<BoxCollider>();
                    boxCollider.size = Vector3.one * needsCollider.Size;

                    // 存储引用
                    ecb.AddComponent(entity, new ColliderReference
                    {
                        GameObjectInstanceID = colliderGO.GetInstanceID()
                    });

                    // 禁用 NeedsCollider 组件（标记已处理）
                    ecb.SetComponentEnabled<NeedsCollider>(entity, false);

                }).Run();

            // 同步 Collider 位置（Transform 变化时）
            Entities
                .WithAll<ColliderReference>()
                .WithoutBurst()
                .ForEach((Entity entity, in LocalTransform transform, in ColliderReference colliderRef) =>
                {
                    var colliderGO = Resources.InstanceIDToObject(colliderRef.GameObjectInstanceID) as GameObject;
                    if (colliderGO != null)
                    {
                        colliderGO.transform.SetPositionAndRotation(transform.Position, transform.Rotation);
                        colliderGO.transform.localScale = Vector3.one * transform.Scale;
                    }
                    else
                    {
                        // Collider 已被删除，清理引用
                        ecb.RemoveComponent<ColliderReference>(entity);
                    }

                }).Run();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}

