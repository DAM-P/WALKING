using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NarrationManager : MonoBehaviour
{
	[Header("UI")]
	public Canvas canvas;
	public RectTransform panel;
	public Text text;
	public AudioSource audioSource;
	public Font defaultFont;
	public int fontSize = 20;
	public Color textColor = Color.white;
	public Color panelColor = new Color(0f, 0f, 0f, 0.5f);
	public Vector2 panelPadding = new Vector2(16f, 8f);
	public bool showBackground = false;
	public float panelHeight = 100f;
	public bool backgroundAutoSize = true;
	[Range(0.2f, 1f)] public float backgroundMaxWidthPercent = 0.9f;
	public float backgroundMinWidth = 200f;

	[Header("Text Effects")]
	public bool enableOutline = true;
	public Color outlineColor = new Color(0f, 0f, 0f, 0.95f);
	public Vector2 outlineDistance = new Vector2(1.5f, -1.5f);
	public bool outlineUseGraphicAlpha = true;
	[Header("Debug")]
	public bool debugLog = false;
	public bool overrideSorting = true;
	public int sortingOrder = 1000;

	[Header("Background Gradient")]
	public bool backgroundUseVerticalGradient = true;
	public Color backgroundColor = new Color(0f, 0f, 0f, 1f);
	[Range(0f, 1f)] public float backgroundMaxAlpha = 0.5f;
	[Range(8, 1024)] public int backgroundTextureHeight = 128;

	Texture2D _gradientTex;
	Sprite _gradientSprite;

	[Header("Timing")]
	public float defaultDuration = 3f;
	public float minDuration = 1.2f;
	public float charsPerSecond = 18f; // 用于估算时长

	readonly Queue<NarrationItem> queue = new Queue<NarrationItem>();
	Coroutine playRoutine;

	struct NarrationItem
	{
		public string text;
		public AudioClip voice;
		public float? duration;
	}

	void Awake()
	{
		EnsureUi();
		if (audioSource == null)
		{
			audioSource = gameObject.GetComponent<AudioSource>();
			if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.playOnAwake = false;
		}
		// 应用文字描边等特效
		EnsureTextOutline();
		// 默认隐藏
		Hide();
	}

	void EnsureUi()
	{
		if (canvas == null)
		{
			var go = new GameObject("NarrationCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			canvas = go.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.overrideSorting = overrideSorting;
			canvas.sortingOrder = sortingOrder;
			var scaler = go.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920, 1080);
		}
		if (panel == null)
		{
			var panelGo = new GameObject("NarrationPanel", typeof(RectTransform), typeof(Image));
			panel = panelGo.GetComponent<RectTransform>();
			panel.SetParent(canvas.transform, false);
			var image = panelGo.GetComponent<Image>();
			ApplyBackgroundStyle(image);
			// 底部居中，自适应宽高
			panel.anchorMin = new Vector2(0.5f, 0f);
			panel.anchorMax = new Vector2(0.5f, 0f);
			panel.pivot = new Vector2(0.5f, 0f);
			panel.anchoredPosition = new Vector2(0f, 40f);
			panel.sizeDelta = new Vector2(0f, 0f);
			var fitter = panel.gameObject.GetComponent<ContentSizeFitter>();
			if (fitter == null) fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
			fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			var hlg = panel.gameObject.GetComponent<HorizontalLayoutGroup>();
			if (hlg == null) hlg = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
			hlg.padding = new RectOffset((int)panelPadding.x, (int)panelPadding.x, (int)panelPadding.y, (int)panelPadding.y);
			hlg.childAlignment = TextAnchor.MiddleCenter;
			hlg.childControlWidth = true;
			hlg.childControlHeight = true;
			hlg.childForceExpandWidth = false;
			hlg.childForceExpandHeight = false;
		}
		if (text == null)
		{
			var textGo = new GameObject("NarrationText", typeof(RectTransform), typeof(Text));
			var rt = textGo.GetComponent<RectTransform>();
			rt.SetParent(panel, false);
			text = textGo.GetComponent<Text>();
			text.font = defaultFont != null ? defaultFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
			if (text.font == null)
			{
				// 兜底：从系统字体创建动态字体
				try
				{
					string[] candidates = new string[] { "Arial", "Microsoft YaHei", "SimHei", "Noto Sans CJK SC" };
					for (int i = 0; i < candidates.Length && text.font == null; i++)
					{
						var f = Font.CreateDynamicFontFromOSFont(candidates[i], fontSize);
						if (f != null) text.font = f;
					}
				}
				catch { }
			}
			text.fontSize = fontSize;
			text.color = textColor;
			text.alignment = TextAnchor.MiddleCenter;
			text.horizontalOverflow = HorizontalWrapMode.Wrap;
			text.verticalOverflow = VerticalWrapMode.Overflow;
			text.raycastTarget = false;
			if (text.font != null && text.font.material != null)
			{
				text.material = text.font.material; // 确保有有效材质
			}
			// 由布局组与ContentSizeFitter控制
			rt.anchorMin = new Vector2(0.5f, 0.5f);
			rt.anchorMax = new Vector2(0.5f, 0.5f);
			rt.pivot = new Vector2(0.5f, 0.5f);
			rt.anchoredPosition = Vector2.zero;
			var le = text.gameObject.GetComponent<LayoutElement>();
			if (le == null) le = text.gameObject.AddComponent<LayoutElement>();
			// 新建时也应用描边
			EnsureTextOutline();
		}
		else
		{
			// 若场景中预置了 Text，也要应用描边配置
			EnsureTextOutline();
		}
	}

	void EnsureTextOutline()
	{
		if (text == null) return;
		var outline = text.GetComponent<Outline>();
		if (enableOutline)
		{
			if (outline == null) outline = text.gameObject.AddComponent<Outline>();
			outline.effectColor = outlineColor;
			outline.effectDistance = outlineDistance;
			outline.useGraphicAlpha = outlineUseGraphicAlpha;
		}
		else
		{
			if (outline != null) Destroy(outline);
		}
	}

	public void EnqueueText(string content, AudioClip voice = null, float? duration = null)
	{
		queue.Enqueue(new NarrationItem { text = content, voice = voice, duration = duration });
		if (debugLog) Debug.Log($"[NarrationManager] EnqueueText: '{content}' dur={(duration.HasValue ? duration.Value.ToString("F2") : "est")} ", this);
		TryPlay();
	}

	public void EnqueueKey(string key, NarrationDatabase database)
	{
		if (database != null && database.TryGet(key, out var line))
		{
			float? dur = line.OverrideDuration ? line.DurationSeconds : (float?)null;
			EnqueueText(line.Text, line.Voice, dur);
		}
		else if (debugLog)
		{
			Debug.LogWarning($"[NarrationManager] Key not found or database missing: '{key}'", this);
		}
	}

	void TryPlay()
	{
		if (playRoutine == null)
		{
			if (debugLog) Debug.Log("[NarrationManager] Start PlayQueue", this);
			playRoutine = StartCoroutine(PlayQueue());
		}
	}

	IEnumerator PlayQueue()
	{
		while (queue.Count > 0)
		{
			var item = queue.Dequeue();
			Show(item.text);
			if (debugLog) Debug.Log($"[NarrationManager] Show: '{item.text}'", this);
			PlayVoice(item.voice);
			float wait = item.duration ?? EstimateDuration(item.text);
			yield return new WaitForSeconds(wait);
		}
		Hide();
		playRoutine = null;
	}

	void Show(string content)
	{
		if (text != null) text.text = content ?? string.Empty;
		if (panel != null)
		{
			panel.gameObject.SetActive(true);
			var img = panel.GetComponent<Image>();
			if (img != null)
			{
				img.enabled = showBackground;
				if (showBackground) ApplyBackgroundStyle(img);
			}
			if (backgroundAutoSize)
			{
				var canvasRT = canvas.transform as RectTransform;
				float maxWidth = canvasRT != null ? canvasRT.rect.width * Mathf.Clamp01(backgroundMaxWidthPercent) : 1920f * backgroundMaxWidthPercent;
				float minWidth = backgroundMinWidth;
				// 先强制重建文字尺寸
				LayoutRebuilder.ForceRebuildLayoutImmediate(text.transform as RectTransform);
				var textRT = text.transform as RectTransform;
				float targetW = Mathf.Clamp(textRT.sizeDelta.x + panelPadding.x * 2f, minWidth, maxWidth);
				float targetH = Mathf.Max(panelHeight, textRT.sizeDelta.y + panelPadding.y * 2f);
				panel.sizeDelta = new Vector2(targetW, targetH);
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
			Canvas.ForceUpdateCanvases();
		}
	}

	void Hide()
	{
		if (panel != null) panel.gameObject.SetActive(false);
		if (text != null) text.text = string.Empty;
	}

	void PlayVoice(AudioClip clip)
	{
		if (audioSource == null) return;
		if (clip == null)
		{
			if (audioSource.isPlaying) audioSource.Stop();
			return;
		}
		audioSource.clip = clip;
		audioSource.Play();
	}

	float EstimateDuration(string s)
	{
		if (string.IsNullOrEmpty(s)) return minDuration;
		float byLen = Mathf.Max(minDuration, s.Length / Mathf.Max(1f, charsPerSecond));
		return Mathf.Max(minDuration, defaultDuration, byLen);
	}

	void ApplyBackgroundStyle(Image image)
	{
		if (image == null) return;
		if (!backgroundUseVerticalGradient)
		{
			image.sprite = null;
			image.type = Image.Type.Simple;
			image.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundMaxAlpha);
			return;
		}

		// 生成一张竖向Alpha渐变贴图：中间最大透明度，向上下边缘渐变到0
		int h = Mathf.Clamp(backgroundTextureHeight, 8, 1024);
		if (_gradientTex == null || _gradientTex.height != h)
		{
			if (_gradientTex != null) Destroy(_gradientTex);
			_gradientTex = new Texture2D(2, h, TextureFormat.ARGB32, false);
			_gradientTex.wrapMode = TextureWrapMode.Clamp;
		}
		Color32[] cols = new Color32[2 * h];
		for (int y = 0; y < h; y++)
		{
			float t = (y + 0.5f) / h; // 0..1 (bottom->top)
			float centerBand = 1f - Mathf.Abs(t * 2f - 1f); // 中间高，两端低
			float alpha = Mathf.Clamp01(centerBand / Mathf.Max(0.0001f, 1f - 0f)) * backgroundMaxAlpha;
			byte a = (byte)Mathf.RoundToInt(alpha * 255f);
			byte r = (byte)Mathf.RoundToInt(backgroundColor.r * 255f);
			byte g = (byte)Mathf.RoundToInt(backgroundColor.g * 255f);
			byte b = (byte)Mathf.RoundToInt(backgroundColor.b * 255f);
			int rowStart = y * 2;
			cols[rowStart] = new Color32(r, g, b, a);
			cols[rowStart + 1] = new Color32(r, g, b, a);
		}
		_gradientTex.SetPixels32(cols);
		_gradientTex.Apply(false, false);

		if (_gradientSprite == null || _gradientSprite.texture != _gradientTex)
		{
			_gradientSprite = Sprite.Create(_gradientTex, new Rect(0, 0, _gradientTex.width, _gradientTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
		}
		image.sprite = _gradientSprite;
		image.type = Image.Type.Sliced;
		image.color = Color.white; // 颜色已烘焙进纹理的RGB，透明度由纹理A控制
	}
}


