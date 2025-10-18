// File: Assets/_Project/Scripts/Spawning/Authoring/SpawnerAuthoring.cs
using UnityEngine;
using Unity.Entities;

public class SpawnerAuthoring : MonoBehaviour
{
    public GameObject CubePrefab; // 在编辑器里指定要生成的GameObject Prefab
    public int SpawnCount;

    [Header("Planet + Ring Layout (overrides SpawnCount if > 0)")]
    public int PlanetCount = 0;
    public int RingCount = 0;
    public float PlanetRadius = 5f;
    public float RingRadius = 10f;
    public float RingHalfThickness = 0.2f;
    public float RingHalfWidth = 1.0f;
    public float PlanetAngularSpeed = 0.5f;
    public float RingAngularSpeed = 0.8f;
    public float TransitionDuration = 2f;

    class Baker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new Spawner
            {
                // GetEntity是关键！它将GameObject Prefab转换成Entity Prefab的引用
                CubePrefab = GetEntity(authoring.CubePrefab, TransformUsageFlags.Dynamic),
                SpawnCount = authoring.SpawnCount,
                PlanetCount = authoring.PlanetCount,
                RingCount = authoring.RingCount,
                PlanetRadius = authoring.PlanetRadius,
                RingRadius = authoring.RingRadius,
                RingHalfThickness = authoring.RingHalfThickness,
                RingHalfWidth = authoring.RingHalfWidth,
                PlanetAngularSpeed = authoring.PlanetAngularSpeed,
                RingAngularSpeed = authoring.RingAngularSpeed,
                TransitionDuration = authoring.TransitionDuration
            });
        }
    }
}