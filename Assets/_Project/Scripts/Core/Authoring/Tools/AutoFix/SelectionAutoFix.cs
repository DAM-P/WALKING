using UnityEngine;
using Unity.Entities;
using System.Linq;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 选择系统自动修复工具
    /// 尝试自动修复常见的选择失效问题
    /// </summary>
    [ExecuteAlways]
    public class SelectionAutoFix : MonoBehaviour
    {
        [Header("自动修复设置")]
        [Tooltip("自动修复 Proxy Layer")]
        public bool autoFixProxyLayers = true;

        [Tooltip("自动修复 CubeSelectionManager Layer Mask")]
        public bool autoFixSelectionLayerMask = true;

        [Tooltip("自动检查 Proxy 激活状态")]
        public bool autoCheckProxyActive = true;

        [Header("Proxy Layer 设置")]
        [Tooltip("目标 Layer 名称")]
        public string targetLayerName = "Default";

        // 日志节流
        private int _lastInactiveCount = -1;
        private float _lastLogTime = 0f;
        private const float LogIntervalSeconds = 5f;

        private void Update()
        {
            // 只在 Play 模式下运行
            if (!Application.isPlaying)
                return;

            if (autoFixProxyLayers)
            {
                FixProxyLayers();
            }

            if (autoFixSelectionLayerMask)
            {
                FixSelectionLayerMask();
            }

            if (autoCheckProxyActive)
            {
                CheckProxyActiveState();
            }
        }

        /// <summary>
        /// 修复所有 Proxy 的 Layer
        /// </summary>
        private void FixProxyLayers()
        {
            var proxies = FindObjectsOfType<InteractableProxy>();
            int targetLayer = LayerMask.NameToLayer(targetLayerName);

            if (targetLayer == -1)
            {
                Debug.LogWarning($"[AutoFix] Layer '{targetLayerName}' 不存在！");
                return;
            }

            foreach (var proxy in proxies)
            {
                if (proxy.gameObject.layer != targetLayer)
                {
                    proxy.gameObject.layer = targetLayer;
                    Debug.Log($"[AutoFix] ✅ 修复 {proxy.name} Layer: {targetLayerName}");
                }
            }
        }

        /// <summary>
        /// 修复 CubeSelectionManager 的 Layer Mask
        /// </summary>
        private void FixSelectionLayerMask()
        {
            var selectionManager = FindObjectOfType<CubeSelectionManager>();
            if (selectionManager == null)
                return;

            // 确保 interactableLayer 包含目标 Layer
            int targetLayer = LayerMask.NameToLayer(targetLayerName);
            if (targetLayer == -1)
                return;

            int targetLayerMask = 1 << targetLayer;

            if ((selectionManager.interactableLayer.value & targetLayerMask) == 0)
            {
                // 添加目标 Layer 到 Mask
                selectionManager.interactableLayer |= targetLayerMask;
                Debug.Log($"[AutoFix] ✅ 添加 '{targetLayerName}' 到 CubeSelectionManager.interactableLayer");
            }
        }

        /// <summary>
        /// 检查 Proxy 激活状态
        /// </summary>
        private void CheckProxyActiveState()
        {
            var proxies = FindObjectsOfType<InteractableProxy>(includeInactive: true);
            int inactiveCount = 0;

            foreach (var proxy in proxies)
            {
                // 忽略运行时用于克隆的隐藏预制（无实体关联，且一般名为 InteractableProxyPrefab）
                bool isTemplate = proxy.linkedEntity == Entity.Null || proxy.name.Contains("InteractableProxyPrefab");
                if (!proxy.gameObject.activeInHierarchy && !isTemplate)
                {
                    inactiveCount++;
                    // 可选：自动激活
                    // proxy.gameObject.SetActive(true);
                }
            }

            // 仅当数量变化或超过节流间隔时输出一次，避免刷屏
            if (inactiveCount > 0)
            {
                if (inactiveCount != _lastInactiveCount || Time.time - _lastLogTime > LogIntervalSeconds)
                {
                    Debug.LogWarning($"[AutoFix] ⚠️ 发现 {inactiveCount} 个未激活的 Proxy！（已忽略隐藏模板）");
                    _lastInactiveCount = inactiveCount;
                    _lastLogTime = Time.time;
                }
            }
        }

        /// <summary>
        /// 手动触发完整修复
        /// </summary>
        [ContextMenu("执行完整修复")]
        public void PerformFullFix()
        {
            Debug.Log("<color=cyan>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
            Debug.Log("<color=cyan>[AutoFix] 开始完整修复...</color>");

            FixProxyLayers();
            FixSelectionLayerMask();
            CheckProxyActiveState();
            ValidateSetup();

            Debug.Log("<color=green>[AutoFix] ✅ 完整修复完成！</color>");
            Debug.Log("<color=cyan>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
        }

        /// <summary>
        /// 验证设置
        /// </summary>
        [ContextMenu("验证选择系统设置")]
        public void ValidateSetup()
        {
            Debug.Log("<color=yellow>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
            Debug.Log("<color=yellow>[验证] 检查选择系统设置...</color>");

            // 1. 检查 CubeSelectionManager
            var selectionManager = FindObjectOfType<CubeSelectionManager>();
            if (selectionManager == null)
            {
                Debug.LogError("[验证] ❌ 未找到 CubeSelectionManager！");
            }
            else
            {
                Debug.Log($"[验证] ✅ CubeSelectionManager 存在");
                Debug.Log($"  - Enabled: {selectionManager.enabled}");
                Debug.Log($"  - GameObject Active: {selectionManager.gameObject.activeInHierarchy}");
                Debug.Log($"  - Main Camera: {(selectionManager.mainCamera != null ? "✅" : "❌")}");
                Debug.Log($"  - Interactable Layer: {selectionManager.interactableLayer.value}");
            }

            // 2. 检查 ExtendInputManager
            var extendManager = FindObjectOfType<ExtendInputManager>();
            if (extendManager == null)
            {
                Debug.LogWarning("[验证] ⚠️ 未找到 ExtendInputManager");
            }
            else
            {
                Debug.Log($"[验证] ✅ ExtendInputManager 存在");
                Debug.Log($"  - Enabled: {extendManager.enabled}");
            }

            // 3. 检查 Proxy
            var proxies = FindObjectsOfType<InteractableProxy>(includeInactive: true);
            var activeProxies = proxies.Where(p => p.gameObject.activeInHierarchy).ToArray();

            Debug.Log($"[验证] Proxy 统计:");
            Debug.Log($"  - 总数: {proxies.Length}");
            Debug.Log($"  - 激活: {activeProxies.Length}");
            Debug.Log($"  - 未激活: {proxies.Length - activeProxies.Length}");

            if (activeProxies.Length > 0)
            {
                var sampleProxy = activeProxies[0];
                Debug.Log($"[验证] 示例 Proxy ({sampleProxy.name}):");
                Debug.Log($"  - Layer: {LayerMask.LayerToName(sampleProxy.gameObject.layer)}");
                Debug.Log($"  - Entity: {sampleProxy.linkedEntity}");
                Debug.Log($"  - Has Collider: {(sampleProxy.GetComponent<Collider>() != null ? "✅" : "❌")}");
                
                var collider = sampleProxy.GetComponent<Collider>();
                if (collider != null)
                {
                    Debug.Log($"  - Collider Enabled: {collider.enabled}");
                    Debug.Log($"  - Is Trigger: {collider.isTrigger}");
                }
            }

            // 4. 检查 Entity
            if (Application.isPlaying && World.DefaultGameObjectInjectionWorld != null)
            {
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                var query = entityManager.CreateEntityQuery(typeof(InteractableCubeTag));
                int entityCount = query.CalculateEntityCount();
                query.Dispose();

                Debug.Log($"[验证] Entity 统计:");
                Debug.Log($"  - InteractableCubeTag 数量: {entityCount}");

                if (entityCount != activeProxies.Length)
                {
                    Debug.LogWarning($"[验证] ⚠️ Entity 数量({entityCount}) 与 Proxy 数量({activeProxies.Length})不匹配！");
                }
            }

            Debug.Log("<color=yellow>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>");
        }

        /// <summary>
        /// 重置所有选择状态
        /// </summary>
        [ContextMenu("重置所有选择状态")]
        public void ResetAllSelectionStates()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[AutoFix] 只能在 Play 模式下重置选择状态");
                return;
            }

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var query = entityManager.CreateEntityQuery(typeof(SelectionState));

            foreach (var entity in query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                entityManager.SetComponentData(entity, new SelectionState 
                { 
                    IsSelected = 0,
                    SelectTime = 0f
                });
            }

            query.Dispose();
            Debug.Log("<color=green>[AutoFix] ✅ 已重置所有选择状态</color>");
        }

        void OnGUI()
        {
            // 简单的 GUI 快捷按钮
            GUILayout.BeginArea(new Rect(Screen.width - 210, Screen.height - 120, 200, 110));
            GUILayout.Box("选择系统自动修复", GUILayout.Width(200));

            if (GUILayout.Button("执行完整修复"))
            {
                PerformFullFix();
            }

            if (GUILayout.Button("验证设置"))
            {
                ValidateSetup();
            }

            if (GUILayout.Button("重置选择状态"))
            {
                ResetAllSelectionStates();
            }

            GUILayout.EndArea();
        }
    }
}

