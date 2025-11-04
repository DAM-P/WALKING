using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Project.Core.Components;

namespace Project.Core.Authoring
{
	[Serializable]
	public class DanceTriggerEntryAuthoring
	{
		public int stageIndex = 0;
		public int typeId = 0;
		public bool useCoord = false;
		public Vector3Int coord;
	}

	/// 在场景中放置此组件，配置“踩到哪个 StepCube 触发世界之舞”
	public class DanceTriggerConfigAuthoring : MonoBehaviour
	{
		public List<DanceTriggerEntryAuthoring> entries = new List<DanceTriggerEntryAuthoring>();
	}

	public class DanceTriggerConfigBaker : Baker<DanceTriggerConfigAuthoring>
	{
		public override void Bake(DanceTriggerConfigAuthoring authoring)
		{
			var e = GetEntity(TransformUsageFlags.None);
			var buffer = AddBuffer<DanceTriggerEntry>(e);
			buffer.Clear();
			if (authoring.entries != null)
			{
				foreach (var a in authoring.entries)
				{
					buffer.Add(new DanceTriggerEntry
					{
						StageIndex = a.stageIndex,
						TypeId = a.typeId,
						UseCoord = (byte)(a.useCoord ? 1 : 0),
						Coord = new int3(a.coord.x, a.coord.y, a.coord.z)
					});
				}
			}
		}
	}
}




