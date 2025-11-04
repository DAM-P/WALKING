using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Project.Core.Authoring
{
	public class LevelSequenceAuthoring : MonoBehaviour
	{
		[Header("Sequence")]
		public LevelSequence sequence;
	}

		public struct LevelSequenceRuntime : IComponentData
	{
		public int CurrentIndex;
		public int Started; // 是否已经生成了首关
	}

		public struct LevelBlobRef : IBufferElementData
	{
		public BlobAssetReference<LayoutBlob> Layout;
			public Entity Prefab;
			public int SpawnPerFrame;
			public int ApplyInstanceColor; // bool as int
			public float EmissionIntensity;
			public int RemoveOnComplete; // bool as int
			public CubeLayoutColliderGenerator.ColliderType ColliderType;
			public CubeLayoutColliderGenerator.MergeMode MergeMode;
				// Rise In per-stage settings
				public int RiseEnabled; // bool as int
				public float RiseHeightMultiplier;
				public float RiseDuration;
				public float RisePerCubeDelay;
	}

	public struct CubeCellData
	{
		public int3 coord;
		public int typeId;
		public float4 color;
	}

	public struct LayoutBlob
	{
		public float cellSize;
		public float3 origin;
		public BlobArray<CubeCellData> cells;
	}

	public class LevelSequenceBaker : Baker<LevelSequenceAuthoring>
	{
		public override void Bake(LevelSequenceAuthoring authoring)
		{
			if (authoring.sequence == null)
			{
				Debug.LogError("[LevelSequenceBaker] sequence 为空", authoring);
				return;
			}

			var holder = GetEntity(TransformUsageFlags.None);

			AddComponent(holder, new LevelSequenceRuntime
			{
				CurrentIndex = math.max(0, authoring.sequence.startIndex),
				Started = 0
			});

			var buffer = AddBuffer<LevelBlobRef>(holder);
			buffer.Clear();

			var stages = authoring.sequence.stages;
			if (stages != null)
			{
				for (int i = 0; i < stages.Count; i++)
				{
					var stage = stages[i];
					if (stage == null || stage.layout == null || stage.cubePrefab == null) continue;

					var builder = new BlobBuilder(Allocator.Temp);
					ref var root = ref builder.ConstructRoot<LayoutBlob>();
					root.cellSize = stage.layout.cellSize > 0 ? stage.layout.cellSize : 1f;
					root.origin = new float3(stage.layout.origin.x, stage.layout.origin.y, stage.layout.origin.z);

					int count = stage.layout.cells != null ? stage.layout.cells.Count : 0;
					var cellArray = builder.Allocate(ref root.cells, count);
					for (int c = 0; c < count; c++)
					{
						var src = stage.layout.cells[c];
						cellArray[c] = new CubeCellData
						{
							coord = new int3(src.coord.x, src.coord.y, src.coord.z),
							typeId = src.typeId,
							color = new float4(src.color.r, src.color.g, src.color.b, src.color.a)
						};
					}

					var blobRef = builder.CreateBlobAssetReference<LayoutBlob>(Allocator.Persistent);
					builder.Dispose();

					var prefab = GetEntity(stage.cubePrefab, TransformUsageFlags.Renderable);
					buffer.Add(new LevelBlobRef 
					{ 
						Layout = blobRef,
						Prefab = prefab,
						SpawnPerFrame = math.max(64, stage.spawnPerFrame),
						ApplyInstanceColor = stage.applyInstanceColor ? 1 : 0,
						EmissionIntensity = stage.emissionIntensity,
						RemoveOnComplete = 1,
						ColliderType = stage.colliderType,
						MergeMode = stage.mergeMode,
						RiseEnabled = stage.riseInEnabled ? 1 : 0,
						RiseHeightMultiplier = Mathf.Max(0f, stage.riseHeightMultiplier),
						RiseDuration = Mathf.Max(0.05f, stage.riseDuration),
						RisePerCubeDelay = Mathf.Max(0f, stage.perCubeDelay)
					});
				}
			}
		}
	}
}



