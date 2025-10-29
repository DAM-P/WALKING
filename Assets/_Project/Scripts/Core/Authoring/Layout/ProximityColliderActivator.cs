using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Authoring
{
	/// <summary>
	/// 仅在距离目标一定范围内启用碰撞体，超出范围禁用，以降低碰撞开销。
	/// 适用于由 CubeLayoutColliderGenerator 生成的大量 BoxCollider。
	/// </summary>
	public class ProximityColliderActivator : MonoBehaviour
	{
		[Header("Activation Target")]
		[Tooltip("作为范围中心的目标（例如玩家或相机）。为空时自动使用 Camera.main")] public Transform target;
		[Tooltip("是否搜索子物体中的 Collider（生成器若放在父物体并把 Collider 生成在子物体时开启）")] public bool includeChildren = false;

		[Header("Distance (meters)")]
		[Tooltip("当与目标距离小于该值时启用 Collider")] public float enableRadius = 40f;
		[Tooltip("当与目标距离大于该值时禁用 Collider（需大于等于启用半径，形成迟滞避免抖动）")] public float disableRadius = 55f;

		[Header("Update Budget")]
		[Tooltip("轮询的时间间隔（秒），越大越省性能但响应稍慢")] public float updateInterval = 0.2f;
		[Tooltip("每帧最多检查/切换的碰撞体数量（分片更新避免卡顿）")] public int maxChecksPerFrame = 128;

		[Header("Debug")] public bool drawRadiiGizmos = true;

		readonly List<Collider> _colliders = new List<Collider>();
		int _scanIndex;
		float _timer;

		void OnEnable()
		{
			RefreshColliders();
			if (target == null && Camera.main != null) target = Camera.main.transform;
			ClampConfig();
		}

		void OnValidate()
		{
			ClampConfig();
		}

		void ClampConfig()
		{
			if (disableRadius < enableRadius) disableRadius = enableRadius;
			if (maxChecksPerFrame < 8) maxChecksPerFrame = 8;
			if (updateInterval < 0.02f) updateInterval = 0.02f;
		}

		public void RefreshColliders()
		{
			_colliders.Clear();
			if (includeChildren)
			{
				GetComponentsInChildren(true, _colliders);
			}
			else
			{
				GetComponents(_colliders);
			}
			_scanIndex = 0;
		}

		void Update()
		{
			_timer += Time.deltaTime;
			if (_timer < updateInterval) return;
			_timer = 0f;

			if (target == null)
			{
				if (Camera.main != null) target = Camera.main.transform;
				if (target == null) return;
			}

			if (_colliders.Count == 0)
			{
				RefreshColliders();
				if (_colliders.Count == 0) return;
			}

			Vector3 targetPos = target.position;
			float enableSqr = enableRadius * enableRadius;
			float disableSqr = disableRadius * disableRadius;

			int budget = Mathf.Min(maxChecksPerFrame, _colliders.Count);
			for (int i = 0; i < budget; i++)
			{
				if (_scanIndex >= _colliders.Count) _scanIndex = 0;
				var col = _colliders[_scanIndex++];
				if (col == null) continue;

				// 使用 Collider.ClosestPoint 近似到碰撞体表面，计算与目标的最近距离
				Vector3 closest = col.ClosestPoint(targetPos);
				float sqr = (closest - targetPos).sqrMagnitude;

				bool shouldEnable = col.enabled ? sqr <= disableSqr : sqr <= enableSqr;
				if (col.enabled != shouldEnable)
				{
					col.enabled = shouldEnable;
				}
			}
		}

		void OnDrawGizmosSelected()
		{
			if (!drawRadiiGizmos) return;
			var center = (target != null) ? target.position : transform.position;
			Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
			Gizmos.DrawWireSphere(center, enableRadius);
			Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
			Gizmos.DrawWireSphere(center, disableRadius);
		}
	}
}


