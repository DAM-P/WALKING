using UnityEngine;
using Unity.Entities;
using Project.Core.Components;

namespace Project.Core.Authoring
{
	/// 方向键秘籍：上 上 下 下 左 右 左 右 → 启用自由飞行
	public class FreeFlyCheatCode : MonoBehaviour
	{
		[Header("Cheat Sequence")]
		public KeyCode[] sequence = new KeyCode[]
		{
			KeyCode.UpArrow, KeyCode.UpArrow,
			KeyCode.DownArrow, KeyCode.DownArrow,
			KeyCode.LeftArrow, KeyCode.RightArrow,
			KeyCode.LeftArrow, KeyCode.RightArrow
		};
		[Tooltip("两次按键之间的最大间隔（秒），超时则重置")] public float maxStepInterval = 0.8f;
		[Tooltip("触发后是否自动重置以便再次触发")] public bool resetAfterTrigger = true;
		[Tooltip("触发后是否打印日志")] public bool debugLog = false;

		int _index = 0;
		float _lastTime = -999f;
		EntityManager _em;

		void Awake()
		{
			if (World.DefaultGameObjectInjectionWorld != null)
				_em = World.DefaultGameObjectInjectionWorld.EntityManager;
		}

		void Update()
		{
			if (sequence == null || sequence.Length == 0) return;
			if (Time.unscaledTime - _lastTime > maxStepInterval)
			{
				_index = 0; // 超时重置
			}

			if (_index < sequence.Length && Input.GetKeyDown(sequence[_index]))
			{
				_index++;
				_lastTime = Time.unscaledTime;
				if (_index >= sequence.Length)
				{
					TriggerFreeFly();
					_index = resetAfterTrigger ? 0 : sequence.Length; // 触发后可再次输入
				}
			}
			else
			{
				// 非期望键：若按了其他箭头键，且与首键相同则回到索引1，否则回到0
				if (Input.anyKeyDown)
				{
					// 忽略非键盘按键，只处理我们关心的方向键/常见键
					if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
					{
						_index = (sequence.Length > 0 && Input.GetKeyDown(sequence[0])) ? 1 : 0;
						_lastTime = Time.unscaledTime;
					}
				}
			}
		}

		void TriggerFreeFly()
		{
			if (_em == default) return;
			var e = _em.CreateEntity(typeof(EnableFreeFlyRequest));
			_em.SetComponentData(e, new EnableFreeFlyRequest { Enable = 1 });
			if (debugLog) Debug.Log("[FreeFlyCheatCode] Cheat matched → Enable FreeFly", this);
		}
	}
}






