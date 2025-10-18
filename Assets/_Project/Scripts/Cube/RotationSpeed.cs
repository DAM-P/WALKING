using Unity.Entities;
using Unity.Mathematics;

public struct RotationSpeed : IComponentData
{
    public float3 Value; // 每轴角速度（弧度/秒）
}


