using Unity.Entities;

public struct SpawnIndex : IComponentData
{
    public int Index;      // 全局索引（0..total-1）
    public int SubIndex;   // 在各自集合内的索引（星球内或环内）
    public byte IsPlanet;  // 1=星球, 0=环
    public float RandY;    // 环的Y随机比率[0,1]
    public float RandR;    // 环的径向随机比率[0,1]
}






