using Unity.Entities;

namespace Project.Core.Components
{
    /// <summary>
    /// 标记 Cube 可以进行拉伸操作
    /// 通常添加到可交互的 Cube 上
    /// </summary>
    public struct ExtendableTag : IComponentData
    {
        /// <summary>
        /// 最大拉伸长度（可拉伸的 Cube 数量）
        /// </summary>
        public int MaxExtendLength;

        /// <summary>
        /// 当前已拉伸的链数量（用于限制同时存在的链）
        /// </summary>
        public int CurrentExtensions;

        /// <summary>
        /// 是否允许多条链（true = 可以从多个方向拉伸）
        /// </summary>
        public bool AllowMultipleChains;
    }
}

