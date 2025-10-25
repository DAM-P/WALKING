using Unity.Entities;
using Unity.Mathematics;

namespace Project.Core.Components
{
    /// <summary>
    /// 拉伸系统设置 Singleton
    /// 存储拉伸时需要的全局配置
    /// </summary>
    public struct ExtendSettings : IComponentData
    {
        /// <summary>
        /// 拉伸使用的 Cube Prefab Entity
        /// </summary>
        public Entity CubePrefab;

        /// <summary>
        /// Cube 尺寸（用于计算位置）
        /// </summary>
        public float CubeSize;

        /// <summary>
        /// 是否应用实例颜色
        /// </summary>
        public bool ApplyInstanceColor;

        /// <summary>
        /// 拉伸出的 Cube 的默认颜色
        /// </summary>
        public float4 DefaultColor;

        /// <summary>
        /// 是否自动添加 BoxCollider
        /// </summary>
        public bool AutoAddCollider;
    }
}

