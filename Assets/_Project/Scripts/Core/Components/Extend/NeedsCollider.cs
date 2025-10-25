using Unity.Entities;

namespace Project.Core.Components
{
    /// <summary>
    /// 标记 Entity 需要添加 GameObject BoxCollider
    /// 用于拉伸出的 Cube（与 CharacterController 兼容）
    /// </summary>
    public struct NeedsCollider : IComponentData, IEnableableComponent
    {
        public float Size;
    }

    /// <summary>
    /// 存储已创建的 Collider GameObject 的引用
    /// </summary>
    public struct ColliderReference : IComponentData
    {
        public int GameObjectInstanceID;
    }
}

