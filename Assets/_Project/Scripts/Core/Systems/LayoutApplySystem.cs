using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct LayoutApplySystem : ISystem
{
    EntityQuery _query;
    int _lastMode;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Spawner>();
        _query = state.GetEntityQuery(ComponentType.ReadOnly<SpawnIndex>(), typeof(LocalTransform), typeof(PositionLerp));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        LayoutController lc;
        if (!SystemAPI.TryGetSingleton(out lc)) return;
        if (lc.LayoutMode == _lastMode) return;
        _lastMode = lc.LayoutMode;

        var spawner = SystemAPI.GetSingleton<Spawner>();
        int total = _query.CalculateEntityCount();
        if (total == 0) return;

        int cols = (int)math.ceil(math.sqrt(total));
        int rows = (int)math.ceil(total / (float)cols);
        float spacing = 2f;
        float startX = -0.5f * (cols - 1) * spacing;
        float startZ = -0.5f * (rows - 1) * spacing;

        float planetR = math.max(0.01f, spawner.PlanetRadius);
        float ringRBase = math.max(planetR + 1f, spawner.RingRadius);
        float halfH = math.max(0f, spawner.RingHalfThickness);
        float halfW = math.max(0f, spawner.RingHalfWidth);
        float dur = math.max(0f, spawner.TransitionDuration);

        foreach (var (xf, lerp, idx, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PositionLerp>, RefRO<SpawnIndex>>().WithEntityAccess())
        {
            int i = idx.ValueRO.Index;
            int gr = i / cols;
            int gc = i % cols;
            float3 gridPos = new float3(startX + gc * spacing, 0f, startZ + gr * spacing);

            float3 targetPos;
            if (lc.LayoutMode == 1) // PlanetRing 模式
            {
                if (idx.ValueRO.IsPlanet == 1)
                {
                    int nPlanet = math.max(1, spawner.PlanetCount);
                    int pi = idx.ValueRO.SubIndex;
                    float k = (pi + 0.5f) / nPlanet;
                    float phi = math.acos(1f - 2f * k);
                    float theta = 2f * math.PI * (pi * 0.61803398875f % 1f);
                    float x = math.sin(phi) * math.cos(theta);
                    float y = math.cos(phi);
                    float z = math.sin(phi) * math.sin(theta);
                    targetPos = new float3(x, y, z) * planetR;
                }
                else
                {
                    int rj = idx.ValueRO.SubIndex;
                    int nRing = math.max(1, spawner.RingCount);
                    float t = (rj / (float)nRing) * 2f * math.PI;
                    float ry = idx.ValueRO.RandY;
                    float rr = idx.ValueRO.RandR;
                    float y = halfH > 0f ? math.lerp(-halfH, halfH, ry) : 0f;
                    float r = ringRBase + math.lerp(-halfW, halfW, rr);
                    targetPos = new float3(math.cos(t) * r, y, math.sin(t) * r);
                }
            }
            else // Grid 模式（或其它回退）
            {
                targetPos = gridPos;
            }

            lerp.ValueRW.Start = xf.ValueRO.Position;
            lerp.ValueRW.Target = targetPos;
            lerp.ValueRW.Duration = dur;
            lerp.ValueRW.Elapsed = 0f;
        }
    }
}


