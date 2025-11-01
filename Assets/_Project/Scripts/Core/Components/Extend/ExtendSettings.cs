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

        /// <summary>
        /// 启用 Collider 的半径（米/世界单位）。仅玩家附近启用，远处禁用
        /// </summary>
        public float ColliderActiveRadius;

        /// <summary>
        /// 迟滞范围，避免边界来回抖动（米）
        /// </summary>
        public float ColliderDeactivateHysteresis;

		/// <summary>
		/// 拉伸产生的 Cube 在该秒数后自动消失（<=0 表示不自动消失）
		/// </summary>
		public float ExtendedLifetimeSeconds;
    }

    public static class ExtendSettingsExtensions
    {
        // 兼容旧资产：若未设置，给默认值
        public static float CubeColliderActiveRadiusFallback(this in ExtendSettings s)
        {
            return s.ColliderActiveRadius > 0f ? s.ColliderActiveRadius : 25f;
        }
    }
}

