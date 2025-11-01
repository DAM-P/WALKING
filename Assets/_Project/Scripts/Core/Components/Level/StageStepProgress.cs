using Unity.Entities;

namespace Project.Core.Components
{
	/// 当前关卡 StepTrigger 统计
	public struct StageStepProgress : IComponentData
	{
		public int StageIndex;
		public int TotalToTrigger;
		public int Triggered;
	}
}



