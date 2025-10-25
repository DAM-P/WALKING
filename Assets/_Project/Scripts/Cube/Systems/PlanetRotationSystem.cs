using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct PlanetRotationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Spawner>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 仅在 PlanetRing 模式旋转
        LayoutController lc;
        if (SystemAPI.TryGetSingleton(out lc))
        {
            if (lc.LayoutMode != 1) return;
        }

        float dt = SystemAPI.Time.DeltaTime;
        float ang = SystemAPI.GetSingleton<Spawner>().PlanetAngularSpeed * dt;
        if (math.abs(ang) < 1e-6f) return;

        var job = new RotatePlanetJob
        {
            DeltaAngle = ang
        };
        job.ScheduleParallel();
    }
}

[BurstCompile]
[WithAll(typeof(PlanetTag))]
public partial struct RotatePlanetJob : IJobEntity
{
    public float DeltaAngle;

    void Execute(ref LocalTransform transform)
    {
        // 绕Y轴旋转位置（保持半径不变）
        float3 p = transform.Position;
        float s = math.sin(DeltaAngle);
        float c = math.cos(DeltaAngle);
        float3 rotated = new float3(p.x * c + p.z * s, p.y, -p.x * s + p.z * c);
        transform.Position = rotated;
    }
}



