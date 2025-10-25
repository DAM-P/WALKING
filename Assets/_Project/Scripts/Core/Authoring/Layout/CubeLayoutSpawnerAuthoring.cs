using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Project.Core.Authoring
{
    public class CubeLayoutSpawnerAuthoring : MonoBehaviour
    {
        [Header("Prefab (Entities Graphics compatible)")]
        public GameObject cubePrefab;

        [Header("Layout")]
        public CubeLayout layout;

        [Header("Spawn Settings")]
        [Tooltip("每帧实例化数量上限，避免大布局一次性卡顿")]
        public int spawnPerFrame = 2048;

        [Header("Instance Properties (URP)")]
        [Tooltip("将 CubeLayout 的颜色写入每实例 _BaseColor/_EmissionColor（需要 URP + Entities Graphics）")]
        public bool applyInstanceColor = true;
        [Tooltip("发光强度（乘以颜色），0 表示不写入发光属性")]
        [Range(0f, 5f)] public float emissionIntensity = 1.2f;

        [Header("Lifecycle")]
        [Tooltip("全部生成完成后移除 Spawner 组件（减少系统开销）")]
        public bool removeOnComplete = true;
    }

    public struct CubeLayoutSpawner : IComponentData
    {
        public Entity Prefab;
        public float3 Origin;
        public float CellSize;
        public int SpawnPerFrame;
        public int SpawnedCount; // 运行时进度
        public int ApplyInstanceColor; // bool as int
        public float EmissionIntensity;
        public int RemoveOnComplete; // bool as int
    }

    public struct CubeCell : IBufferElementData
    {
        public int3 Coord;
        public int TypeId;
        public float4 Color; // 可用于每实例颜色（如后续添加URP属性组件）
    }

    public class CubeLayoutSpawnerBaker : Baker<CubeLayoutSpawnerAuthoring>
    {
        public override void Bake(CubeLayoutSpawnerAuthoring authoring)
        {
            if (authoring.cubePrefab == null)
            {
                UnityEngine.Debug.LogError($"[Baker] cubePrefab 为空！请在 Inspector 中赋值", authoring);
                return;
            }
            if (authoring.layout == null)
            {
                UnityEngine.Debug.LogError($"[Baker] layout 为空！请在 Inspector 中赋值", authoring);
                return;
            }

            var holder = GetEntity(TransformUsageFlags.None);
            var prefab = GetEntity(authoring.cubePrefab, TransformUsageFlags.Renderable);

            AddComponent(holder, new CubeLayoutSpawner
            {
                Prefab = prefab,
                Origin = authoring.layout.origin,
                CellSize = authoring.layout.cellSize > 0 ? authoring.layout.cellSize : 1f,
                SpawnPerFrame = math.max(64, authoring.spawnPerFrame),
                SpawnedCount = 0,
                ApplyInstanceColor = authoring.applyInstanceColor ? 1 : 0,
                EmissionIntensity = authoring.emissionIntensity,
                RemoveOnComplete = authoring.removeOnComplete ? 1 : 0
            });

            var buffer = AddBuffer<CubeCell>(holder);
            buffer.Clear();
            if (authoring.layout.cells != null)
            {
                for (int i = 0; i < authoring.layout.cells.Count; i++)
                {
                    var c = authoring.layout.cells[i];
                    buffer.Add(new CubeCell
                    {
                        Coord = new int3(c.coord.x, c.coord.y, c.coord.z),
                        TypeId = c.typeId,
                        Color = new float4(c.color.r, c.color.g, c.color.b, c.color.a)
                    });
                }
            }
        }
    }
}


