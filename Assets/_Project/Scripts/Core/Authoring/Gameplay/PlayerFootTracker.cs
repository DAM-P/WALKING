using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Project.Core.Components;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 将玩家 Transform 的位置转换为栅格坐标，写入 PlayerGridFoot 单例。
    /// </summary>
    public class PlayerFootTracker : MonoBehaviour
    {
        public Transform player;
        public Vector3 origin;
        public float cellSize = 1f;

        private EntityManager _em;
        private bool _hasEm;
        private Entity _singleton;
        private bool _initialized;
        private int3 _lastLogged = new int3(int.MinValue, int.MinValue, int.MinValue);

        private void Awake()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _em = world.EntityManager;
                _hasEm = true;
            }
            EnsureSingleton();
        }

        private void OnEnable()
        {
            EnsureSingleton();
        }

        private void EnsureSingleton()
        {
            if (_initialized || !_hasEm) return;
            var q = _em.CreateEntityQuery(ComponentType.ReadOnly<PlayerGridFoot>());
            if (!q.TryGetSingletonEntity<PlayerGridFoot>(out _singleton))
            {
                _singleton = _em.CreateEntity(typeof(PlayerGridFoot));
                _em.SetComponentData(_singleton, new PlayerGridFoot { CurrentCell = int3.zero, LastCell = new int3(int.MinValue, int.MinValue, int.MinValue) });
            }
            _initialized = true;
        }

        private void Update()
        {
            if (!_hasEm || !_initialized || player == null || cellSize <= 0.0001f) return;
            float3 p = player.position;
            float3 o = origin;
            float size = Mathf.Max(0.0001f, cellSize);
            int3 cell = new int3(Mathf.RoundToInt((p.x - o.x) / size), Mathf.RoundToInt((p.y - o.y) / size), Mathf.RoundToInt((p.z - o.z) / size));
            var data = _em.GetComponentData<PlayerGridFoot>(_singleton);
            data.CurrentCell = cell;
            _em.SetComponentData(_singleton, data);

#if UNITY_EDITOR
            if (!cell.Equals(_lastLogged))
            {
                _lastLogged = cell;
                Debug.Log($"[PlayerFootTracker] CurrentCell={cell}");
            }
#endif
        }
    }
}


