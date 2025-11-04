using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Project.Core.Components;

namespace Project.Core.Authoring
{
	/// 播放“世界之舞”音乐：收到 WorldDanceStart/WorldDanceSequenceStart 时淡入播放，可选在结束时淡出
	public class WorldDanceAudioController : MonoBehaviour
	{
		[Header("Audio")]
		public AudioSource audioSource;
		public AudioClip music;
		[Range(0f,1f)] public float targetVolume = 0.8f;
		public bool loop = true;
		[Tooltip("可选：若提供，则通过 BGM 控制器播放并淡入/切换")]
		public BackgroundMusicController bgmController;
		[Header("Fade")] public float fadeInSeconds = 0.8f;
		public float fadeOutSeconds = 1.0f;
		[Tooltip("当舞蹈结束（无拆解/飞行/状态）时自动淡出")] public bool autoStopOnEnd = true;

		EntityManager _em;
		EntityQuery _startQ;
		EntityQuery _seqStartQ;
		EntityQuery _stateQ;
		EntityQuery _seqStateQ;
		EntityQuery _flightQ;
		EntityQuery _dismantleQ;
		float _fadeTimer;
		bool _fadingIn;
		bool _fadingOut;
		bool _started;
		[HideInInspector] public float lastStartTime;
		[HideInInspector] public float lastClipLength;

		[Header("End Fade")]
		[Tooltip("歌曲结束前开始黑屏渐入的时长（秒）")] public float endFadeSeconds = 1.0f;
		bool _endFadeActive;
		float _endFadeTimer;
		Image _fadeImage;
		GameObject _fadeCanvasGO;
		static bool _endFadeStartedGlobally;

		void Awake()
		{
			if (audioSource == null)
			{
				audioSource = gameObject.AddComponent<AudioSource>();
			}
			audioSource.playOnAwake = false;
			audioSource.spatialBlend = 0f;
			audioSource.dopplerLevel = 0f;
			audioSource.ignoreListenerPause = true;
			audioSource.loop = loop;
			if (music != null)
			{
				// 预加载音频数据，降低首次播放延迟
				audioSource.clip = music;
				music.LoadAudioData();
			}
			audioSource.volume = targetVolume;

			if (World.DefaultGameObjectInjectionWorld != null)
			{
				_em = World.DefaultGameObjectInjectionWorld.EntityManager;
				_startQ = _em.CreateEntityQuery(typeof(WorldDanceStart));
				_seqStartQ = _em.CreateEntityQuery(typeof(WorldDanceSequenceStart));
				_stateQ = _em.CreateEntityQuery(typeof(WorldDanceState));
				_seqStateQ = _em.CreateEntityQuery(typeof(WorldDanceSequenceState));
				_flightQ = _em.CreateEntityQuery(typeof(DanceFlight));
				_dismantleQ = _em.CreateEntityQuery(typeof(DanceDismantle));
			}
		}

		void Update()
		{
			if (_em == default) return;

			// Trigger on start events
			if (!_startQ.IsEmpty || !_seqStartQ.IsEmpty)
			{
				BeginPlay();
				// 消耗一次性世界事件（系统会自己销毁，这里无需处理）
			}

			// Any active dance?
			bool anyActive = (!_stateQ.IsEmpty || !_seqStateQ.IsEmpty || !_flightQ.IsEmpty || !_dismantleQ.IsEmpty);
			if (anyActive && !_started)
			{
				BeginPlay();
				_started = true;
			}

			// Auto stop on end
			if (autoStopOnEnd && _started)
			{
				bool anyNow = (!_stateQ.IsEmpty || !_seqStateQ.IsEmpty || !_flightQ.IsEmpty || !_dismantleQ.IsEmpty);
				if (!anyNow)
				{
					BeginFadeOut();
					_started = false;
				}
			}

			// Handle fade-out only（不淡入）
			if (_fadingOut)
			{
				_fadeTimer += Time.unscaledDeltaTime;
				float t = Mathf.Clamp01(_fadeTimer / Mathf.Max(0.01f, fadeOutSeconds));
				audioSource.volume = Mathf.Lerp(targetVolume, 0f, t);
				if (t >= 1f)
				{
					_fadingOut = false;
					audioSource.Stop();
				}
			}

			// 尾声黑屏渐入（仅第一个控制器生效）
			if (!_endFadeStartedGlobally && _started && music != null && lastClipLength > 0f)
			{
				float now = Time.unscaledTime;
				float lead = Mathf.Max(0.01f, endFadeSeconds);
				if (now >= lastStartTime + lastClipLength - lead)
				{
					StartEndFade();
				}
			}

			if (_endFadeActive && _fadeImage != null)
			{
				_endFadeTimer += Time.unscaledDeltaTime;
				float t = Mathf.Clamp01(_endFadeTimer / Mathf.Max(0.01f, endFadeSeconds));
				var c = _fadeImage.color; c.a = t; _fadeImage.color = c;
			}
		}

		void LateUpdate()
		{
			// 再做一遍检测，保证在 ECS 写入状态后当帧也能起声
			if (_em == default) return;
			bool anyActive = (!_stateQ.IsEmpty || !_seqStateQ.IsEmpty || !_flightQ.IsEmpty || !_dismantleQ.IsEmpty);
			if (anyActive && !_started)
			{
				BeginPlay();
				_started = true;
			}
		}

		public void BeginPlay()
		{
			if (music == null) return;
			if (bgmController != null)
			{
				bgmController.Play(music, 0.1f, false); // 不循环，便于“结束后”进入结局
			}
			else
			{
				// 立即起声：PlayOneShot，不做淡入（允许重复触发，不依赖 isPlaying）
				music.LoadAudioData();
				audioSource.PlayOneShot(music, Mathf.Clamp01(targetVolume));
			}
			lastStartTime = Time.unscaledTime;
			lastClipLength = music.length;
			_fadingOut = false;
			_fadingIn = false;
			_fadeTimer = 0f;
		}

		void StartEndFade()
		{
			_endFadeStartedGlobally = true;
			_endFadeActive = true;
			_endFadeTimer = 0f;
			EnsureFadeOverlay();
			// 关闭 SelectionAutoFix 的 UI
			var autoFixes = FindObjectsOfType<SelectionAutoFix>();
			for (int i = 0; i < autoFixes.Length; i++) autoFixes[i].enabled = false;

			// 同时淡出 bgmController（若存在）
			if (bgmController != null)
			{
				bgmController.Stop(endFadeSeconds);
			}

			// 切场景时移除本覆盖层，避免遮挡下一场景内容
			SceneManager.sceneLoaded -= OnSceneLoaded;
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		void EnsureFadeOverlay()
		{
			if (_fadeImage != null) return;
			_fadeCanvasGO = new GameObject("EndFadeCanvas", typeof(Canvas), typeof(CanvasScaler));
			var canvas = _fadeCanvasGO.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 10000; // 顶层

			var imgGO = new GameObject("Fade", typeof(Image));
			imgGO.transform.SetParent(_fadeCanvasGO.transform, false);
			var rt = imgGO.GetComponent<RectTransform>();
			rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
			_fadeImage = imgGO.GetComponent<Image>();
			_fadeImage.color = new Color(0f, 0f, 0f, 0f);
			_fadeImage.raycastTarget = true; // 防止误点
		}

		void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			// 清理覆盖层，交给下一场景自己的淡入逻辑
			if (_fadeCanvasGO != null)
			{
				Destroy(_fadeCanvasGO);
			}
			_fadeCanvasGO = null;
			_fadeImage = null;
			_endFadeActive = false;
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		public void BeginFadeOut()
		{
			if (!audioSource.isPlaying) return;
			_fadingIn = false;
			_fadingOut = true;
			_fadeTimer = 0f;
		}
	}
}



