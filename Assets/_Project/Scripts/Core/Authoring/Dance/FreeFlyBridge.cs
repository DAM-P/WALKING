using UnityEngine;
using Unity.Entities;
using Project.Core.Components;

namespace Project.Core.Authoring
{
	/// 监听 EnableFreeFlyRequest，启用/关闭 FreeFlyController
	public class FreeFlyBridge : MonoBehaviour
	{
		EntityManager _em;
		EntityQuery _q;
		FreeFlyController _fly;

		void Awake()
		{
			if (World.DefaultGameObjectInjectionWorld != null)
			{
				_em = World.DefaultGameObjectInjectionWorld.EntityManager;
				_q = _em.CreateEntityQuery(typeof(EnableFreeFlyRequest));
			}
			_fly = FindObjectOfType<FreeFlyController>();
		}

		void Update()
		{
			if (_em == default) return;
			var ents = _q.ToEntityArray(Unity.Collections.Allocator.Temp);
			for (int i = 0; i < ents.Length; i++)
			{
				var e = ents[i];
				var req = _em.GetComponentData<EnableFreeFlyRequest>(e);
				if (_fly != null) _fly.SetActiveExternal(req.Enable != 0);
				_em.DestroyEntity(e);
			}
			ents.Dispose();
		}
	}
}



