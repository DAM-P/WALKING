using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Project.Core.Authoring;

namespace Project.Core.Systems
{
    /// <summary>
    /// 为带 InteractableCubeTag 的 Entity 自动生成 GameObject 代理
    /// </summary>
    public partial class InteractableProxySpawnSystem : SystemBase
    {
        private GameObject _proxyPrefab;
        private bool _initialized = false;

        protected override void OnCreate()
        {
            RequireForUpdate<InteractableCubeTag>();
        }

        protected override void OnUpdate()
        {
            // 延迟初始化代理预制
            if (!_initialized)
            {
                InitializeProxyPrefab();
                _initialized = true;
            }

            if (_proxyPrefab == null)
            {
                Debug.LogWarning("[ProxySpawn] 代理预制未设置，跳过生成");
                return;
            }

            // 使用 EntityCommandBuffer 进行结构性变更
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // 查询需要代理但尚未生成的 Entity
            Entities
                .WithAll<InteractableCubeTag>()
                .WithNone<ProxyReference>()
                .WithoutBurst() // 访问 GameObject API 需要禁用 Burst
                .ForEach((Entity entity, in LocalTransform transform, in InteractableCubeTag tag) =>
                {
                    // 生成代理 GameObject
                    var proxyGO = Object.Instantiate(_proxyPrefab, transform.Position, transform.Rotation);
                    proxyGO.name = $"InteractableProxy_{entity.Index}";
                    
                    // 确保激活（Prefab 是禁用的，实例需要启用）
                    proxyGO.SetActive(true);

                    var proxy = proxyGO.GetComponent<InteractableProxy>();
                    if (proxy == null)
                    {
                        proxy = proxyGO.AddComponent<InteractableProxy>();
                    }

                    // 关联 Entity
                    proxy.linkedEntity = entity;
                    proxy.interactionType = tag.InteractionType;

                    // 使用 ECB 写入引用到 Entity（延迟执行）
                    ecb.AddComponent(entity, new ProxyReference 
                    { 
                        GameObjectInstanceID = proxyGO.GetInstanceID() 
                    });

                    Debug.Log($"[ProxySpawn] 为 Entity {entity.Index} 生成代理，位置={transform.Position}");
                }).Run();

            // 执行所有命令
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void InitializeProxyPrefab()
        {
            // 创建简单的代理预制（运行时）
            _proxyPrefab = new GameObject("InteractableProxyPrefab");
            _proxyPrefab.AddComponent<InteractableProxy>();
            
            var collider = _proxyPrefab.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
            collider.isTrigger = false; // 确保不是 Trigger，否则 Raycast 检测不到
            
            // 设置 Layer（默认为 Default，你可以在 CubeSelectionManager 中配置）
            // _proxyPrefab.layer = LayerMask.NameToLayer("Interactable"); // 如果有自定义层
            
            // 隐藏预制（实例化后会在代码中启用）
            _proxyPrefab.SetActive(false);
            
            Debug.Log("[ProxySpawn] 代理预制初始化完成");
        }
    }
}

