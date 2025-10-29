using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 根据 CubeLayout 自动生成合并的碰撞体（优化性能，支持 CharacterController）
    /// </summary>
    public class CubeLayoutColliderGenerator : MonoBehaviour
    {
        [Header("Layout")]
        public CubeLayout layout;

        [Header("Collider Type")]
        [Tooltip("推荐使用 BoxCollider（与 CharacterController 兼容性最佳）")]
        public ColliderType colliderType = ColliderType.BoxCollider;

        [Header("BoxCollider Settings")]
        [Tooltip("智能合并算法：贪心合并相邻方块为大 Box（强烈推荐，大幅减少碰撞体数量）")]
        public MergeMode mergeMode = MergeMode.GreedyMerge;
        [Tooltip("调试：在 Scene 视图中绘制生成的 BoxCollider 边界")]
        public bool drawDebugGizmos = false;

        [Header("MeshCollider Settings (不推荐，可能导致卡住)")]
        [Tooltip("是否将 MeshCollider 标记为 Convex（不推荐用于地形）")]
        public bool convex = false;

        [Header("General")]
        [Tooltip("生成时自动清除旧碰撞体")]
        public bool clearOldColliders = true;

        public enum ColliderType
        {
            MeshCollider,
            BoxCollider
        }

        public enum MergeMode
        {
            None,           // 每个方块一个 BoxCollider（不推荐，数量多）
            LayerMerge,     // 按 Y 层合并（中等优化）
            GreedyMerge     // 贪心算法合并（强烈推荐，最优化）
        }

        private List<BoxCollider> _generatedColliders = new List<BoxCollider>();

        [ContextMenu("Generate Collider")]
        public void GenerateCollider()
        {
            Debug.Log($"[ColliderGen] 开始生成碰撞体，Type={colliderType}", this);
            
            if (layout == null || layout.cells == null || layout.cells.Count == 0)
            {
                Debug.LogError("[ColliderGen] Layout 为空或无 cells！", this);
                return;
            }

            Debug.Log($"[ColliderGen] Layout={layout.name}, Cells={layout.cells.Count}, CellSize={layout.cellSize}", this);

            if (clearOldColliders)
            {
                ClearColliders();
            }

            float cellSize = layout.cellSize > 0 ? layout.cellSize : 1f;

            if (colliderType == ColliderType.BoxCollider)
            {
                GenerateBoxColliders(cellSize);
            }
            else
            {
                GenerateMeshCollider(cellSize);
            }
            
            Debug.Log($"[ColliderGen] 碰撞体生成完成！GameObject={gameObject.name}", this);
        }

        [ContextMenu("Clear Colliders")]
        public void ClearColliders()
        {
            // 清除本对象及子层级的所有 Collider，避免旧的逐块 Collider 残留
            var colliders = GetComponentsInChildren<Collider>(true);
            int count = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (Application.isPlaying)
                    Destroy(col);
                else
                    DestroyImmediate(col);
                count++;
            }
            Debug.Log($"[ColliderGen] 已清除(含子层级) {count} 个碰撞体", this);
        }

        private void GenerateMeshCollider(float cellSize)
        {
            Debug.Log($"[ColliderGen] 开始生成 MeshCollider，方块数={layout.cells.Count}");
            
            // 合并所有 cells 为一个网格
            CombineInstance[] combines = new CombineInstance[layout.cells.Count];
            
            // 创建单位立方体网格（1x1x1）
            Mesh cubeMesh = CreateUnitCubeMesh();

            for (int i = 0; i < layout.cells.Count; i++)
            {
                var cell = layout.cells[i];
                Vector3 pos = layout.origin + new Vector3(cell.coord.x, cell.coord.y, cell.coord.z) * cellSize;
                
                combines[i].mesh = cubeMesh;
                combines[i].transform = Matrix4x4.TRS(
                    pos - transform.position, // 相对于此 GameObject 的位置
                    Quaternion.identity,
                    Vector3.one * cellSize
                );
            }

            Mesh combinedMesh = new Mesh();
            combinedMesh.name = $"{layout.name}_ColliderMesh";
            combinedMesh.CombineMeshes(combines, true, true);
            combinedMesh.RecalculateBounds();

            var meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = combinedMesh;
            meshCollider.convex = convex;

            Debug.Log($"[ColliderGen] 生成 MeshCollider：{layout.cells.Count} 个方块，{combinedMesh.vertexCount} 顶点，Convex={convex}");
        }

        private void GenerateBoxColliders(float cellSize)
        {
            Debug.Log($"[ColliderGen] 开始生成 BoxCollider，方块数={layout.cells.Count}, MergeMode={mergeMode}");
            _generatedColliders.Clear();

            switch (mergeMode)
            {
                case MergeMode.None:
                    GenerateIndividualBoxes(cellSize);
                    break;
                case MergeMode.LayerMerge:
                    GenerateLayerMergedBoxes(cellSize);
                    break;
                case MergeMode.GreedyMerge:
                    GenerateGreedyMergedBoxes(cellSize);
                    break;
            }

            Debug.Log($"[ColliderGen] 生成 {_generatedColliders.Count} 个 BoxCollider（从 {layout.cells.Count} 个方块合并）");
        }

        private void GenerateIndividualBoxes(float cellSize)
        {
            foreach (var cell in layout.cells)
            {
                AddBoxCollider(cell.coord, cellSize, Vector3Int.one);
            }
        }

        private void GenerateLayerMergedBoxes(float cellSize)
        {
            var layers = layout.cells.GroupBy(c => c.coord.y).OrderBy(g => g.Key);

            foreach (var layer in layers)
            {
                int y = layer.Key;
                var cellsInLayer = layer.ToList();
                GenerateGreedyMergedBoxesForLayer(cellsInLayer, y, cellSize);
            }
        }

        private void GenerateGreedyMergedBoxes(float cellSize)
        {
            // 贪心算法：尝试合并最大的立方体/长方体
            HashSet<Vector3Int> remaining = new HashSet<Vector3Int>(layout.cells.Select(c => c.coord));

            while (remaining.Count > 0)
            {
                var start = remaining.First();
                var size = FindLargestBox(start, remaining);
                
                // 移除已合并的方块
                for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                for (int z = 0; z < size.z; z++)
                {
                    remaining.Remove(start + new Vector3Int(x, y, z));
                }

                AddBoxCollider(start, cellSize, size);
            }
        }

        private void GenerateGreedyMergedBoxesForLayer(List<CubeLayout.Cell> cells, int y, float cellSize)
        {
            HashSet<Vector3Int> remaining = new HashSet<Vector3Int>();
            foreach (var cell in cells)
            {
                remaining.Add(new Vector3Int(cell.coord.x, 0, cell.coord.z));
            }

            while (remaining.Count > 0)
            {
                var start = remaining.First();
                var size = FindLargestBox2D(start, remaining);
                
                for (int x = 0; x < size.x; x++)
                for (int z = 0; z < size.y; z++)
                {
                    remaining.Remove(start + new Vector3Int(x, 0, z));
                }

                AddBoxCollider(new Vector3Int(start.x, y, start.z), cellSize, new Vector3Int(size.x, 1, size.y));
            }
        }

        private Vector3Int FindLargestBox(Vector3Int start, HashSet<Vector3Int> available)
        {
            // 贪心：尝试从 start 扩展最大的长方体
            Vector3Int bestSize = Vector3Int.one;
            int bestVolume = 1;

            // 先沿 X 尽量扩，再沿 Y，再沿 Z（线性扫描，稳定且高效）
            int maxX = 1;
            while (CanFormBox(start, new Vector3Int(maxX + 1, 1, 1), available)) maxX++;
            for (int x = maxX; x >= 1; x--)
            {
                int maxY = 1;
                while (CanFormBox(start, new Vector3Int(x, maxY + 1, 1), available)) maxY++;
                for (int y = maxY; y >= 1; y--)
                {
                    int maxZ = 1;
                    while (CanFormBox(start, new Vector3Int(x, y, maxZ + 1), available)) maxZ++;
                    int volume = x * y * maxZ;
                    if (volume > bestVolume)
                    {
                        bestVolume = volume;
                        bestSize = new Vector3Int(x, y, maxZ);
                    }
                }
            }

            return bestSize;
        }

        private Vector2Int FindLargestBox2D(Vector3Int start, HashSet<Vector3Int> available)
        {
            Vector2Int bestSize = Vector2Int.one;
            int bestArea = 1;

            for (int maxX = 1; maxX <= 64; maxX++)
            {
                for (int maxZ = 1; maxZ <= 64; maxZ++)
                {
                    if (CanFormBox2D(start, new Vector2Int(maxX, maxZ), available))
                    {
                        int area = maxX * maxZ;
                        if (area > bestArea)
                        {
                            bestArea = area;
                            bestSize = new Vector2Int(maxX, maxZ);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return bestSize;
        }

        private bool CanFormBox(Vector3Int start, Vector3Int size, HashSet<Vector3Int> available)
        {
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            for (int z = 0; z < size.z; z++)
            {
                if (!available.Contains(start + new Vector3Int(x, y, z)))
                    return false;
            }
            return true;
        }

        private bool CanFormBox2D(Vector3Int start, Vector2Int size, HashSet<Vector3Int> available)
        {
            for (int x = 0; x < size.x; x++)
            for (int z = 0; z < size.y; z++)
            {
                if (!available.Contains(start + new Vector3Int(x, 0, z)))
                    return false;
            }
            return true;
        }

        private void AddBoxCollider(Vector3Int startCoord, float cellSize, Vector3Int size)
        {
            // 计算合并后 Box 的中心位置
            Vector3 boxCenter = layout.origin + new Vector3(
                startCoord.x + (size.x - 1) * 0.5f,
                startCoord.y + (size.y - 1) * 0.5f,
                startCoord.z + (size.z - 1) * 0.5f
            ) * cellSize;
            
            Vector3 localCenter = boxCenter - transform.position;
            Vector3 boxSize = new Vector3(size.x, size.y, size.z) * cellSize;

            var box = gameObject.AddComponent<BoxCollider>();
            box.center = localCenter;
            box.size = boxSize;
            
            _generatedColliders.Add(box);
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugGizmos || _generatedColliders == null || _generatedColliders.Count == 0)
                return;

            Gizmos.color = new Color(0, 1, 0, 0.3f);
            foreach (var box in _generatedColliders)
            {
                if (box != null)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                }
            }
        }

        private Mesh CreateUnitCubeMesh()
        {
            // 创建 1x1x1 的立方体网格（中心在原点）
            Mesh mesh = new Mesh();
            mesh.name = "UnitCube";

            Vector3[] vertices = new Vector3[]
            {
                // 前面
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f),
                // 后面
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f),
                // 左面
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f),
                // 右面
                new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                // 上面
                new Vector3(-0.5f,  0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f),
                // 下面
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f)
            };

            int[] triangles = new int[]
            {
                0, 2, 1, 0, 3, 2,       // 前
                4, 6, 5, 4, 7, 6,       // 后
                8, 10, 9, 8, 11, 10,    // 左
                12, 14, 13, 12, 15, 14, // 右
                16, 18, 17, 16, 19, 18, // 上
                20, 22, 21, 20, 23, 22  // 下
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}

