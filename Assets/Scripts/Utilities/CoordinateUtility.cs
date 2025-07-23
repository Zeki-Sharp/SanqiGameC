using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 坐标系统工具类
/// 
/// 提供Tilemap坐标转换、计算和验证功能，是建造系统的核心坐标处理工具。
/// 包含世界坐标与cell坐标的相互转换、地图中心计算、坐标调整等功能。
/// 
/// 主要功能：
/// - 坐标转换：世界坐标 ↔ cell坐标
/// - 地图计算：中心点计算、边界验证
/// - 坐标调整：相对坐标转绝对坐标
/// - 坐标验证：范围检查、有效性验证
/// 
/// 使用示例：
/// ```csharp
/// // 世界坐标转cell坐标
/// Vector3Int cellPos = CoordinateUtility.WorldToCellPosition(tilemap, worldPos);
/// 
/// // cell坐标转世界坐标（格子中心）
/// Vector3 worldPos = CoordinateUtility.CellToWorldPosition(tilemap, cellPos);
/// 
/// // 计算地图中心
/// Vector3Int center = CoordinateUtility.CalculateTilemapCenter(tilemap);
/// ```
/// </summary>
public static class CoordinateUtility
{
    #region Tilemap坐标计算

    /// <summary>
    /// 根据当前Tilemap范围计算中心点
    /// </summary>
    /// <param name="tilemap">Tilemap引用</param>
    /// <returns>Tilemap中心点cell坐标</returns>
    public static Vector3Int CalculateTilemapCenter(Tilemap tilemap)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap未赋值，无法计算中心点");
            return Vector3Int.zero;
        }
        
        BoundsInt bounds = tilemap.cellBounds;
        
        // 计算中心点（考虑奇偶性）
        int centerX = bounds.xMin + (bounds.size.x / 2);
        int centerY = bounds.yMin + (bounds.size.y / 2);
        
        return new Vector3Int(centerX, centerY, 0);
    }

    /// <summary>
    /// 计算矩形区域的中心坐标（向上取整，适用于奇数尺寸）
    /// </summary>
    /// <param name="width">矩形宽度</param>
    /// <param name="height">矩形高度</param>
    /// <returns>中心点cell坐标</returns>
    public static Vector3Int GetCenterCell(int width, int height)
    {
        int centerX = Mathf.FloorToInt(width / 2f);
        int centerY = Mathf.FloorToInt(height / 2f);
        return new Vector3Int(centerX, centerY, 0);
    }


    /// <summary>
    /// 预测抛物线运动的落点坐标
    /// </summary>
    /// <param name="origin">抛射起点的世界坐标</param>
    /// <param name="initialVelocity">初始速度向量（x,y方向）</param>
    /// <param name="map">Tilemap地图引用，用于检测碰撞</param>
    /// <returns>命中位置的cell坐标，若未命中返回(-999, -999, 0)</returns>
    public static Vector3 PredictParabolaImpact(Vector3 origin,float initialHeight, Vector2 initialVelocity,Tilemap map)
    {
        if (map == null)
        {
            Debug.LogError("Tilemap未赋值，无法进行坐标转换");
            return Vector3.zero;
        }
        
        Vector3 pos = origin;
        Vector2 velocity = initialVelocity;
        float currentHeight = initialHeight;
        Vector2 gravity = Physics2D.gravity;
        float timeStep = 0.05f;
        float maxTime = 5f;

        for (float t = 0; t < maxTime; t += timeStep)
        {
            // 水平移动（X/Y）
            pos += (Vector3)(velocity * timeStep);

            // 高度更新（Z轴或模拟的“抛物高度”）
            currentHeight += velocity.y * timeStep;
            velocity -= gravity * timeStep;

            // 判断是否到达地面（高度 <= 0）
            if (currentHeight <= origin.y)
            {
                Debug.Log( "命中");
                Vector3Int cell = map.WorldToCell(pos);
                if (map.HasTile(cell))
                    return pos;
            }
        }

        // 如果循环结束仍未找到命中点
        return new Vector3(-999, -999, 0); // 表示没命中
    }

    #endregion

    #region 坐标转换

    /// <summary>
    /// 将世界坐标转换为Tilemap cell坐标
    /// </summary>
    /// <param name="tilemap">Tilemap引用</param>
    /// <param name="worldPosition">世界坐标</param>
    /// <returns>对应的cell坐标</returns>
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
    /// 将世界坐标偏移量转换为cell坐标
    /// </summary>
    /// <param name="offset">世界坐标偏移量</param>
    /// <returns>对应的cell坐标</returns>
    public static Vector3Int WorldOffsetToCell(Vector3 offset)
    {
        // 使用更精确的四舍五入计算
        int x = Mathf.RoundToInt(offset.x);
        int y = Mathf.RoundToInt(offset.y);
        
        // 添加更精确的格子对齐逻辑
        // 如果偏移量接近整数（误差在0.1以内），视为对齐
        if (Mathf.Abs(x - offset.x) < 0.1f) x = (int)offset.x;
        if (Mathf.Abs(y - offset.y) < 0.1f) y = (int)offset.y;
        
        return new Vector3Int(x, y, 0);
    }

    #endregion

    #region 坐标调整和计算

    /// <summary>
    /// 将原始坐标转换为相对于Tilemap中心的新坐标
    /// </summary>
    /// <param name="originalPositions">原始cell坐标数组</param>
    /// <param name="tilemapCenter">Tilemap中心点cell坐标</param>
    /// <returns>调整后的cell坐标数组</returns>
    public static Vector3Int[] AdjustPositionsToTilemapCenter(Vector3Int[] originalPositions, Vector3Int tilemapCenter)
    {
        if (originalPositions == null || originalPositions.Length == 0)
        {
            Debug.LogWarning("原始坐标数组为空，无法调整");
            return new Vector3Int[0];
        }

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
        int geometricCenterX = (maxX + minX) / 2;
        int geometricCenterY = (maxY + minY) / 2;
        
        // 如果宽度是偶数，几何中心在右侧
        if (originalWidth % 2 == 0)
        {
            geometricCenterX = (maxX + minX + 1) / 2;
        }
        
        // 如果高度是偶数，几何中心在上侧
        if (originalHeight % 2 == 0)
        {
            geometricCenterY = (maxY + minY + 1) / 2;
        }
        
        // 计算偏移量以使几何中心与Tilemap中心对齐
        int offsetX = tilemapCenter.x - geometricCenterX;
        int offsetY = tilemapCenter.y - geometricCenterY;
        
        // 创建新的坐标数组
        Vector3Int[] adjustedPositions = new Vector3Int[originalPositions.Length];
        for (int i = 0; i < originalPositions.Length; i++)
        {
            adjustedPositions[i] = new Vector3Int(originalPositions[i].x + offsetX, originalPositions[i].y + offsetY, 0);
        }
        
        return adjustedPositions;
    }

    #endregion

    #region 坐标验证

    /// <summary>
    /// 验证坐标是否在有效范围内
    /// </summary>
    /// <param name="position">要验证的坐标</param>
    /// <param name="minBounds">最小边界</param>
    /// <param name="maxBounds">最大边界</param>
    /// <returns>是否在有效范围内</returns>
    public static bool IsPositionInBounds(Vector3Int position, Vector3Int minBounds, Vector3Int maxBounds)
    {
        return position.x >= minBounds.x && position.x <= maxBounds.x &&
               position.y >= minBounds.y && position.y <= maxBounds.y;
    }

    /// <summary>
    /// 验证坐标数组是否有效
    /// </summary>
    /// <param name="positions">坐标数组</param>
    /// <returns>是否有效</returns>
    public static bool ValidatePositions(Vector3Int[] positions)
    {
        return positions != null && positions.Length > 0;
    }

    #endregion
} 