using Unity.Entities;

public struct LayoutController : IComponentData
{
    public int LayoutMode;  // 0=Grid, 1=PlanetRing, ... 可扩展
    public int ModeCount;   // 模式总数（默认2）
}


