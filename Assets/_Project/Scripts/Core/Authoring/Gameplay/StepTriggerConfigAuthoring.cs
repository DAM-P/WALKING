using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Project.Core.Components;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 配置：当踩到指定 TypeId 的方块时触发一次性效果。
    /// </summary>
    public class StepTriggerConfigAuthoring : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            [Tooltip("Cube TypeId")] public int typeId = 1;
            [Tooltip("变色颜色")] public Color color = Color.yellow;
            [Tooltip("发光强度")] [Range(0f, 8f)] public float emissionIntensity = 2f;
            [Tooltip("对应音效")] public AudioClip sfx;
        }

        [Tooltip("不同 Cube Type 的触发设置")] public List<Entry> entries = new List<Entry>();

        private EntityManager _em;
        private bool _hasEm;
        private Entity _entity;

        private void OnEnable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _em = world.EntityManager;
                _hasEm = true;
            }
            if (!_hasEm) return;

            // 单例实体 + Buffer
            var q = _em.CreateEntityQuery(ComponentType.ReadOnly<StepTriggerColor>());
            if (!q.TryGetSingletonEntity<StepTriggerColor>(out _entity))
            {
                _entity = _em.CreateEntity(typeof(StepTriggerColor));
                _em.AddBuffer<StepTriggerColor>(_entity);
            }
            if (!_em.HasBuffer<StepTriggerColor>(_entity))
            {
                _em.AddBuffer<StepTriggerColor>(_entity);
            }

            var buffer = _em.GetBuffer<StepTriggerColor>(_entity);
            buffer.Clear();
            foreach (var e in entries)
            {
                buffer.Add(new StepTriggerColor
                {
                    TypeId = e.typeId,
                    Color = new float4(e.color.r, e.color.g, e.color.b, e.color.a),
                    EmissionIntensity = e.emissionIntensity
                });
            }
        }

        public AudioClip GetClipForType(int typeId)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].typeId == typeId) return entries[i].sfx;
            }
            return null;
        }
    }
}


