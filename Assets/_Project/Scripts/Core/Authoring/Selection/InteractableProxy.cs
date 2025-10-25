using UnityEngine;
using Unity.Entities;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 可交互 Cube 的 GameObject 代理
    /// 负责射线检测与碰撞，关联 DOTS Entity
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class InteractableProxy : MonoBehaviour
    {
        [Header("Entity Link")]
        [Tooltip("关联的 DOTS Entity（运行时自动设置）")]
        public Entity linkedEntity = Entity.Null;

        [Header("Interaction")]
        [Tooltip("交互类型：0=可选择, 1=拉伸起点, 2=特殊功能")]
        public int interactionType = 0;

        [Header("Visual")]
        [Tooltip("高亮颜色（选中时）")]
        public Color highlightColor = new Color(1f, 1f, 0f, 1f);

        private BoxCollider _collider;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
            }
            
            // 设置为触发器（如果不需要物理碰撞）
            // _collider.isTrigger = true;
            
            // 设置 Layer 为 "Interactable"（需要在 Project Settings 中创建）
            gameObject.layer = LayerMask.NameToLayer("Default"); // 可改为 "Interactable"
        }

        private void OnDrawGizmosSelected()
        {
            // 在编辑器中显示交互边界
            Gizmos.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0.5f);
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }

        /// <summary>
        /// 鼠标悬停时的可视化反馈（可选）
        /// </summary>
        private void OnMouseEnter()
        {
            // 可在此处添加悬停效果
        }

        private void OnMouseExit()
        {
            // 移除悬停效果
        }
    }
}


