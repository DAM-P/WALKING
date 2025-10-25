using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Project.Core.Components;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 拉伸系统设置 Authoring（创建 Singleton）
    /// 将此组件添加到场景中的任意 GameObject 上
    /// </summary>
    public class ExtendSettingsAuthoring : MonoBehaviour
    {
        [Header("Cube Prefab")]
        [Tooltip("拉伸时使用的 Cube Prefab（必须与地形 Cube 相同或兼容）")]
        public GameObject cubePrefab;

        [Header("设置")]
        [Tooltip("Cube 尺寸（网格单位）")]
        public float cubeSize = 1f;

        [Header("视觉")]
        [Tooltip("拉伸出的 Cube 的默认颜色")]
        public Color defaultColor = new Color(0.8f, 0.8f, 1f, 1f);

        [Tooltip("是否应用实例颜色（需要 URP）")]
        public bool applyInstanceColor = true;

        [Header("物理")]
        [Tooltip("是否自动为拉伸的 Cube 添加 BoxCollider")]
        public bool autoAddCollider = true;
    }

    public class ExtendSettingsBaker : Baker<ExtendSettingsAuthoring>
    {
        public override void Bake(ExtendSettingsAuthoring authoring)
        {
            if (authoring.cubePrefab == null)
            {
                Debug.LogError("[ExtendSettings] Cube Prefab 未设置！", authoring);
                return;
            }

            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new ExtendSettings
            {
                CubePrefab = GetEntity(authoring.cubePrefab, TransformUsageFlags.Dynamic),
                CubeSize = authoring.cubeSize,
                ApplyInstanceColor = authoring.applyInstanceColor,
                DefaultColor = new float4(
                    authoring.defaultColor.r,
                    authoring.defaultColor.g,
                    authoring.defaultColor.b,
                    authoring.defaultColor.a
                ),
                AutoAddCollider = authoring.autoAddCollider
            });
        }
    }
}

