using Unity.Entities;
using Unity.Mathematics;

namespace Project.Core.Components
{
    /// <summary>
    /// 拉伸预览数据，存储当前预览的拉伸状态
    /// 添加到被选中且准备拉伸的 Cube 上
    /// </summary>
    public struct ExtendPreview : IComponentData
    {
        /// <summary>
        /// 预览的拉伸长度（Cube 数量）
        /// </summary>
        public int PreviewLength;

        /// <summary>
        /// 预览的拉伸方向（单位向量：±X, ±Y, ±Z）
        /// </summary>
        public int3 PreviewDirection;

        /// <summary>
        /// 预览是否有效（是否可以拉伸到该长度）
        /// false 表示有碰撞或超出限制
        /// </summary>
        public bool IsValid;

        /// <summary>
        /// 实际可拉伸的最大长度（考虑碰撞后）
        /// </summary>
        public int ValidLength;
    }
}

