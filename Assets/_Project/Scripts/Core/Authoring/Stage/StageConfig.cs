using UnityEngine;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 关卡/阶段配置（ScriptableObject）
    /// 包含 Layout、渲染与碰撞的所有配置
    /// </summary>
    [CreateAssetMenu(menuName = "Level/Stage Config", fileName = "StageConfig")]
    public class StageConfig : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("关卡名称/标识")]
        public string stageName = "Stage 1";
        [Tooltip("关卡描述")]
        [TextArea(2, 4)] public string description;

        [Header("Layout & Rendering")]
        [Tooltip("关卡的方块布局")]
        public CubeLayout layout;
        [Tooltip("渲染用的 Cube Prefab（Entities Graphics 兼容）")]
        public GameObject cubePrefab;
        [Tooltip("每帧生成实体数量上限")]
        public int spawnPerFrame = 2048;
        [Tooltip("应用每实例颜色（需要 URP 支持）")]
        public bool applyInstanceColor = true;
        [Tooltip("发光强度")]
        [Range(0f, 5f)] public float emissionIntensity = 1.2f;

        [Header("Collision")]
        [Tooltip("碰撞体类型")]
        public CubeLayoutColliderGenerator.ColliderType colliderType = CubeLayoutColliderGenerator.ColliderType.BoxCollider;
        [Tooltip("BoxCollider 合并模式")]
        public CubeLayoutColliderGenerator.MergeMode mergeMode = CubeLayoutColliderGenerator.MergeMode.GreedyMerge;

        [Header("Gameplay")]
        [Tooltip("玩家出生点（世界坐标）")]
        public Vector3 playerSpawnPoint = Vector3.zero;
        [Tooltip("相机初始位置（可选）")]
        public Vector3 cameraPosition = Vector3.zero;
        [Tooltip("是否重置相机")]
        public bool resetCamera = false;

        [Header("Transition")]
        [Tooltip("切换到此关卡的过渡时间（秒）")]
        public float transitionDuration = 1f;
        [Tooltip("过渡类型")]
        public TransitionType transitionType = TransitionType.Fade;

        public enum TransitionType
        {
            Instant,    // 立即切换
            Fade,       // 淡入淡出
            Slide       // 滑动（未实现）
        }
    }
}


