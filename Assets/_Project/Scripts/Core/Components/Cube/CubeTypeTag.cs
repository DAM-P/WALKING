using Unity.Entities;

namespace Project.Core.Components
{
    /// <summary>
    /// Cube 类型标记，用于区分静态地形、玩家创建、拉伸生成的 Cube
    /// </summary>
    public struct CubeTypeTag : IComponentData
    {
        public CubeType Type;
    }

    /// <summary>
    /// Cube 类型枚举
    /// </summary>
    public enum CubeType : byte
    {
        /// <summary>
        /// 静态地形 Cube（从 CubeLayout 生成）
        /// </summary>
        Static = 0,

        /// <summary>
        /// 玩家动态创建的 Cube
        /// </summary>
        Dynamic = 1,

        /// <summary>
        /// 通过拉伸功能生成的 Cube（属于某条链）
        /// </summary>
        Extended = 2
    }
}

