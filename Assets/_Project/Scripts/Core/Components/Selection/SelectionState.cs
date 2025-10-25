using Unity.Entities;

/// <summary>
/// Cube 的选择状态
/// </summary>
public struct SelectionState : IComponentData
{
    /// <summary>
    /// 是否被选中：0=未选中, 1=选中
    /// </summary>
    public int IsSelected;
    
    /// <summary>
    /// 选中时间戳（用于动画）
    /// </summary>
    public float SelectTime;
}


