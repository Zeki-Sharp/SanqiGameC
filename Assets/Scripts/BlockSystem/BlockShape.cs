using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BlockShape
{
    #region Old Version
    // [SerializeField] private Vector2Int[] coordinates;
    //
    // public Vector2Int[] Coordinates => coordinates;
    //
    // public BlockShape(Vector2Int[] coords)
    // {
    //     coordinates = coords;
    // }
    //
    // /// <summary>
    // /// 根据形状名称获取预定义的方块形状
    // /// </summary>
    // /// <param name="shapeName">形状名称</param>
    // /// <returns>对应的方块形状</returns>
    // public static BlockShape GetShape(string shapeName)
    // {
    //     switch (shapeName.ToUpper())
    //     {
    //         case "LINE2H": // 水平两格
    //             return new BlockShape(new Vector2Int[]
    //             {
    //                 new Vector2Int(0, 0),
    //                 new Vector2Int(1, 0)
    //             });
    //             
    //         case "LINE2V": // 垂直两格
    //             return new BlockShape(new Vector2Int[]
    //             {
    //                 new Vector2Int(0, 0),
    //                 new Vector2Int(0, 1)
    //             });
    //             
    //         case "L3": // L型三格
    //             return new BlockShape(new Vector2Int[]
    //             {
    //                 new Vector2Int(0, 0),
    //                 new Vector2Int(1, 0),
    //                 new Vector2Int(1, 1)
    //             });
    //             
    //         case "L3R": // 反向L型三格
    //             return new BlockShape(new Vector2Int[]
    //             {
    //                 new Vector2Int(0, 0),
    //                 new Vector2Int(1, 0),
    //                 new Vector2Int(0, 1)
    //             });
    //             
    //         case "SQUARE2": // 2x2方块
    //             return new BlockShape(new Vector2Int[]
    //             {
    //                 new Vector2Int(0, 0),
    //                 new Vector2Int(1, 0),
    //                 new Vector2Int(0, 1),
    //                 new Vector2Int(1, 1)
    //             });
    //             
    //         case "LINE3H": // 水平三格
    //             return new BlockShape(new Vector2Int[]
    //             {
    //                 new Vector2Int(0, 0),
    //                 new Vector2Int(1, 0),
    //                 new Vector2Int(2, 0)
    //             });
    //             
    //         case "T3": // T型三格
    //             return new BlockShape(new Vector2Int[]
    //             {
    //                 new Vector2Int(0, 0),
    //                 new Vector2Int(1, 0),
    //                 new Vector2Int(1, 1)
    //             });
    //             
    //         case "SINGLE": // 单格
    //             return new BlockShape(new Vector2Int[]
    //             {
    //                 new Vector2Int(0, 0)
    //             });
    //             
    //         default:
    //             Debug.LogWarning($"未知的形状名称: {shapeName}，返回单格形状");
    //             return new BlockShape(new Vector2Int[]
    //             {
    //                 new Vector2Int(0, 0)
    //             });
    //     }
    // }
    //
    // /// <summary>
    // /// 获取形状的边界大小
    // /// </summary>
    // /// <returns>形状的宽度和高度</returns>
    // public Vector2Int GetSize()
    // {
    //     if (coordinates == null || coordinates.Length == 0)
    //         return Vector2Int.zero;
    //         
    //     int maxX = 0, maxY = 0;
    //     foreach (var coord in coordinates)
    //     {
    //         maxX = Mathf.Max(maxX, coord.x);
    //         maxY = Mathf.Max(maxY, coord.y);
    //     }
    //     
    //     return new Vector2Int(maxX + 1, maxY + 1);
    // }
    //
    // /// <summary>
    // /// 获取所有可用的形状名称
    // /// </summary>
    // /// <returns>形状名称数组</returns>
    // public static string[] GetAvailableShapes()
    // {
    //     return new string[]
    //     {
    //         "SINGLE",
    //         "LINE2H",
    //         "LINE2V", 
    //         "LINE3H",
    //         "L3",
    //         "L3R",
    //         "T3",
    //         "SQUARE2"
    //     };
    // }
    #endregion
    // public BlockGenerationConfig blockGenerationConfig;
    // public string Name;
    
} 
public enum BlockShapeType
{
    SINGLE,
    LINE2H,
    LINE2V,
    LINE3H,
    L3,
    L3R,
    T3,
    SQUARE2,
    DEFAULT,
}