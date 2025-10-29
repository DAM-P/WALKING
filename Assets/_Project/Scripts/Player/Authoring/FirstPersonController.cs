using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
	[Header("Movement")]
	public float moveSpeed = 5f;

	[Header("Sprint")]
	public bool enableSprint = true;
	[Tooltip("按住 Shift 冲刺的速度倍率")] public float sprintMultiplier = 1.75f;
	[Tooltip("冲刺按键")] public KeyCode sprintKey = KeyCode.LeftShift;
	[Tooltip("是否允许在空中保持冲刺倍率")] public bool allowAirSprint = false;

	[Header("Jump & Gravity")]
	[Tooltip("重力（负值向下）")] public float gravity = -20f;
	[Tooltip("跳跃高度（米）")] public float jumpHeight = 1.5f;
	[Tooltip("是否在空中允许水平方向微调")]
	public bool allowAirControl = true;
	[Tooltip("落地时将竖直速度压到该值以保持贴地")]
	public float groundedStickVelocity = -2f;
	[Tooltip("使用控制器的 isGrounded 判定着地")]
	public bool useControllerIsGrounded = true;
	[Tooltip("可选自定义地面检测球半径（当 useControllerIsGrounded=false 时生效）")] public float groundCheckRadius = 0.2f;
	public Vector3 groundCheckLocalOffset = new Vector3(0f, 0.1f, 0f);
	public LayerMask groundLayers = ~0;

	[Header("Wall Jump")]
	[Tooltip("是否启用登墙/墙跳")] public bool enableWallJump = true;
	[Tooltip("检测墙面的最大距离（米）")] public float wallCheckDistance = 0.6f;
	[Tooltip("作为墙面参与检测的层级")] public LayerMask wallLayers = ~0;
	[Tooltip("沿墙下滑的最大速度（米/秒）")] public float wallSlideMaxSpeed = 5f;
	[Tooltip("墙跳向上的速度（米/秒）")] public float wallJumpUpSpeed = 6f;
	[Tooltip("墙跳离墙的水平速度（米/秒）")] public float wallJumpPushSpeed = 6f;
	[Tooltip("墙跳后锁定空中横向控制的时长（秒）")] public float wallJumpControlLockTime = 0.15f;
	[Tooltip("认为是墙的法线 y 分量上限（<=该值为垂直壁面）")] public float wallMaxUpDot = 0.2f;
	[Tooltip("墙面接触的记忆（Coyote）时间（秒）")] public float wallCoyoteTime = 0.15f;
	[Tooltip("墙跳产生的额外水平速度衰减率（每秒减少量）")] public float externalPlanarDecay = 8f;

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

	[Header("Camera Bob (Headbob)")]
	public bool enableHeadbob = true;
	[Tooltip("基础晃动幅度（米）")] public float headbobAmplitude = 0.03f;
	[Tooltip("基础晃动频率（Hz）")] public float headbobFrequency = 7.5f;
	[Tooltip("冲刺时幅度倍率")] public float sprintBobAmplitudeMul = 1.3f;
	[Tooltip("冲刺时频率倍率")] public float sprintBobFrequencyMul = 1.15f;
	[Tooltip("进入运动的权重增长速度")] public float headbobFadeInSpeed = 6f;
	[Tooltip("停止运动的权重衰减速度")] public float headbobFadeOutSpeed = 6f;

	[Header("Animation")]
	public Animator animator;
	public string forwardParam = "Forward"; // -1(后退) ~ 0(待机) ~ 1(前进)
	public string rightParam = "Right"; // -1(左移) ~ 0 ~ 1(右移)
	public float speedDampTime = 0.1f;

	CharacterController controller;
	float yaw;
	float pitch;
	float verticalVelocity;
	bool isGrounded;
	float lastPlanarSpeed;
	bool lastSprinting;
	float headbobTime;
	float headbobWeight; // 0..1
	Vector3 headbobOffset;

	// Wall jump state
	Vector3 lastWallNormal;
	float lastWallContactTime;
	Vector3 externalPlanarVelocity;
	float airControlLockedUntil;

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


		// Grounded check
		if (useControllerIsGrounded)
		{
			isGrounded = controller.isGrounded;
		}
		else
		{
			Vector3 checkOrigin = transform.TransformPoint(groundCheckLocalOffset);
			isGrounded = Physics.CheckSphere(checkOrigin, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
		}

		// Jump input
		bool jumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
		if (isGrounded && verticalVelocity < 0f)
		{
			verticalVelocity = groundedStickVelocity; // 小负值保证贴地
		}

		// 墙面检测（空中且启用墙跳时）
		bool canInteractWithWall = false;
		if (!isGrounded && enableWallJump)
		{
			Vector3 origin = controller != null ? controller.bounds.center : transform.position + Vector3.up * 1f;
			float radius = controller != null ? Mathf.Max(0.05f, controller.radius * 0.95f) : 0.3f;
			Vector2 moveInput = ReadMoveInput();
			Vector3 desiredDir = ComputePlanarMove(moveInput);
			float dirMag = desiredDir.magnitude;
			bool found = false;
			float bestDist = float.PositiveInfinity;
			Vector3 bestNormal = Vector3.zero;
			System.Action<Vector3> TryProbe = (Vector3 dir) =>
			{
				if (dir.sqrMagnitude < 1e-6f) return;
				dir.Normalize();
				if (Physics.SphereCast(origin, radius, dir, out var h, wallCheckDistance, wallLayers, QueryTriggerInteraction.Ignore))
				{
					if (h.normal.y <= wallMaxUpDot && h.distance < bestDist)
					{
						bestDist = h.distance;
						bestNormal = h.normal;
						found = true;
					}
				}
			};
			if (dirMag > 0.0001f) TryProbe(desiredDir);
			else
			{
				TryProbe(transform.forward);
				TryProbe(-transform.forward);
				TryProbe(transform.right);
				TryProbe(-transform.right);
			}
			if (found)
			{
				lastWallNormal = bestNormal;
				lastWallContactTime = Time.time;
			}
			canInteractWithWall = (Time.time - lastWallContactTime) <= wallCoyoteTime;
			if (canInteractWithWall && verticalVelocity < -wallSlideMaxSpeed)
			{
				verticalVelocity = -wallSlideMaxSpeed;
			}
		}

		if (isGrounded && jumpPressed)
		{
			verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
			airControlLockedUntil = 0f;
		}
		else if (!isGrounded && jumpPressed && enableWallJump && ((Time.time - lastWallContactTime) <= wallCoyoteTime))
		{
			verticalVelocity = wallJumpUpSpeed;
			externalPlanarVelocity = lastWallNormal * wallJumpPushSpeed;
			airControlLockedUntil = Time.time + wallJumpControlLockTime;
		}

		// Gravity integrate
		verticalVelocity += gravity * Time.deltaTime;

		bool sprinting = enableSprint && (ReadSprintInput());
		if (!isGrounded && !allowAirSprint)
		{
			sprinting = false;
		}
		float speedMul = sprinting ? sprintMultiplier : 1f;
		Vector2 moveInput2 = ReadMoveInput();
		Vector3 inputPlanarMove = ComputePlanarMove(moveInput2) * moveSpeed * speedMul;
		if (!isGrounded && (!allowAirControl || Time.time < airControlLockedUntil))
		{
			inputPlanarMove = Vector3.zero;
		}
		Vector3 planarMove = inputPlanarMove + externalPlanarVelocity;
		Vector3 motion = planarMove;
		motion.y = verticalVelocity;
		controller.Move(motion * Time.deltaTime);
		externalPlanarVelocity = Vector3.MoveTowards(externalPlanarVelocity, Vector3.zero, externalPlanarDecay * Time.deltaTime);

		lastPlanarSpeed = planarMove.magnitude;
		lastSprinting = sprinting;
		UpdateHeadbob(Time.deltaTime);
		UpdateAnimator(planarMove);
	}

	void LateUpdate()
	{
		if (cameraTransform == null) return;

		Vector3 desiredLocal = GetDesiredCameraLocalOffset();
		if (!cameraCollisionEnabled)
		{
			cameraTransform.localPosition = desiredLocal;
			return;
		}

		// 以枢轴点为起点，检测到理想相机位置的路径上是否发生碰撞
		Vector3 pivotWorld = transform.TransformPoint(cameraPivotLocalOffset);
		Vector3 desiredWorld = transform.TransformPoint(desiredLocal);
		Vector3 delta = desiredWorld - pivotWorld;
		float distance = delta.magnitude;
		if (distance <= 1e-4f)
		{
			cameraTransform.localPosition = desiredLocal;
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
			cameraTransform.localPosition = desiredLocal;
		}
	}

	Vector3 GetDesiredCameraLocalOffset()
	{
		if (!enableHeadbob || cameraTransform == null)
			return cameraLocalOffset;
		return cameraLocalOffset + headbobOffset;
	}

	void UpdateHeadbob(float dt)
	{
		if (!enableHeadbob || cameraTransform == null)
		{
			headbobOffset = Vector3.zero;
			return;
		}

		// 仅在冲刺且有移动速度并且落地时启用晃动
		bool moving = lastSprinting && lastPlanarSpeed > 0.05f && isGrounded;
		float target = moving ? 1f : 0f;
		float fadeSpeed = moving ? headbobFadeInSpeed : headbobFadeOutSpeed;
		headbobWeight = Mathf.MoveTowards(headbobWeight, target, fadeSpeed * dt);

		if (headbobWeight <= 0.0001f)
		{
			headbobOffset = Vector3.zero;
			return;
		}

		float amp = headbobAmplitude * (lastSprinting ? sprintBobAmplitudeMul : 1f);
		float freq = headbobFrequency * (lastSprinting ? sprintBobFrequencyMul : 1f);
		headbobTime += dt * freq;
		float twoPi = Mathf.PI * 2f;
		float y = Mathf.Sin(headbobTime * twoPi) * amp; // 垂直起伏
		float x = Mathf.Sin(headbobTime * twoPi * 2f) * (amp * 0.5f); // 轻微左右摆
		headbobOffset = new Vector3(x, y, 0f) * headbobWeight;
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

	bool ReadSprintInput()
	{
		// 同时兼容左右 Shift
		return Input.GetKey(sprintKey) || Input.GetKey(KeyCode.RightShift);
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


