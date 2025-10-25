using Unity.Entities;

/// <summary>
/// 标记关卡中生成的 Cube 实体，便于清理
/// </summary>
public struct StageCubeTag : IComponentData
{
    public int StageIndex;  // 所属关卡索引（可选）
}


