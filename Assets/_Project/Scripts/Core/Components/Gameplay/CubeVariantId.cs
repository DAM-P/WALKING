using Unity.Entities;

namespace Project.Core.Components
{
    /// <summary>
    /// 每个 Cube 的变体/类型编号（来自布局的 typeId），用于玩法判定。
    /// </summary>
    public struct CubeVariantId : IComponentData
    {
        public int Value;
    }
}



