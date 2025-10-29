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

            // 读取设置与玩家位置
            float activeRadius = 25f;
            float hysteresis = 3f;
            if (SystemAPI.TryGetSingleton<ExtendSettings>(out var settings))
            {
                activeRadius = settings.ColliderActiveRadius > 0f ? settings.ColliderActiveRadius : 25f;
                hysteresis = settings.ColliderDeactivateHysteresis > 0f ? settings.ColliderDeactivateHysteresis : 3f;
            }
            Vector3 playerPos = Vector3.zero;
            var fpc = Object.FindObjectOfType<FirstPersonController>();
            if (fpc != null)
            {
                playerPos = fpc.transform.position;
            }
            else if (Camera.main != null)
            {
                playerPos = Camera.main.transform.position;
            }
            else
            {
                // 无法获取玩家位置，保持为原点（可根据需要添加日志）
            }

            // 创建新 Collider（仅在距离阈值内创建）
            Entities
                .WithAll<NeedsCollider>()
                .WithNone<ColliderReference>()
                .WithoutBurst()
                .ForEach((Entity entity, in LocalTransform transform, in NeedsCollider needsCollider) =>
                {
                    float dist = Vector3.Distance(playerPos, (Vector3)transform.Position);
                    if (dist > activeRadius + hysteresis)
                    {
                        // 超出范围：暂不创建，等待靠近（确保 NeedsCollider 处于启用状态）
                        if (!EntityManager.IsComponentEnabled<NeedsCollider>(entity))
                            ecb.SetComponentEnabled<NeedsCollider>(entity, true);
                        return;
                    }
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

            // 同步 Collider 位置（Transform 变化时），并在远处销毁 Collider
            Entities
                .WithAll<ColliderReference>()
                .WithoutBurst()
                .ForEach((Entity entity, in LocalTransform transform, in ColliderReference colliderRef) =>
                {
                    var colliderGO = Resources.InstanceIDToObject(colliderRef.GameObjectInstanceID) as GameObject;
                    if (colliderGO != null)
                    {
                        float dist = Vector3.Distance(playerPos, (Vector3)transform.Position);
                        if (dist > activeRadius + hysteresis)
                        {
                            Object.Destroy(colliderGO);
                            ecb.RemoveComponent<ColliderReference>(entity);
                            // 重新启用 NeedsCollider，靠近时会再次创建
                            ecb.SetComponentEnabled<NeedsCollider>(entity, true);
                        }
                        else
                        {
                            colliderGO.transform.SetPositionAndRotation(transform.Position, transform.Rotation);
                            colliderGO.transform.localScale = Vector3.one * transform.Scale;
                        }
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

