using Unity.Entities;
using Unity.Mathematics;

namespace Project.Core.Components
{
	public struct WorldDanceStart : IComponentData
	{
		public int StageIndex;
	}

	public struct WorldDanceState : IComponentData
	{
		public int StageIndex;
		public int Phase; // 0=Idle,1=Dismantle,2=Flight,3=Assemble,4=Done
		public float PhaseStartTime;
		public float GatherHeight;
		public float PlanetRadius;
		public int TotalParticipants;
	}

	public struct DanceIndex : IComponentData
	{
		public int Value;
	}

	public struct DanceDismantle : IComponentData
	{
		public float StartTime;
		public float Duration;
		public float OutwardPower;
		public float NoiseAmplitude;
		public uint Seed;
		public float3 BasePosition;
	}

	public struct DanceFlight : IComponentData
	{
		public float StartTime;
		public float DurationGather;
		public float DurationToSphere;
		public float Delay;
		public float3 StartPos;
		public float3 GatherPoint;
		public float3 TargetPos;
	}

	public struct WorldDanceSettings : IComponentData
	{
		public float GatherHeight;       // 聚集高度（当前未使用聚集，但可保留）
		public float PlanetRadius;       // 星球半径（星球大小）
		public float ScatterDuration;    // 分散阶段时长（速度）
		public float ScatterPower;       // 分散强度（密度/幅度）
		public float ScatterNoise;       // 分散噪声幅度（密度感）
		public float FlightDuration;     // 直达目标的飞行时长（速度）
		public float DelayStep;          // 分批延迟步长
		public float SequenceIntervalSeconds; // 多关按序触发的间隔秒数

		// Planet ring
		public int RingEnabled;          // 0/1 是否启用行星环
		public float RingInnerRadius;    // 内半径
		public float RingOuterRadius;    // 外半径
		public float RingTiltDeg;        // 倾角（度）
		public float RingShare;          // 分配到环上的比例 0..1

		// Spins
		public float PlanetSpinDegPerSec;      // 星球自转角速度（度/秒）
		public float CubeSpinMinDegPerSec;     // 方块自旋最小角速度
		public float CubeSpinMaxDegPerSec;     // 方块自旋最大角速度
	}

	public struct CubeSpin : IComponentData
	{
		public float3 Axis;            // 单位轴
		public float SpeedDegPerSec;   // 角速度
	}

	public struct EnableFreeFlyRequest : IComponentData
	{
		public int Enable; // 1=enable, 0=disable
	}

	public struct WorldDanceSequenceStart : IComponentData
	{
		public int StartIndex;
		public int EndIndex; // <0 表示使用 LevelSequence 的最后一关
		public float IntervalSeconds;
	}

	public struct WorldDanceSequenceState : IComponentData
	{
		public int Current;
		public int End;
		public float Interval;
		public float NextTime;
		public int TotalStages;
	}
}



