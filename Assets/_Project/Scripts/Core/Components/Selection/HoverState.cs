using Unity.Entities;

namespace Project.Core.Components
{
	/// 鼠标/准星悬停状态（用于未选中时的可交互提示）
	public struct HoverState : IComponentData
	{
		public byte IsHovered; // 0/1
	}
}



