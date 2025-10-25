using Unity.Entities;
using Unity.Mathematics;

public struct PositionLerp : IComponentData
{
    public float3 Start;
    public float3 Target;
    public float Duration;
    public float Elapsed;
}















