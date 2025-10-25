using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Project.Core.Components;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 拉伸输入管理器（MonoBehaviour）
    /// 处理方向键输入，控制拉伸预览和执行
    /// </summary>
    public class ExtendInputManager : MonoBehaviour
    {
        [Header("输入设置")]
        [Tooltip("按住时间超过此值才开始预览（防止误触）")]
        public float holdThreshold = 0.1f;

        [Tooltip("每次延长预览的时间间隔（秒）")]
        public float extendInterval = 0.2f;

        [Tooltip("最大拉伸长度")]
        public int maxExtendLength = 10;

        [Header("角色控制器")]
        [Tooltip("玩家角色控制器（拉伸模式时自动禁用移动）")]
        public FirstPersonController playerController;

        [Tooltip("自动禁用角色移动（选中 Cube 时）- 推荐使用 InputModeCoordinator 代替")]
        public bool autoDisableMovement = false; // 改为 false，由协调器接管

        [Header("调试")]
        public bool showDebugLog = true;

        private EntityManager _entityManager;
        private Entity _currentSelectedEntity = Entity.Null;
        private bool _wasInExtendMode = false;
        private int3 _currentDirection = int3.zero;
        private float _holdTime = 0f;
        private float _lastExtendTime = 0f;
        private int _currentPreviewLength = 0;
        private bool _isHoldingDirection = false;

        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        void Update()
        {
            // 查找当前选中的 Cube
            UpdateSelectedEntity();

            bool isInExtendMode = _currentSelectedEntity != Entity.Null && _entityManager.Exists(_currentSelectedEntity);

            // 自动切换角色移动控制
            if (autoDisableMovement && playerController != null)
            {
                // 进入拉伸模式：禁用角色移动
                if (isInExtendMode && !_wasInExtendMode)
                {
                    playerController.enabled = false;
                    if (showDebugLog)
                        Debug.Log("<color=cyan>[拉伸模式]</color> 角色移动已禁用");
                }
                // 退出拉伸模式：启用角色移动
                else if (!isInExtendMode && _wasInExtendMode)
                {
                    playerController.enabled = true;
                    if (showDebugLog)
                        Debug.Log("<color=green>[移动模式]</color> 角色移动已启用");
                }
            }

            _wasInExtendMode = isInExtendMode;

            if (!isInExtendMode)
            {
                // 没有选中 Cube，清除预览
                ClearPreview();
                return;
            }

            // 检测方向输入
            int3 inputDirection = GetDirectionInput();

            if (!inputDirection.Equals(int3.zero))
            {
                // 按住方向键
                HandleDirectionHold(inputDirection);
            }
            else
            {
                // 松开方向键
                HandleDirectionRelease();
            }
        }

        /// <summary>
        /// 更新当前选中的 Entity
        /// </summary>
        private void UpdateSelectedEntity()
        {
            var query = _entityManager.CreateEntityQuery(
                typeof(SelectionState),
                typeof(InteractableCubeTag),
                typeof(ExtendableTag)
            );

            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            _currentSelectedEntity = Entity.Null;
            foreach (var entity in entities)
            {
                var selection = _entityManager.GetComponentData<SelectionState>(entity);
                if (selection.IsSelected == 1)
                {
                    _currentSelectedEntity = entity;
                    break;
                }
            }

            entities.Dispose();
            query.Dispose();
        }

        /// <summary>
        /// 获取方向输入（转换为 int3）
        /// </summary>
        private int3 GetDirectionInput()
        {
            // 方向键 / WASD
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                return new int3(0, 0, 1);  // 前
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                return new int3(0, 0, -1); // 后
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                return new int3(-1, 0, 0); // 左
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                return new int3(1, 0, 0);  // 右

            // Q/E 控制上下
            if (Input.GetKey(KeyCode.E))
                return new int3(0, 1, 0);  // 上
            if (Input.GetKey(KeyCode.Q))
                return new int3(0, -1, 0); // 下

            return int3.zero;
        }

        /// <summary>
        /// 处理方向键按住
        /// </summary>
        private void HandleDirectionHold(int3 direction)
        {
            // 方向改变，重置
            if (!direction.Equals(_currentDirection))
            {
                _currentDirection = direction;
                _holdTime = 0f;
                _lastExtendTime = 0f;
                _currentPreviewLength = 0;
                _isHoldingDirection = true;
            }

            _holdTime += Time.deltaTime;

            // 超过阈值，开始预览
            if (_holdTime >= holdThreshold)
            {
                // 延长预览
                if (Time.time - _lastExtendTime >= extendInterval)
                {
                    _currentPreviewLength++;
                    _lastExtendTime = Time.time;

                    if (_currentPreviewLength > maxExtendLength)
                        _currentPreviewLength = maxExtendLength;

                    UpdatePreview();
                }
            }
        }

        /// <summary>
        /// 处理方向键松开
        /// </summary>
        private void HandleDirectionRelease()
        {
            if (_isHoldingDirection && _currentPreviewLength > 0)
            {
                // 确认拉伸
                ConfirmExtend();

                if (showDebugLog)
                {
                    Debug.Log($"<color=green>确认拉伸</color>: 方向={_currentDirection}, 长度={_currentPreviewLength}");
                }
            }

            // 清除状态
            ClearPreview();
            _currentDirection = int3.zero;
            _holdTime = 0f;
            _lastExtendTime = 0f;
            _currentPreviewLength = 0;
            _isHoldingDirection = false;
        }

        /// <summary>
        /// 更新预览
        /// </summary>
        private void UpdatePreview()
        {
            if (_currentSelectedEntity == Entity.Null || !_entityManager.Exists(_currentSelectedEntity))
                return;

            // 添加或更新 ExtendPreview 组件
            if (!_entityManager.HasComponent<ExtendPreview>(_currentSelectedEntity))
            {
                _entityManager.AddComponentData(_currentSelectedEntity, new ExtendPreview
                {
                    PreviewLength = _currentPreviewLength,
                    PreviewDirection = _currentDirection,
                    IsValid = true,  // 由 PreviewSystem 计算
                    ValidLength = 0
                });
            }
            else
            {
                var preview = _entityManager.GetComponentData<ExtendPreview>(_currentSelectedEntity);
                preview.PreviewLength = _currentPreviewLength;
                preview.PreviewDirection = _currentDirection;
                _entityManager.SetComponentData(_currentSelectedEntity, preview);
            }

            if (showDebugLog && _currentPreviewLength == 1)
            {
                Debug.Log($"<color=cyan>开始预览</color>: 方向={_currentDirection}, 长度={_currentPreviewLength}");
            }
        }

        /// <summary>
        /// 清除预览
        /// </summary>
        private void ClearPreview()
        {
            if (_currentSelectedEntity != Entity.Null && 
                _entityManager.Exists(_currentSelectedEntity) &&
                _entityManager.HasComponent<ExtendPreview>(_currentSelectedEntity))
            {
                _entityManager.RemoveComponent<ExtendPreview>(_currentSelectedEntity);
            }
        }

        /// <summary>
        /// 确认拉伸（添加执行标记）
        /// </summary>
        private void ConfirmExtend()
        {
            if (_currentSelectedEntity == Entity.Null || !_entityManager.Exists(_currentSelectedEntity))
                return;

            // 添加执行请求组件
            if (!_entityManager.HasComponent<ExtendExecutionRequest>(_currentSelectedEntity))
            {
                var preview = _entityManager.GetComponentData<ExtendPreview>(_currentSelectedEntity);
                
                _entityManager.AddComponentData(_currentSelectedEntity, new ExtendExecutionRequest
                {
                    Direction = preview.PreviewDirection,
                    Length = preview.ValidLength > 0 ? preview.ValidLength : preview.PreviewLength,
                    ChainID = (int)(Time.time * 1000f) // 使用时间戳作为 ChainID
                });
            }
        }

        void OnGUI()
        {
            if (!showDebugLog) return;

            bool isInExtendMode = _currentSelectedEntity != Entity.Null && _entityManager != null && _entityManager.Exists(_currentSelectedEntity);

            GUILayout.BeginArea(new Rect(10, 150, 400, 250));
            GUILayout.Label($"<b>拉伸输入</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
            
            // 模式指示
            if (isInExtendMode)
            {
                GUILayout.Label("<b><color=cyan>【拉伸模式】</color></b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 });
                GUILayout.Label("角色移动已禁用，WASD 控制拉伸方向");
            }
            else
            {
                GUILayout.Label("<b><color=green>【移动模式】</color></b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 });
                GUILayout.Label("点击 Cube 进入拉伸模式");
            }
            
            GUILayout.Label("---");
            GUILayout.Label($"选中 Entity: {(_currentSelectedEntity != Entity.Null ? _currentSelectedEntity.ToString() : "无")}");
            GUILayout.Label($"方向: {_currentDirection}");
            GUILayout.Label($"预览长度: {_currentPreviewLength}");
            GUILayout.Label($"按住时间: {_holdTime:F2}s");
            GUILayout.Label("---");
            GUILayout.Label("<color=yellow>操作提示:</color>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("  左键: 选中 Cube（进入拉伸模式）");
            GUILayout.Label("  WASD / 方向键: 拉伸方向");
            GUILayout.Label("  Q / E: 下 / 上");
            GUILayout.Label("  ESC: 取消选择（回到移动模式）");
            GUILayout.EndArea();
        }
    }

    /// <summary>
    /// 拉伸执行请求（单帧标记）
    /// </summary>
    public struct ExtendExecutionRequest : IComponentData
    {
        public int3 Direction;
        public int Length;
        public int ChainID;
    }
}

