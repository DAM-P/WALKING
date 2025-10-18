// File: Assets/_Project/Scripts/Spawning/Systems/SpawningSystem.cs
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct SpawningSystem : ISystem
{
    // OnCreate在System被创建时调用一次
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 关键点1: 设置更新条件
        state.RequireForUpdate<Spawner>();
    }
    
    // OnUpdate每帧调用 (如果满足更新条件)
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 关键点2: 完成工作后立刻“退休”
        state.Enabled = false;

        // 关键点3: 获取全局唯一的Spawner组件
        var spawner = SystemAPI.GetSingleton<Spawner>();

        // 关键点4: 实例化预制体
        // 如果设置了 Planet/Ring 计数，则按星球+行星环布局；否则回退到 SpawnCount 网格布局
        int totalCount = spawner.PlanetCount > 0 || spawner.RingCount > 0
            ? spawner.PlanetCount + spawner.RingCount
            : spawner.SpawnCount;

        var instances = state.EntityManager.Instantiate(spawner.CubePrefab, totalCount, Allocator.Temp);

        if (spawner.PlanetCount > 0 || spawner.RingCount > 0)
        {
            // 先准备网格初始位置（用于平滑过渡起点）
            int gridCols = (int)math.ceil(math.sqrt(totalCount));
            int gridRows = (int)math.ceil(totalCount / (float)gridCols);
            float gridSpacing = 2f;
            float gridStartX = -0.5f * (gridCols - 1) * gridSpacing;
            float gridStartZ = -0.5f * (gridRows - 1) * gridSpacing;

            // 星球（斐波那契球分布）
            int nPlanet = math.max(0, spawner.PlanetCount);
            float planetR = math.max(0.01f, spawner.PlanetRadius);
            for (int i = 0; i < nPlanet; i++)
            {
                float k = (i + 0.5f) / nPlanet;
                float phi = math.acos(1f - 2f * k);
                float theta = 2f * math.PI * (i * 0.61803398875f % 1f);
                float x = math.sin(phi) * math.cos(theta);
                float y = math.cos(phi);
                float z = math.sin(phi) * math.sin(theta);
                float3 pos = new float3(x, y, z) * planetR;
                // 初始为网格位置
                int gi = i;
                int gr = gi / gridCols;
                int gc = gi % gridCols;
                float3 startPos = new float3(gridStartX + gc * gridSpacing, 0f, gridStartZ + gr * gridSpacing);
                var lt = LocalTransform.FromPositionRotationScale(startPos, quaternion.identity, 1f);
                state.EntityManager.SetComponentData(instances[i], lt);
                // Lerp 到目标分布
                float dur = math.max(0f, spawner.TransitionDuration);
                state.EntityManager.AddComponentData(instances[i], new PositionLerp { Start = startPos, Target = pos, Duration = dur, Elapsed = 0f });
                state.EntityManager.AddComponent<PlanetTag>(instances[i]);
                // 随机旋转速度（每轴）
                float rx = 0.5f + (math.hash(new uint2((uint)i, 11u)) % 150) / 100f;
                float ry = 0.5f + (math.hash(new uint2((uint)i, 22u)) % 150) / 100f;
                float rz = 0.5f + (math.hash(new uint2((uint)i, 33u)) % 150) / 100f;
                state.EntityManager.AddComponentData(instances[i], new RotationSpeed { Value = new float3(rx, ry, rz) });
                // 索引信息（用于切换布局时重建目标位置）
                state.EntityManager.AddComponentData(instances[i], new SpawnIndex
                {
                    Index = i,
                    SubIndex = i,
                    IsPlanet = 1,
                    RandY = 0f,
                    RandR = 0f
                });
            }

            // 行星环（圆环 + 可选厚度）
            int nRing = math.max(0, spawner.RingCount);
            float ringR = math.max(planetR + 1f, spawner.RingRadius);
            float halfH = math.max(0f, spawner.RingHalfThickness);
            for (int j = 0; j < nRing; j++)
            {
                float t = (j / (float)nRing) * 2f * math.PI;
                float randY = (math.hash(new uint2((uint)j, 123u)) % 1000) / 999f;
                float y = halfH > 0f ? math.lerp(-halfH, halfH, randY) : 0f;
                // 径向随机偏移（均匀分布在 [ringR - w, ringR + w]）
                float w = math.max(0f, spawner.RingHalfWidth);
                float randR = (math.hash(new uint2((uint)j, 777u)) % 1000) / 999f;
                float r = ringR + math.lerp(-w, w, randR);
                float3 pos = new float3(math.cos(t) * r, y, math.sin(t) * r);
                // 初始为网格位置
                int gi = nPlanet + j;
                int gr = gi / gridCols;
                int gc = gi % gridCols;
                float3 startPos = new float3(gridStartX + gc * gridSpacing, 0f, gridStartZ + gr * gridSpacing);
                var lt = LocalTransform.FromPositionRotationScale(startPos, quaternion.identity, 1f);
                state.EntityManager.SetComponentData(instances[nPlanet + j], lt);
                float dur = math.max(0f, spawner.TransitionDuration);
                state.EntityManager.AddComponentData(instances[nPlanet + j], new PositionLerp { Start = startPos, Target = pos, Duration = dur, Elapsed = 0f });
                state.EntityManager.AddComponent<RingTag>(instances[nPlanet + j]);
                float rx = 0.5f + (math.hash(new uint2((uint)(nPlanet + j), 44u)) % 150) / 100f;
                float ry = 0.5f + (math.hash(new uint2((uint)(nPlanet + j), 55u)) % 150) / 100f;
                float rz = 0.5f + (math.hash(new uint2((uint)(nPlanet + j), 66u)) % 150) / 100f;
                state.EntityManager.AddComponentData(instances[nPlanet + j], new RotationSpeed { Value = new float3(rx, ry, rz) });
                // 索引信息
                state.EntityManager.AddComponentData(instances[nPlanet + j], new SpawnIndex
                {
                    Index = nPlanet + j,
                    SubIndex = j,
                    IsPlanet = 0,
                    RandY = randY,
                    RandR = randR
                });
            }
        }
        else
        {
            // 回退：网格分布
            int count = totalCount;
            if (count > 0)
            {
                int cols = (int)math.ceil(math.sqrt(count));
                int rows = (int)math.ceil(count / (float)cols);
                float spacing = 2f;
                float startX = -0.5f * (cols - 1) * spacing;
                float startZ = -0.5f * (rows - 1) * spacing;
                for (int i = 0; i < count; i++)
                {
                    int r = i / cols;
                    int c = i % cols;
                    float3 pos = new float3(startX + c * spacing, 0f, startZ + r * spacing);
                    var lt = LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f);
                    state.EntityManager.SetComponentData(instances[i], lt);
                    float rx = 0.5f + (math.hash(new uint2((uint)i, 77u)) % 150) / 100f;
                    float ry = 0.5f + (math.hash(new uint2((uint)i, 88u)) % 150) / 100f;
                    float rz = 0.5f + (math.hash(new uint2((uint)i, 99u)) % 150) / 100f;
                    state.EntityManager.AddComponentData(instances[i], new RotationSpeed { Value = new float3(rx, ry, rz) });
                }
            }
        }

        // 重要：释放NativeArray防止内存泄露
        instances.Dispose();
    }
}