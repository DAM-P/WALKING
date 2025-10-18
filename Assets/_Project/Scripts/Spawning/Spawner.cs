// File: Assets/_Project/Scripts/Spawning/Components/Spawner.cs
using Unity.Entities;

public struct Spawner : IComponentData
{
    public Entity CubePrefab; // 用来存放我们想要生成的实体预制体
    public int SpawnCount;

    // Planet + Ring 参数（若 PlanetCount+RingCount>0 则优先生效）
    public int PlanetCount;
    public int RingCount;
    public float PlanetRadius;
    public float RingRadius;
    public float RingHalfThickness; // 环在Y方向的半厚度（可为0表示扁平）
    public float RingHalfWidth;     // 环在径向的半宽度（0为单一半径）

    public float PlanetAngularSpeed; // 星球整体自转角速度（弧度/秒）
    public float RingAngularSpeed;   // 行星环整体自转角速度（弧度/秒）
    public float TransitionDuration; // 从网格过渡到星球/环的时长（秒）
}