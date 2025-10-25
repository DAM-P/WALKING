using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

#if HAS_URP_MATERIAL_PROPERTY
using Unity.Rendering;
#endif

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
            foreach (var (highlight, emission) in SystemAPI.Query<RefRO<HighlightState>, RefRW<URPMaterialPropertyEmissionColor>>())
            {
                emission.ValueRW.Value = highlight.ValueRO.Color * highlight.ValueRO.Intensity;
            }
#else
            // 未启用 URP 材质属性，仅更新 HighlightState（可通过其他方式读取）
#endif
        }
    }
}

