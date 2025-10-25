using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Project.Core.Systems
{
    /// <summary>
    /// 同步 Entity 位置到 GameObject 代理
    /// （仅在方块会移动时需要）
    /// </summary>
    public partial class ProxySyncSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // 查询有代理且位置可能变化的 Entity
            Entities
                .WithAll<InteractableCubeTag>()
                .WithoutBurst() // 访问 GameObject API 需要禁用 Burst
                .ForEach((in LocalTransform transform, in ProxyReference proxyRef) =>
                {
                    // 通过 InstanceID 获取 GameObject
                    var go = Resources.InstanceIDToObject(proxyRef.GameObjectInstanceID) as GameObject;
                    if (go != null)
                    {
                        // 同步位置和旋转
                        go.transform.position = transform.Position;
                        go.transform.rotation = transform.Rotation;
                    }
                }).Run();
        }
    }
}


