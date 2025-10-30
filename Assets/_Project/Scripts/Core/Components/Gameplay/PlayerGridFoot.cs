using Unity.Entities;
using Unity.Mathematics;

namespace Project.Core.Components
{
    /// <summary>
    /// 玩家脚下所在的栅格坐标（当前与上一次），用于触发进入事件。
    /// </summary>
    public struct PlayerGridFoot : IComponentData
    {
        public int3 CurrentCell;
        public int3 LastCell;
    }
}



