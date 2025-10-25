using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct PositionLerpSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var job = new LerpJob { DeltaTime = dt };
        job.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct LerpJob : IJobEntity
{
    public float DeltaTime;

    void Execute(ref LocalTransform transform, ref PositionLerp lerp)
    {
        // 当插值完成后，不再写入位置，避免覆盖其他系统（如行星/环自转）对位置的更新
        if (lerp.Duration <= 0f) { return; }
        lerp.Elapsed = math.min(lerp.Elapsed + DeltaTime, lerp.Duration);
        float t = lerp.Elapsed / lerp.Duration;
        // 使用平滑曲线（ease in-out）
        t = t * t * (3f - 2f * t);
        transform.Position = math.lerp(lerp.Start, lerp.Target, t);
        if (lerp.Elapsed >= lerp.Duration)
        {
            // 标记完成：后续帧不再覆盖位置
            lerp.Duration = 0f;
        }
    }
}


