// File: Assets/_Project/Scripts/Cube/Systems/RotatingSystem.cs
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct RotatingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new RotatingJob { DeltaTime = SystemAPI.Time.DeltaTime };
        job.ScheduleParallel();
    }
}

[BurstCompile]
[WithAll(typeof(RotatingCube))]
public partial struct RotatingJob : IJobEntity
{
    public float DeltaTime;

    void Execute(ref LocalTransform transform, in RotatingCube cubeTag, in RotationSpeed speed)
    {
        float3 ang = speed.Value * DeltaTime;
        quaternion qx = quaternion.RotateX(ang.x);
        quaternion qy = quaternion.RotateY(ang.y);
        quaternion qz = quaternion.RotateZ(ang.z);
        quaternion dq = math.mul(math.mul(qy, qx), qz);
        transform.Rotation = math.mul(transform.Rotation, dq);
    }
}