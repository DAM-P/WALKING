using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Project.Core.Components
{
    /// <summary>
    /// 空间哈希表 Singleton，存储所有 Cube 占用的坐标
    /// 用于快速碰撞检测（O(1) 查询）
    /// </summary>
    public struct OccupiedCubeMap : IComponentData
    {
        /// <summary>
        /// 坐标 → Entity 的映射表
        /// Key: int3 坐标（整数网格坐标）
        /// Value: Entity（占用该坐标的 Cube Entity）
        /// 使用 NativeParallelHashMap 支持并行写入
        /// </summary>
        public NativeParallelHashMap<int3, Entity> Map;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized;
    }

    /// <summary>
    /// 标记需要注册到空间哈希表的 Cube
    /// </summary>
    public struct CubeGridPosition : IComponentData
    {
        /// <summary>
        /// 网格坐标（整数坐标，用于哈希表）
        /// </summary>
        public int3 GridPosition;

        /// <summary>
        /// 是否已注册到哈希表
        /// </summary>
        public bool IsRegistered;
    }
}

