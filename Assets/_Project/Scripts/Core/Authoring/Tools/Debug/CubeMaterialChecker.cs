using UnityEngine;

namespace Project.Core.Authoring
{
    /// <summary>
    /// Cube 材质检查器 - 检查 Cube Prefab 材质配置
    /// </summary>
    public class CubeMaterialChecker : MonoBehaviour
    {
        [Header("Cube Prefab Reference")]
        [Tooltip("拖入你的 Cube Prefab（用于生成的那个）")]
        public GameObject cubePrefab;

        [ContextMenu("Check Material")]
        public void CheckMaterial()
        {
            if (cubePrefab == null)
            {
                Debug.LogError("❌ Cube Prefab 未赋值！请在 Inspector 中拖入 Cube Prefab");
                return;
            }

            Debug.Log("========== Cube 材质检查 ==========");
            Debug.Log($"检查对象: {cubePrefab.name}");

            var renderer = cubePrefab.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                Debug.LogError("❌ Cube Prefab 缺少 MeshRenderer 组件！");
                return;
            }

            if (renderer.sharedMaterial == null)
            {
                Debug.LogError("❌ Cube Prefab 的 Material 为空！");
                return;
            }

            var material = renderer.sharedMaterial;
            Debug.Log($"<color=cyan>Material: {material.name}</color>");
            Debug.Log($"<color=cyan>Shader: {material.shader.name}</color>");

            // 检查 Shader
            if (!material.shader.name.Contains("Universal Render Pipeline"))
            {
                Debug.LogWarning($"⚠️ Shader 不是 URP Shader！\n当前: {material.shader.name}\n推荐: Universal Render Pipeline/Lit");
            }
            else
            {
                Debug.Log($"<color=green>✅ Shader 是 URP Shader</color>");
            }

            // 检查 Emission
            if (material.IsKeywordEnabled("_EMISSION"))
            {
                Debug.Log($"<color=green>✅ Material 启用了 Emission Keyword</color>");
                
                if (material.HasProperty("_EmissionColor"))
                {
                    Color emissionColor = material.GetColor("_EmissionColor");
                    Debug.Log($"Emission Color: {emissionColor}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Material 未启用 Emission！\n解决方法：\n1. 选中 Material\n2. 勾选 'Emission' 复选框\n3. 保存");
            }

            // 检查 HDR
            if (material.HasProperty("_EmissionColor"))
            {
                Debug.Log($"<color=green>✅ Material 有 _EmissionColor 属性</color>");
            }
            else
            {
                Debug.LogError($"❌ Material 缺少 _EmissionColor 属性！");
            }

            // 检查渲染队列
            Debug.Log($"Render Queue: {material.renderQueue}");

            Debug.Log("========== 检查完成 ==========");

            // 给出建议
            Debug.Log("\n<color=yellow>【配置建议】</color>");
            if (!material.shader.name.Contains("Universal Render Pipeline/Lit"))
            {
                Debug.Log("1. 选中 Material");
                Debug.Log("2. Shader 下拉菜单 → Universal Render Pipeline → Lit");
            }
            if (!material.IsKeywordEnabled("_EMISSION"))
            {
                Debug.Log("3. 勾选 'Emission' 复选框");
                Debug.Log("4. Emission Color 设置为非黑色（如 (0, 0, 0, 1)）");
            }
            Debug.Log("5. 保存 Material 和 Prefab");
        }

        [ContextMenu("Auto Fix Material (Attempt)")]
        public void AutoFixMaterial()
        {
            if (cubePrefab == null)
            {
                Debug.LogError("❌ Cube Prefab 未赋值！");
                return;
            }

            var renderer = cubePrefab.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                Debug.LogError("❌ 无法修复：缺少 Renderer 或 Material");
                return;
            }

            var material = renderer.sharedMaterial;

            Debug.Log("========== 尝试自动修复 Material ==========");

            // 尝试启用 Emission
            material.EnableKeyword("_EMISSION");
            Debug.Log("✅ 启用了 _EMISSION Keyword");

            // 设置 Emission Color（初始为黑色，运行时由系统控制）
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.black);
                Debug.Log("✅ 设置 _EmissionColor 为黑色");
            }

            // 强制保存
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(material);
            Debug.Log("✅ 标记 Material 为 Dirty（需要手动保存）");
#endif

            Debug.Log("========== 自动修复完成 ==========");
            Debug.Log("<color=yellow>请手动保存 Material 和 Prefab（Ctrl+S）</color>");
        }
    }
}


