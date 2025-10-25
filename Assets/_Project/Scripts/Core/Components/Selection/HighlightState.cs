using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Cube 的高亮状态（用于视觉反馈）
/// </summary>
public struct HighlightState : IComponentData
{
    /// <summary>
    /// 高亮强度：0-1
    /// </summary>
    public float Intensity;
    
    /// <summary>
    /// 高亮颜色
    /// </summary>
    public float4 Color;
    
    /// <summary>
    /// 动画时间（用于闪烁效果）
    /// </summary>
    public float AnimTime;
}


