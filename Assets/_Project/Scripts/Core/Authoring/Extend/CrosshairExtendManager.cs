using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using System.Collections.Generic;
using Project.Core.Components;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 准星驱动的所见即所得“渐进拉伸”管理器
    /// - 锁定光标，使用屏幕中心射线
    /// - 轴向吸附（6方向）
    /// - 每帧根据目标长度与已生成长度差值增/减
    /// </summary>
    public class CrosshairExtendManager : MonoBehaviour
    {
        [Header("引用")]
        public Camera mainCamera;

        [Header("参数")]
        [Tooltip("每帧最多生成的方块数（防止一次性爆量）")]
        public int maxBlocksPerFrame = 4;
        [Tooltip("最大拉伸长度（安全上限）")]
        public int maxExtendLength = 16;
        [Tooltip("准星检测最大距离（物理射线）")]
        public float raycastDistance = 100f;
        [Tooltip("可交互层（命中代理或关卡几何体均可）")]
        public LayerMask rayLayerMask = ~0;

        [Header("调试")]
        public bool showDebug = true;
        [Tooltip("在末端绘制一个预览方块（线框）")]
        public bool drawGhostAtEnd = true;
        [Tooltip("绘制基线与视线调试线（黄色=视线，绿色=轴线，洋红=基线点，青色=当前最近点）")]
        public bool drawBaselineAndRay = true;
        [Header("方向选择")]
        [Tooltip("优先使用命中面的法线来确定轴向（推荐）")]
        public bool useHitNormal = true;
        [Tooltip("法线吸附到主轴的最大夹角（度）")]
        [Range(1f, 89f)] public float axisSnapAngleDeg = 60f;
        [Tooltip("在未生成前支持滚轮切换轴向（6方向轮换）")]
        public bool allowScrollAxisCycle = true;
        [Tooltip("在未生成前优先使用鼠标拖拽方向来选择轴向（基于鼠标 X/Y 累积）")]
        public bool preferDragDirection = true;
        [Range(1f, 50f)] public float dragPickThreshold = 6f; // 超过阈值才以拖拽定向

        private EntityManager _entityManager;
        private Entity _selected = Entity.Null;
        private int3 _axisDir = int3.zero;
        private int _spawnedLength = 0;   // 已生成长度
        private int _activeChainId = -1;  // 当前会话链ID
        private int _lastComputedLen = 0; // 最近一次计算出的目标长度（调试）
        private CubeSelectionManager _selectionManager;
        private Entity _lastSelected = Entity.Null;
        private int _selectedAxisIndex = -1; // 0..5, 在未生成前可滚轮切换
        private Vector2 _dragAccum = Vector2.zero;
        private bool _axisPickedByDrag = false;
        private int _currentPreviewLen = 0;
        // 所见即所得的基线：确定/变更方向的那一刻记录投影，之后长度=当前投影-基线投影
        private bool _baseProjSet = false;
        private float _baseProj = 0f;
        private int3 _baseAxis = int3.zero;
        private bool _isDragging = false;
        private bool _suppressPreviewOneFrame = false;

        void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (World.DefaultGameObjectInjectionWorld != null)
                _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _selectionManager = FindObjectOfType<CubeSelectionManager>();
        }

        void Update()
        {
            if (_entityManager == null) return;
            if (mainCamera == null)
            {
                mainCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
                if (mainCamera == null) return; // 无相机无法工作
            }

            UpdateSelected();
            if (_selected == Entity.Null) { ResetSession(endKeep: _spawnedLength); return; }

            // 左键按下：捕获基线（从0开始增长）并确定当前轴
            if (Input.GetMouseButtonDown(0))
            {
                EnsureSession();
                // 先确定一个当前轴向（优先：拖拽/滚轮/命中法线/视线）
                int dummyLen;
                var axisNow = CalcAxisAndTargetLength(out dummyLen); // 仅取轴
                if (math.all(axisNow == int3.zero)) axisNow = AbsorbToAxis((float3)mainCamera.transform.forward);
                _axisDir = axisNow;

                // 记录基线：使用“射线-轴线”最近点参数（所见即所得从0开始）
                var ray0 = mainCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
                // 让轴方向与当前视线大致同向，避免出现“反向拉才增长”
                float nd0 = math.dot((float3)_axisDir, (float3)ray0.direction);
                if (nd0 < 0f) _axisDir = -_axisDir;
                var rootT0 = _entityManager.GetComponentData<LocalTransform>(_selected);
                float3 rootPos0 = rootT0.Position;
                float3 axisUnit0 = math.normalize((float3)_axisDir);
                _baseProj = ComputeAxisParamFromRay(ray0, rootPos0, axisUnit0);
                _baseAxis = _axisDir;
                _baseProjSet = true;
                _currentPreviewLen = 0;
                _isDragging = true;
                _suppressPreviewOneFrame = true; // 避免按下当帧就拉满
            }

            // 左键按住：仅预览，不生成实体
            if (Input.GetMouseButton(0))
            {
                EnsureSession();

                // 计算目标长度：对三个轴（±X,±Y,±Z）采样，选最近的交汇点；无需事先定方向
                var ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
                var rootT = _entityManager.GetComponentData<LocalTransform>(_selected);
                float3 rootPos = rootT.Position;
                int3 bestAxis;
                int targetLen = ComputeBestAxisAndLengthBySampling3(ray, rootPos, maxExtendLength, raycastDistance, 0.5f, out bestAxis);
                _axisDir = bestAxis;
                _lastComputedLen = targetLen;

                // 第一次按下后的首帧不预览，防止瞬间拉到远端
                if (_suppressPreviewOneFrame)
                {
                    _suppressPreviewOneFrame = false;
                    _currentPreviewLen = 0;
                }

                // 拖拽时：整段蓝线框预览（所见即所得）
                if (targetLen > 0)
                {
                    var rootT2 = _entityManager.GetComponentData<LocalTransform>(_selected);
                    int3 startPos = (int3)math.round(rootT2.Position);
                    int clampedTarget = math.clamp(targetLen, 0, maxExtendLength);
                    _currentPreviewLen = ValidateTargetLength(startPos, _axisDir, clampedTarget);
                    float3 startPosF = rootT2.Position;
                    DrawPreviewChain(startPosF, _axisDir, _currentPreviewLen, new Color(0.2f, 0.6f, 1f, 0.8f));
                }
            }
            // 左键松开：结束
            if (Input.GetMouseButtonUp(0))
            {
                // 规则：若视线仍与选中方块相交，或没有有效预览，则不生成
                bool hitSelected = false;
                var rayEnd = mainCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
                if (Physics.Raycast(rayEnd, out var hitEnd, raycastDistance, rayLayerMask))
                {
                    var proxy = hitEnd.collider.GetComponent<Project.Core.Authoring.InteractableProxy>();
                    if (proxy != null && proxy.linkedEntity == _selected)
                        hitSelected = true;
                }

                if (!hitSelected && _currentPreviewLen > 0)
                {
                    // 确认一次性生成（使用预览的有效长度）
                    ConfirmExtend(_currentPreviewLen);
                }
                ResetSession(endKeep: 0);
                // 完成一次拉伸后自动退出选中
                if (_selectionManager != null)
                {
                    _selectionManager.DeselectAll();
                }
                // 重置拖拽状态
                _dragAccum = Vector2.zero;
                _axisPickedByDrag = false;
                _currentPreviewLen = 0;
                _baseProjSet = false;
                _isDragging = false;
            }

            // 右键：若有预览则取消；否则对选中根撤销所有拉伸链
            if (Input.GetMouseButtonDown(1))
            {
                if (_currentPreviewLen > 0 || _isDragging)
                {
                    // 仅取消预览，不取消选中
                    _currentPreviewLen = 0;
                    _dragAccum = Vector2.zero;
                    _axisPickedByDrag = false;
                    _baseProjSet = false;
                    _isDragging = false;
                }
                else if (_selected != Entity.Null && _entityManager != null && _entityManager.Exists(_selected))
                {
                    RetractAllChainsOfSelectedRoot();
                }
            }

            // 调试：绘制基线与视线
            if (showDebug && drawBaselineAndRay)
            {
                DrawAxisAndRayDebug();
            }
        }

        private void UpdateSelected()
        {
            var query = _entityManager.CreateEntityQuery(typeof(SelectionState), typeof(InteractableCubeTag), typeof(ExtendableTag));
            _selected = Entity.Null;
            foreach (var e in query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var s = _entityManager.GetComponentData<SelectionState>(e);
                if (s.IsSelected == 1) { _selected = e; break; }
            }
            query.Dispose();

            // 新选中目标：重置方向与会话，允许每次选中都重新选择方向
            if (_selected != _lastSelected)
            {
                _axisDir = int3.zero;
                _spawnedLength = 0;
                _activeChainId = -1;
                _lastSelected = _selected;
                _dragAccum = Vector2.zero;
                _axisPickedByDrag = false;
                _baseProjSet = false;
                _baseProj = 0f;
                _baseAxis = int3.zero;
            }
        }

        private void EnsureSession()
        {
            if (_activeChainId < 0)
            {
                _activeChainId = (int)(Time.time * 1000f);
                _spawnedLength = 0;
            }
        }

        private void ResetSession(int endKeep)
        {
            _activeChainId = -1;
            _spawnedLength = endKeep;
        }

        private int3 CalcAxisAndTargetLength(out int targetLen)
        {
            targetLen = 0;
            if (_selected == Entity.Null || mainCamera == null) return int3.zero;

            // 中心射线
            var ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
            bool hasHit = Physics.Raycast(ray, out var hit, raycastDistance, rayLayerMask);
            if (!hasHit)
            {
                if (showDebug) Debug.DrawRay(ray.origin, ray.direction * 3f, Color.red);
                // 无命中：使用轴向与视线远端点的“面穿越”估算
                int3 noHitAxis;
                if (math.any(_axisDir != int3.zero)) noHitAxis = _axisDir; 
                else if (allowScrollAxisCycle && _selectedAxisIndex >= 0) noHitAxis = IndexToAxis(_selectedAxisIndex);
                else noHitAxis = AbsorbToAxis(ray.direction.normalized);

                var rootTnh = _entityManager.GetComponentData<LocalTransform>(_selected);
                float3 rootPosF = rootTnh.Position;
                float nd = math.dot((float3)noHitAxis, (float3)ray.direction);
                int lenAim = 0;
                if (nd > 1e-4f)
                {
                    float3 far = (float3)(ray.origin + ray.direction * raycastDistance);
                    float s = math.dot(far - rootPosF, (float3)noHitAxis); // 沿轴距离（单位：格）
                    lenAim = (int)math.floor(math.max(0f, s) + 0.5f);      // 跨越面(0.5,1.5,...) → 长度(1,2,...)
                }
                targetLen = math.clamp(lenAim, 0, maxExtendLength);
                return noHitAxis;
            }

            // 根位置
            var rootT = _entityManager.GetComponentData<LocalTransform>(_selected);
            var rootPos = (int3)math.round(rootT.Position);

            // 吸附主轴（若已锁定方向，则沿锁定轴计算）
            int3 axis;
            if (math.any(_axisDir != int3.zero))
            {
                axis = _axisDir;
            }
            else
            {
                if (preferDragDirection && _axisPickedByDrag && _spawnedLength == 0)
                {
                    axis = _axisDir; // 已由拖拽确定
                }
                else if (allowScrollAxisCycle && _selectedAxisIndex >= 0)
                {
                    axis = IndexToAxis(_selectedAxisIndex);
                }
                else if (useHitNormal)
                {
                    axis = NormalToAxis(hit.normal, axisSnapAngleDeg);
                    if (math.all(axis == int3.zero))
                    {
                        var dir = ray.direction.normalized;
                        axis = AbsorbToAxis(dir);
                    }
                }
                else
                {
                    var dir = ray.direction.normalized;
                    axis = AbsorbToAxis(dir);
                }
            }

            // 估算目标长度（沿轴投影）
            float3 delta = (float3)hit.point - (float3)rootPos;
            float projHit = math.dot(delta, (float3)axis);

            // 用“轴向投影 + 面跨越取整”直接得到长度
            int lenHit = (int)math.floor(math.max(0f, projHit) + 0.5f);
            targetLen = math.clamp(lenHit, 0, maxExtendLength);
            return axis;
        }

        private static int3 AbsorbToAxis(float3 v)
        {
            float ax = math.abs(v.x), ay = math.abs(v.y), az = math.abs(v.z);
            if (ax >= ay && ax >= az) return new int3(v.x >= 0 ? 1 : -1, 0, 0);
            if (ay >= ax && ay >= az) return new int3(0, v.y >= 0 ? 1 : -1, 0);
            return new int3(0, 0, v.z >= 0 ? 1 : -1);
        }

        private static int3 NormalToAxis(Vector3 n, float maxAngleDeg)
        {
            if (n.sqrMagnitude < 1e-6f) return int3.zero;
            n.Normalize();
            Vector3[] cand = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            int3[] axes = { new int3(1,0,0), new int3(-1,0,0), new int3(0,1,0), new int3(0,-1,0), new int3(0,0,1), new int3(0,0,-1) };
            float bestDot = -1f; int bestIdx = -1;
            for (int i = 0; i < 6; i++)
            {
                float d = Vector3.Dot(n, cand[i]);
                if (d > bestDot) { bestDot = d; bestIdx = i; }
            }
            if (bestIdx < 0) return int3.zero;
            float angle = Mathf.Acos(Mathf.Clamp(bestDot, -1f, 1f)) * Mathf.Rad2Deg;
            if (angle <= maxAngleDeg) return axes[bestIdx];
            return int3.zero;
        }

        // 计算“从根沿轴的有向距离参数 s”，使用“射线-轴线”最近点公式
        // 轴线：P(s) = rootPos + axisUnit * s
        // 射线：R(t) = ray.origin + ray.dir * t, t>=0
        // 求最小化 |P(s) - R(t)|^2 的 s
        private static float ComputeAxisParamFromRay(Ray ray, float3 rootPos, float3 axisUnit)
        {
            float3 w0 = (float3)ray.origin - rootPos;
            float a = math.dot(axisUnit, axisUnit);          // =1（axisUnit 已单位化）
            float b = math.dot(axisUnit, (float3)ray.direction);
            float c = math.dot((float3)ray.direction, (float3)ray.direction);
            float d = math.dot(axisUnit, w0);
            float e = math.dot((float3)ray.direction, w0);
            float denom = a * c - b * b;
            if (denom < 1e-6f)
            {
                // 退化：射线平行于轴，使用射线原点到轴的投影参数
                return d;
            }
            float s = (b * e - c * d) / denom;
            return s;
        }

        // 采样版本的目标长度计算：
        // 从基线 s0 开始，以 0.5 为步长在 [0, maxLen+0.5] 采样，
        // 对于每个 s = s0 + k*0.5，取射线上最近点 Q(t)（t∈[0,raycastDistance]），
        // 选择距离 |P(s)-Q| 最小的 k，对应目标长度 round(k)。
        private static int ComputeTargetLenBySampling(Ray ray, float3 rootPos, float3 axisUnit, float s0, int maxLen, float maxRayDist)
        {
            // 规范化射线方向
            Vector3 d3 = ray.direction.normalized;
            float3 d = new float3(d3.x, d3.y, d3.z);
            float bestDist = float.MaxValue;
            float bestK = 0f;
            float step = 0.5f;
            int steps = (int)math.ceil(maxLen + 0.5f) * 2; // 覆盖到 maxLen+0.5

            for (int i = 0; i <= steps; i++)
            {
                float k = i * step;          // 相对基线的步长
                float s = s0 + k;            // 轴参数
                float3 P = rootPos + axisUnit * s;

                // 射线到点 P 的最近 t（投影到射线）
                float t = math.dot(P - (float3)ray.origin, d);
                t = math.clamp(t, 0f, maxRayDist);
                float3 Q = (float3)ray.origin + d * t;

                float dist = math.length(Q - P);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestK = k;
                }
            }

            int len = (int)math.round(bestK); // 将 0.5 步长的 k 映射为格数
            len = math.clamp(len, 0, maxLen);
            return len;
        }

        // 三轴（±X, ±Y, ±Z）采样：无需预先确定方向，选择最接近视线的那条轴上的采样点
        private static int ComputeBestAxisAndLengthBySampling3(Ray ray, float3 rootPos, int maxLen, float maxRayDist, float step, out int3 bestAxis)
        {
            Vector3 d3 = ray.direction.normalized;
            float3 d = new float3(d3.x, d3.y, d3.z);
            int3[] axes = new int3[]
            {
                new int3(1,0,0), new int3(-1,0,0),
                new int3(0,1,0), new int3(0,-1,0),
                new int3(0,0,1), new int3(0,0,-1)
            };

            float bestDist = float.MaxValue;
            float bestK = 0f;
            int3 bestA = new int3(0,0,0);
            int steps = (int)math.ceil((maxLen + 0.5f) / step);

            for (int ai = 0; ai < axes.Length; ai++)
            {
                float3 axisUnit = math.normalize((float3)axes[ai]);
                for (int i = 1; i <= steps; i++)
                {
                    float k = i * step; // 从 step 开始（默认 0.5）
                    float3 P = rootPos + axisUnit * k;
                    float t = math.dot(P - (float3)ray.origin, d);
                    t = math.clamp(t, 0f, maxRayDist);
                    float3 Q = (float3)ray.origin + d * t;
                    float dist = math.length(Q - P);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestK = k;
                        bestA = axes[ai];
                    }
                }
            }

            bestAxis = bestA;
            int len = (int)math.round(bestK);
            len = math.clamp(len, 0, maxLen);
            return len;
        }

        private static int3 IndexToAxis(int idx)
        {
            switch (idx % 6)
            {
                case 0: return new int3(1,0,0);
                case 1: return new int3(-1,0,0);
                case 2: return new int3(0,1,0);
                case 3: return new int3(0,-1,0);
                case 4: return new int3(0,0,1);
                default: return new int3(0,0,-1);
            }
        }

        private void HandleAxisCycle()
        {
            if (!allowScrollAxisCycle || _spawnedLength > 0) return;
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 1e-4f)
            {
                if (_selectedAxisIndex < 0) _selectedAxisIndex = 0;
                _selectedAxisIndex = (scroll > 0f) ? (_selectedAxisIndex + 1) % 6 : (_selectedAxisIndex + 5) % 6;
            }
        }

        private void HandleDragAxisPick()
        {
            if (!preferDragDirection || _spawnedLength > 0) return;
            // 鼠标移动增量（光标锁定时也有 X/Y 值）
            float dx = Input.GetAxis("Mouse X");
            float dy = Input.GetAxis("Mouse Y");
            _dragAccum += new Vector2(dx, dy);

            if (!_axisPickedByDrag && _dragAccum.magnitude >= dragPickThreshold)
            {
                // 将拖拽增量映射到世界方向：cam.right * dx + cam.up * dy
                Vector3 worldDrag = mainCamera.transform.right * _dragAccum.x + mainCamera.transform.up * _dragAccum.y;
                if (worldDrag.sqrMagnitude > 1e-4f)
                {
                    int3 axis = AbsorbToAxis((float3)worldDrag);
                    _axisDir = axis;
                    _axisPickedByDrag = true;
                    _selectedAxisIndex = -1; // 拖拽选向后，滚轮不再干扰
                }
            }
        }

        void OnGUI()
        {
            if (!showDebug) return;
            GUILayout.BeginArea(new Rect(10, 380, 360, 140));
            GUILayout.Box("Crosshair Extend Debug", GUILayout.Width(360));
            GUILayout.Label($"Selected: {_selected}");
            GUILayout.Label($"Axis: {_axisDir}");
            GUILayout.Label($"SpawnedLength: {_spawnedLength}");
            GUILayout.Label($"Camera: {(mainCamera != null ? mainCamera.name : "null")}");
            // 提示：按住左键进行渐进拉伸
            GUILayout.Label("Hold LMB to grow, hold RMB to shrink");
            GUILayout.EndArea();
        }

        // 绘制基线与视线调试线
        private void DrawAxisAndRayDebug()
        {
            if (mainCamera == null || _selected == Entity.Null) return;

            // 计算当前轴（优先使用基线时的轴，保证与生成/预览一致）
            int3 axis = _baseProjSet ? _baseAxis : (math.any(_axisDir != int3.zero) ? _axisDir : AbsorbToAxis((float3)mainCamera.transform.forward));
            float3 axisUnit = math.normalize((float3)axis);

            // 根位置
            var rootT = _entityManager.GetComponentData<LocalTransform>(_selected);
            float3 rootPos = rootT.Position;

            // 当前准心射线
            Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));

            // 绘制视线（黄色）
            Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.yellow);

            // 绘制轴线（绿色）
            float axisLen = math.max(2f, maxExtendLength + 1f);
            Vector3 axisStart = (Vector3)(rootPos - axisUnit * axisLen);
            Vector3 axisEnd = (Vector3)(rootPos + axisUnit * axisLen);
            Debug.DrawLine(axisStart, axisEnd, Color.green);

            // 基线点（洋红）
            if (_baseProjSet && math.all(axis == _baseAxis))
            {
                float3 p0 = rootPos + axisUnit * _baseProj;
                DrawWireCube(p0, 0.3f, new Color(1f, 0f, 1f, 0.9f));
            }

            // 预览终点（青色）：使用基线 + 预览长度，确保与蓝色链末端一致
            float s0 = (_baseProjSet && math.all(axis == _baseAxis)) ? _baseProj : 0f;
            float endS = s0 + _currentPreviewLen;
            float3 pc = rootPos + axisUnit * endS;
            DrawWireCube(pc, 0.3f, new Color(0f, 1f, 1f, 0.9f));

            // 可选：当前最近点（白色小方块），用于对比（如果需要可开启）
            // float currS = ComputeAxisParamFromRay(ray, rootPos, axisUnit);
            // float3 pcNow = rootPos + axisUnit * currS;
            // DrawWireCube(pcNow, 0.2f, Color.white);
        }

        // 线框方块绘制（与预览系统一致的视觉）
        private void DrawWireCube(float3 center, float size, Color color)
        {
            float half = size * 0.5f;
            Vector3 c = center;
            Vector3[] v = new Vector3[8]
            {
                c + new Vector3(-half,-half,-half),
                c + new Vector3( half,-half,-half),
                c + new Vector3( half,-half, half),
                c + new Vector3(-half,-half, half),
                c + new Vector3(-half, half,-half),
                c + new Vector3( half, half,-half),
                c + new Vector3( half, half, half),
                c + new Vector3(-half, half, half)
            };
            Debug.DrawLine(v[0], v[1], color);
            Debug.DrawLine(v[1], v[2], color);
            Debug.DrawLine(v[2], v[3], color);
            Debug.DrawLine(v[3], v[0], color);
            Debug.DrawLine(v[4], v[5], color);
            Debug.DrawLine(v[5], v[6], color);
            Debug.DrawLine(v[6], v[7], color);
            Debug.DrawLine(v[7], v[4], color);
            Debug.DrawLine(v[0], v[4], color);
            Debug.DrawLine(v[1], v[5], color);
            Debug.DrawLine(v[2], v[6], color);
            Debug.DrawLine(v[3], v[7], color);
        }

        private void DrawPreviewChain(float3 startPos, int3 axis, int length, Color color)
        {
            if (length <= 0) return;
            for (int i = 1; i <= length; i++)
            {
                float3 pos = startPos + (float3)axis * i;
                DrawWireCube(pos, 0.95f, color);
                float3 prev = i == 1 ? startPos : startPos + (float3)axis * (i - 1);
                Debug.DrawLine(prev, pos, color);
            }
        }

        private void ConfirmExtend(int finalLength)
        {
            if (_selected == Entity.Null || finalLength <= 0) return;
            if (_entityManager.HasComponent<ExtendExecutionRequest>(_selected))
                _entityManager.RemoveComponent<ExtendExecutionRequest>(_selected);

            if (_activeChainId < 0) _activeChainId = (int)(Time.time * 1000f);

            _entityManager.AddComponentData(_selected, new ExtendExecutionRequest
            {
                Direction = _axisDir,
                Length = finalLength,
                StartIndex = 0,
                ChainID = _activeChainId
            });
        }

        private int ValidateTargetLength(int3 startPos, int3 axis, int requestedLen)
        {
            int req = math.max(0, requestedLen);
            var q = _entityManager.CreateEntityQuery(typeof(OccupiedCubeMap));
            if (q.CalculateEntityCount() == 0) { q.Dispose(); return req; }
            var cubeMap = q.GetSingleton<OccupiedCubeMap>();
            q.Dispose();
            var map = cubeMap.Map;
            int valid = 0;
            for (int i = 1; i <= req; i++)
            {
                int3 pos = startPos + axis * i;
                if (map.ContainsKey(pos)) break;
                valid = i;
            }
            return valid;
        }

        // 撤销当前选中根实体的所有拉伸链（整链收回）
        private void RetractAllChainsOfSelectedRoot()
        {
            var query = _entityManager.CreateEntityQuery(typeof(ExtendChainData));
            var chains = query.ToComponentDataArray<ExtendChainData>(Unity.Collections.Allocator.Temp);
            var chainIds = new HashSet<int>();
            for (int i = 0; i < chains.Length; i++)
            {
                if (chains[i].RootEntity == _selected)
                {
                    chainIds.Add(chains[i].ChainID);
                }
            }
            chains.Dispose();
            query.Dispose();

            if (chainIds.Count == 0)
            {
                if (showDebug)
                    Debug.LogWarning($"[CrosshairExtend] 未找到任何链可撤销，Root={_selected}");
                return;
            }

            foreach (var chainId in chainIds)
            {
                var req = _entityManager.CreateEntity();
                _entityManager.AddComponentData(req, new Project.Core.Systems.RetractRequest
                {
                    ChainID = chainId,
                    RetractWholeChain = true,
                    TargetLength = 0
                });
                if (showDebug)
                    Debug.Log($"[CrosshairExtend] 提交撤销请求 ChainID={chainId}");
            }

            if (showDebug)
            {
                Debug.Log($"[CrosshairExtend] RetractAll chains of root {_selected} (count={chainIds.Count})");
            }
        }
    }
}


