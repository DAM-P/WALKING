using UnityEngine;
using Unity.Entities;
using Project.Core.Components;

namespace Project.Core.Authoring
{
	public class WorldDanceTester : MonoBehaviour
	{
		[Tooltip("触发按键（单关）")] public KeyCode triggerKey = KeyCode.K;
		[Tooltip("触发按键（序列）")] public KeyCode triggerSequenceKey = KeyCode.L;
		[Tooltip("使用当前 StageStepProgress 的 StageIndex；关闭则使用指定值")] public bool useCurrentStage = true;
		public int stageIndexOverride = 0;
		[Header("Sequence")]
		public bool useSequenceLength = true;
		public int endIndexOverride = 0;
		public float sequenceIntervalSeconds = 1.0f;

		EntityManager _em;
		void Awake()
		{
			if (World.DefaultGameObjectInjectionWorld != null)
				_em = World.DefaultGameObjectInjectionWorld.EntityManager;
		}

		void Update()
		{
			if (_em == default) return;
			if (Input.GetKeyDown(triggerKey))
			{
				int idx = stageIndexOverride;
				if (useCurrentStage)
				{
					var q = _em.CreateEntityQuery(typeof(StageStepProgress));
					if (!q.IsEmpty)
					{
						idx = _em.GetComponentData<StageStepProgress>(q.GetSingletonEntity()).StageIndex;
					}
				}
				var e = _em.CreateEntity(typeof(WorldDanceStart));
				_em.SetComponentData(e, new WorldDanceStart { StageIndex = idx });
				Debug.Log($"[WorldDanceTester] Trigger stage={idx}");
			}

			if (Input.GetKeyDown(triggerSequenceKey))
			{
				int startIdx = stageIndexOverride;
				if (useCurrentStage)
				{
					var q = _em.CreateEntityQuery(typeof(StageStepProgress));
					if (!q.IsEmpty)
					{
						startIdx = _em.GetComponentData<StageStepProgress>(q.GetSingletonEntity()).StageIndex;
					}
				}
				int endIdx = useSequenceLength ? -1 : endIndexOverride;
				var e2 = _em.CreateEntity(typeof(WorldDanceSequenceStart));
				_em.SetComponentData(e2, new WorldDanceSequenceStart { StartIndex = startIdx, EndIndex = endIdx, IntervalSeconds = Mathf.Max(0.01f, sequenceIntervalSeconds) });
				Debug.Log($"[WorldDanceTester] Trigger sequence start={startIdx} end={(endIdx<0?"auto":endIdx)} interval={sequenceIntervalSeconds:F2}s");
			}
		}
	}
}



