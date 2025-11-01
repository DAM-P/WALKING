using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Core.Authoring
{
	/// 按键重开当前场景（默认 R 键）
	public class RestartOnKey : MonoBehaviour
	{
		[Tooltip("触发重开的按键")] public KeyCode restartKey = KeyCode.R;
		[Tooltip("是否需要按住而不是单击")] public bool requireHold = false;
		[Tooltip("按住多久触发（秒），仅当 requireHold=true 时生效")] public float holdSeconds = 0.5f;

		float _downTime;

		void Update()
		{
			if (!requireHold)
			{
				if (Input.GetKeyDown(restartKey)) Reload();
				return;
			}

			if (Input.GetKeyDown(restartKey)) _downTime = Time.unscaledTime;
			if (Input.GetKey(restartKey))
			{
				if (Time.unscaledTime - _downTime >= Mathf.Max(0.05f, holdSeconds)) Reload();
			}
		}

		void Reload()
		{
			var scene = SceneManager.GetActiveScene();
			SceneManager.LoadScene(scene.buildIndex);
		}
	}
}



