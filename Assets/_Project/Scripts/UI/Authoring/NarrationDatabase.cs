using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Narration/Database", fileName = "NarrationDatabase")]
public class NarrationDatabase : ScriptableObject
{
	[Serializable]
	public struct NarrationLine
	{
		public string Key;
		[TextArea]
		public string Text;
		public AudioClip Voice;
		public bool OverrideDuration;
		public float DurationSeconds;
	}

	public List<NarrationLine> Lines = new List<NarrationLine>();

	public bool TryGet(string key, out NarrationLine line)
	{
		for (int i = 0; i < Lines.Count; i++)
		{
			if (string.Equals(Lines[i].Key, key, StringComparison.Ordinal))
			{
				line = Lines[i];
				return true;
			}
		}
		line = default;
		return false;
	}
}


