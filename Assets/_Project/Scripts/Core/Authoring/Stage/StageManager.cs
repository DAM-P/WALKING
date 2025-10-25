using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 关卡/阶段管理器
    /// 统一管理 Layout 加载、Collider 生成、实体清理与切换
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        [Header("Stages")]
        [Tooltip("所有关卡配置（按顺序）")]
        public List<StageConfig> stages = new List<StageConfig>();
        [Tooltip("初始加载的关卡索引（-1 表示不自动加载）")]
        public int initialStageIndex = 0;

        [Header("References")]
        [Tooltip("用于生成碰撞体的 GameObject（持有 CubeLayoutColliderGenerator）")]
        public GameObject colliderGeneratorPrefab;
        [Tooltip("玩家角色 Transform（用于重置位置）")]
        public Transform playerTransform;
        [Tooltip("相机 Transform（可选）")]
        public Transform cameraTransform;

        [Header("Transition")]
        [Tooltip("过渡淡入淡出的 UI Panel（可选，用于 Fade 效果）")]
        public CanvasGroup fadePanel;

        [Header("Runtime")]
        [Tooltip("当前加载的关卡索引")]
        [SerializeField] private int _currentStageIndex = -1;

        private GameObject _currentColliderGenerator;
        private EntityManager _entityManager;
        private EntityQuery _cubeEntityQuery;

        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (initialStageIndex >= 0 && initialStageIndex < stages.Count)
            {
                LoadStage(initialStageIndex);
            }
        }

        /// <summary>
        /// 加载指定索引的关卡
        /// </summary>
        public void LoadStage(int stageIndex)
        {
            if (stageIndex < 0 || stageIndex >= stages.Count)
            {
                Debug.LogError($"[StageManager] 关卡索引 {stageIndex} 超出范围（总数: {stages.Count}）");
                return;
            }

            var config = stages[stageIndex];
            if (config == null)
            {
                Debug.LogError($"[StageManager] 关卡索引 {stageIndex} 的配置为空");
                return;
            }

            StartCoroutine(LoadStageCoroutine(stageIndex, config));
        }

        /// <summary>
        /// 加载下一关卡
        /// </summary>
        public void LoadNextStage()
        {
            int nextIndex = _currentStageIndex + 1;
            if (nextIndex < stages.Count)
            {
                LoadStage(nextIndex);
            }
            else
            {
                Debug.LogWarning("[StageManager] 已经是最后一关");
            }
        }

        /// <summary>
        /// 加载上一关卡
        /// </summary>
        public void LoadPreviousStage()
        {
            int prevIndex = _currentStageIndex - 1;
            if (prevIndex >= 0)
            {
                LoadStage(prevIndex);
            }
            else
            {
                Debug.LogWarning("[StageManager] 已经是第一关");
            }
        }

        /// <summary>
        /// 重新加载当前关卡
        /// </summary>
        public void ReloadCurrentStage()
        {
            if (_currentStageIndex >= 0)
            {
                LoadStage(_currentStageIndex);
            }
        }

        private IEnumerator LoadStageCoroutine(int stageIndex, StageConfig config)
        {
            Debug.Log($"[StageManager] 开始加载关卡 {stageIndex}: {config.stageName}");

            // 1. 过渡开始（淡出）
            if (config.transitionType == StageConfig.TransitionType.Fade && fadePanel != null)
            {
                yield return StartCoroutine(FadeOut(config.transitionDuration * 0.4f));
            }

            // 2. 清理旧关卡
            ClearCurrentStage();

            // 3. 生成新碰撞体
            GenerateColliders(config);

            // 4. 生成新实体（通过 DOTS）
            // 注意：这里简化处理，实际可能需要更复杂的实体生成逻辑
            // 如果使用 CubeLayoutSpawnerAuthoring，需要动态创建并烘焙
            Debug.Log($"[StageManager] 实体生成需要集成 DOTS Spawner（当前简化跳过）");

            // 5. 移动玩家到出生点
            if (playerTransform != null)
            {
                playerTransform.position = config.playerSpawnPoint;
                Debug.Log($"[StageManager] 玩家移动到: {config.playerSpawnPoint}");
            }

            // 6. 重置相机（可选）
            if (config.resetCamera && cameraTransform != null)
            {
                cameraTransform.position = config.cameraPosition;
            }

            _currentStageIndex = stageIndex;

            // 7. 过渡结束（淡入）
            if (config.transitionType == StageConfig.TransitionType.Fade && fadePanel != null)
            {
                yield return StartCoroutine(FadeIn(config.transitionDuration * 0.6f));
            }

            Debug.Log($"[StageManager] 关卡 {stageIndex} 加载完成");
        }

        private void ClearCurrentStage()
        {
            Debug.Log("[StageManager] 清理旧关卡...");

            // 清理碰撞体生成器
            if (_currentColliderGenerator != null)
            {
                Destroy(_currentColliderGenerator);
                _currentColliderGenerator = null;
            }

            // 清理所有方块实体（DOTS）
            // 注意：这里需要根据你的实体标记来查询，暂时清理所有带特定组件的实体
            // 你可能需要给生成的实体加一个 Tag 组件（如 CubeLevelEntityTag）
            ClearCubeEntities();
        }

        private void ClearCubeEntities()
        {
            // 清理所有带 StageCubeTag 的实体
            var query = _entityManager.CreateEntityQuery(typeof(StageCubeTag));
            int count = query.CalculateEntityCount();
            
            if (count > 0)
            {
                _entityManager.DestroyEntity(query);
                Debug.Log($"[StageManager] 已清理 {count} 个关卡实体");
            }
        }

        private void GenerateColliders(StageConfig config)
        {
            if (config.layout == null || colliderGeneratorPrefab == null)
            {
                Debug.LogWarning("[StageManager] Layout 或 ColliderGeneratorPrefab 为空，跳过碰撞体生成");
                return;
            }

            // 实例化碰撞体生成器
            _currentColliderGenerator = Instantiate(colliderGeneratorPrefab, Vector3.zero, Quaternion.identity);
            _currentColliderGenerator.name = $"Collider_{config.stageName}";

            var generator = _currentColliderGenerator.GetComponent<CubeLayoutColliderGenerator>();
            if (generator == null)
            {
                generator = _currentColliderGenerator.AddComponent<CubeLayoutColliderGenerator>();
            }

            // 配置生成器
            generator.layout = config.layout;
            generator.colliderType = config.colliderType;
            generator.mergeMode = config.mergeMode;
            generator.clearOldColliders = true;

            // 生成碰撞体
            generator.GenerateCollider();

            Debug.Log($"[StageManager] 碰撞体生成完成: {config.stageName}");
        }

        private IEnumerator FadeOut(float duration)
        {
            if (fadePanel == null) yield break;

            fadePanel.gameObject.SetActive(true);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            fadePanel.alpha = 1f;
        }

        private IEnumerator FadeIn(float duration)
        {
            if (fadePanel == null) yield break;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            fadePanel.alpha = 0f;
            fadePanel.gameObject.SetActive(false);
        }

        // 供外部调用的便捷方法
        public int CurrentStageIndex => _currentStageIndex;
        public StageConfig CurrentStage => _currentStageIndex >= 0 && _currentStageIndex < stages.Count 
            ? stages[_currentStageIndex] 
            : null;
    }
}

