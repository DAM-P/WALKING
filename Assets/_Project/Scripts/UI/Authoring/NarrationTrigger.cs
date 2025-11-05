using UnityEngine;

public class NarrationTrigger : MonoBehaviour
{
	public NarrationManager manager;
	public NarrationDatabase database;
	public string key;
	public string overrideText;
	public AudioClip overrideVoice;
	public float overrideDuration = -1f; // <0 则不用

	public bool playOnStart = false;
	public bool playOnEnable = false;
	public bool playOnTriggerEnter = true;
	public string requiredTag = "Player";
	public KeyCode testKey = KeyCode.None;

	[Header("One-shot")]
	[Tooltip("开启后，本组件在首次触发后将不再重复触发")] public bool triggerOnlyOnce = false;
	bool _hasTriggered = false;

	[Header("Debug/Utility")]
	public bool autoCreateManagerIfMissing = true;
	public bool debugLog = false;

	void Reset()
	{
		manager = FindObjectOfType<NarrationManager>();
	}

	void Start()
	{
		if (playOnStart) Enqueue();
	}

	void OnEnable()
	{
		if (playOnEnable) Enqueue();
	}

	void Update()
	{
		if (testKey != KeyCode.None && Input.GetKeyDown(testKey))
		{
			if (debugLog) Debug.Log($"[NarrationTrigger] testKey pressed: {testKey}", this);
			Enqueue();
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (!playOnTriggerEnter) return;
		if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
		Enqueue();
	}

	public void Enqueue()
	{
		if (triggerOnlyOnce && _hasTriggered) return;

		if (manager == null) manager = FindObjectOfType<NarrationManager>();
		if (manager == null && autoCreateManagerIfMissing)
		{
			var go = new GameObject("NarrationManager(Auto)");
			manager = go.AddComponent<NarrationManager>();
			if (debugLog) Debug.Log("[NarrationTrigger] Auto-created NarrationManager in scene.", this);
		}
		if (manager == null)
		{
			if (debugLog) Debug.LogWarning("[NarrationTrigger] No NarrationManager found. Cannot enqueue.", this);
			return;
		}
		bool didEnqueue = false;
		if (!string.IsNullOrEmpty(overrideText))
		{
			float? dur = overrideDuration >= 0f ? (float?)overrideDuration : null;
			manager.EnqueueText(overrideText, overrideVoice, dur);
			didEnqueue = true;
		}
		else if (!string.IsNullOrEmpty(key))
		{
			if (debugLog) Debug.Log($"[NarrationTrigger] EnqueueKey: {key}", this);
			manager.EnqueueKey(key, database);
			didEnqueue = true;
		}
		else
		{
			if (debugLog) Debug.LogWarning("[NarrationTrigger] Neither overrideText nor key provided.", this);
		}

		if (didEnqueue && triggerOnlyOnce) _hasTriggered = true;
	}
}


