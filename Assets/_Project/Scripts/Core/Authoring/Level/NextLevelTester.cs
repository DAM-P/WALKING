using UnityEngine;
using Unity.Entities;
using Project.Core.Components;

namespace Project.Core.Authoring
{
	/// 在运行时按 L 发起“下一关”请求（测试用）
	public class NextLevelTester : MonoBehaviour
	{
		EntityManager _em;
		void Awake()
		{
			_em = World.DefaultGameObjectInjectionWorld != null ? World.DefaultGameObjectInjectionWorld.EntityManager : default;
			if (_em == default)
			{
				Debug.LogWarning("[NextLevelTester] 无默认 World，禁用");
				enabled = false;
			}
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.L))
			{
				var e = _em.CreateEntity(typeof(NextLevelRequest));
				Debug.Log("[NextLevelTester] 请求切换到下一关");
			}
		}
	}
}



