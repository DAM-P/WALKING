using Unity.Entities;
using UnityEngine;
using Project.Core.Components;

namespace Project.Core.Authoring
{
	public class WorldDanceSettingsAuthoring : MonoBehaviour
	{
		[Header("Planet")]
		public float planetRadius = 20f;
		public float gatherHeight = 50f;

		[Header("Scatter (Dismantle)")]
		[Tooltip("分散开的时长，越小越快")] public float scatterDuration = 1.0f;
		[Tooltip("分散开的密度/强度，越大越分散")] public float scatterPower = 1.0f;
		[Tooltip("分散噪声幅度，越大越杂乱")] public float scatterNoise = 0.2f;

		[Header("Flight")]
		[Tooltip("飞行至目标点的时长，越小越快")] public float flightDuration = 2.0f;
		[Tooltip("分批延迟步长（秒）")] public float delayStep = 0.03f;
		[Tooltip("多关按序触发的间隔秒数")] public float sequenceIntervalSeconds = 0.5f;

		[Header("Ring")]
		public bool ringEnabled = true;
		public float ringInnerRadius = 24f;
		public float ringOuterRadius = 32f;
		public float ringTiltDeg = 20f;
		[Range(0f,1f)] public float ringShare = 0.25f;

		[Header("Spin")]
		public float planetSpinDegPerSec = 10f;
		public float cubeSpinMinDegPerSec = 20f;
		public float cubeSpinMaxDegPerSec = 60f;
	}

	public class WorldDanceSettingsBaker : Baker<WorldDanceSettingsAuthoring>
	{
		public override void Bake(WorldDanceSettingsAuthoring authoring)
		{
			var e = GetEntity(TransformUsageFlags.None);
			AddComponent(e, new WorldDanceSettings
			{
				GatherHeight = authoring.gatherHeight,
				PlanetRadius = authoring.planetRadius,
				ScatterDuration = Mathf.Max(0.01f, authoring.scatterDuration),
				ScatterPower = authoring.scatterPower,
				ScatterNoise = authoring.scatterNoise,
				FlightDuration = Mathf.Max(0.01f, authoring.flightDuration),
				DelayStep = Mathf.Max(0f, authoring.delayStep),
				SequenceIntervalSeconds = Mathf.Max(0.01f, authoring.sequenceIntervalSeconds),
				RingEnabled = authoring.ringEnabled ? 1 : 0,
				RingInnerRadius = Mathf.Max(0.01f, authoring.ringInnerRadius),
				RingOuterRadius = Mathf.Max(authoring.ringInnerRadius + 0.01f, authoring.ringOuterRadius),
				RingTiltDeg = authoring.ringTiltDeg,
				RingShare = Mathf.Clamp01(authoring.ringShare),
				PlanetSpinDegPerSec = authoring.planetSpinDegPerSec,
				CubeSpinMinDegPerSec = authoring.cubeSpinMinDegPerSec,
				CubeSpinMaxDegPerSec = Mathf.Max(authoring.cubeSpinMinDegPerSec, authoring.cubeSpinMaxDegPerSec)
			});
		}
	}
}



