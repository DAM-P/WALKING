using UnityEngine;

namespace Project.Core.Authoring
{
    /// <summary>
    /// 简单的屏幕中央准心（十字）
    /// - 支持尺寸、颜色、线宽、启用/禁用
    /// - 可在 Play 模式下自动隐藏/显示鼠标光标
    /// </summary>
    public class CrosshairUI : MonoBehaviour
    {
        [Header("外观")]
        public Color color = new Color(1f, 1f, 1f, 0.9f);
        [Range(2f, 20f)] public float lineLength = 12f;
        [Range(1f, 6f)] public float lineThickness = 2f;
        [Range(0f, 10f)] public float gap = 4f; // 中心留白

        [Header("行为")]
        public bool hideSystemCursorInPlayMode = true;
        public bool onlyWhenLocked = false; // 仅在 Cursor.lockState == Locked 时显示

        [Header("调试")]
        public bool showDebugBounds = false;

        private Texture2D _whiteTex;

        void Awake()
        {
            _whiteTex = Texture2D.whiteTexture;
        }

        void OnEnable()
        {
            if (Application.isPlaying && hideSystemCursorInPlayMode)
            {
                Cursor.visible = false;
            }
        }

        void OnDisable()
        {
            if (Application.isPlaying && hideSystemCursorInPlayMode)
            {
                Cursor.visible = true;
            }
        }

        void OnGUI()
        {
            if (onlyWhenLocked && Cursor.lockState != CursorLockMode.Locked)
                return;

            // 屏幕中心点（GUI 坐标从左上角开始）
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            GUI.color = color;

            // 横线（左、右各一段，中间留白）
            DrawRect(cx - gap - lineLength, cy - lineThickness * 0.5f, lineLength, lineThickness);
            DrawRect(cx + gap,             cy - lineThickness * 0.5f, lineLength, lineThickness);

            // 竖线（上、下各一段）
            DrawRect(cx - lineThickness * 0.5f, cy - gap - lineLength, lineThickness, lineLength);
            DrawRect(cx - lineThickness * 0.5f, cy + gap,             lineThickness, lineLength);

            if (showDebugBounds)
            {
                GUI.color = new Color(1, 0, 0, 0.2f);
                DrawRect(cx - 1, cy - 1, 2, 2);
            }

            GUI.color = Color.white;
        }

        private void DrawRect(float x, float y, float w, float h)
        {
            GUI.DrawTexture(new Rect(x, y, w, h), _whiteTex);
        }
    }
}














