using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Project.Core.Components;
#if HAS_URP_MATERIAL_PROPERTY
using Unity.Rendering;
#endif
using UnityEngine;

namespace Project.Core.Systems
{
	[BurstCompile]
	public partial struct ExtendedLifetimeSystem : ISystem
	{
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<ExtendedLifetime>();
		}

		public void OnUpdate(ref SystemState state)
		{
			float dt = SystemAPI.Time.DeltaTime;
			var ecb = new EntityCommandBuffer(Allocator.Temp);

			foreach (var (life, entity) in SystemAPI.Query<RefRW<ExtendedLifetime>>().WithEntityAccess())
			{
				life.ValueRW.RemainingSeconds -= dt;
				float total = math.max(0.0001f, life.ValueRO.TotalSeconds);
				float t = math.saturate(life.ValueRO.RemainingSeconds / total); // 1 -> 0

#if HAS_URP_MATERIAL_PROPERTY
				if (state.EntityManager.HasComponent<URPMaterialPropertyBaseColor>(entity))
				{
					var baseColor = state.EntityManager.GetComponentData<URPMaterialPropertyBaseColor>(entity);
					baseColor.Value.w = life.ValueRO.OriginalAlpha * t;
					state.EntityManager.SetComponentData(entity, baseColor);
				}
#endif

				if (life.ValueRO.RemainingSeconds <= 0f)
				{
					// 销毁关联的 Collider GameObject（如果存在）
					if (state.EntityManager.HasComponent<ColliderReference>(entity))
					{
						var cref = state.EntityManager.GetComponentData<ColliderReference>(entity);
						var go = Resources.InstanceIDToObject(cref.GameObjectInstanceID) as GameObject;
						if (go != null) Object.Destroy(go);
						ecb.RemoveComponent<ColliderReference>(entity);
					}
					ecb.DestroyEntity(entity);
				}
			}

			ecb.Playback(state.EntityManager);
		}
	}
}



