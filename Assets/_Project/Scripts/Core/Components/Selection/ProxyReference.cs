using Unity.Entities;

/// <summary>
/// Entity 到 GameObject 代理的引用
/// </summary>
public struct ProxyReference : IComponentData
{
    /// <summary>
    /// 关联的 GameObject InstanceID
    /// </summary>
    public int GameObjectInstanceID;
}


