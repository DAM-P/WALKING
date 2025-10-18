// File: Assets/_Project/Scripts/Cube/Authoring/RotatingCubeAuthoring.cs
using UnityEngine;
using Unity.Entities;

public class RotatingCubeAuthoring : MonoBehaviour
{
    class Baker : Baker<RotatingCubeAuthoring>
    {
        public override void Bake(RotatingCubeAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            // 在烘焙时，给这个实体贴上“RotatingCube”的标签
            AddComponent<RotatingCube>(entity);
        }
    }
}