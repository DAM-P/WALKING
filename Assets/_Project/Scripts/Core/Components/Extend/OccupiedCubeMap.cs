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
        /// Key: int4 坐标（x,y,z,stageIndex）
        /// Value: Entity（占用该坐标的 Cube Entity；按关卡区分，避免叠关冲突）
        /// 使用 NativeParallelHashMap 支持并行写入
        /// </summary>
        public NativeParallelHashMap<int4, Entity> Map;

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

