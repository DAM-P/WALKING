using Unity.Entities;
using UnityEngine;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 标记可交互 Cube 的 Authoring 组件
    /// 烘焙时会添加 InteractableCubeTag 到 Entity
    /// </summary>
    public class InteractableCubeMarker : MonoBehaviour
    {
        [Header("Interaction")]
        [Tooltip("交互类型：0=可选择, 1=拉伸起点, 2=特殊功能")]
        public int interactionType = 0;

        class Baker : Baker<InteractableCubeMarker>
        {
            public override void Bake(InteractableCubeMarker authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new InteractableCubeTag 
                { 
                    InteractionType = authoring.interactionType 
                });
                
                // 同时添加选择和高亮状态组件
                AddComponent(entity, new SelectionState { IsSelected = 0, SelectTime = 0f });
                AddComponent(entity, new HighlightState { Intensity = 0f, Color = new Unity.Mathematics.float4(1, 1, 0, 1) });
            }
        }
    }
}


