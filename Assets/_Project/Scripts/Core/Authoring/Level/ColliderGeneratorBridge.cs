using UnityEngine;
using Unity.Entities;
using Project.Core.Components;

namespace Project.Core.Authoring
{
	/// Mono 桥接：监听 GenerateCollidersRequest，基于 Blob 构建临时 CubeLayout 并调用 CubeLayoutColliderGenerator
	public class ColliderGeneratorBridge : MonoBehaviour
	{
		EntityManager _em;
		EntityQuery _query;
		public GameObject generatorPrefab; // 可选：外部指定；不指定则在自身上添加
		[Tooltip("保留之前生成的碰撞体（不清理旧的）")]
		public bool preserveExistingColliders = true;

		void Awake()
		{
			_em = World.DefaultGameObjectInjectionWorld.EntityManager;
			_query = _em.CreateEntityQuery(typeof(GenerateCollidersRequest));
		}

		void Update()
		{
			var entities = _query.ToEntityArray(Unity.Collections.Allocator.Temp);
			for (int i = 0; i < entities.Length; i++)
			{
				var e = entities[i];
				var req = _em.GetComponentData<GenerateCollidersRequest>(e);
				Debug.Log($"[ColliderBridge] 收到生成请求：cells={req.Layout.Value.cells.Length}, type={req.ColliderType}, merge={req.MergeMode}");
				TryGenerate(req);
				_em.DestroyEntity(e);
			}
			entities.Dispose();
		}

		void TryGenerate(GenerateCollidersRequest req)
		{
			// 构建临时 CubeLayout 资产以复用已有生成器逻辑
			var tmp = ScriptableObject.CreateInstance<CubeLayout>();
			ref var layout = ref req.Layout.Value;
			tmp.cellSize = layout.cellSize;
			tmp.origin = new Vector3(layout.origin.x, layout.origin.y, layout.origin.z);
			int count = layout.cells.Length;
			tmp.cells.Capacity = count;
			for (int i = 0; i < count; i++)
			{
				var c = layout.cells[i];
				tmp.cells.Add(new CubeLayout.Cell
				{
					coord = new Vector3Int(c.coord.x, c.coord.y, c.coord.z),
					typeId = c.typeId,
					color = new Color(c.color.x, c.color.y, c.color.z, c.color.w)
				});
			}

			GameObject host = generatorPrefab != null ? Instantiate(generatorPrefab) : gameObject;
			var gen = host.GetComponent<CubeLayoutColliderGenerator>();
			if (gen == null) gen = host.AddComponent<CubeLayoutColliderGenerator>();
			gen.layout = tmp;
			gen.colliderType = req.ColliderType;
			gen.mergeMode = req.MergeMode;
			// 是否清理旧碰撞体：若希望叠加生成，则关闭清理
			gen.clearOldColliders = !preserveExistingColliders ? true : false;
			gen.GenerateCollider();
			Debug.Log($"[ColliderBridge] 已生成 PhysX 碰撞体（Box/Mesh），对象={host.name}");
		}
	}
}



