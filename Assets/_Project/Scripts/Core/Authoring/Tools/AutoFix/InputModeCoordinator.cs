using UnityEngine;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 输入模式协调器
    /// 修复"有时候无法重新选择 cube"的问题
    /// 
    /// 问题原因：
    /// 1. FirstPersonController.OnDisable() 会解锁光标
    /// 2. 光标解锁后，鼠标点击可能无法正确触发射线检测
    /// 3. 模式切换时缺少协调，导致输入冲突
    /// 
    /// 解决方案：
    /// 1. 接管光标控制，不让 FirstPersonController 直接控制
    /// 2. 协调选择和移动模式的切换
    /// 3. 确保光标状态始终正确
    /// </summary>
    public class InputModeCoordinator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("选择管理器")]
        public CubeSelectionManager selectionManager;
        
        [Tooltip("拉伸输入管理器")]
        public ExtendInputManager extendInputManager;
        
        [Tooltip("玩家控制器")]
        public FirstPersonController playerController;

        [Header("Cursor Settings")]
        [Tooltip("在移动模式下锁定光标")]
        public bool lockCursorInMoveMode = true;
        
        [Tooltip("在拉伸模式下显示光标")]
        public bool showCursorInExtendMode = true;

        [Header("Debug")]
        [Tooltip("显示调试日志")]
        public bool showDebugLog = false;

        private InputMode _currentMode = InputMode.MoveMode;
        private InputMode _lastMode = InputMode.MoveMode;

        public enum InputMode
        {
            MoveMode,    // 移动模式：玩家可以移动，不能选择 Cube
            ExtendMode   // 拉伸模式：玩家不能移动，可以选择和拉伸 Cube
        }

        void Start()
        {
            // 自动查找组件
            if (selectionManager == null)
                selectionManager = FindObjectOfType<CubeSelectionManager>();
            if (extendInputManager == null)
                extendInputManager = FindObjectOfType<ExtendInputManager>();
            if (playerController == null)
                playerController = FindObjectOfType<FirstPersonController>();

            // 禁用 FirstPersonController 的光标控制
            if (playerController != null)
            {
                playerController.lockCursor = false; // 由协调器接管
            }

            // 初始化为移动模式
            SwitchToMoveMode();

            if (showDebugLog)
                Debug.Log("[InputCoordinator] 初始化完成，当前模式：移动模式");
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

            // 禁用玩家移动
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            // 显示光标（拉伸模式，方便点击）
            if (showCursorInExtendMode)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // 依然锁定光标，但可以接收点击
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (showDebugLog)
                Debug.Log("<color=cyan>[模式切换] → 拉伸模式</color>");
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
                desiredLockState = showCursorInExtendMode ? CursorLockMode.None : CursorLockMode.Locked;
                desiredVisibility = showCursorInExtendMode;
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

            GUILayout.BeginArea(new Rect(Screen.width - 310, 480, 300, 120));
            GUILayout.Box("输入模式协调器", GUILayout.Width(300));

            string modeColor = _currentMode == InputMode.MoveMode ? "green" : "cyan";
            string modeName = _currentMode == InputMode.MoveMode ? "移动模式" : "拉伸模式";
            GUILayout.Label($"<b>当前模式: <color={modeColor}>{modeName}</color></b>", 
                new GUIStyle(GUI.skin.label) { richText = true });

            GUILayout.Label($"光标锁定: {Cursor.lockState}");
            GUILayout.Label($"光标可见: {Cursor.visible}");

            GUILayout.Label("---");

            if (GUILayout.Button("强制移动模式"))
            {
                ForceMoveMode();
            }

            GUILayout.EndArea();
        }
    }
}

