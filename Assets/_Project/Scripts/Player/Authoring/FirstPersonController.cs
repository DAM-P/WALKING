using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
	[Header("Movement")]
	public float moveSpeed = 5f;

	[Header("Mouse Look")]
	public float lookSensitivity = 2f;
	public bool holdRightMouseToLook = false;
	public bool lockCursor = true;

	[Header("Camera")]
	public Transform cameraTransform;
	public Vector3 cameraLocalOffset = new Vector3(0f, 1.7f, 0f);
	[Tooltip("SphereCast 相机防穿墙开关")] public bool cameraCollisionEnabled = true;
	[Tooltip("用于检测的球半径")] public float cameraCollisionRadius = 0.08f;
	[Tooltip("与命中点保持的安全距离")] public float cameraCollisionSkin = 0.02f;
	[Tooltip("碰撞检测的层级遮罩")] public LayerMask cameraCollisionMask = ~0;
	[Tooltip("从该枢轴点(本地)向相机目标位置做 SphereCast")] public Vector3 cameraPivotLocalOffset = new Vector3(0f, 1.6f, 0f);

	[Header("Animation")]
	public Animator animator;
	public string forwardParam = "Forward"; // -1(后退) ~ 0(待机) ~ 1(前进)
	public string rightParam = "Right"; // -1(左移) ~ 0 ~ 1(右移)
	public float speedDampTime = 0.1f;

	CharacterController controller;
	float yaw;
	float pitch;

	void Awake()
	{
		controller = GetComponent<CharacterController>();
	}

	void Start()
	{
		var e = transform.eulerAngles;
		yaw = e.y;
		pitch = e.x;
		ApplyCursorState(lockCursor && !holdRightMouseToLook);

		if (cameraTransform == null && Camera.main != null)
		{
			cameraTransform = Camera.main.transform;
		}
		if (cameraTransform != null)
		{
			if (cameraTransform.parent != transform)
			{
				cameraTransform.SetParent(transform, false);
			}
			cameraTransform.localPosition = cameraLocalOffset;
			cameraTransform.localRotation = Quaternion.identity;
		}

		if (animator == null)
		{
			animator = GetComponentInChildren<Animator>();
		}
	}

	void OnDisable()
	{
		ApplyCursorState(false);
	}

	void Update()
	{
		bool looking = IsLookingActive();
		if (looking)
		{
			ApplyLook(GetLookDelta() * lookSensitivity);
		}

		if (lockCursor)
		{
			ApplyCursorState(looking);
		}

		Vector2 moveInput = ReadMoveInput();
		Vector3 planarMove = ComputePlanarMove(moveInput) * moveSpeed;
		controller.Move(planarMove * Time.deltaTime);

		UpdateAnimator(planarMove);
	}

	void LateUpdate()
	{
		if (cameraTransform == null) return;

		if (!cameraCollisionEnabled)
		{
			cameraTransform.localPosition = cameraLocalOffset;
			return;
		}

		// 以枢轴点为起点，检测到理想相机位置的路径上是否发生碰撞
		Vector3 pivotWorld = transform.TransformPoint(cameraPivotLocalOffset);
		Vector3 desiredWorld = transform.TransformPoint(cameraLocalOffset);
		Vector3 delta = desiredWorld - pivotWorld;
		float distance = delta.magnitude;
		if (distance <= 1e-4f)
		{
			cameraTransform.localPosition = cameraLocalOffset;
			return;
		}
		Vector3 dir = delta / distance;

		var hits = Physics.SphereCastAll(pivotWorld, cameraCollisionRadius, dir, distance + cameraCollisionSkin, cameraCollisionMask, QueryTriggerInteraction.Ignore);
		bool found = false;
		float nearest = float.PositiveInfinity;
		for (int i = 0; i < hits.Length; i++)
		{
			var h = hits[i];
			// 忽略自身与子层级
			if (h.collider != null && (h.collider.transform == transform || h.collider.transform.IsChildOf(transform))) continue;
			if (h.distance < nearest)
			{
				nearest = h.distance;
				found = true;
			}
		}

		if (found)
		{
			float safeDist = Mathf.Max(0f, nearest - cameraCollisionSkin);
			cameraTransform.position = pivotWorld + dir * safeDist;
		}
		else
		{
			cameraTransform.localPosition = cameraLocalOffset;
		}
	}

	bool IsLookingActive()
	{
		return !holdRightMouseToLook || Input.GetMouseButton(1);
	}

	Vector2 GetLookDelta()
	{
		return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
	}

	void ApplyLook(Vector2 lookDelta)
	{
		yaw += lookDelta.x;
		pitch -= lookDelta.y;
		pitch = Mathf.Clamp(pitch, -89f, 89f);
		// 水平旋转施加在玩家身上，俯仰施加在相机上
		transform.rotation = Quaternion.Euler(0f, yaw, 0f);
		if (cameraTransform != null)
		{
			cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
		}
	}

	Vector2 ReadMoveInput()
	{
		Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
		if (input.sqrMagnitude > 1f) input.Normalize();
		return input;
	}

	Vector3 ComputePlanarMove(Vector2 input)
	{
		return (transform.right * input.x) + (transform.forward * input.y);
	}

	void UpdateAnimator(Vector3 planarMove)
	{
		if (animator == null) return;
		float planarSpeed = planarMove.magnitude;
		float normalized = moveSpeed > 0f ? (planarSpeed / moveSpeed) : 0f; // 0..1

		// 计算前后/左右的有符号值：分别与角色前向、右向的点积乘以速度归一化
		float forwardSigned = 0f;
		float rightSigned = 0f;
		if (planarSpeed > 0.0001f)
		{
			Vector3 dir = planarMove.normalized;
			forwardSigned = Mathf.Clamp(Vector3.Dot(dir, transform.forward), -1f, 1f) * normalized; // [-1,1]
			rightSigned = Mathf.Clamp(Vector3.Dot(dir, transform.right), -1f, 1f) * normalized; // [-1,1]
		}
		if (!string.IsNullOrEmpty(forwardParam))
		{
			animator.SetFloat(forwardParam, forwardSigned, speedDampTime, Time.deltaTime);
		}
		if (!string.IsNullOrEmpty(rightParam))
		{
			animator.SetFloat(rightParam, rightSigned, speedDampTime, Time.deltaTime);
		}
	}

	void ApplyCursorState(bool shouldLock)
	{
		Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
		Cursor.visible = !shouldLock;
	}
}


