using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapUtility
{
    /// <summary>
    /// 根据当前Tilemap范围计算中心点
    /// </summary>
    /// <param name="tilemap">Tilemap引用</param>
    /// <returns>Tilemap中心点cell坐标</returns>
    /// <example>
    /// <code>
    /// // 计算Tilemap中心点，用于放置初始建筑或计算偏移
    /// Vector3Int center = TileMapUtility.CalculateTilemapCenter(tilemap);
    /// Debug.Log($"地图中心点: ({center.x}, {center.y})");
    /// 
    /// // 在中心点放置初始建筑
    /// Vector3 worldCenter = TileMapUtility.CellToWorldPosition(tilemap, center);
    /// Instantiate(buildingPrefab, worldCenter, Quaternion.identity);
    /// </code>
    /// </example>
    /// <remarks>
    /// 注意：返回的是cell坐标，如需世界坐标请使用CellToWorldPosition转换
    /// </remarks>
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
    /// <param name="originalPositions">原始cell坐标数组</param>
    /// <param name="tilemapCenter">Tilemap中心点cell坐标</param>
    /// <returns>调整后的cell坐标数组</returns>
    /// <example>
    /// <code>
    /// // 将建筑形状坐标调整到地图中心
    /// Vector3Int[] buildingShape = { new Vector3Int(0,0,0), new Vector3Int(1,0,0), new Vector3Int(0,1,0) };
    /// Vector3Int mapCenter = TileMapUtility.CalculateTilemapCenter(tilemap);
    /// Vector3Int[] adjustedShape = TileMapUtility.AdjustPositionsToTilemapCenter(buildingShape, mapCenter);
    /// 
    /// // 在调整后的位置放置建筑
    /// foreach (var pos in adjustedShape)
    /// {
    ///     Vector3 worldPos = TileMapUtility.CellToWorldPosition(tilemap, pos);
    ///     Instantiate(tilePrefab, worldPos, Quaternion.identity);
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// 注意：此方法用于将相对坐标调整为绝对坐标，常用于建筑预览和放置
    /// </remarks>
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

    /// <summary>
    /// 将世界坐标转换为Tilemap cell坐标
    /// </summary>
    /// <param name="tilemap">Tilemap引用</param>
    /// <param name="worldPosition">世界坐标</param>
    /// <returns>对应的cell坐标</returns>
    /// <example>
    /// <code>
    /// // 将鼠标世界坐标转换为cell坐标
    /// Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    /// mouseWorldPos.z = 0; // 确保Z轴为0
    /// Vector3Int cellPos = TileMapUtility.WorldToCellPosition(tilemap, mouseWorldPos);
    /// </code>
    /// </example>
    /// <remarks>
    /// 注意：确保传入的worldPosition的Z轴为0，否则可能影响转换精度
    /// </remarks>
    public static Vector3Int WorldToCellPosition(Tilemap tilemap, Vector3 worldPosition)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap未赋值，无法进行坐标转换");
            return Vector3Int.zero;
        }
        return tilemap.WorldToCell(worldPosition);
    }

    /// <summary>
    /// 将Tilemap cell坐标转换为世界坐标（格子中心）
    /// </summary>
    /// <param name="tilemap">Tilemap引用</param>
    /// <param name="cellPos">cell坐标</param>
    /// <returns>对应的世界坐标（格子中心）</returns>
    /// <example>
    /// <code>
    /// // 将cell坐标转换为世界坐标，用于放置物体到格子中心
    /// Vector3Int cellPos = new Vector3Int(5, 3, 0);
    /// Vector3 worldPos = TileMapUtility.CellToWorldPosition(tilemap, cellPos);
    /// transform.position = worldPos; // 将物体放置到格子中心
    /// </code>
    /// </example>
    /// <remarks>
    /// 注意：返回的是格子的中心点坐标，适合放置物体。如需格子左下角坐标，请使用CellToWorldPositionBottomLeft
    /// </remarks>
    public static Vector3 CellToWorldPosition(Tilemap tilemap, Vector3Int cellPos)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap未赋值，无法进行坐标转换");
            return Vector3.zero;
        }
        return tilemap.GetCellCenterWorld(cellPos);
    }

    /// <summary>
    /// 将Tilemap cell坐标转换为世界坐标（格子左下角）
    /// </summary>
    /// <param name="tilemap">Tilemap引用</param>
    /// <param name="cellPos">cell坐标</param>
    /// <returns>对应的世界坐标（格子左下角）</returns>
    /// <example>
    /// <code>
    /// // 获取格子的左下角坐标，用于计算边界或碰撞检测
    /// Vector3Int cellPos = new Vector3Int(5, 3, 0);
    /// Vector3 bottomLeft = TileMapUtility.CellToWorldPositionBottomLeft(tilemap, cellPos);
    /// Vector3 center = TileMapUtility.CellToWorldPosition(tilemap, cellPos);
    /// // 计算格子的右上角坐标
    /// Vector3 topRight = bottomLeft + tilemap.cellSize;
    /// </code>
    /// </example>
    /// <remarks>
    /// 注意：返回的是格子的左下角坐标，适合计算边界。如需格子中心坐标，请使用CellToWorldPosition
    /// </remarks>
    public static Vector3 CellToWorldPositionBottomLeft(Tilemap tilemap, Vector3Int cellPos)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap未赋值，无法进行坐标转换");
            return Vector3.zero;
        }
        return tilemap.CellToWorld(cellPos);
    }

    /// <summary>
    /// 获取Tilemap的边界世界坐标
    /// </summary>
    /// <param name="tilemap">Tilemap引用</param>
    /// <returns>边界的世界坐标范围</returns>
    /// <example>
    /// <code>
    /// // 获取Tilemap的边界范围，用于相机控制或UI布局
    /// var (min, max) = TileMapUtility.GetTilemapWorldBounds(tilemap);
    /// Debug.Log($"地图边界: 左下角({min.x}, {min.y}) 右上角({max.x}, {max.y})");
    /// 
    /// // 计算地图中心点
    /// Vector3 center = (min + max) / 2f;
    /// Camera.main.transform.position = center;
    /// </code>
    /// </example>
    /// <remarks>
    /// 注意：返回的是世界坐标范围，min为左下角，max为右上角。可用于相机控制、UI布局等
    /// </remarks>
    public static (Vector3 min, Vector3 max) GetTilemapWorldBounds(Tilemap tilemap)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap未赋值，无法获取边界");
            return (Vector3.zero, Vector3.zero);
        }
        
        BoundsInt bounds = tilemap.cellBounds;
        Vector3 worldMin = tilemap.CellToWorld(bounds.min);
        Vector3 worldMax = tilemap.CellToWorld(bounds.max);
        
        return (worldMin, worldMax);
    }
}
