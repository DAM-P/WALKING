using UnityEngine;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 输入模式协调器（增强版）
    /// 支持键盘拉伸（ExtendInputManager）和鼠标拖拽拉伸（MouseDragExtendManager）
    /// 
    /// 功能：
    /// 1. 接管光标控制，确保光标状态正确
    /// 2. 协调选择和移动模式的切换
    /// 3. 支持跑酷游戏的混合操作模式
    /// 
    /// 模式说明：
    /// - 移动模式（MoveMode）：玩家可以移动，光标锁定
    /// - 拉伸模式（ExtendMode）：
    ///   * 键盘拉伸：禁用移动，光标显示
    ///   * 鼠标拖拽：允许移动，光标显示（跑酷游戏模式）
    /// </summary>
    public class InputModeCoordinator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("选择管理器")]
        public CubeSelectionManager selectionManager;
        
        [Tooltip("键盘拉伸输入管理器（旧系统，可选）")]
        public ExtendInputManager extendInputManager;
        
        [Tooltip("准星渐进拉伸管理器（新方案，推荐）")]
        public CrosshairExtendManager crosshairExtendManager;
        
        [Tooltip("玩家控制器")]
        public FirstPersonController playerController;

        [Header("跑酷模式设置")]
        [Tooltip("跑酷模式：选中cube时允许继续移动（适合鼠标拖拽）")]
        public bool parkourMode = true;
        
        [Tooltip("在跑酷模式下，拉伸时显示光标但不锁定移动")]
        public bool allowMovementWhileExtending = true;

        [Header("Cursor Settings")]
        [Tooltip("在移动模式下锁定光标")]
        public bool lockCursorInMoveMode = true;
        
        [Tooltip("在拉伸模式下显示光标")]
        public bool showCursorInExtendMode = true;

        [Header("Mouse Drag Cursor")]
        [Tooltip("在鼠标拖拽拉伸时隐藏并锁定系统光标（推荐）")]
        public bool hideCursorOnMouseDragExtend = true;

        [Header("Debug")]
        [Tooltip("显示调试日志")]
        public bool showDebugLog = false;

        private InputMode _currentMode = InputMode.MoveMode;
        private InputMode _lastMode = InputMode.MoveMode;
        private ExtendInputType _activeInputType = ExtendInputType.None;

        public enum InputMode
        {
            MoveMode,    // 移动模式：玩家可以移动，光标锁定
            ExtendMode   // 拉伸模式：玩家操作cube（根据输入类型决定是否允许移动）
        }

        public enum ExtendInputType
        {
            None,           // 无拉伸操作
            Keyboard,       // 键盘拉伸（WASD，禁用移动）
            MouseDrag       // 鼠标拖拽（允许移动）
        }

        void Start()
        {
            // 自动查找组件
            if (selectionManager == null)
                selectionManager = FindObjectOfType<CubeSelectionManager>();
            if (extendInputManager == null)
                extendInputManager = FindObjectOfType<ExtendInputManager>();
            if (crosshairExtendManager == null)
                crosshairExtendManager = FindObjectOfType<CrosshairExtendManager>();
            if (playerController == null)
                playerController = FindObjectOfType<FirstPersonController>();

            // 禁用 FirstPersonController 的光标控制
            if (playerController != null)
            {
                playerController.lockCursor = false; // 由协调器接管
            }

            // 检测使用哪种拉伸系统（优先：准星模式）
            if (crosshairExtendManager != null && crosshairExtendManager.enabled)
            {
                _activeInputType = ExtendInputType.MouseDrag; // 复用“鼠标系”类型
                if (showDebugLog)
                    Debug.Log("[InputCoordinator] 使用准星渐进拉伸系统");

                // 禁用其它输入避免冲突
                if (extendInputManager != null && extendInputManager.enabled)
                {
                    extendInputManager.enabled = false;
                    if (showDebugLog) Debug.Log("[InputCoordinator] 已禁用 ExtendInputManager（避免冲突）");
                }
            }
            else if (extendInputManager != null && extendInputManager.enabled)
            {
                _activeInputType = ExtendInputType.Keyboard;
                if (showDebugLog)
                    Debug.Log("[InputCoordinator] 使用键盘拉伸系统");
            }

            // 初始化为移动模式
            SwitchToMoveMode();

            if (showDebugLog)
                Debug.Log($"[InputCoordinator] 初始化完成，跑酷模式：{parkourMode}");
        }

        void Update()
        {
            // 检测当前应该处于哪个模式
            bool shouldBeInExtendMode = selectionManager != null && selectionManager.HasSelection();

            if (shouldBeInExtendMode)
            {
                if (_currentMode != InputMode.ExtendMode)
                {
                    SwitchToExtendMode();
                }
            }
            else
            {
                if (_currentMode != InputMode.MoveMode)
                {
                    SwitchToMoveMode();
                }
            }

            // 强制光标状态（防止其他脚本修改）
            EnforceCursorState();
        }

        /// <summary>
        /// 切换到移动模式
        /// </summary>
        private void SwitchToMoveMode()
        {
            _lastMode = _currentMode;
            _currentMode = InputMode.MoveMode;

            // 启用玩家移动
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // 锁定光标（移动模式）
            if (lockCursorInMoveMode)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (showDebugLog)
                Debug.Log("<color=green>[模式切换] → 移动模式</color>");
        }

        /// <summary>
        /// 切换到拉伸模式
        /// </summary>
        private void SwitchToExtendMode()
        {
            _lastMode = _currentMode;
            _currentMode = InputMode.ExtendMode;

            // 根据输入类型和跑酷模式决定是否禁用移动
            bool shouldDisableMovement = !parkourMode || 
                                        (_activeInputType == ExtendInputType.Keyboard) ||
                                        !allowMovementWhileExtending;

            if (playerController != null)
            {
                playerController.enabled = !shouldDisableMovement;
                
                if (showDebugLog)
                {
                    string movementStatus = shouldDisableMovement ? "禁用" : "保持启用";
                    Debug.Log($"<color=cyan>[模式切换] → 拉伸模式，移动{movementStatus}</color>");
                }
            }

            // 光标策略：
            // - 鼠标拖拽拉伸：可选强制隐藏并锁定（避免选中后鼠标冒出来）
            // - 其它情况：沿用 showCursorInExtendMode 开关
            bool useMouseDrag = _activeInputType == ExtendInputType.MouseDrag;
            if (useMouseDrag && hideCursorOnMouseDragExtend)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                if (showCursorInExtendMode)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        /// <summary>
        /// 强制光标状态（防止被其他脚本修改）
        /// </summary>
        private void EnforceCursorState()
        {
            CursorLockMode desiredLockState;
            bool desiredVisibility;

            if (_currentMode == InputMode.MoveMode)
            {
                desiredLockState = lockCursorInMoveMode ? CursorLockMode.Locked : CursorLockMode.None;
                desiredVisibility = !lockCursorInMoveMode;
            }
            else // ExtendMode
            {
                bool useMouseDrag = _activeInputType == ExtendInputType.MouseDrag;
                if (useMouseDrag && hideCursorOnMouseDragExtend)
                {
                    desiredLockState = CursorLockMode.Locked;
                    desiredVisibility = false;
                }
                else
                {
                    desiredLockState = showCursorInExtendMode ? CursorLockMode.None : CursorLockMode.Locked;
                    desiredVisibility = showCursorInExtendMode;
                }
            }

            // 只在状态不一致时修正
            if (Cursor.lockState != desiredLockState)
            {
                Cursor.lockState = desiredLockState;
                if (showDebugLog)
                    Debug.LogWarning($"[InputCoordinator] 修正光标锁定状态: {desiredLockState}");
            }

            if (Cursor.visible != desiredVisibility)
            {
                Cursor.visible = desiredVisibility;
                if (showDebugLog)
                    Debug.LogWarning($"[InputCoordinator] 修正光标可见性: {desiredVisibility}");
            }
        }

        /// <summary>
        /// 获取当前模式
        /// </summary>
        public InputMode GetCurrentMode() => _currentMode;

        /// <summary>
        /// 手动切换模式（调试用）
        /// </summary>
        [ContextMenu("切换到移动模式")]
        public void ForceMoveMode()
        {
            if (selectionManager != null)
            {
                selectionManager.DeselectAll();
            }
            SwitchToMoveMode();
        }

        [ContextMenu("切换到拉伸模式（需要先选中 Cube）")]
        public void ForceExtendMode()
        {
            if (selectionManager != null && !selectionManager.HasSelection())
            {
                Debug.LogWarning("[InputCoordinator] 无法切换到拉伸模式：未选中任何 Cube");
            }
            else
            {
                SwitchToExtendMode();
            }
        }

        void OnGUI()
        {
            if (!showDebugLog) return;

            GUILayout.BeginArea(new Rect(Screen.width - 310, 480, 300, 160));
            GUILayout.Box("输入模式协调器（增强版）", GUILayout.Width(300));

            string modeColor = _currentMode == InputMode.MoveMode ? "green" : "cyan";
            string modeName = _currentMode == InputMode.MoveMode ? "移动模式" : "拉伸模式";
            GUILayout.Label($"<b>当前模式: <color={modeColor}>{modeName}</color></b>", 
                new GUIStyle(GUI.skin.label) { richText = true });

            string inputTypeText = _activeInputType switch
            {
                ExtendInputType.Keyboard => "键盘拉伸",
                ExtendInputType.MouseDrag => "鼠标拖拽",
                _ => "无"
            };
            GUILayout.Label($"输入类型: {inputTypeText}");
            GUILayout.Label($"跑酷模式: {(parkourMode ? "开启" : "关闭")}");

            GUILayout.Label($"光标锁定: {Cursor.lockState}");
            GUILayout.Label($"光标可见: {Cursor.visible}");
            GUILayout.Label($"玩家移动: {(playerController != null && playerController.enabled ? "启用" : "禁用")}");

            GUILayout.Label("---");

            if (GUILayout.Button("强制移动模式"))
            {
                ForceMoveMode();
            }

            GUILayout.EndArea();
        }
    }
}

