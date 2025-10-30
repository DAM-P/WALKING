using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Authoring.Gameplay
{
    /// <summary>
    /// 轻量级全局 SFX 管理器（单例）。
    /// - 提供在世界坐标播放一次性音效的方法。
    /// - 使用简易音源池避免频繁创建/销毁。
    /// </summary>
    public class SfxManager : MonoBehaviour
    {
        public static SfxManager Instance { get; private set; }

        [SerializeField]
        private int initialPoolSize = 8;

        private readonly Queue<AudioSource> _idle = new Queue<AudioSource>();
        private readonly HashSet<AudioSource> _busy = new HashSet<AudioSource>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            for (int i = 0; i < Mathf.Max(1, initialPoolSize); i++)
            {
                _idle.Enqueue(CreateAudioSource());
            }
        }

        private AudioSource CreateAudioSource()
        {
            var go = new GameObject("SFX_AudioSource");
            go.transform.parent = transform;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = 50f;
            go.SetActive(false);
            return src;
        }

        private AudioSource GetSource()
        {
            var src = _idle.Count > 0 ? _idle.Dequeue() : CreateAudioSource();
            _busy.Add(src);
            src.gameObject.SetActive(true);
            return src;
        }

        private void ReleaseSource(AudioSource src)
        {
            if (src == null) return;
            src.Stop();
            src.clip = null;
            src.gameObject.SetActive(false);
            _busy.Remove(src);
            _idle.Enqueue(src);
        }

        private System.Collections.IEnumerator ReleaseAfterPlay(AudioSource src)
        {
            yield return new WaitWhile(() => src != null && src.isPlaying);
            ReleaseSource(src);
        }

        public void PlayOneShotAtPosition(AudioClip clip, Vector3 position, float volume = 1f, bool spatial = false)
        {
            if (clip == null) return;
            var src = GetSource();
            src.transform.position = position;
            src.volume = Mathf.Clamp01(volume);
            src.spatialBlend = spatial ? 1f : 0f;
            src.clip = clip;
            src.Play();
            StartCoroutine(ReleaseAfterPlay(src));
        }
    }
}


