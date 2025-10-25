using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Project.Core.Authoring;
using UnityEngine.Rendering;

namespace Project.Editor.GridBrush
{
    public class GridBrushWindow : EditorWindow
    {
        private CubeLayout _layout;
        private float _cellSize = 1f;
        private int _brushSize = 1;
        private int _typeId = 0;
        private Color _color = Color.white;
        private bool _erase;
        private bool _snapToGround = false;
        private bool _requireHoldKey = true; // 仅按住 B 键时启用画刷
        private static bool _isActivationKeyHeld = false;
        private bool _drawLayout = true; // 是否渲染整个布局
        private float _layoutAlpha = 0.18f; // 布局渲染的不透明度

        private readonly HashSet<Vector3Int> _previewCells = new HashSet<Vector3Int>();

        [MenuItem("Tools/Grid Brush")] 
        public static void Open()
        {
            var wnd = GetWindow<GridBrushWindow>("Grid Brush");
            wnd.minSize = new Vector2(320, 200);
            wnd.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            _layout = (CubeLayout)EditorGUILayout.ObjectField("Cube Layout", _layout, typeof(CubeLayout), false);
            if (_layout != null)
            {
                _layout.cellSize = EditorGUILayout.FloatField("Cell Size", Mathf.Max(0.01f, _layout.cellSize));
                _layout.origin = EditorGUILayout.Vector3Field("Origin", _layout.origin);
                _cellSize = _layout.cellSize;
            }
            else
            {
                _cellSize = EditorGUILayout.FloatField("Cell Size", Mathf.Max(0.01f, _cellSize));
            }

            _brushSize = EditorGUILayout.IntSlider("Brush Size", _brushSize, 1, 8);
            _typeId = EditorGUILayout.IntField("Type Id", _typeId);
            _color = EditorGUILayout.ColorField("Color", _color);
            _erase = EditorGUILayout.Toggle("Erase Mode", _erase);
            _snapToGround = EditorGUILayout.ToggleLeft("Snap To Ground (raycast down)", _snapToGround);
            _requireHoldKey = EditorGUILayout.ToggleLeft("Hold 'B' to activate brush", _requireHoldKey);
            _drawLayout = EditorGUILayout.ToggleLeft("Draw Layout Cells in Scene", _drawLayout);
            _layoutAlpha = EditorGUILayout.Slider("Layout Opacity", _layoutAlpha, 0.02f, 0.6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Layout Asset"))
                {
                    CreateLayoutAsset();
                }
                if (_layout != null && GUILayout.Button("Clear"))
                {
                    Undo.RecordObject(_layout, "Clear Cube Layout");
                    _layout.cells.Clear();
                    EditorUtility.SetDirty(_layout);
                }
            }

            EditorGUILayout.HelpBox("在 Scene 视图中按住左键绘制，右键擦除。按住 Shift 可暂时切换擦除，按住 Ctrl 对齐地面。", MessageType.Info);
        }

        private void OnSceneGUI(SceneView view)
        {
            if (_layout == null) return;

            var e = Event.current;

            // 处理激活按键（B）按下/抬起，仅在需要时启用
            if (_requireHoldKey)
            {
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.B) { _isActivationKeyHeld = true; }
                if (e.type == EventType.KeyUp && e.keyCode == KeyCode.B) { _isActivationKeyHeld = false; }
            }

            // 始终尝试渲染整个布局（不依赖激活状态）
            if (_drawLayout)
            {
                DrawLayoutGizmos();
            }

            bool active = !_requireHoldKey || _isActivationKeyHeld;
            if (!active) return; // 未激活则不占用 SceneView 行为

            // 获取并声明本工具的控制权，阻止默认场景拖拽/选择（仅在激活时）
            int controlId = GUIUtility.GetControlID("GridBrushControl".GetHashCode(), FocusType.Passive);
            if (e.type == EventType.Layout) { HandleUtility.AddDefaultControl(controlId); }

            // 支持 ALT 进行场景旋转：按住 ALT 时不响应画刷
            if (e.alt) return;

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!TryGetPlaneHit(ray, out var hitPoint)) return;

            var snapped = SnapToGrid(hitPoint, _layout.origin, _cellSize);
            UpdatePreview(snapped);
            DrawPreviewGizmos();
            SceneView.RepaintAll();

            bool isErase = _erase ^ e.shift;
            bool snapDown = _snapToGround || e.control;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 || e.button == 1)
                    {
                        GUIUtility.hotControl = controlId;
                        if (e.button == 1 || isErase)
                        {
                            EraseCells(_previewCells);
                        }
                        else
                        {
                            PaintCells(_previewCells, snapDown);
                        }
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId && (e.button == 0 || e.button == 1))
                    {
                        if (e.button == 1 || isErase)
                        {
                            EraseCells(_previewCells);
                        }
                        else
                        {
                            PaintCells(_previewCells, snapDown);
                        }
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId && (e.button == 0 || e.button == 1))
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
                case EventType.KeyUp:
                    if (_requireHoldKey && e.keyCode == KeyCode.B && GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
            }
        }

        private void DrawLayoutGizmos()
        {
            if (_layout == null || _layout.cells == null || _layout.cells.Count == 0) return;

            var prevColor = Handles.color;
            var prevZ = Handles.zTest;
            Handles.zTest = CompareFunction.LessEqual;

            for (int i = 0; i < _layout.cells.Count; i++)
            {
                var cell = _layout.cells[i];
                var center = _layout.origin + (Vector3)cell.coord * (_layout.cellSize > 0 ? _layout.cellSize : _cellSize);
                var baseColor = (cell.color.a > 0.0001f ? (Color)cell.color : new Color(0.6f, 0.6f, 0.6f, 1f));
                var fill = new Color(baseColor.r, baseColor.g, baseColor.b, _layoutAlpha);
                var outline = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(_layoutAlpha * 3f));

                Handles.color = fill;
                Handles.CubeHandleCap(0, center, Quaternion.identity, (_layout.cellSize > 0 ? _layout.cellSize : _cellSize), EventType.Repaint);
                Handles.color = outline;
                Handles.DrawWireCube(center, Vector3.one * (_layout.cellSize > 0 ? _layout.cellSize : _cellSize) * 0.99f);
            }

            Handles.color = prevColor;
            Handles.zTest = prevZ;
        }

        private static bool TryGetPlaneHit(Ray ray, out Vector3 hit)
        {
            // 以 y=0 的水平面作为默认绘制平面
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            float enter;
            if (plane.Raycast(ray, out enter))
            {
                hit = ray.GetPoint(enter);
                return true;
            }
            hit = Vector3.zero;
            return false;
        }

        private static Vector3Int SnapToGrid(Vector3 worldPos, Vector3 origin, float size)
        {
            Vector3 p = (worldPos - origin) / Mathf.Max(0.0001f, size);
            return new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
        }

        private void UpdatePreview(Vector3Int center)
        {
            _previewCells.Clear();
            int r = Mathf.Max(0, _brushSize - 1);
            for (int x = -r; x <= r; x++)
            for (int y = -r; y <= r; y++)
            for (int z = -r; z <= r; z++)
            {
                _previewCells.Add(center + new Vector3Int(x, y, z));
            }
        }

        private void DrawPreviewGizmos()
        {
            var prevColor = Handles.color;
            var prevZ = Handles.zTest;
            Handles.zTest = CompareFunction.LessEqual;

            Color fill = _erase
                ? new Color(1f, 0.2f, 0.2f, 0.25f)
                : new Color(_color.r, _color.g, _color.b, 0.25f);
            Color outline = _erase
                ? new Color(1f, 0.2f, 0.2f, 0.9f)
                : new Color(_color.r, _color.g, _color.b, 0.9f);

            foreach (var c in _previewCells)
            {
                var center = _layout.origin + (Vector3)c * _cellSize;
                // 实心预览
                Handles.color = fill;
                Handles.CubeHandleCap(0, center, Quaternion.identity, _cellSize, EventType.Repaint);
                // 轮廓线
                Handles.color = outline;
                Handles.DrawWireCube(center, Vector3.one * _cellSize * 0.99f);
            }

            Handles.color = prevColor;
            Handles.zTest = prevZ;
        }

        private void PaintCells(IEnumerable<Vector3Int> cells, bool snapDown)
        {
            Undo.RecordObject(_layout, "Paint Cubes");
            foreach (var c in cells)
            {
                var coord = c;
                if (snapDown)
                {
                    coord = RaycastSnapDown(coord);
                }

                int idx = _layout.cells.FindIndex(x => x.coord == coord);
                var cell = new CubeLayout.Cell { coord = coord, typeId = _typeId, color = _color };
                if (idx >= 0) _layout.cells[idx] = cell; else _layout.cells.Add(cell);
            }
            EditorUtility.SetDirty(_layout);
        }

        private void EraseCells(IEnumerable<Vector3Int> cells)
        {
            Undo.RecordObject(_layout, "Erase Cubes");
            foreach (var c in cells)
            {
                int idx = _layout.cells.FindIndex(x => x.coord == c);
                if (idx >= 0) _layout.cells.RemoveAt(idx);
            }
            EditorUtility.SetDirty(_layout);
        }

        private Vector3Int RaycastSnapDown(Vector3Int coord)
        {
            // 将方块往下投射到最接近地面的 y，避免悬空绘制（编辑器帮助）
            var world = _layout.origin + (Vector3)coord * _cellSize + Vector3.up * (_cellSize * 0.5f + 0.01f);
            if (Physics.Raycast(world, Vector3.down, out var hit, 1000f))
            {
                float y = (hit.point.y - _layout.origin.y) / _cellSize;
                coord.y = Mathf.RoundToInt(y);
            }
            return coord;
        }

        private void CreateLayoutAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Cube Layout", "CubeLayout", "asset", "Choose a location for the layout asset");
            if (string.IsNullOrEmpty(path)) return;
            var asset = ScriptableObject.CreateInstance<CubeLayout>();
            asset.cellSize = Mathf.Max(0.01f, _cellSize);
            asset.origin = Vector3.zero;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _layout = asset;
        }
    }
}


