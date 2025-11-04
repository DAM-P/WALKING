using Unity.Entities;
using Project.Core.Authoring;

namespace Project.Core.Components
{
	/// 请求在 Mono 侧生成碰撞体（服务于 PhysX/CharacterController）
	public struct GenerateCollidersRequest : IComponentData
	{
		public BlobAssetReference<LayoutBlob> Layout;
		public CubeLayoutColliderGenerator.ColliderType ColliderType;
		public CubeLayoutColliderGenerator.MergeMode MergeMode;
		public int StageIndex;
	}

	/// 请求销毁某一关生成的静态碰撞体
	public struct DestroyStageCollidersRequest : IComponentData
	{
		public int StageIndex;
	}
}
















