using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Project.Core.Components;

#if HAS_URP_MATERIAL_PROPERTY
using Unity.Rendering;
#endif

namespace Project.Core.Authoring
{
    /// <summary>
    /// Cube 选择管理器
    /// 负责射线检测、选择/取消选择、与 DOTS 交互
    /// </summary>
    public class CubeSelectionManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("主相机（自动获取）")]
        public Camera mainCamera;

        [Header("Selection Settings")]
        [Tooltip("射线检测距离")]
        public float raycastDistance = 100f;
        [Tooltip("可交互的 Layer Mask")]
        public LayerMask interactableLayer = ~0; // 默认所有层
        [Tooltip("是否显示射线调试线")]
        public bool showDebugRay = true;

        [Header("Highlight Settings")]
        [Tooltip("选中时的高亮强度")]
        [Range(0f, 2f)] public float highlightIntensity = 1.2f;
        [Tooltip("高亮颜色")]
        public Color highlightColor = new Color(1f, 1f, 0f, 1f);

        [Header("Runtime State")]
        [Tooltip("当前选中的 Entity")]
        [SerializeField] private Entity _currentSelection = Entity.Null;
        [Tooltip("当前悬停的代理")]
        [SerializeField] private InteractableProxy _hoveredProxy;
        private Entity _lastHoveredEntity = Entity.Null;

        [Header("Debug")]
        [Tooltip("显示详细调试日志")]
        public bool showDetailedLog = false;

        private EntityManager _entityManager;
        private bool _isInitialized = false;

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (World.DefaultGameObjectInjectionWorld != null)
            {
                _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                _isInitialized = true;
                Debug.Log("[Selection] 初始化成功");
            }
            else
            {
                Debug.LogError("[Selection] 无法获取 EntityManager！");
            }
        }

        private void Update()
        {
            // 确保初始化成功
            if (!_isInitialized)
            {
                if (World.DefaultGameObjectInjectionWorld != null)
                {
                    _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                    _isInitialized = true;
                    Debug.Log("[Selection] 延迟初始化成功");
                }
                else
                {
                    return;
                }
            }

            // 鼠标左键点击选择
            if (Input.GetMouseButtonDown(0))
            {
                if (showDetailedLog)
                    Debug.Log("[Selection] 检测到鼠标左键点击");
                TrySelectCube();
            }

            // ESC 取消选择（右键不再用于取消，避免与功能键冲突）
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (showDetailedLog)
                    Debug.Log("[Selection] 检测到 ESC 取消选择输入");
                DeselectAll();
            }

            // 实时检测悬停（可选）
            UpdateHover();
        }

        /// <summary>
        /// 尝试选择鼠标指向的 Cube
        /// </summary>
        private void TrySelectCube()
        {
            if (mainCamera == null)
            {
                Debug.LogError("[Selection] 主相机为空！");
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (showDebugRay || showDetailedLog)
            {
                Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.yellow, 1f);
                if (showDetailedLog)
                    Debug.Log($"[Selection] 射线: 起点={ray.origin}, 方向={ray.direction}");
            }

            if (Physics.Raycast(ray, out var hit, raycastDistance, interactableLayer))
            {
                if (showDetailedLog)
                    Debug.Log($"[Selection] 射线命中: {hit.collider.gameObject.name}, 距离={hit.distance:F2}");

                var proxy = hit.collider.GetComponent<InteractableProxy>();
                if (proxy != null && proxy.linkedEntity != Entity.Null)
                {
                    if (showDetailedLog)
                        Debug.Log($"[Selection] 找到代理: Entity={proxy.linkedEntity}");
                    
                    SelectCube(proxy.linkedEntity, proxy);
                    Debug.Log($"[Selection] ✅ 选中 Cube：Entity={proxy.linkedEntity}, Type={proxy.interactionType}");
                }
                else
                {
                    if (showDetailedLog)
                    {
                        if (proxy == null)
                            Debug.LogWarning($"[Selection] ⚠️ 命中物体但无代理组件: {hit.collider.gameObject.name}");
                        else
                            Debug.LogWarning($"[Selection] ⚠️ 代理的 linkedEntity 为空");
                    }
                }
            }
            else
            {
                if (showDetailedLog)
                    Debug.Log("[Selection] 射线未命中任何物体，取消选择");
                
                // 点击空白处，取消选择
                DeselectAll();
            }
        }

        /// <summary>
        /// 更新鼠标悬停状态
        /// </summary>
        private void UpdateHover()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out var hit, raycastDistance, interactableLayer))
            {
                var proxy = hit.collider.GetComponent<InteractableProxy>();
                if (proxy != _hoveredProxy)
                {
                    _hoveredProxy = proxy;
                    // 更新 ECS 悬停状态（用于未选中提示发光）
                    if (_entityManager != null)
                    {
                        // 清除上一悬停
                        if (_lastHoveredEntity != Entity.Null && _entityManager.Exists(_lastHoveredEntity))
                        {
                            if (_entityManager.HasComponent<HoverState>(_lastHoveredEntity))
                                _entityManager.SetComponentData(_lastHoveredEntity, new HoverState { IsHovered = 0 });
                        }

                        // 设置当前悬停
                        if (proxy != null && proxy.linkedEntity != Entity.Null && _entityManager.Exists(proxy.linkedEntity))
                        {
                            if (!_entityManager.HasComponent<HoverState>(proxy.linkedEntity))
                                _entityManager.AddComponentData(proxy.linkedEntity, new HoverState { IsHovered = 0 });
                            _entityManager.SetComponentData(proxy.linkedEntity, new HoverState { IsHovered = 1 });
                            _lastHoveredEntity = proxy.linkedEntity;
                        }
                        else
                        {
                            _lastHoveredEntity = Entity.Null;
                        }
                    }
                }
            }
            else
            {
                _hoveredProxy = null;
                // 清除上一悬停
                if (_entityManager != null && _lastHoveredEntity != Entity.Null && _entityManager.Exists(_lastHoveredEntity))
                {
                    if (_entityManager.HasComponent<HoverState>(_lastHoveredEntity))
                        _entityManager.SetComponentData(_lastHoveredEntity, new HoverState { IsHovered = 0 });
                }
                _lastHoveredEntity = Entity.Null;
            }
        }

        /// <summary>
        /// 选中指定的 Cube Entity
        /// </summary>
        public void SelectCube(Entity entity, InteractableProxy proxy = null)
        {
            // 取消之前的选择
            if (_currentSelection != Entity.Null && _entityManager.Exists(_currentSelection))
            {
                if (_entityManager.HasComponent<SelectionState>(_currentSelection))
                {
                    _entityManager.SetComponentData(_currentSelection, new SelectionState 
                    { 
                        IsSelected = 0,
                        SelectTime = 0f
                    });
                }
                
                if (_entityManager.HasComponent<HighlightState>(_currentSelection))
                {
                    _entityManager.SetComponentData(_currentSelection, new HighlightState 
                    { 
                        Intensity = 0f 
                    });
                }
            }

            // 选择新 Cube
            _currentSelection = entity;

            if (_entityManager.Exists(entity))
            {
                // 添加或更新 SelectionState
                if (!_entityManager.HasComponent<SelectionState>(entity))
                {
                    _entityManager.AddComponentData(entity, new SelectionState());
                }
                _entityManager.SetComponentData(entity, new SelectionState 
                { 
                    IsSelected = 1,
                    SelectTime = (float)Time.timeAsDouble
                });

                // 添加或更新 HighlightState
                if (!_entityManager.HasComponent<HighlightState>(entity))
                {
                    _entityManager.AddComponentData(entity, new HighlightState());
                }

                Color proxyColor = proxy != null ? proxy.highlightColor : highlightColor;
                _entityManager.SetComponentData(entity, new HighlightState 
                { 
                    Intensity = highlightIntensity,
                    Color = new float4(proxyColor.r, proxyColor.g, proxyColor.b, proxyColor.a),
                    AnimTime = 0f
                });

                // 调试：检查 Emission 组件
#if HAS_URP_MATERIAL_PROPERTY
                if (!_entityManager.HasComponent<Unity.Rendering.URPMaterialPropertyEmissionColor>(entity))
                {
                    Debug.LogWarning($"[Selection] Entity {entity} 缺少 URPMaterialPropertyEmissionColor！尝试添加...");
                    _entityManager.AddComponentData(entity, new Unity.Rendering.URPMaterialPropertyEmissionColor 
                    { 
                        Value = new float4(proxyColor.r, proxyColor.g, proxyColor.b, 1f) * highlightIntensity 
                    });
                }
                else
                {
                    Debug.Log($"[Selection] ✅ Entity 有 Emission 组件");
                }
#else
                Debug.LogError("[Selection] ❌ HAS_URP_MATERIAL_PROPERTY 未定义！");
#endif
            }
        }

        /// <summary>
        /// 取消所有选择
        /// </summary>
        public void DeselectAll()
        {
            if (_currentSelection != Entity.Null && _entityManager.Exists(_currentSelection))
            {
                if (_entityManager.HasComponent<SelectionState>(_currentSelection))
                {
                    _entityManager.SetComponentData(_currentSelection, new SelectionState 
                    { 
                        IsSelected = 0,
                        SelectTime = 0f
                    });
                }
                
                if (_entityManager.HasComponent<HighlightState>(_currentSelection))
                {
                    _entityManager.SetComponentData(_currentSelection, new HighlightState 
                    { 
                        Intensity = 0f 
                    });
                }

                Debug.Log($"[Selection] 取消选择");
            }

            _currentSelection = Entity.Null;
        }

        /// <summary>
        /// 获取当前选中的 Entity
        /// </summary>
        public Entity GetCurrentSelection() => _currentSelection;

        /// <summary>
        /// 是否有 Cube 被选中
        /// </summary>
        public bool HasSelection() => _currentSelection != Entity.Null && _entityManager.Exists(_currentSelection);
    }
}

