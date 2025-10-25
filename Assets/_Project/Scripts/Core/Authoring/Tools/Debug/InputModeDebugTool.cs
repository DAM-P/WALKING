using UnityEngine;
using Unity.Entities;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 输入模式诊断工具
    /// 诊断"有时候无法重新选择 cube"的问题
    /// </summary>
    public class InputModeDebugTool : MonoBehaviour
    {
        [Header("References")]
        public CubeSelectionManager selectionManager;
        public ExtendInputManager extendInputManager;
        public FirstPersonController playerController;

        [Header("Debug Settings")]
        public bool enableDetailedLog = true;
        public bool showGUI = true;

        private bool _lastPlayerControllerState = true;
        private int _clickAttempts = 0;
        private int _successfulSelections = 0;
        private float _lastClickTime = 0f;

        void Start()
        {
            // 自动查找组件
            if (selectionManager == null)
                selectionManager = FindObjectOfType<CubeSelectionManager>();
            if (extendInputManager == null)
                extendInputManager = FindObjectOfType<ExtendInputManager>();
            if (playerController == null)
                playerController = FindObjectOfType<FirstPersonController>();

            Debug.Log("<color=cyan>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
            Debug.Log("<color=cyan>[输入模式诊断] 工具已启动</color>");
            Debug.Log($"  - CubeSelectionManager: {(selectionManager != null ? "✅" : "❌")}");
            Debug.Log($"  - ExtendInputManager: {(extendInputManager != null ? "✅" : "❌")}");
            Debug.Log($"  - FirstPersonController: {(playerController != null ? "✅" : "❌")}");
            Debug.Log("<color=cyan>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
        }

        void Update()
        {
            if (!enableDetailedLog) return;

            // 监控 PlayerController 状态变化
            if (playerController != null)
            {
                bool currentState = playerController.enabled;
                if (currentState != _lastPlayerControllerState)
                {
                    Debug.Log($"<color=yellow>[模式切换]</color> PlayerController.enabled: {_lastPlayerControllerState} → {currentState}");
                    Debug.Log($"  光标状态: Locked={Cursor.lockState == CursorLockMode.Locked}, Visible={Cursor.visible}");
                    _lastPlayerControllerState = currentState;
                }
            }

            // 监控鼠标左键点击
            if (Input.GetMouseButtonDown(0))
            {
                _clickAttempts++;
                _lastClickTime = Time.time;

                Debug.Log("<color=magenta>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.Log($"<color=magenta>[点击诊断] 第 {_clickAttempts} 次点击尝试</color>");
                
                // 检查各个系统状态
                Debug.Log("<color=yellow>系统状态检查：</color>");
                
                if (selectionManager != null)
                {
                    Debug.Log($"  - CubeSelectionManager.enabled: {selectionManager.enabled}");
                    Debug.Log($"  - 当前选择: {selectionManager.GetCurrentSelection()}");
                }
                else
                {
                    Debug.LogError("  - CubeSelectionManager: ❌ 不存在");
                }

                if (extendInputManager != null)
                {
                    Debug.Log($"  - ExtendInputManager.enabled: {extendInputManager.enabled}");
                }
                else
                {
                    Debug.LogWarning("  - ExtendInputManager: ⚠️ 不存在");
                }

                if (playerController != null)
                {
                    Debug.Log($"  - PlayerController.enabled: {playerController.enabled}");
                }
                else
                {
                    Debug.LogWarning("  - PlayerController: ⚠️ 不存在");
                }

                // 检查输入状态
                Debug.Log("<color=yellow>输入状态检查：</color>");
                Debug.Log($"  - 鼠标位置: {Input.mousePosition}");
                Debug.Log($"  - 光标锁定: {Cursor.lockState}");
                Debug.Log($"  - 光标可见: {Cursor.visible}");
                Debug.Log($"  - WASD 按键: W={Input.GetKey(KeyCode.W)}, A={Input.GetKey(KeyCode.A)}, S={Input.GetKey(KeyCode.S)}, D={Input.GetKey(KeyCode.D)}");

                // 检查相机
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    Debug.Log($"  - 主相机: ✅ {mainCamera.name}");
                    Debug.Log($"  - 相机位置: {mainCamera.transform.position}");
                }
                else
                {
                    Debug.LogError("  - 主相机: ❌ 不存在");
                }

                Debug.Log("<color=magenta>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");

                // 延迟检查是否选择成功
                StartCoroutine(CheckSelectionResult());
            }

            // 监控 ESC 键
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("<color=cyan>[输入] ESC 键被按下 - 取消选择</color>");
                Debug.Log($"  - PlayerController 当前状态: {(playerController != null ? playerController.enabled.ToString() : "null")}");
            }
        }

        System.Collections.IEnumerator CheckSelectionResult()
        {
            yield return new WaitForSeconds(0.1f); // 等待 0.1 秒

            if (selectionManager != null && selectionManager.HasSelection())
            {
                _successfulSelections++;
                Debug.Log($"<color=green>[选择成功] ✅ 第 {_successfulSelections} 次成功选择</color>");
            }
            else
            {
                Debug.LogWarning($"<color=red>[选择失败] ❌ 点击后未选中任何 Cube</color>");
                Debug.LogWarning($"  成功率: {_successfulSelections}/{_clickAttempts} = {(_clickAttempts > 0 ? (_successfulSelections * 100f / _clickAttempts) : 0):F1}%");
            }
        }

        void OnGUI()
        {
            if (!showGUI) return;

            GUILayout.BeginArea(new Rect(Screen.width - 310, 170, 300, 300));
            GUILayout.Box("输入模式诊断", GUILayout.Width(300));

            // 显示统计
            GUILayout.Label($"<b>点击统计</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label($"  尝试次数: {_clickAttempts}");
            GUILayout.Label($"  成功次数: {_successfulSelections}");
            float successRate = _clickAttempts > 0 ? (_successfulSelections * 100f / _clickAttempts) : 0;
            string rateColor = successRate >= 90 ? "green" : (successRate >= 70 ? "yellow" : "red");
            GUILayout.Label($"  成功率: <color={rateColor}>{successRate:F1}%</color>", 
                new GUIStyle(GUI.skin.label) { richText = true });

            GUILayout.Label("---");

            // 显示当前模式
            bool isInExtendMode = extendInputManager != null && 
                                  selectionManager != null && 
                                  selectionManager.HasSelection();

            GUILayout.Label($"<b>当前模式</b>", new GUIStyle(GUI.skin.label) { richText = true });
            if (isInExtendMode)
            {
                GUILayout.Label("<color=cyan>拉伸模式</color>", 
                    new GUIStyle(GUI.skin.label) { richText = true });
            }
            else
            {
                GUILayout.Label("<color=green>移动模式</color>", 
                    new GUIStyle(GUI.skin.label) { richText = true });
            }

            GUILayout.Label("---");

            // 显示组件状态
            GUILayout.Label($"<b>组件状态</b>", new GUIStyle(GUI.skin.label) { richText = true });
            if (selectionManager != null)
                GUILayout.Label($"  Selection: {(selectionManager.enabled ? "✅" : "❌")}");
            if (extendInputManager != null)
                GUILayout.Label($"  Extend: {(extendInputManager.enabled ? "✅" : "❌")}");
            if (playerController != null)
                GUILayout.Label($"  Player: {(playerController.enabled ? "✅" : "❌")}");

            GUILayout.Label("---");

            // 显示光标状态
            GUILayout.Label($"<b>光标状态</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label($"  锁定: {(Cursor.lockState == CursorLockMode.Locked ? "是" : "否")}");
            GUILayout.Label($"  可见: {(Cursor.visible ? "是" : "否")}");

            GUILayout.Label("---");

            if (GUILayout.Button("重置统计"))
            {
                _clickAttempts = 0;
                _successfulSelections = 0;
                Debug.Log("<color=cyan>[诊断] 统计已重置</color>");
            }

            GUILayout.EndArea();
        }
    }
}

