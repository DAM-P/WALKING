using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

#if HAS_URP_MATERIAL_PROPERTY
using Unity.Rendering;
#endif
using Project.Core.Components;

namespace Project.Core.Systems
{
    /// <summary>
    /// 高亮渲染系统
    /// 更新高亮状态动画，写入材质属性（需要集成 Entities Graphics）
    /// </summary>
    [BurstCompile]
    public partial struct HighlightRenderSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // 更新高亮动画
            foreach (var (highlight, selection) in SystemAPI.Query<RefRW<HighlightState>, RefRO<SelectionState>>())
            {
                if (selection.ValueRO.IsSelected == 1)
                {
                    // 选中状态：脉冲动画
                    highlight.ValueRW.AnimTime += dt * 2f;
                    float pulse = math.sin(highlight.ValueRW.AnimTime) * 0.2f + 0.8f; // 0.6 ~ 1.0
                    highlight.ValueRW.Intensity = pulse;
                }
                else
                {
                    // 未选中：淡出
                    highlight.ValueRW.Intensity = math.max(0f, highlight.ValueRW.Intensity - dt * 2f);
                    highlight.ValueRW.AnimTime = 0f;
                }
            }

#if HAS_URP_MATERIAL_PROPERTY
            // 写入 URP 材质属性（Emission）
            foreach (var (highlight, selection, emission, entity) in SystemAPI
                .Query<RefRO<HighlightState>, RefRO<SelectionState>, RefRW<URPMaterialPropertyEmissionColor>>()
                .WithEntityAccess())
            {
                float4 baseGlow = highlight.ValueRO.Color * highlight.ValueRO.Intensity;
                float4 finalGlow = baseGlow;

                bool hasExtend = state.EntityManager.HasComponent<ExtendableTag>(entity);
                bool hoverActive = false;
                if (state.EntityManager.HasComponent<HoverState>(entity))
                {
                    var h = state.EntityManager.GetComponentData<HoverState>(entity);
                    hoverActive = h.IsHovered != 0;
                }

                // 未选中但悬停且可拉伸：使用淡蓝提示
                if (selection.ValueRO.IsSelected == 0 && hoverActive && hasExtend)
                {
                    finalGlow = new float4(0.2f, 0.6f, 1.0f, 1.0f) * 0.35f;
                }
                // 选中且可拉伸：叠加一层淡蓝，使其与普通选中区分
                else if (selection.ValueRO.IsSelected == 1 && hasExtend)
                {
                    float4 extendGlow = new float4(0.2f, 0.6f, 1.0f, 1.0f); // 淡蓝
                    float extendStrength = 0.35f; // 叠加强度（小于选中高亮）
                    finalGlow += extendGlow * extendStrength;
                }

                // 写回（保持 alpha 为 1）
                finalGlow.w = 1f;
                emission.ValueRW.Value = finalGlow;
            }
#else
            // 未启用 URP 材质属性，仅更新 HighlightState（可通过其他方式读取）
#endif
        }
    }
}

