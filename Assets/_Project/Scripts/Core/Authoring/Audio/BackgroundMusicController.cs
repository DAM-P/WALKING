using UnityEngine;

namespace Project.Core.Authoring
{
	/// 背景音乐控制：循环播放、渐入渐出、无缝切换（双 AudioSource 交替）
	public class BackgroundMusicController : MonoBehaviour
	{
		[Header("Defaults")]
		[Range(0f,1f)] public float targetVolume = 0.8f;
		[Tooltip("默认淡入/淡出时长（秒）")] public float defaultFadeSeconds = 0.6f;
		public bool playOnStart = false;
		public AudioClip startClip;

		AudioSource _a;
		AudioSource _b;
		AudioSource _active;
		AudioSource _idle;
		float _fadeTime;
		float _fadeDur;
		bool _fading;
		bool _fadingOutOnly;
		float _fromVol;
		float _toVol;

		void Awake()
		{
			_a = gameObject.AddComponent<AudioSource>();
			_b = gameObject.AddComponent<AudioSource>();
			foreach (var s in new[]{_a,_b})
			{
				s.playOnAwake = false;
				s.loop = true;
				s.volume = 0f;
				s.spatialBlend = 0f;
				s.dopplerLevel = 0f;
				s.ignoreListenerPause = true;
			}
			_active = _a; _idle = _b;
			if (playOnStart && startClip != null)
			{
				Play(startClip, defaultFadeSeconds, true);
			}
		}

		void Update()
		{
			if (!_fading) return;
			_fadeTime += Time.unscaledDeltaTime;
			float t = _fadeDur <= 0.01f ? 1f : Mathf.Clamp01(_fadeTime/_fadeDur);
			_active.volume = Mathf.Lerp(_fromVol, _toVol, t);
			_idle.volume = Mathf.Lerp(_toVol, _fromVol, t); // 对称处理，若是 crossfade 则另一边反向
			if (t >= 1f)
			{
				_fading = false;
				// 停止空闲源
				_idle.Stop();
				_idle.clip = null;
			}
		}

		public void Play(AudioClip clip, float fadeSeconds = -1f, bool loop = true)
		{
			if (clip == null) return;
			clip.LoadAudioData();
			// 交给空闲源开始播放，然后交叉淡入
			SwapIfNeeded();
			_idle.clip = clip;
			_idle.loop = loop;
			_idle.volume = 0f;
			_idle.Play();
			// 交换角色：让新曲成为 active
			var tmp = _active; _active = _idle; _idle = tmp;
			BeginFade(0f, targetVolume, fadeSeconds);
		}

		public void PlayImmediate(AudioClip clip, bool loop = true)
		{
			if (clip == null) return;
			clip.LoadAudioData();
			SwapIfNeeded();
			_active.Stop();
			_active.clip = clip;
			_active.loop = loop;
			_active.volume = targetVolume;
			_active.Play();
			// 停掉另一源
			_idle.Stop();
			_idle.clip = null;
			_fading = false;
		}

		public void Stop(float fadeOutSeconds = -1f)
		{
			BeginFade(_active.volume, 0f, fadeOutSeconds);
		}

		public void SetVolume(float v)
		{
			targetVolume = Mathf.Clamp01(v);
			if (!_fading) _active.volume = targetVolume;
		}

		void BeginFade(float from, float to, float fadeSeconds)
		{
			_fadeDur = fadeSeconds >= 0f ? fadeSeconds : defaultFadeSeconds;
			_fromVol = from;
			_toVol = to;
			_fadeTime = 0f;
			_fading = true;
		}

		void SwapIfNeeded()
		{
			// 确保 _active 与 _idle 指向不同源
			if (_active == null || _idle == null)
			{
				_active = _a; _idle = _b;
			}
			if (_active == _idle)
			{
				_idle = (_active == _a) ? _b : _a;
			}
		}
	}
}




