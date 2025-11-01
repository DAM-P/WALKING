using Unity.Entities;
using Unity.Mathematics;

namespace Project.Core.Components
{
    /// <summary>
    /// 触发配置：不同 Cube 类型对应的颜色与发光强度（仅数据，音效由 Mono 侧选择）。
    /// 作为动态缓冲挂在单例实体上供系统读取。
    /// </summary>
    public struct StepTriggerColor : IBufferElementData
    {
        public int TypeId;
        /// <summary>
        /// 原始底色（用于生成时覆盖 BaseColor）；为 (0,0,0,0) 则不覆盖
        /// </summary>
        public float4 OriginalColor;
        public float4 Color;
        public float EmissionIntensity;
    }
}


