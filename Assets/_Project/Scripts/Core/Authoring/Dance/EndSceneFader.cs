using UnityEngine;
using UnityEngine.UI;

namespace Project.Core.Authoring
{
	/// 结局场景内：先保持黑屏，再渐入图片
	public class EndSceneFader : MonoBehaviour
	{
		public Image blackOverlay;   // 全屏黑图（初始Alpha=1）
		public Image endImage;       // 结局图片（初始Alpha=0）
		[Tooltip("黑屏停留时长")] public float blackHoldSeconds = 0.5f;
		[Tooltip("图片渐入时长")] public float imageFadeSeconds = 2.0f;
		float _t;
		bool _started;

		void OnEnable()
		{
			SetAlpha(blackOverlay, 1f);
			SetAlpha(endImage, 0f);
			_t = 0f;
			_started = true;
		}

		void Update()
		{
			if (!_started) return;
			_t += Time.unscaledDeltaTime;
			if (_t <= blackHoldSeconds)
			{
				SetAlpha(blackOverlay, 1f);
				SetAlpha(endImage, 0f);
			}
			else
			{
				float tt = Mathf.Clamp01((_t - blackHoldSeconds) / Mathf.Max(0.01f, imageFadeSeconds));
				// 渐显图片，同时保持黑屏为1或同步反向淡出
				SetAlpha(endImage, tt);
				SetAlpha(blackOverlay, 1f - tt);
			}
		}

		static void SetAlpha(Image img, float a)
		{
			if (img == null) return;
			var c = img.color; c.a = a; img.color = c;
		}
	}
}




