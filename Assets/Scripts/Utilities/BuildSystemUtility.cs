using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 建造系统工具类
/// 
/// 提供建造系统专用功能，专注于塔防游戏的建造逻辑。
/// 包含建造区域计算、建造验证、资源检查等功能。
/// 
/// 主要功能：
/// - 建造区域：计算塔的建造区域
/// - 建造验证：检查建造位置的有效性
/// - 资源管理：检查建造资源是否足够
/// - 建造计算：建造成本、重叠检测
/// 
/// 使用示例：
/// ```csharp
/// // 计算建造区域
/// Vector3Int[] buildArea = BuildSystemUtility.CalculateTowerBuildArea(towerSize, centerPos);
/// 
/// // 验证建造位置
/// bool canBuild = BuildSystemUtility.ValidateBuildPositions(buildArea, tilemap, mapBounds);
/// 
/// // 检查资源
/// bool hasResources = BuildSystemUtility.HasEnoughResources(cost, playerResources);
/// ```
/// </summary>
public static class BuildSystemUtility
{
    #region 建造区域计算

    /// <summary>
    /// 计算塔的建造区域
    /// </summary>
    /// <param name="towerSize">塔的尺寸</param>
    /// <param name="centerPosition">中心位置</param>
    /// <returns>建造区域坐标数组</returns>
    public static Vector3Int[] CalculateTowerBuildArea(int towerSize, Vector3Int centerPosition)
    {
        if (towerSize <= 0)
        {
            Debug.LogError("塔的尺寸必须大于0");
            return new Vector3Int[0];
        }

        int halfSize = towerSize / 2;
        int startX = centerPosition.x - halfSize;
        int startY = centerPosition.y - halfSize;
        
        Vector3Int[] buildArea = new Vector3Int[towerSize * towerSize];
        int index = 0;
        
        for (int x = 0; x < towerSize; x++)
        {
            for (int y = 0; y < towerSize; y++)
            {
                buildArea[index] = new Vector3Int(startX + x, startY + y, 0);
                index++;
            }
        }
        
        return buildArea;
    }

    /// <summary>
    /// 计算塔的建造区域（基于世界坐标）
    /// </summary>
    /// <param name="towerSize">塔的尺寸</param>
    /// <param name="worldPosition">世界坐标中心位置</param>
    /// <param name="tilemap">Tilemap引用</param>
    /// <returns>建造区域坐标数组</returns>
    public static Vector3Int[] CalculateTowerBuildAreaFromWorld(int towerSize, Vector3 worldPosition, Tilemap tilemap)
    {
        Vector3Int centerCell = CoordinateUtility.WorldToCellPosition(tilemap, worldPosition);
        return CalculateTowerBuildArea(towerSize, centerCell);
    }

    #endregion

    #region 建造验证

    /// <summary>
    /// 验证建造位置是否有效
    /// </summary>
    /// <param name="buildPositions">建造位置数组</param>
    /// <param name="tilemap">Tilemap引用</param>
    /// <param name="mapBounds">地图边界</param>
    /// <returns>是否有效</returns>
    public static bool ValidateBuildPositions(Vector3Int[] buildPositions, Tilemap tilemap, BoundsInt mapBounds)
    {
        if (!CoordinateUtility.ValidatePositions(buildPositions))
        {
            return false;
        }

        foreach (var position in buildPositions)
        {
            // 检查是否在地图边界内
            if (!CoordinateUtility.IsPositionInBounds(position, mapBounds.min, mapBounds.max))
            {
                return false;
            }

            // 检查位置是否已被占用
            if (MapUtility.GetTileAt(tilemap, position) != null)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 检查建造位置是否与现有建筑重叠
    /// </summary>
    /// <param name="buildPositions">建造位置数组</param>
    /// <param name="existingBuildings">现有建筑位置数组</param>
    /// <returns>是否重叠</returns>
    public static bool CheckBuildOverlap(Vector3Int[] buildPositions, Vector3Int[] existingBuildings)
    {
        if (!CoordinateUtility.ValidatePositions(buildPositions) || 
            !CoordinateUtility.ValidatePositions(existingBuildings))
        {
            return false;
        }

        foreach (var buildPos in buildPositions)
        {
            foreach (var existingPos in existingBuildings)
            {
                if (buildPos == existingPos)
                {
                    return true; // 发现重叠
                }
            }
        }

        return false;
    }

    #endregion

    #region 建造成本计算

    /// <summary>
    /// 计算建造总成本
    /// </summary>
    /// <param name="baseCost">基础成本</param>
    /// <param name="towerSize">塔的尺寸</param>
    /// <param name="costMultiplier">成本倍数</param>
    /// <returns>总成本</returns>
    public static int CalculateBuildCost(int baseCost, int towerSize, float costMultiplier = 1f)
    {
        if (baseCost <= 0)
        {
            Debug.LogError("基础成本必须大于0");
            return 0;
        }

        if (towerSize <= 0)
        {
            Debug.LogError("塔的尺寸必须大于0");
            return 0;
        }

        int area = towerSize * towerSize;
        return Mathf.RoundToInt(baseCost * area * costMultiplier);
    }

    /// <summary>
    /// 验证是否有足够的资源进行建造
    /// </summary>
    /// <param name="requiredCost">所需成本</param>
    /// <param name="availableResources">可用资源</param>
    /// <returns>是否有足够资源</returns>
    public static bool HasEnoughResources(int requiredCost, int availableResources)
    {
        return availableResources >= requiredCost;
    }

    #endregion
} 