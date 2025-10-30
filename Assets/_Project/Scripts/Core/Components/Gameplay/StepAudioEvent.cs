using Unity.Entities;
using Unity.Mathematics;

namespace Project.Core.Components
{
    /// <summary>
    /// 播放脚步音效事件（由 Mono 桥接播放后销毁）。
    /// </summary>
    public struct StepAudioEvent : IComponentData
    {
        public float3 Position;
        public int TypeId;
    }
}



