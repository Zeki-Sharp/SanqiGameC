using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapUtility
{
/// <summary>
    /// 根据当前Tilemap范围计算中心点
    /// </summary>
    /// <returns>Tilemap中心点坐标</returns>
    public static Vector3Int CalculateTilemapCenter(Tilemap tilemap)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap未赋值，无法计算中心点");
            return Vector3Int.zero;
        }
        
        // 获取Tilemap的边界信息
        BoundsInt bounds = tilemap.cellBounds;
        
        // 计算中心点（考虑奇偶性）
        int centerX, centerY;
        
        // 对于X轴：如果宽度是偶数，选择右侧作为中心
        int totalWidth = bounds.size.x;
        if (totalWidth % 2 == 0) // 偶数
        {
            centerX = bounds.xMin + (totalWidth / 2);
            // Debug.Log($"Tilemap宽度为偶数({totalWidth})，选择右侧格子作为X轴中心");
        }
        else // 奇数
        {
            centerX = bounds.xMin + (totalWidth / 2);
            // Debug.Log($"Tilemap宽度为奇数({totalWidth})，选择中间格子作为X轴中心");
        }
        
        // 对于Y轴：如果高度是偶数，选择上侧作为中心
        int totalHeight = bounds.size.y;
        if (totalHeight % 2 == 0) // 偶数
        {
            centerY = bounds.yMin + (totalHeight / 2);
            // Debug.Log($"Tilemap高度为偶数({totalHeight})，选择上侧格子作为Y轴中心");
        }
        else // 奇数
        {
            centerY = bounds.yMin + (totalHeight / 2);
            // Debug.Log($"Tilemap高度为奇数({totalHeight})，选择中间格子作为Y轴中心");
        }
        
        // Debug.Log($"Tilemap中心点坐标: ({centerX}, {centerY})");
        return new Vector3Int(centerX, centerY);
    }

    /// <summary>
    /// 将原始坐标转换为相对于Tilemap中心的新坐标
    /// </summary>
    public static Vector3Int[] AdjustPositionsToTilemapCenter(Vector3Int[] originalPositions, Vector3Int tilemapCenter)
    {
        // 查找原始坐标范围
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        
        foreach (var pos in originalPositions)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }
        
        // 计算原始形状的尺寸
        int originalWidth = maxX - minX + 1;
        int originalHeight = maxY - minY + 1;
        
        // 计算几何中心
        int geometricCenterX, geometricCenterY;
        
        // 如果宽度是偶数，几何中心在右侧
        if (originalWidth % 2 == 0)
        {
            geometricCenterX = (maxX + minX + 1) / 2;
            // Debug.Log($"塔组宽度为偶数({originalWidth})，几何中心X坐标调整到右侧");
        }
        else // 奇数
        {
            geometricCenterX = (maxX + minX) / 2;
        }
        
        // 如果高度是偶数，几何中心在上侧
        if (originalHeight % 2 == 0)
        {
            geometricCenterY = (maxY + minY + 1) / 2;
            // Debug.Log($"塔组高度为偶数({originalHeight})，几何中心Y坐标调整到上侧");
        }
        else // 奇数
        {
            geometricCenterY = (maxY + minY) / 2;
        }
        
        // 计算偏移量以使几何中心与Tilemap中心对齐
        int offsetX = tilemapCenter.x - geometricCenterX;
        int offsetY = tilemapCenter.y - geometricCenterY;
        
        // 创建新的坐标数组
        Vector3Int[] adjustedPositions = new Vector3Int[originalPositions.Length];
        for (int i = 0; i < originalPositions.Length; i++)
        {
            adjustedPositions[i] = new Vector3Int(originalPositions[i].x + offsetX, originalPositions[i].y + offsetY);
        }
        
        // Debug.Log($"坐标调整完成: 原始范围({minX},{minY})-({maxX},{maxY}), " +
                  // $"新中心({tilemapCenter.x},{tilemapCenter.y}), " +
                  // $"几何中心({geometricCenterX},{geometricCenterY}), " +
                  // $"应用偏移({offsetX},{offsetY})");
        return adjustedPositions;
    }
}
