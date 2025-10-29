using Unity.Entities;
using Unity.Mathematics;

namespace Project.Core.Components
{
    /// <summary>
    /// 拉伸执行请求（单帧标记）
    /// 由输入层（如 CrosshairExtendManager）添加，由 ExtendExecutionSystem 消费
    /// </summary>
    public struct ExtendExecutionRequest : IComponentData
    {
        public int3 Direction;
        public int Length;
        public int ChainID;
        public int StartIndex; // 渐进式生成的起始索引（0 表示从第一个开始）
    }
}


