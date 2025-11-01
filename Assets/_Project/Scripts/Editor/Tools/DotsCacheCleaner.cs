using UnityEditor;
using UnityEngine;
using System.IO;

namespace Project.EditorTools
{
	public static class DotsCacheCleaner
	{
		[MenuItem("Tools/DOTS/Clear LMDB Build Cache (fix MDB_READERS_FULL)")]
		public static void ClearLmdbCaches()
		{
			// Common DOTS/LMDB cache locations inside the project
			string[] paths = new[]
			{
				"Library/BuildCache",
				"Library/EntitiesCache",
				"Library/SceneDependencyCache",
				"Library/Bee"
			};

			int removed = 0;
			foreach (var p in paths)
			{
				if (Directory.Exists(p))
				{
					FileUtil.DeleteFileOrDirectory(p);
					removed++;
				}
			}
			AssetDatabase.Refresh();
			Debug.Log($"[DOTS] Cleared {removed} cache folders. If the editor still shows MDB_READERS_FULL, close the editor and run this again after restart.");
		}

		[MenuItem("Tools/DOTS/Open Cache Folder")]
		public static void OpenCacheFolder()
		{
			EditorUtility.RevealInFinder(Path.GetFullPath("Library"));
		}
	}
}



