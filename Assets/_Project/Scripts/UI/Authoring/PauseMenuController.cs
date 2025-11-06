using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.UI
{
	/// <summary>
	/// Minimal, extensible ESC pause menu.
	/// - Press Escape to toggle.
	/// - Pauses via Time.timeScale (configurable) and shows a simple UI with Resume/Quit.
	/// - Auto-creates a Canvas/UI when not provided, but you can also wire your own.
	/// </summary>
	public class PauseMenuController : MonoBehaviour
	{
		[Header("Input")]
		public KeyCode toggleKey = KeyCode.Escape;

		[Header("Pause Behavior")]
		public bool useTimeScalePause = true;
		[Range(0f, 1f)] public float pausedTimeScale = 0f;
		public bool lockCursorWhenPlaying = true;
		public bool showCursorWhenPaused = true;
		public bool pauseAudioListener = false;
		[Tooltip("Disable physics simulation while paused (classic PhysX).")]
		public bool pausePhysicsSimulation = false;
		[Tooltip("Hide the whole pause canvas when not paused (recommended).")]
		public bool hideCanvasWhenPlaying = true;

		[Header("UI (Optional – will auto build if null)")]
		public Canvas canvas;
		public RectTransform menuRoot;
		public Button resumeButton;
		public Button quitButton;
		public int sortingOrder = 5000;

		[Header("Disable Behaviours While Paused")]
		[Tooltip("Components to disable when paused (e.g., FirstPersonController)")]
		public Behaviour[] disableOnPause;
		[Tooltip("Disable ALL Behaviour components on these GameObjects while paused")]
		public GameObject[] disableAllBehavioursOn;
#if ENABLE_INPUT_SYSTEM
		[Header("Input System (Optional)")]
		[Tooltip("PlayerInput components to disable while paused (new Input System)")]
		public PlayerInput[] disablePlayerInputs;
#endif

		[Header("Events")]
		public UnityEvent onPaused;
		public UnityEvent onResumed;

		bool _isPaused = false;
		float _previousTimeScale = 1f;
		bool _prevAutoSimulation;
		bool[] _cachedEnabledStates;
		readonly System.Collections.Generic.Dictionary<Behaviour, bool> _behaviourEnabledBefore = new System.Collections.Generic.Dictionary<Behaviour, bool>(64);
#if ENABLE_INPUT_SYSTEM
		readonly System.Collections.Generic.Dictionary<PlayerInput, bool> _playerInputEnabledBefore = new System.Collections.Generic.Dictionary<PlayerInput, bool>(8);
#endif
#if ENABLE_INPUT_SYSTEM
		bool[] _cachedPlayerInputEnabled;
#endif

		void Awake()
		{
			EnsureUi();
			if (disableOnPause != null && disableOnPause.Length > 0)
			{
				_cachedEnabledStates = new bool[disableOnPause.Length];
				for (int i = 0; i < disableOnPause.Length; i++)
				{
					_cachedEnabledStates[i] = disableOnPause[i] != null && disableOnPause[i].enabled;
				}
			}
#if ENABLE_INPUT_SYSTEM
			if (disablePlayerInputs != null && disablePlayerInputs.Length > 0)
			{
				_cachedPlayerInputEnabled = new bool[disablePlayerInputs.Length];
				for (int i = 0; i < disablePlayerInputs.Length; i++)
				{
					var pi = disablePlayerInputs[i];
					_cachedPlayerInputEnabled[i] = pi != null && pi.enabled;
				}
			}
#endif
			ApplyPauseUi(false);
		}
		System.Collections.Generic.IEnumerable<Behaviour> EnumerateBehavioursToDisable()
		{
			var seen = new System.Collections.Generic.HashSet<int>();
			if (disableOnPause != null)
			{
				for (int i = 0; i < disableOnPause.Length; i++)
				{
					var b = disableOnPause[i];
					if (b == null) continue;
					int id = b.GetInstanceID();
					if (seen.Add(id)) yield return b;
				}
			}
			if (disableAllBehavioursOn != null)
			{
				for (int i = 0; i < disableAllBehavioursOn.Length; i++)
				{
					var go = disableAllBehavioursOn[i];
					if (go == null) continue;
					var behaviours = go.GetComponents<Behaviour>();
					for (int j = 0; j < behaviours.Length; j++)
					{
						var b = behaviours[j];
						if (b == null) continue;
						int id = b.GetInstanceID();
						if (seen.Add(id)) yield return b;
					}
				}
			}
		}


		void OnEnable()
		{
			// Wire buttons (idempotent)
			if (resumeButton != null)
			{
				resumeButton.onClick.RemoveListener(Resume);
				resumeButton.onClick.AddListener(Resume);
			}
			if (quitButton != null)
			{
				quitButton.onClick.RemoveListener(QuitGame);
				quitButton.onClick.AddListener(QuitGame);
			}
		}

		void Update()
		{
			if (Input.GetKeyDown(toggleKey))
			{
				if (_isPaused) Resume(); else Pause();
			}
		}

		public void Pause()
		{
			if (_isPaused) return;
			_isPaused = true;
			if (useTimeScalePause)
			{
				_previousTimeScale = Time.timeScale;
				Time.timeScale = pausedTimeScale;
			}
			if (pauseAudioListener) AudioListener.pause = true;
			if (pausePhysicsSimulation)
			{
				_prevAutoSimulation = Physics.autoSimulation;
				Physics.autoSimulation = false;
			}
			// Disable behaviours (explicit list + all on specified GameObjects)
			foreach (var b in EnumerateBehavioursToDisable())
			{
				if (b == null) continue;
				if (!_behaviourEnabledBefore.ContainsKey(b))
				{
					_behaviourEnabledBefore[b] = b.enabled;
				}
				b.enabled = false;
			}
#if ENABLE_INPUT_SYSTEM
			// Disable PlayerInput (explicit list)
			if (disablePlayerInputs != null)
			{
				for (int i = 0; i < disablePlayerInputs.Length; i++)
				{
					var pi = disablePlayerInputs[i];
					if (pi == null) continue;
					if (!_playerInputEnabledBefore.ContainsKey(pi))
						_playerInputEnabledBefore[pi] = pi.enabled;
					pi.DeactivateInput();
					pi.enabled = false;
				}
			}
			// Also disable PlayerInput on listed GameObjects
			if (disableAllBehavioursOn != null)
			{
				for (int i = 0; i < disableAllBehavioursOn.Length; i++)
				{
					var go = disableAllBehavioursOn[i];
					if (go == null) continue;
					var inputs = go.GetComponents<PlayerInput>();
					for (int j = 0; j < inputs.Length; j++)
					{
						var pi = inputs[j];
						if (pi == null) continue;
						if (!_playerInputEnabledBefore.ContainsKey(pi))
							_playerInputEnabledBefore[pi] = pi.enabled;
						pi.DeactivateInput();
						pi.enabled = false;
					}
				}
			}
#endif
			SetCursorState(paused: true);
			ApplyPauseUi(true);
			onPaused?.Invoke();
		}

		public void Resume()
		{
			if (!_isPaused) return;
			_isPaused = false;
			if (useTimeScalePause)
			{
				Time.timeScale = _previousTimeScale;
			}
			if (pauseAudioListener) AudioListener.pause = false;
			if (pausePhysicsSimulation)
			{
				Physics.autoSimulation = _prevAutoSimulation;
			}
			// Restore behaviours
			if (_behaviourEnabledBefore.Count > 0)
			{
				var keys = new System.Collections.Generic.List<Behaviour>(_behaviourEnabledBefore.Keys);
				for (int i = 0; i < keys.Count; i++)
				{
					var b = keys[i];
					if (b == null) continue;
					bool prev;
					if (_behaviourEnabledBefore.TryGetValue(b, out prev)) b.enabled = prev;
				}
				_behaviourEnabledBefore.Clear();
			}
#if ENABLE_INPUT_SYSTEM
			// Restore PlayerInput
			if (_playerInputEnabledBefore.Count > 0)
			{
				var keysPi = new System.Collections.Generic.List<PlayerInput>(_playerInputEnabledBefore.Keys);
				for (int i = 0; i < keysPi.Count; i++)
				{
					var pi = keysPi[i];
					if (pi == null) continue;
					bool prev;
					if (_playerInputEnabledBefore.TryGetValue(pi, out prev))
					{
						pi.enabled = prev;
						if (prev) pi.ActivateInput();
					}
				}
				_playerInputEnabledBefore.Clear();
			}
#endif
			SetCursorState(paused: false);
			ApplyPauseUi(false);
			onResumed?.Invoke();
		}

		public void QuitGame()
		{
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
			#else
			Application.Quit();
			#endif
		}

		void OnDestroy()
		{
			// Safety: ensure we restore timeScale if destroyed while paused
			if (_isPaused && useTimeScalePause)
			{
				Time.timeScale = _previousTimeScale;
			}
			if (pauseAudioListener) AudioListener.pause = false;
			if (pausePhysicsSimulation)
			{
				Physics.autoSimulation = _prevAutoSimulation;
			}
		}

		void SetCursorState(bool paused)
		{
			if (paused)
			{
				Cursor.visible = showCursorWhenPaused;
				Cursor.lockState = showCursorWhenPaused ? CursorLockMode.None : CursorLockMode.Locked;
			}
			else
			{
				Cursor.visible = !lockCursorWhenPlaying;
				Cursor.lockState = lockCursorWhenPlaying ? CursorLockMode.Locked : CursorLockMode.None;
			}
		}

		void ApplyPauseUi(bool show)
		{
			if (menuRoot != null)
			{
				menuRoot.gameObject.SetActive(show);
			}
			if (canvas != null && hideCanvasWhenPlaying)
			{
				canvas.gameObject.SetActive(show);
			}
		}

		void EnsureUi()
		{
			if (canvas == null)
			{
				var go = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
				canvas = go.GetComponent<Canvas>();
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;
				canvas.sortingOrder = sortingOrder;
				var scaler = go.GetComponent<CanvasScaler>();
				scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				scaler.referenceResolution = new Vector2(1920, 1080);
			}

			if (menuRoot == null)
			{
				var panelGo = new GameObject("PauseMenu", typeof(RectTransform), typeof(Image));
				menuRoot = panelGo.GetComponent<RectTransform>();
				menuRoot.SetParent(canvas.transform, false);
				menuRoot.anchorMin = new Vector2(0.5f, 0.5f);
				menuRoot.anchorMax = new Vector2(0.5f, 0.5f);
				menuRoot.pivot = new Vector2(0.5f, 0.5f);
				menuRoot.sizeDelta = new Vector2(420, 260);
				menuRoot.anchoredPosition = Vector2.zero;
				var bg = panelGo.GetComponent<Image>();
				bg.color = new Color(0f, 0f, 0f, 0.6f);
			}

			if (resumeButton == null)
			{
				resumeButton = CreateButton(menuRoot, "Resume", new Vector2(0, 50));
			}
			if (quitButton == null)
			{
				quitButton = CreateButton(menuRoot, "Quit", new Vector2(0, -50));
			}
		}

		Button CreateButton(RectTransform parent, string label, Vector2 anchoredPos)
		{
			var btnGo = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
			var rt = btnGo.GetComponent<RectTransform>();
			rt.SetParent(parent, false);
			rt.anchorMin = new Vector2(0.5f, 0.5f);
			rt.anchorMax = new Vector2(0.5f, 0.5f);
			rt.pivot = new Vector2(0.5f, 0.5f);
			rt.sizeDelta = new Vector2(280, 60);
			rt.anchoredPosition = anchoredPos;

			var img = btnGo.GetComponent<Image>();
			img.color = new Color(1f, 1f, 1f, 0.9f);
			var btn = btnGo.GetComponent<Button>();

			// Text label
			var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
			var trt = txtGo.GetComponent<RectTransform>();
			trt.SetParent(rt, false);
			trt.anchorMin = new Vector2(0, 0);
			trt.anchorMax = new Vector2(1, 1);
			trt.offsetMin = Vector2.zero;
			trt.offsetMax = Vector2.zero;
			var txt = txtGo.GetComponent<Text>();
			txt.text = label;
			txt.fontSize = 28;
			txt.alignment = TextAnchor.MiddleCenter;
			txt.color = Color.black;
			txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

			return btn;
		}
	}
}


