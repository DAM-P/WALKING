using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Project.Core.Systems
{
    /// <summary>
    /// 为 DOTS Entity 同步一个 GameObject Collider（Hybrid 方案）
    /// 使 CharacterController 能检测到 Entity 方块
    /// 仅用于必须与 GameObject 物理交互的场景
    /// </summary>
    public partial class EntityColliderSyncSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // 此系统需要自定义实现，基本思路：
            // 1. 查询所有需要碰撞的 Entity
            // 2. 为每个 Entity 在场景中创建/维护一个对应的 GameObject（仅含 BoxCollider）
            // 3. 每帧同步 Transform
            // 注意：性能开销大，仅用于少量可交互体

            // TODO: 实现细节较复杂，建议优先使用方案A（静态场景用 GameObject）
        }
    }
}




