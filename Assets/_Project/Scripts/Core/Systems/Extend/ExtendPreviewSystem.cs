using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Project.Core.Components;
using Project.Core.Authoring.Debugging;

namespace Project.Core.Systems
{
    /// <summary>
    /// 拉伸预览系统，处理碰撞检测和预览可视化
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class ExtendPreviewSystem : SystemBase
    {
        public static bool PreviewEnabled = true; // 开启预览计算（Game 视图需由 Drawer 渲染）
        protected override void OnUpdate()
        {
            if (!PreviewEnabled)
                return;
            // 获取空间哈希表
            if (!SystemAPI.TryGetSingleton<OccupiedCubeMap>(out var cubeMap))
                return;

            var map = cubeMap.Map;

            // 处理所有有预览的 Cube
            Entities
                .WithAll<InteractableCubeTag, ExtendableTag>()
                .ForEach((Entity entity, ref ExtendPreview preview, in LocalTransform transform, in SelectionState selection, in StageCubeTag stageTag) =>
                {
                    if (selection.IsSelected == 0)
                    {
                        preview.IsValid = false;
                        preview.ValidLength = 0;
                        return;
                    }

                    // 计算起点坐标
                    int3 startPos = new int3(math.round(transform.Position));

                    // 碰撞检测，找到最大可拉伸长度
                    int validLength = CalculateValidLength(startPos, preview.PreviewDirection, preview.PreviewLength, map, stageTag.StageIndex);

                    preview.ValidLength = validLength;
                    preview.IsValid = validLength > 0;

                }).WithoutBurst().Run();

            // 绘制预览
            DrawPreview();
        }

        /// <summary>
        /// 计算有效拉伸长度（碰撞检测）
        /// </summary>
        private int CalculateValidLength(int3 startPos, int3 direction, int requestedLength, NativeParallelHashMap<int4, Entity> map, int stageIndex)
        {
            int validLength = 0;

            for (int i = 1; i <= requestedLength; i++)
            {
                int3 testPos = startPos + direction * i;

                // 检查该位置是否已被占用
                var key = new int4(testPos.x, testPos.y, testPos.z, stageIndex);
                if (map.ContainsKey(key))
                {
                    // 碰撞，停止
                    break;
                }

                validLength = i;
            }

            return validLength;
        }

        /// <summary>
        /// 绘制预览（使用 Debug.DrawLine 和 Gizmos）
        /// </summary>
        private void DrawPreview()
        {
            Entities
                .WithAll<InteractableCubeTag, ExtendableTag>()
                .WithoutBurst()
                .ForEach((Entity entity, in ExtendPreview preview, in LocalTransform transform, in SelectionState selection) =>
                {
                    if (selection.IsSelected == 0 || preview.PreviewLength == 0)
                        return;

                    float3 startPos = transform.Position;
                    float3 direction = (float3)preview.PreviewDirection;

                    // 绘制请求的长度（可能有部分碰撞）
                    for (int i = 1; i <= preview.PreviewLength; i++)
                    {
                        float3 cubePos = startPos + direction * i;
                        bool isValid = i <= preview.ValidLength;

                        // 绘制方块轮廓（增强效果）
                        Color color = isValid ? new Color(0f, 1f, 0f, 0.6f) : new Color(1f, 0f, 0f, 0.6f);
                        DrawWireCube(cubePos, 0.95f, color);
                        
                        // 添加填充效果（更醒目）
                        if (isValid)
                        {
                            DrawWireCube(cubePos, 0.85f, new Color(0f, 1f, 0f, 0.3f));
                        }

                        // 绘制连接线（加粗）
                        if (i == 1)
                        {
                            DebugDraw.DrawLine(startPos, cubePos, color, 0f, true, DebugDraw.Channel.ExtendPreview);
                            // 加粗效果：绘制多条平行线
                            DebugDraw.DrawLine(startPos + new float3(0.05f, 0, 0), cubePos + new float3(0.05f, 0, 0), color, 0f, true, DebugDraw.Channel.ExtendPreview);
                        }
                        else
                        {
                            float3 prevPos = startPos + direction * (i - 1);
                            DebugDraw.DrawLine(prevPos, cubePos, color, 0f, true, DebugDraw.Channel.ExtendPreview);
                            DebugDraw.DrawLine(prevPos + new float3(0.05f, 0, 0), cubePos + new float3(0.05f, 0, 0), color, 0f, true, DebugDraw.Channel.ExtendPreview);
                        }
                    }

                    // 绘制方向指示箭头（增强版）
                    float3 arrowStart = startPos + direction * 0.3f;
                    float3 arrowEnd = startPos + direction * 1.5f;
                    DebugDraw.DrawLine(arrowStart, arrowEnd, Color.yellow, 0f, true, DebugDraw.Channel.ExtendPreview);
                    
                    // 箭头头部
                    float3 perpendicular = math.cross(direction, new float3(0, 1, 0));
                    if (math.lengthsq(perpendicular) < 0.1f)
                        perpendicular = math.cross(direction, new float3(1, 0, 0));
                    perpendicular = math.normalize(perpendicular) * 0.2f;
                    
                    DebugDraw.DrawLine(arrowEnd, arrowEnd - direction * 0.3f + perpendicular, Color.yellow, 0f, true, DebugDraw.Channel.ExtendPreview);
                    DebugDraw.DrawLine(arrowEnd, arrowEnd - direction * 0.3f - perpendicular, Color.yellow, 0f, true, DebugDraw.Channel.ExtendPreview);

                }).Run();
        }

        /// <summary>
        /// 绘制线框立方体
        /// </summary>
        private void DrawWireCube(float3 center, float size, Color color)
        {
            float halfSize = size * 0.5f;

            // 8个顶点
            float3[] vertices = new float3[8]
            {
                center + new float3(-halfSize, -halfSize, -halfSize),
                center + new float3(halfSize, -halfSize, -halfSize),
                center + new float3(halfSize, -halfSize, halfSize),
                center + new float3(-halfSize, -halfSize, halfSize),
                center + new float3(-halfSize, halfSize, -halfSize),
                center + new float3(halfSize, halfSize, -halfSize),
                center + new float3(halfSize, halfSize, halfSize),
                center + new float3(-halfSize, halfSize, halfSize)
            };

            // 底面 (0-1-2-3)
            DebugDraw.DrawLine(vertices[0], vertices[1], color, 0f, true, DebugDraw.Channel.ExtendPreview);
            DebugDraw.DrawLine(vertices[1], vertices[2], color, 0f, true, DebugDraw.Channel.ExtendPreview);
            DebugDraw.DrawLine(vertices[2], vertices[3], color, 0f, true, DebugDraw.Channel.ExtendPreview);
            DebugDraw.DrawLine(vertices[3], vertices[0], color, 0f, true, DebugDraw.Channel.ExtendPreview);

            // 顶面 (4-5-6-7)
            DebugDraw.DrawLine(vertices[4], vertices[5], color, 0f, true, DebugDraw.Channel.ExtendPreview);
            DebugDraw.DrawLine(vertices[5], vertices[6], color, 0f, true, DebugDraw.Channel.ExtendPreview);
            DebugDraw.DrawLine(vertices[6], vertices[7], color, 0f, true, DebugDraw.Channel.ExtendPreview);
            DebugDraw.DrawLine(vertices[7], vertices[4], color, 0f, true, DebugDraw.Channel.ExtendPreview);

            // 垂直边 (0-4, 1-5, 2-6, 3-7)
            DebugDraw.DrawLine(vertices[0], vertices[4], color, 0f, true, DebugDraw.Channel.ExtendPreview);
            DebugDraw.DrawLine(vertices[1], vertices[5], color, 0f, true, DebugDraw.Channel.ExtendPreview);
            DebugDraw.DrawLine(vertices[2], vertices[6], color, 0f, true, DebugDraw.Channel.ExtendPreview);
            DebugDraw.DrawLine(vertices[3], vertices[7], color, 0f, true, DebugDraw.Channel.ExtendPreview);
        }
    }
}

