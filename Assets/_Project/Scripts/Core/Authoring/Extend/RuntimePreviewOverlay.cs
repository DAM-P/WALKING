using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Authoring
{
	/// 运行时（打包后）伸缩预览渲染：使用 Graphics.DrawMesh 绘制半透明方块
	public class RuntimePreviewOverlay : MonoBehaviour
	{
		[Header("Render")]
		public Material previewMaterial;
		public Color previewColor = new Color(0.2f, 0.6f, 1f, 0.35f);
		public float queueOffset = 0f; // 可选：修改材质渲染队列偏移

		Mesh _cubeMesh;
		readonly List<(Vector3 center, Vector3 size, Color color)> _boxes = new List<(Vector3, Vector3, Color)>();

		void Awake()
		{
			EnsureMeshAndMaterial();
		}

		void EnsureMeshAndMaterial()
		{
			if (_cubeMesh == null)
			{
				var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
				var mf = temp.GetComponent<MeshFilter>();
				_cubeMesh = mf != null ? mf.sharedMesh : null;
				Destroy(temp); // 仅保留网格
			}
			if (previewMaterial == null)
			{
				var shader = Shader.Find("Universal Render Pipeline/Lit");
				if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
				previewMaterial = new Material(shader);
				previewMaterial.SetColor("_BaseColor", previewColor);
				previewMaterial.SetFloat("_Surface", 1f); // Transparent
				previewMaterial.SetFloat("_ZWrite", 1f);
				previewMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
				previewMaterial.renderQueue = 3000 + (int)queueOffset;
			}
		}

		public void Begin()
		{
			_boxes.Clear();
		}

		public void AddBox(Vector3 center, Vector3 size, Color? colorOverride = null)
		{
			_boxes.Add((center, size, colorOverride ?? previewColor));
		}

		public void End()
		{
			// no-op: 渲染在 LateUpdate 统一进行
		}

		void LateUpdate()
		{
			if (_boxes.Count == 0 || _cubeMesh == null || previewMaterial == null) return;
			for (int i = 0; i < _boxes.Count; i++)
			{
				var (c, s, col) = _boxes[i];
				previewMaterial.SetColor("_BaseColor", col);
				var m = Matrix4x4.TRS(c, Quaternion.identity, s);
				Graphics.DrawMesh(_cubeMesh, m, previewMaterial, 0);
			}
			_boxes.Clear();
		}
	}
}



