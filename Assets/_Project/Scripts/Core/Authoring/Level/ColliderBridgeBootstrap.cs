using UnityEngine;

namespace Project.Core.Authoring
{
	/// 自动确保场景中存在 ColliderGeneratorBridge，避免忘记放置导致不生效
	public class ColliderBridgeBootstrap : MonoBehaviour
	{
		[Tooltip("可选：指定带 CubeLayoutColliderGenerator 的预制体作为宿主")]
		public GameObject generatorPrefab;

		void Awake()
		{
			var bridge = FindObjectOfType<ColliderGeneratorBridge>();
			if (bridge == null)
			{
				var go = new GameObject("ColliderGeneratorBridge_Auto");
				bridge = go.AddComponent<ColliderGeneratorBridge>();
				bridge.generatorPrefab = generatorPrefab;
				DontDestroyOnLoad(go);
				Debug.Log("[ColliderBootstrap] 自动创建 ColliderGeneratorBridge");
			}
		}
	}
}
















