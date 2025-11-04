using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Core.Authoring
{
	/// 在世界之舞音乐播放结束后，跳转到结局场景
	public class WorldDanceEndSceneLoader : MonoBehaviour
	{
		public WorldDanceAudioController audioController;
		[Tooltip("结局场景名（需在 Build Settings 中）")] public string endSceneName = "Ending";
		[Tooltip("额外等待秒数（用于尾音/残响）")] public float extraWaitSeconds = 0.0f;
		bool _loaded;

		void Awake()
		{
			if (audioController == null) audioController = FindObjectOfType<WorldDanceAudioController>();
		}

		void Update()
		{
			if (_loaded || audioController == null || audioController.music == null) return;
			float t0 = audioController.lastStartTime;
			float len = audioController.lastClipLength;
			if (len <= 0f || t0 <= 0f) return;
			if (Time.unscaledTime >= t0 + len + Mathf.Max(0f, extraWaitSeconds))
			{
				_loaded = true;
				SceneManager.LoadScene(endSceneName);
			}
		}
	}
}




