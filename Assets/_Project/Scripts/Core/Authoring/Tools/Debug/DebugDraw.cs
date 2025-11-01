using UnityEngine;

namespace Project.Core.Authoring.Debugging
{
	public static class DebugDraw
	{
		public enum Channel
		{
			ExtendPreview,
			Mantle,
			Crosshair,
			Selection,
			Raycast,
			General
		}

		// 默认：仅允许 ExtendPreview 在 Game 视图渲染
		public static bool onlyExtendPreview = true;
		public static bool showMantle = false;

		static bool IsAllowed(Channel channel)
		{
			if (!onlyExtendPreview) return true;
			if (channel == Channel.ExtendPreview) return true;
			if (channel == Channel.Mantle && showMantle) return true;
			return false;
		}

		public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0f, bool depthTest = true, Channel channel = Channel.General)
		{
			if (!IsAllowed(channel)) return;
			Debug.DrawLine(start, end, color, duration, depthTest);
		}

		public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration = 0f, bool depthTest = true, Channel channel = Channel.General)
		{
			if (!IsAllowed(channel)) return;
			Debug.DrawRay(start, dir, color, duration, depthTest);
		}
	}
}
















