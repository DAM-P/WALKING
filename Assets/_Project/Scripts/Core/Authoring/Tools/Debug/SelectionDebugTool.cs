using UnityEngine;
using Unity.Entities;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 选择系统调试工具
    /// 用于诊断"选中一次后无法再次选中"的问题
    /// </summary>
    public class SelectionDebugTool : MonoBehaviour
    {
        private CubeSelectionManager _selectionManager;
        private ExtendInputManager _extendInputManager;
        private Camera _mainCamera;

        void Start()
        {
            _selectionManager = FindObjectOfType<CubeSelectionManager>();
            _extendInputManager = FindObjectOfType<ExtendInputManager>();
            _mainCamera = Camera.main;

            if (_selectionManager == null)
                Debug.LogError("[诊断] 未找到 CubeSelectionManager！");
            if (_extendInputManager == null)
                Debug.LogError("[诊断] 未找到 ExtendInputManager！");
        }

        void Update()
        {
            // 监控鼠标左键点击
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("<color=yellow>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.Log("<color=yellow>[诊断] 检测到鼠标左键点击</color>");
                
                // 检查组件状态
                Debug.Log($"  CubeSelectionManager 存在: {_selectionManager != null}");
                Debug.Log($"  CubeSelectionManager enabled: {_selectionManager != null && _selectionManager.enabled}");
                Debug.Log($"  ExtendInputManager 存在: {_extendInputManager != null}");
                Debug.Log($"  ExtendInputManager enabled: {_extendInputManager != null && _extendInputManager.enabled}");

                // 执行射线检测
                if (_mainCamera != null)
                {
                    Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                    Debug.Log($"  射线起点: {ray.origin}");
                    Debug.Log($"  射线方向: {ray.direction}");
                    
                    if (Physics.Raycast(ray, out var hit, 100f))
                    {
                        Debug.Log($"  <color=green>✅ 射线命中:</color> {hit.collider.gameObject.name}");
                        Debug.Log($"    命中位置: {hit.point}");
                        Debug.Log($"    距离: {hit.distance:F2}");
                        Debug.Log($"    Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                        
                        var proxy = hit.collider.GetComponent<InteractableProxy>();
                        if (proxy != null)
                        {
                            Debug.Log($"    <color=green>✅ 找到代理:</color> Entity={proxy.linkedEntity}");
                            Debug.Log($"       代理激活: {proxy.gameObject.activeInHierarchy}");
                            Debug.Log($"       Collider 激活: {proxy.GetComponent<Collider>().enabled}");
                        }
                        else
                        {
                            Debug.LogWarning($"    <color=orange>⚠️ 未找到 InteractableProxy 组件</color>");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"  <color=red>❌ 射线未命中任何物体</color>");
                        
                        // 查找所有代理
                        var allProxies = FindObjectsOfType<InteractableProxy>();
                        Debug.Log($"  场景中代理数量: {allProxies.Length}");
                        foreach (var proxy in allProxies)
                        {
                            Debug.Log($"    代理: {proxy.name}, 激活={proxy.gameObject.activeInHierarchy}, Entity={proxy.linkedEntity}");
                        }
                    }
                }
                else
                {
                    Debug.LogError("  <color=red>主相机不存在！</color>");
                }
                
                Debug.Log("<color=yellow>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 150));
            GUILayout.Box("选择系统诊断", GUILayout.Width(300));
            
            if (_selectionManager != null)
            {
                bool hasSelection = _selectionManager.HasSelection();
                var currentSelection = _selectionManager.GetCurrentSelection();
                
                GUILayout.Label($"<color={(hasSelection ? "green" : "red")}>{(hasSelection ? "✅" : "❌")} 当前选择: {currentSelection}</color>", 
                    new GUIStyle(GUI.skin.label) { richText = true });
            }
            
            var allProxies = FindObjectsOfType<InteractableProxy>();
            GUILayout.Label($"代理数量: {allProxies.Length}");
            
            int activeProxies = 0;
            foreach (var proxy in allProxies)
            {
                if (proxy.gameObject.activeInHierarchy)
                    activeProxies++;
            }
            GUILayout.Label($"激活代理: {activeProxies}");
            
            GUILayout.Label("---");
            GUILayout.Label("<color=cyan>点击左键查看射线检测详情</color>", 
                new GUIStyle(GUI.skin.label) { richText = true });
            
            GUILayout.EndArea();
        }
    }
}

