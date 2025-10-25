using UnityEngine;
using Unity.Entities;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 选择系统测试工具
    /// 用于快速测试和调试 Cube 选择功能
    /// </summary>
    public class SelectionTester : MonoBehaviour
    {
        [Header("References")]
        public CubeSelectionManager selectionManager;

        [Header("Test Settings")]
        [Tooltip("按下快捷键取消选择")]
        public KeyCode deselectKey = KeyCode.Escape;
        [Tooltip("显示调试信息")]
        public bool showDebugInfo = true;

        private EntityManager _entityManager;

        private void Start()
        {
            if (selectionManager == null)
            {
                selectionManager = FindObjectOfType<CubeSelectionManager>();
            }

            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Update()
        {
            if (Input.GetKeyDown(deselectKey))
            {
                selectionManager?.DeselectAll();
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo || selectionManager == null) return;

            GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 200));
            GUILayout.Box("Selection System Debug");

            var current = selectionManager.GetCurrentSelection();
            if (current != Entity.Null && _entityManager.Exists(current))
            {
                GUILayout.Label($"选中 Entity: {current.Index}");

                if (_entityManager.HasComponent<InteractableCubeTag>(current))
                {
                    var tag = _entityManager.GetComponentData<InteractableCubeTag>(current);
                    GUILayout.Label($"交互类型: {tag.InteractionType}");
                }

                if (_entityManager.HasComponent<SelectionState>(current))
                {
                    var state = _entityManager.GetComponentData<SelectionState>(current);
                    GUILayout.Label($"选中状态: {(state.IsSelected == 1 ? "是" : "否")}");
                    GUILayout.Label($"选中时间: {state.SelectTime:F2}s");
                }

                if (_entityManager.HasComponent<HighlightState>(current))
                {
                    var highlight = _entityManager.GetComponentData<HighlightState>(current);
                    GUILayout.Label($"高亮强度: {highlight.Intensity:F2}");
                }
            }
            else
            {
                GUILayout.Label("未选中任何 Cube");
            }

            GUILayout.Space(10);
            GUILayout.Label($"按 {deselectKey} 取消选择");
            GUILayout.Label("左键点击选择 Cube");
            GUILayout.Label("右键取消选择");

            GUILayout.EndArea();
        }
    }
}


