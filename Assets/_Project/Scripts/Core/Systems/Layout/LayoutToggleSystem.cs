using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct LayoutToggleSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Spawner>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // 空格切换（也可以换成你的自定义输入）
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        var em = state.EntityManager;
        LayoutController lc;
        if (!SystemAPI.TryGetSingleton(out lc))
        {
            var e = em.CreateEntity(typeof(LayoutController));
            em.SetComponentData(e, new LayoutController { LayoutMode = 1, ModeCount = 2 });
            return;
        }
        if (lc.ModeCount <= 0) lc.ModeCount = 2;
        lc.LayoutMode = (lc.LayoutMode + 1) % lc.ModeCount;
        SystemAPI.SetSingleton(lc);
    }
}


