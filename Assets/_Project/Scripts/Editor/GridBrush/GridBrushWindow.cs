using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Project.Core.Authoring;
using UnityEngine.Rendering;
using System.IO;

namespace Project.Editor.GridBrush
{
    public class GridBrushWindow : EditorWindow
    {
        private enum EditPlane
        {
            XZ, // Horizontal (height on Y)
            XY, // Vertical facing Z (height on Z)
            YZ  // Vertical facing X (height on X)
        }

        private CubeLayout _layout;
        private float _cellSize = 1f;
        private int _brushSize = 1;
        private int _typeId = 0;
        private Color _color = Color.white;
        private bool _erase;
        private bool _snapToGround = false;
        private bool _paintVerticalLine = false; // 是否绘制垂直线
        private int _verticalLineHeight = 3; // 垂直线高度（格）
        private bool _requireHoldKey = true; // 仅按住 B 键时启用画刷
        private static bool _isActivationKeyHeld = false;
        private bool _drawLayout = true; // 是否渲染整个布局
        private float _layoutAlpha = 0.18f; // 布局渲染的不透明度
		private const string ColorHistoryPrefsKey = "Project.GridBrush.ColorHistory";
		private const int MaxColorHistory = 16;
		private readonly List<Color> _colorHistory = new List<Color>();

        // Plane editing options
        private EditPlane _editPlane = EditPlane.XZ;
        private int _planeHeight = 0; // grid units along the plane's orthogonal axis

        // Rectangle painting options
        private bool _rectangleMode = false; // 长方形绘制
        private bool _rectangleHollow = false; // 仅边框
        private bool _isRectSelecting = false;
        private Vector3Int _rectStart;
        private bool _rectErase = false;

        private readonly HashSet<Vector3Int> _previewCells = new HashSet<Vector3Int>();
		private bool _hasHover;
		private Vector3Int _hoverCoord;
		private bool _hasHoverCell;
		private CubeLayout.Cell _hoverCell;

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
			LoadColorHistory();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
			SaveColorHistory();
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
			var newColor = EditorGUILayout.ColorField("Color", _color);
			if (newColor != _color)
			{
				_color = newColor;
				AddColorToHistory(_color);
				SaveColorHistory();
			}
			DrawColorHistoryGUI();
            _erase = EditorGUILayout.Toggle("Erase Mode", _erase);
            // Plane selection and height
            _editPlane = (EditPlane)EditorGUILayout.EnumPopup("Edit Plane", _editPlane);
            using (new EditorGUILayout.HorizontalScope())
            {
                string axis = _editPlane == EditPlane.XZ ? "Y" : (_editPlane == EditPlane.XY ? "Z" : "X");
                _planeHeight = EditorGUILayout.IntField($"{axis} (Plane Height)", _planeHeight);
                if (GUILayout.Button("-", GUILayout.Width(24))) _planeHeight--;
                if (GUILayout.Button("+", GUILayout.Width(24))) _planeHeight++;
            }
            // Rectangle controls
            _rectangleMode = EditorGUILayout.ToggleLeft("Rectangle Mode (drag to size)", _rectangleMode);
            if (_rectangleMode)
            {
                _rectangleHollow = EditorGUILayout.ToggleLeft("Hollow Rectangle", _rectangleHollow);
            }
            _paintVerticalLine = EditorGUILayout.ToggleLeft("Paint Vertical Line (Y axis)", _paintVerticalLine);
            if (_paintVerticalLine)
            {
                _verticalLineHeight = EditorGUILayout.IntSlider("Vertical Height", Mathf.Max(1, _verticalLineHeight), 1, 64);
            }
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
                if (_layout != null)
                {
                    bool hasAny = _layout.cells != null && _layout.cells.Count > 0;
                    using (new EditorGUI.DisabledScope(!hasAny))
                    {
                        if (GUILayout.Button("Clear"))
                        {
                            ConfirmAndClearLayout();
                        }
                    }
                }
            }

            // Hover 信息显示
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Hover Cell", EditorStyles.boldLabel);
                if (_hasHover)
                {
                    EditorGUILayout.LabelField($"Coord: {_hoverCoord}");
                    if (_hasHoverCell)
                    {
                        EditorGUILayout.LabelField($"typeId: {_hoverCell.typeId}");
                        string hex = ColorUtility.ToHtmlStringRGBA(_hoverCell.color);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            Rect r = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
                            EditorGUI.DrawRect(r, _hoverCell.color.a > 0.0001f ? (Color)_hoverCell.color : new Color(0.6f, 0.6f, 0.6f, 1f));
                            EditorGUILayout.LabelField($"Color: #{hex}");
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("此位置暂无方块");
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("将鼠标移动到 Scene 中的网格上以查看信息");
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

            // 更新 Hover 信息（不论是否激活）
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            bool hasHit = TryGetPlaneHit(ray, out var hitPoint);
            Vector3Int snapped = default;
            if (hasHit)
            {
                snapped = SnapToGrid(hitPoint, _layout.origin, _cellSize);
                UpdateHover(snapped);
            }

            // 快捷键：按下 C 复制悬停格信息
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.C && _hasHover)
            {
                CopyHoverInfo();
                e.Use();
            }

            bool active = !_requireHoldKey || _isActivationKeyHeld;
            if (!active) return; // 未激活则不占用 SceneView 行为

            // 获取并声明本工具的控制权，阻止默认场景拖拽/选择（仅在激活时）
            int controlId = GUIUtility.GetControlID("GridBrushControl".GetHashCode(), FocusType.Passive);
            if (e.type == EventType.Layout) { HandleUtility.AddDefaultControl(controlId); }

            // 支持 ALT 进行场景旋转：按住 ALT 时不响应画刷
            if (e.alt) return;

            if (!hasHit) return;

            // Update preview (rectangle or normal)
            if (_rectangleMode && _isRectSelecting)
            {
                UpdatePreviewRectangle(_rectStart, snapped);
            }
            else
            {
                UpdatePreview(snapped);
            }
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
                        if (_rectangleMode)
                        {
                            // Start rectangle selection
                            _isRectSelecting = true;
                            _rectStart = snapped;
                            _rectErase = (e.button == 1 || isErase);
                            e.Use();
                        }
                        else
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
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId && (e.button == 0 || e.button == 1))
                    {
                        if (_rectangleMode)
                        {
                            UpdatePreviewRectangle(_rectStart, snapped);
                            e.Use();
                        }
                        else
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
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId && (e.button == 0 || e.button == 1))
                    {
                        if (_rectangleMode && _isRectSelecting)
                        {
                            // Apply rectangle paint/erase
                            var rectCells = GetRectangleCells(_rectStart, snapped);
                            if (_rectErase)
                            {
                                EraseCells(rectCells);
                            }
                            else
                            {
                                PaintCells(rectCells, snapDown);
                            }
                            _isRectSelecting = false;
                            _previewCells.Clear();
                            GUIUtility.hotControl = 0;
                            e.Use();
                        }
                        else
                        {
                            GUIUtility.hotControl = 0;
                            e.Use();
                        }
                    }
                    break;
                case EventType.KeyUp:
                    if (_requireHoldKey && e.keyCode == KeyCode.B && GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
                case EventType.KeyDown:
                    // Plane height hotkeys: [ / ] and , / .
                    if (e.keyCode == KeyCode.LeftBracket || e.keyCode == KeyCode.Comma)
                    {
                        _planeHeight--;
                        e.Use();
                        Repaint();
                    }
                    else if (e.keyCode == KeyCode.RightBracket || e.keyCode == KeyCode.Period)
                    {
                        _planeHeight++;
                        e.Use();
                        Repaint();
                    }
                    // Cycle plane with Tab
                    else if (e.keyCode == KeyCode.Tab)
                    {
                        _editPlane = (EditPlane)(((int)_editPlane + 1) % 3);
                        e.Use();
                        Repaint();
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

        private bool TryGetPlaneHit(Ray ray, out Vector3 hit)
        {
            if (_layout == null)
            {
                hit = Vector3.zero;
                return false;
            }

            float size = _layout.cellSize > 0 ? _layout.cellSize : _cellSize;
            Vector3 planePoint = _layout.origin;
            Vector3 normal;
            switch (_editPlane)
            {
                case EditPlane.XY:
                    normal = Vector3.forward;
                    planePoint += Vector3.forward * (_planeHeight * size);
                    break;
                case EditPlane.YZ:
                    normal = Vector3.right;
                    planePoint += Vector3.right * (_planeHeight * size);
                    break;
                default: // XZ
                    normal = Vector3.up;
                    planePoint += Vector3.up * (_planeHeight * size);
                    break;
            }

            Plane plane = new Plane(normal, planePoint);
            if (plane.Raycast(ray, out float enter))
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
            if (_paintVerticalLine)
            {
                int h = Mathf.Max(1, _verticalLineHeight);
                for (int i = 0; i < h; i++)
                {
                    _previewCells.Add(center + new Vector3Int(0, i, 0));
                }
            }
            else
            {
                int r = Mathf.Max(0, _brushSize - 1);
                for (int x = -r; x <= r; x++)
                for (int y = -r; y <= r; y++)
                for (int z = -r; z <= r; z++)
                {
                    _previewCells.Add(center + new Vector3Int(x, y, z));
                }
            }
        }

        private void UpdatePreviewRectangle(Vector3Int a, Vector3Int b)
        {
            _previewCells.Clear();
            foreach (var c in GetRectangleCells(a, b))
            {
                _previewCells.Add(c);
            }
        }

        private IEnumerable<Vector3Int> GetRectangleCells(Vector3Int a, Vector3Int b)
        {
            // Based on edit plane, sweep inclusive ranges across two axes; keep orthogonal axis fixed
            switch (_editPlane)
            {
                case EditPlane.XY:
                {
                    int minX = Mathf.Min(a.x, b.x);
                    int maxX = Mathf.Max(a.x, b.x);
                    int minY = Mathf.Min(a.y, b.y);
                    int maxY = Mathf.Max(a.y, b.y);
                    int z = a.z; // on the same Z plane
                    for (int x = minX; x <= maxX; x++)
                    for (int y = minY; y <= maxY; y++)
                    {
                        if (_rectangleHollow && x > minX && x < maxX && y > minY && y < maxY) continue;
                        yield return new Vector3Int(x, y, z);
                    }
                    break;
                }
                case EditPlane.YZ:
                {
                    int minY = Mathf.Min(a.y, b.y);
                    int maxY = Mathf.Max(a.y, b.y);
                    int minZ = Mathf.Min(a.z, b.z);
                    int maxZ = Mathf.Max(a.z, b.z);
                    int x = a.x; // on the same X plane
                    for (int y = minY; y <= maxY; y++)
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        if (_rectangleHollow && y > minY && y < maxY && z > minZ && z < maxZ) continue;
                        yield return new Vector3Int(x, y, z);
                    }
                    break;
                }
                default: // XZ
                {
                    int minX = Mathf.Min(a.x, b.x);
                    int maxX = Mathf.Max(a.x, b.x);
                    int minZ = Mathf.Min(a.z, b.z);
                    int maxZ = Mathf.Max(a.z, b.z);
                    int y = a.y; // on the same Y plane
                    for (int x = minX; x <= maxX; x++)
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        if (_rectangleHollow && x > minX && x < maxX && z > minZ && z < maxZ) continue;
                        yield return new Vector3Int(x, y, z);
                    }
                    break;
                }
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

		private void UpdateHover(Vector3Int coord)
		{
			_hasHover = true;
			_hoverCoord = coord;
			int idx = _layout.cells.FindIndex(x => x.coord == coord);
			_hasHoverCell = idx >= 0;
			if (_hasHoverCell) _hoverCell = _layout.cells[idx];
			Repaint();
		}

		private void CopyHoverInfo()
		{
			if (!_hasHover) return;
			string text;
			if (_hasHoverCell)
			{
				var c = _hoverCell.color.a > 0.0001f ? (Color)_hoverCell.color : new Color(0.6f, 0.6f, 0.6f, 1f);
				string hex = ColorUtility.ToHtmlStringRGBA(c);
				text = $"coord: ({_hoverCoord.x},{_hoverCoord.y},{_hoverCoord.z})\n" +
				       $"typeId: {_hoverCell.typeId}\n" +
				       $"color: #{hex}\n" +
				       $"colorRGBA: ({c.r:F3},{c.g:F3},{c.b:F3},{c.a:F3})";
			}
			else
			{
				text = $"coord: ({_hoverCoord.x},{_hoverCoord.y},{_hoverCoord.z})\nempty: true";
			}
			EditorGUIUtility.systemCopyBuffer = text;
			ShowNotification(new GUIContent("已复制悬停格信息"));
		}

		private void ConfirmAndClearLayout()
		{
			string title = "确认清空布局?";
			string message = "此操作会删除当前布局中的所有方块数据。是否先创建备份?";
			int option = EditorUtility.DisplayDialogComplex(title, message, "备份后清空", "取消", "直接清空");
			if (option == 1) // 取消
				return;

			if (option == 0)
			{
				CreateBackupAsset();
			}

			Undo.RecordObject(_layout, "Clear Cube Layout");
			_layout.cells.Clear();
			EditorUtility.SetDirty(_layout);
		}

		private void CreateBackupAsset()
		{
			if (_layout == null) return;
			string originalPath = AssetDatabase.GetAssetPath(_layout);
			if (string.IsNullOrEmpty(originalPath))
			{
				// 对于未保存到资源的临时对象，提示用户保存
				EditorUtility.DisplayDialog("无法备份", "布局尚未保存为资源文件，请先通过 'Create Layout Asset' 创建资源。", "好的");
				return;
			}

			string dir = Path.GetDirectoryName(originalPath).Replace("\\", "/");
			string file = Path.GetFileNameWithoutExtension(originalPath);
			string ext = Path.GetExtension(originalPath);
			string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string backupName = $"{file}_Backup_{time}{ext}";
			string backupPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(dir, backupName));

			// 深拷贝 ScriptableObject 实例
			var copy = ScriptableObject.CreateInstance<CubeLayout>();
			copy.cellSize = _layout.cellSize;
			copy.origin = _layout.origin;
			copy.cells = new List<CubeLayout.Cell>(_layout.cells);

			AssetDatabase.CreateAsset(copy, backupPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.DisplayDialog("已创建备份", $"备份文件已保存:\n{backupPath}", "好的");
		}

		private void DrawColorHistoryGUI()
		{
			if (_colorHistory == null || _colorHistory.Count == 0)
				return;

			EditorGUILayout.LabelField("Recent Colors", EditorStyles.boldLabel);
			using (new EditorGUILayout.HorizontalScope())
			{
				int count = Mathf.Min(_colorHistory.Count, MaxColorHistory);
				for (int i = 0; i < count; i++)
				{
					var c = _colorHistory[i];
					Rect r = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
					EditorGUI.DrawRect(r, c);
					if (GUI.Button(r, GUIContent.none, GUIStyle.none))
					{
						_color = c;
						Repaint();
					}
				}
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Clear", GUILayout.Width(60)))
				{
					_colorHistory.Clear();
					SaveColorHistory();
				}
			}
		}

		private void AddColorToHistory(Color c)
		{
			// 去重（近似比较），将新颜色移到最前
			for (int i = _colorHistory.Count - 1; i >= 0; i--)
			{
				if (ColorsApproximatelyEqual(_colorHistory[i], c))
				{
					_colorHistory.RemoveAt(i);
				}
			}
			_colorHistory.Insert(0, c);
			if (_colorHistory.Count > MaxColorHistory)
			{
				_colorHistory.RemoveRange(MaxColorHistory, _colorHistory.Count - MaxColorHistory);
			}
		}

		private static bool ColorsApproximatelyEqual(Color a, Color b)
		{
			const float eps = 1f / 255f;
			return Mathf.Abs(a.r - b.r) < eps &&
				   Mathf.Abs(a.g - b.g) < eps &&
				   Mathf.Abs(a.b - b.b) < eps &&
				   Mathf.Abs(a.a - b.a) < eps;
		}

		private void SaveColorHistory()
		{
			if (_colorHistory == null) return;
			var parts = new List<string>(_colorHistory.Count);
			for (int i = 0; i < _colorHistory.Count && i < MaxColorHistory; i++)
			{
				parts.Add(ColorUtility.ToHtmlStringRGBA(_colorHistory[i]));
			}
			string data = string.Join(",", parts.ToArray());
			EditorPrefs.SetString(ColorHistoryPrefsKey, data);
		}

		private void LoadColorHistory()
		{
			_colorHistory.Clear();
			string data = EditorPrefs.GetString(ColorHistoryPrefsKey, string.Empty);
			if (string.IsNullOrEmpty(data)) return;
			var parts = data.Split(',');
			for (int i = 0; i < parts.Length && i < MaxColorHistory; i++)
			{
				var hex = parts[i];
				if (string.IsNullOrEmpty(hex)) continue;
				if (!hex.StartsWith("#")) hex = "#" + hex;
				if (ColorUtility.TryParseHtmlString(hex, out var c))
				{
					_colorHistory.Add(c);
				}
			}
		}
    }
}


