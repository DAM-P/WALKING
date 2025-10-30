using Unity.Entities;
using UnityEngine;
using Project.Core.Components;
using Project.Core.Authoring.Gameplay;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 桥接 ECS 的 StepAudioEvent 到 Unity 音频播放。
    /// 将本脚本与 StepTriggerConfigAuthoring 放在同一 GameObject 上以提供 sfx。
    /// </summary>
    public class StepAudioBridge : MonoBehaviour
    {
        private EntityManager _em;
        private bool _hasEm;
        private EntityQuery _query;
        private bool _queryReady;
        private StepTriggerConfigAuthoring _config;

        private void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _em = world.EntityManager;
                _hasEm = true;
            }
            _config = GetComponent<StepTriggerConfigAuthoring>();
            if (_hasEm)
            {
                _query = _em.CreateEntityQuery(ComponentType.ReadOnly<StepAudioEvent>());
                _queryReady = true;
            }
        }

        private void Update()
        {
            if (!_hasEm || !_queryReady) return;
            using (var ents = _query.ToEntityArray(Unity.Collections.Allocator.Temp))
            using (var events = _query.ToComponentDataArray<StepAudioEvent>(Unity.Collections.Allocator.Temp))
            {
                for (int i = 0; i < ents.Length; i++)
                {
                    var e = events[i];
                    AudioClip clip = null;
                    if (_config != null)
                        clip = _config.GetClipForType(e.TypeId);
                    if (clip != null)
                    {
                        var pos = new Vector3(e.Position.x, e.Position.y, e.Position.z);
                        if (SfxManager.Instance != null)
                            SfxManager.Instance.PlayOneShotAtPosition(clip, pos, 1f, false); // 2D 播放，距离不影响音量
                        else
                            AudioSource.PlayClipAtPoint(clip, pos);
#if UNITY_EDITOR
                        Debug.Log($"[StepAudioBridge] Played SFX at {e.Position}");
#endif
                    }
                    _em.DestroyEntity(ents[i]);
                }
            }
        }
    }
}


