using UnityEngine;

namespace Project.Core.Authoring
{
    /// <summary>
    /// StageManager 测试脚本
    /// 用于快速测试关卡切换功能
    /// </summary>
    public class StageManagerTester : MonoBehaviour
    {
        [Header("References")]
        public StageManager stageManager;

        [Header("Controls")]
        [Tooltip("按下切换到下一关")]
        public KeyCode nextStageKey = KeyCode.N;
        [Tooltip("按下切换到上一关")]
        public KeyCode previousStageKey = KeyCode.P;
        [Tooltip("按下重新加载当前关")]
        public KeyCode reloadStageKey = KeyCode.R;

        [Header("Quick Load")]
        [Tooltip("快速加载指定关卡索引（用于测试）")]
        public int quickLoadStageIndex = 0;
        [Tooltip("按下加载指定关卡")]
        public KeyCode quickLoadKey = KeyCode.L;

        private void Update()
        {
            if (stageManager == null) return;

            if (Input.GetKeyDown(nextStageKey))
            {
                Debug.Log("[Tester] 加载下一关");
                stageManager.LoadNextStage();
            }

            if (Input.GetKeyDown(previousStageKey))
            {
                Debug.Log("[Tester] 加载上一关");
                stageManager.LoadPreviousStage();
            }

            if (Input.GetKeyDown(reloadStageKey))
            {
                Debug.Log("[Tester] 重新加载当前关");
                stageManager.ReloadCurrentStage();
            }

            if (Input.GetKeyDown(quickLoadKey))
            {
                Debug.Log($"[Tester] 快速加载关卡 {quickLoadStageIndex}");
                stageManager.LoadStage(quickLoadStageIndex);
            }
        }

        private void OnGUI()
        {
            if (stageManager == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Box("Stage Manager Tester");

            var current = stageManager.CurrentStage;
            if (current != null)
            {
                GUILayout.Label($"当前关卡: {stageManager.CurrentStageIndex} - {current.stageName}");
            }
            else
            {
                GUILayout.Label("未加载关卡");
            }

            GUILayout.Space(10);
            GUILayout.Label($"按 {nextStageKey} 下一关");
            GUILayout.Label($"按 {previousStageKey} 上一关");
            GUILayout.Label($"按 {reloadStageKey} 重新加载");
            GUILayout.Label($"按 {quickLoadKey} 加载关卡 {quickLoadStageIndex}");

            GUILayout.EndArea();
        }
    }
}


