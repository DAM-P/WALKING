using UnityEngine;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 全局调试控制器
    /// 一键关闭/开启所有调试 GUI 和日志
    /// </summary>
    public class GlobalDebugController : MonoBehaviour
    {
        [Header("全局调试开关")]
        [Tooltip("主开关：关闭后所有调试功能都将禁用")]
        public bool enableAllDebug = false;

        [Header("分类开关")]
        public bool showSelectionDebug = false;
        public bool showExtendDebug = false;
        public bool showInputModeDebug = false;
        public bool showRaycastDebug = false;
        public bool showStageDebug = false;

        private static GlobalDebugController _instance;
        public static GlobalDebugController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GlobalDebugController>();
                }
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // 启动时自动关闭所有调试信息
            if (!enableAllDebug)
            {
                DisableAllDebug();
            }
        }

        [ContextMenu("关闭所有调试")]
        public void DisableAllDebug()
        {
            enableAllDebug = false;
            showSelectionDebug = false;
            showExtendDebug = false;
            showInputModeDebug = false;
            showRaycastDebug = false;
            showStageDebug = false;

            // 查找并禁用所有调试组件
            DisableComponent<SelectionTester>();
            DisableComponent<SelectionAutoFix>();
            DisableComponent<RaycastDebugger>();
            DisableComponent<InputModeCoordinator>();
            // 可选测试工具：存在则会被禁用（若无此类型则忽略）

            // 关闭 ExtendInputManager 的调试日志
       
            // 关闭 CrosshairExtendManager 的调试
            var crosshairManager = FindObjectOfType<CrosshairExtendManager>();
            if (crosshairManager != null)
            {
                crosshairManager.showDebug = false;
            }

            Debug.Log("<color=green>[全局调试] ✅ 已关闭所有调试信息</color>");
        }

        [ContextMenu("开启所有调试")]
        public void EnableAllDebug()
        {
            enableAllDebug = true;
            showSelectionDebug = true;
            showExtendDebug = true;
            showInputModeDebug = true;
            showRaycastDebug = true;
            showStageDebug = true;

            Debug.Log("<color=cyan>[全局调试] ⚠️ 已开启所有调试信息</color>");
        }

        private void DisableComponent<T>() where T : MonoBehaviour
        {
            var components = FindObjectsOfType<T>();
            foreach (var component in components)
            {
                component.enabled = false;
            }
        }

        void OnGUI()
        {
            if (!enableAllDebug) return;

            // 右上角显示调试控制面板
            GUILayout.BeginArea(new Rect(Screen.width - 210, 10, 200, 80));
            GUILayout.Box("全局调试控制", GUILayout.Width(200));

            if (GUILayout.Button("关闭所有调试"))
            {
                DisableAllDebug();
            }

            GUILayout.Label($"调试状态: {(enableAllDebug ? "开启" : "关闭")}");

            GUILayout.EndArea();
        }

        void Update()
        {
            // 快捷键：F12 切换调试开关
            if (Input.GetKeyDown(KeyCode.F12))
            {
                if (enableAllDebug)
                {
                    DisableAllDebug();
                }
                else
                {
                    EnableAllDebug();
                }
            }
        }
    }
}

