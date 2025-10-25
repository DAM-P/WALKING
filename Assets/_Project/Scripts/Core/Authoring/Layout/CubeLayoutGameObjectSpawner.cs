using UnityEngine;
using System.Collections.Generic;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 为静态场景生成 GameObject Cube（支持 CharacterController 碰撞）
    /// 用于地形、墙体等不需要 DOTS 逻辑的静态物体
    /// </summary>
    public class CubeLayoutGameObjectSpawner : MonoBehaviour
    {
        [Header("Prefab (GameObject with Collider)")]
        [Tooltip("必须包含 MeshRenderer 和 Collider（BoxCollider）")]
        public GameObject cubePrefab;

        [Header("Layout")]
        public CubeLayout layout;

        [Header("Spawn Settings")]
        [Tooltip("在 Awake 时自动生成")]
        public bool spawnOnAwake = true;
        [Tooltip("生成后标记为 Static（启用批处理）")]
        public bool markAsStatic = true;
        [Tooltip("合并成一个父物体（便于管理）")]
        public bool useParent = true;

        private List<GameObject> _spawnedObjects = new List<GameObject>();

        void Awake()
        {
            if (spawnOnAwake)
            {
                SpawnLayout();
            }
        }

        [ContextMenu("Spawn Layout")]
        public void SpawnLayout()
        {
            if (cubePrefab == null)
            {
                Debug.LogError("[CubeLayoutGO] cubePrefab 为空！", this);
                return;
            }
            if (layout == null || layout.cells == null || layout.cells.Count == 0)
            {
                Debug.LogError("[CubeLayoutGO] layout 为空或无 cells！", this);
                return;
            }

            Transform parent = useParent ? transform : null;
            float cellSize = layout.cellSize > 0 ? layout.cellSize : 1f;

            Debug.Log($"[CubeLayoutGO] 开始生成 GameObject 方块：总数={layout.cells.Count}");

            for (int i = 0; i < layout.cells.Count; i++)
            {
                var cell = layout.cells[i];
                Vector3 pos = layout.origin + new Vector3(cell.coord.x, cell.coord.y, cell.coord.z) * cellSize;

                GameObject go = Instantiate(cubePrefab, pos, Quaternion.identity, parent);
                go.name = $"Cube_{cell.coord.x}_{cell.coord.y}_{cell.coord.z}";

                // 应用颜色（如果有 Renderer）
                if (cell.color.a > 0.001f)
                {
                    var renderer = go.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        var mpb = new MaterialPropertyBlock();
                        mpb.SetColor("_BaseColor", cell.color);
                        renderer.SetPropertyBlock(mpb);
                    }
                }

                // 标记为 Static（批处理 + 光照烘焙 + 剔除优化）
                if (markAsStatic)
                {
                    go.isStatic = true;
                }

                _spawnedObjects.Add(go);
            }

            Debug.Log($"[CubeLayoutGO] 完成生成：共 {_spawnedObjects.Count} 个 GameObject Cube");
        }

        [ContextMenu("Clear Spawned")]
        public void ClearSpawned()
        {
            foreach (var go in _spawnedObjects)
            {
                if (go != null)
                {
                    if (Application.isPlaying)
                        Destroy(go);
                    else
                        DestroyImmediate(go);
                }
            }
            _spawnedObjects.Clear();
            Debug.Log("[CubeLayoutGO] 已清空所有生成的 GameObject");
        }
    }
}




