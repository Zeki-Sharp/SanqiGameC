using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 治疗范围计算器
/// 根据治疗范围类型，计算指定格子位置周围需要治疗的目标
/// </summary>
public static class HealRangeCalculator
{
    /// <summary>
    /// 根据治疗范围类型获取需要检查的格子坐标
    /// </summary>
    /// <param name="centerCell">治疗塔所在的格子坐标</param>
    /// <param name="rangeType">治疗范围类型</param>
    /// <returns>需要检查的格子坐标列表</returns>
    public static List<Vector3Int> GetHealTargetCells(Vector3Int centerCell, HealRangeType rangeType)
    {
        var targetCells = new List<Vector3Int>();
        
        switch (rangeType)
        {
            case HealRangeType.None:
                // 无范围治疗，只治疗自己
                targetCells.Add(centerCell);
                break;
                
            case HealRangeType.Adjacent4:
                // 周围四格（上下左右）
                GetAdjacent4Cells(centerCell, targetCells);
                break;
        }
        
        return targetCells;
    }
    
    /// <summary>
    /// 获取周围四格坐标（上下左右）
    /// </summary>
    private static void GetAdjacent4Cells(Vector3Int center, List<Vector3Int> cells)
    {
        // 上
        cells.Add(center + Vector3Int.up);
        // 下
        cells.Add(center + Vector3Int.down);
        // 左
        cells.Add(center + Vector3Int.left);
        // 右
        cells.Add(center + Vector3Int.right);
    }
    
    /// <summary>
    /// 检查指定格子是否在治疗范围内
    /// </summary>
    /// <param name="centerCell">治疗塔所在的格子坐标</param>
    /// <param name="targetCell">目标格子坐标</param>
    /// <param name="rangeType">治疗范围类型</param>
    /// <returns>是否在治疗范围内</returns>
    public static bool IsInHealRange(Vector3Int centerCell, Vector3Int targetCell, HealRangeType rangeType)
    {
        if (rangeType == HealRangeType.None)
        {
            return centerCell == targetCell;
        }
        
        var targetCells = GetHealTargetCells(centerCell, rangeType);
        return targetCells.Contains(targetCell);
    }
}
