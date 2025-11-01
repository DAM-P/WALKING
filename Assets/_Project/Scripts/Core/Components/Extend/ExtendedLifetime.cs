using Unity.Entities;

namespace Project.Core.Components
{
	/// 为拉伸产生的 Cube 附加寿命，到期后自动销毁
    public struct ExtendedLifetime : IComponentData
    {
        public float RemainingSeconds;
        public float TotalSeconds;
        public float OriginalAlpha; // 记录初始透明度，便于正确计算渐隐
    }
}



