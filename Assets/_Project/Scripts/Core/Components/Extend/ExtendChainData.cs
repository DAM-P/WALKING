using Unity.Entities;
using Unity.Mathematics;

namespace Project.Core.Components
{
    /// <summary>
    /// 拉伸链数据，标记 Cube 属于哪条拉伸链
    /// </summary>
    public struct ExtendChainData : IComponentData
    {
        /// <summary>
        /// 链的起始 Cube Entity（可交互的源 Cube）
        /// </summary>
        public Entity RootEntity;

        /// <summary>
        /// 拉伸方向（单位向量：±X, ±Y, ±Z）
        /// </summary>
        public int3 Direction;

        /// <summary>
        /// 在链中的索引（0 = 起点，1 = 第一个拉伸的 Cube）
        /// </summary>
        public int IndexInChain;

        /// <summary>
        /// 链的唯一标识符（用于批量删除）
        /// </summary>
        public int ChainID;
    }
}

