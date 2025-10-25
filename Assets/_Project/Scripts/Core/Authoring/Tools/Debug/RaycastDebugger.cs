using UnityEngine;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 射线检测调试器
    /// 实时显示射线检测结果
    /// </summary>
    public class RaycastDebugger : MonoBehaviour
    {
        [Header("Settings")]
        public float raycastDistance = 100f;
        public LayerMask layerMask = ~0;
        public bool showVisualRay = true;
        public Color rayColor = Color.green;
        public float rayDuration = 0.5f;

        [Header("Auto References")]
        public Camera mainCamera;

        void Start()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        void Update()
        {
            if (mainCamera == null) return;

            // 每帧进行射线检测
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (showVisualRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * raycastDistance, rayColor, rayDuration);
            }

            // 当点击鼠标左键时输出详细信息
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("<color=yellow>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
                Debug.Log("<color=yellow>[Raycast调试] 鼠标点击检测</color>");
                Debug.Log($"  鼠标位置: {Input.mousePosition}");
                Debug.Log($"  射线起点: {ray.origin}");
                Debug.Log($"  射线方向: {ray.direction}");
                
                if (Physics.Raycast(ray, out var hit, raycastDistance, layerMask))
                {
                    Debug.Log($"<color=green>  ✅ 射线命中！</color>");
                    Debug.Log($"    命中对象: {hit.collider.gameObject.name}");
                    Debug.Log($"    命中位置: {hit.point}");
                    Debug.Log($"    距离: {hit.distance:F2}m");
                    Debug.Log($"    Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)} ({hit.collider.gameObject.layer})");
                    Debug.Log($"    Tag: {hit.collider.gameObject.tag}");
                    
                    // 检查组件
                    var proxy = hit.collider.GetComponent<InteractableProxy>();
                    if (proxy != null)
                    {
                        Debug.Log($"<color=cyan>    ✅ 找到 InteractableProxy！</color>");
                        Debug.Log($"      linkedEntity: {proxy.linkedEntity}");
                        Debug.Log($"      interactionType: {proxy.interactionType}");
                        Debug.Log($"      GameObject 激活: {proxy.gameObject.activeInHierarchy}");
                        Debug.Log($"      Collider 激活: {proxy.GetComponent<Collider>().enabled}");
                    }
                    else
                    {
                        Debug.LogWarning($"<color=orange>    ⚠️ 未找到 InteractableProxy 组件</color>");
                        
                        // 列出该对象上的所有组件
                        var components = hit.collider.gameObject.GetComponents<Component>();
                        Debug.Log($"    对象上的所有组件 ({components.Length}):");
                        foreach (var comp in components)
                        {
                            Debug.Log($"      - {comp.GetType().Name}");
                        }
                    }
                    
                    // 检查是否有其他物体挡住了 Proxy
                    RaycastHit[] allHits = Physics.RaycastAll(ray, raycastDistance, layerMask);
                    if (allHits.Length > 1)
                    {
                        Debug.Log($"<color=yellow>    ⚠️ 射线穿过了 {allHits.Length} 个物体：</color>");
                        for (int i = 0; i < allHits.Length; i++)
                        {
                            var h = allHits[i];
                            var hasProxy = h.collider.GetComponent<InteractableProxy>() != null;
                            Debug.Log($"      [{i}] {h.collider.gameObject.name} (距离: {h.distance:F2}m) {(hasProxy ? "✅ 有Proxy" : "")}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"<color=red>  ❌ 射线未命中任何物体</color>");
                    
                    // 查找场景中的所有 Proxy
                    var allProxies = FindObjectsOfType<InteractableProxy>();
                    Debug.Log($"  场景中 Proxy 数量: {allProxies.Length}");
                    
                    if (allProxies.Length > 0)
                    {
                        Debug.Log("  前 5 个 Proxy 信息:");
                        for (int i = 0; i < Mathf.Min(5, allProxies.Length); i++)
                        {
                            var p = allProxies[i];
                            var collider = p.GetComponent<Collider>();
                            Debug.Log($"    [{i}] {p.name}");
                            Debug.Log($"       - 位置: {p.transform.position}");
                            Debug.Log($"       - 激活: {p.gameObject.activeInHierarchy}");
                            Debug.Log($"       - Layer: {LayerMask.LayerToName(p.gameObject.layer)}");
                            Debug.Log($"       - Collider: {(collider != null ? collider.GetType().Name : "无")}");
                            if (collider != null)
                            {
                                Debug.Log($"       - Collider.enabled: {collider.enabled}");
                                Debug.Log($"       - Collider.isTrigger: {collider.isTrigger}");
                            }
                        }
                    }
                }
                
                Debug.Log("<color=yellow>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Box("射线检测调试器", GUILayout.Width(300));
            
            GUILayout.Label($"鼠标位置: {Input.mousePosition}");
            
            if (mainCamera != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                bool hit = Physics.Raycast(ray, out var hitInfo, raycastDistance, layerMask);
                
                if (hit)
                {
                    GUILayout.Label($"<color=green>✅ 命中: {hitInfo.collider.gameObject.name}</color>", 
                        new GUIStyle(GUI.skin.label) { richText = true });
                    GUILayout.Label($"距离: {hitInfo.distance:F2}m");
                }
                else
                {
                    GUILayout.Label("<color=red>❌ 未命中</color>", 
                        new GUIStyle(GUI.skin.label) { richText = true });
                }
            }
            
            GUILayout.Label("点击左键查看详细信息");
            
            GUILayout.EndArea();
        }
    }
}

