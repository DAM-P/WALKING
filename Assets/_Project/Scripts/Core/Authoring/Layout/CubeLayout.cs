using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Authoring
{
    [CreateAssetMenu(menuName = "Level/Cube Layout", fileName = "CubeLayout")]
    public class CubeLayout : ScriptableObject
    {
        [Tooltip("单元格尺寸（米），建议与游戏中的Cube尺寸一致，默认1m")] public float cellSize = 1f;
        [Tooltip("世界原点偏移（米），用于将栅格对齐到场景特定位置")] public Vector3 origin = Vector3.zero;

        [Serializable]
        public struct Cell
        {
            public Vector3Int coord; // 栅格坐标（整数）
            public int typeId;       // 类型/变体（0=默认）
            public Color color;      // 可选颜色（用于实例属性）
        }

        public List<Cell> cells = new List<Cell>();
    }
}






