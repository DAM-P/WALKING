using UnityEngine;
using Unity.Entities;

namespace Project.Core.Authoring
{
    /// <summary>
    /// Proxy 诊断工具 - 在运行时检查代理状态
    /// </summary>
    public class ProxyDiagnosticTool : MonoBehaviour
    {
        [Header("诊断设置")]
        public bool autoRunOnStart = true;
        public bool continuousDiagnostic = false;
        
        private void Start()
        {
            if (autoRunOnStart)
            {
                Invoke(nameof(RunDiagnostic), 1f); // 延迟1秒，等待生成
            }
        }

        private void Update()
        {
            if (continuousDiagnostic && Input.GetKeyDown(KeyCode.F1))
            {
                RunDiagnostic();
            }
        }

        [ContextMenu("Run Diagnostic")]
        public void RunDiagnostic()
        {
            Debug.Log("========== Proxy 诊断开始 ==========");
            
            // 查找所有 Proxy
            var proxies = FindObjectsOfType<InteractableProxy>();
            Debug.Log($"<color=cyan>找到 {proxies.Length} 个 InteractableProxy</color>");

            if (proxies.Length == 0)
            {
                Debug.LogWarning("❌ 未找到任何 Proxy！检查：\n" +
                                 "1. InteractableProxySpawnSystem 是否运行？\n" +
                                 "2. 是否有 Entity 带 InteractableCubeTag？");
                return;
            }

            int activeCount = 0;
            int inactiveCount = 0;
            int validEntityCount = 0;
            int hasColliderCount = 0;
            int wrongLayerCount = 0;

            EntityManager? entityManager = World.DefaultGameObjectInjectionWorld?.EntityManager;

            foreach (var proxy in proxies)
            {
                // 检查激活状态
                if (proxy.gameObject.activeSelf)
                {
                    activeCount++;
                }
                else
                {
                    inactiveCount++;
                    Debug.LogWarning($"⚠️ Proxy '{proxy.name}' 被禁用！", proxy);
                }

                // 检查 Entity 有效性
                if (entityManager.HasValue && entityManager.Value.Exists(proxy.linkedEntity))
                {
                    validEntityCount++;
                }
                else
                {
                    Debug.LogError($"❌ Proxy '{proxy.name}' 的 linkedEntity 无效！", proxy);
                }

                // 检查 Collider
                var collider = proxy.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    hasColliderCount++;
                    
                    if (collider.isTrigger)
                    {
                        Debug.LogWarning($"⚠️ Proxy '{proxy.name}' 的 Collider 是 Trigger，Raycast 检测不到！", proxy);
                    }
                }
                else
                {
                    Debug.LogError($"❌ Proxy '{proxy.name}' 缺少 BoxCollider！", proxy);
                }

                // 检查 Layer
                if (proxy.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast"))
                {
                    wrongLayerCount++;
                    Debug.LogWarning($"⚠️ Proxy '{proxy.name}' 在 'Ignore Raycast' 层！", proxy);
                }
            }

            // 输出摘要
            Debug.Log($"<color=green>✅ 激活的 Proxy: {activeCount}/{proxies.Length}</color>");
            
            if (inactiveCount > 0)
            {
                Debug.LogError($"<color=red>❌ 禁用的 Proxy: {inactiveCount} (需要修复！)</color>");
            }
            
            Debug.Log($"<color=green>✅ Entity 有效: {validEntityCount}/{proxies.Length}</color>");
            Debug.Log($"<color=green>✅ 有 Collider: {hasColliderCount}/{proxies.Length}</color>");
            
            if (wrongLayerCount > 0)
            {
                Debug.LogWarning($"<color=orange>⚠️ 错误 Layer: {wrongLayerCount}</color>");
            }

            // 检查 CubeSelectionManager
            var selectionManager = FindObjectOfType<CubeSelectionManager>();
            if (selectionManager == null)
            {
                Debug.LogError("❌ 场景中未找到 CubeSelectionManager！");
            }
            else
            {
                Debug.Log($"<color=cyan>CubeSelectionManager 设置：</color>");
                Debug.Log($"  - Camera: {(selectionManager.mainCamera != null ? "✅" : "❌")}");
                Debug.Log($"  - Interactable Layer: {selectionManager.interactableLayer.value}");
                Debug.Log($"  - Raycast Distance: {selectionManager.raycastDistance}");
                Debug.Log($"  - Highlight Intensity: {selectionManager.highlightIntensity}");

                // 测试射线
                if (selectionManager.mainCamera != null)
                {
                    Ray testRay = selectionManager.mainCamera.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(testRay, out var hit, selectionManager.raycastDistance, selectionManager.interactableLayer))
                    {
                        Debug.Log($"<color=green>✅ 当前鼠标位置可以检测到：{hit.collider.name}</color>", hit.collider);
                    }
                    else
                    {
                        Debug.Log("<color=yellow>当前鼠标位置未检测到可交互对象</color>");
                    }
                }
            }

            // 检查 DOTS 系统
            if (entityManager.HasValue)
            {
                var query = entityManager.Value.CreateEntityQuery(ComponentType.ReadOnly<InteractableCubeTag>());
                int entityCount = query.CalculateEntityCount();
                Debug.Log($"<color=cyan>Entity 数量：{entityCount} 个带 InteractableCubeTag</color>");
                query.Dispose();
            }

            Debug.Log("========== 诊断完成 ==========");
        }
    }
}

