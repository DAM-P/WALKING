using Unity.Entities;

/// <summary>
/// 标记可交互的 Cube 实体
/// </summary>
public struct InteractableCubeTag : IComponentData
{
    /// <summary>
    /// 交互类型：0=可选择, 1=拉伸起点, 2=特殊功能
    /// </summary>
    public int InteractionType;
}


