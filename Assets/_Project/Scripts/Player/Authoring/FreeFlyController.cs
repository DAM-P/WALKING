using UnityEngine;

/// 自由飞行控制（可选开启）：
/// - 按切换键启用/关闭（默认 F）
/// - WASD 前后左右，QE 上下；Shift 加速，Ctrl 减速
/// - 可选锁定光标；可滚轮调速
/// - 启用时会自动禁用 FirstPersonController 与 CharacterController；关闭时恢复
public class FreeFlyController : MonoBehaviour
{
	[Header("Toggle")]
	public bool startEnabled = false;
	public KeyCode toggleKey = KeyCode.F;
	public bool lockCursorWhenActive = true;

	[Header("Mouse Look")]
	public Transform cameraTransform;
	public float lookSensitivity = 2f;
	public float pitchMin = -89f;
	public float pitchMax = 89f;

	[Header("Move Speed")]
	public float moveSpeed = 8f;
	public float fastMultiplier = 3f;
	public float slowMultiplier = 0.3f;
	public float scrollSpeedStep = 1f;
	public float minSpeed = 1f;
	public float maxSpeed = 100f;

	float _yaw;
	float _pitch;
	bool _active;
	FirstPersonController _fpc;
	CharacterController _cc;

	void Awake()
	{
		_fpc = GetComponent<FirstPersonController>();
		_cc = GetComponent<CharacterController>();
		if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
		Vector3 e = transform.eulerAngles; _yaw = e.y; _pitch = e.x;
		SetActive(startEnabled, immediate:true);
	}

	void Update()
	{
		if (Input.GetKeyDown(toggleKey))
		{
			SetActive(!_active);
		}
		if (!_active) return;

		// 速度滚轮微调
		float scroll = Input.mouseScrollDelta.y;
		if (Mathf.Abs(scroll) > 0.001f)
		{
			moveSpeed = Mathf.Clamp(moveSpeed + scroll * scrollSpeedStep, minSpeed, maxSpeed);
		}

		// 视角
		Vector2 look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * lookSensitivity;
		_yaw += look.x; _pitch = Mathf.Clamp(_pitch - look.y, pitchMin, pitchMax);
		transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
		if (cameraTransform != null)
		{
			cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
		}

		// 移动
		Vector3 dir = Vector3.zero;
		dir += transform.forward * Input.GetAxisRaw("Vertical");
		dir += transform.right * Input.GetAxisRaw("Horizontal");
		if (Input.GetKey(KeyCode.E)) dir += Vector3.up;
		if (Input.GetKey(KeyCode.Q)) dir += Vector3.down;
		if (dir.sqrMagnitude > 1f) dir.Normalize();

		float speedMul = 1f;
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) speedMul *= fastMultiplier;
		if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) speedMul *= slowMultiplier;

		transform.position += dir * moveSpeed * speedMul * Time.deltaTime;
	}

	void OnDisable()
	{
		if (_active) SetActive(false, immediate:true);
	}

	void SetActive(bool enable, bool immediate=false)
	{
		_active = enable;
		if (_fpc != null) _fpc.enabled = !enable;
		if (_cc != null) _cc.enabled = !enable;
		if (lockCursorWhenActive)
		{
			Cursor.lockState = enable ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !enable;
		}
		// 立即对齐初始角度（避免闪跳）
		if (enable && cameraTransform != null)
		{
			Vector3 e = transform.eulerAngles; _yaw = e.y; _pitch = Mathf.Clamp(cameraTransform.localEulerAngles.x, pitchMin, pitchMax);
		}
	}

	// 供外部（Mono/ECS桥接）启用/关闭
	public void SetActiveExternal(bool enable)
	{
		SetActive(enable);
	}
}


