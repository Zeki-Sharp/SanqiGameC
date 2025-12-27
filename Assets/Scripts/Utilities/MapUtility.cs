using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 地图工具类
/// 
/// 提供地图相关操作和计算功能，专注于地图数据管理和验证。
/// 包含地图边界计算、瓦片操作、地图配置验证等功能。
/// 
/// 主要功能：
/// - 地图边界：获取世界坐标和cell坐标边界
/// - 瓦片操作：获取和设置瓦片数据
/// - 地图验证：配置验证、位置检查
/// - 地图信息：获取地图尺寸和配置
/// 
/// 使用示例：
/// ```csharp
/// // 获取地图边界
/// var (min, max) = MapUtility.GetTilemapWorldBounds(tilemap);
/// 
/// // 检查位置是否在地图范围内
/// bool inBounds = MapUtility.IsPositionInMapBounds(position, mapWidth, mapHeight);
/// 
/// // 获取瓦片数据
/// TileBase tile = MapUtility.GetTileAt(tilemap, cellPos);
/// ```
/// </summary>
public static class MapUtility
{
    #region 地图边界计算

    /// <summary>
    /// 获取Tilemap的边界世界坐标
    /// </summary>
    /// <param name="tilemap">Tilemap引用</param>
    /// <returns>边界的世界坐标范围</returns>
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

    /// <summary>
    /// 获取Tilemap的cell边界
    /// </summary>
    /// <param name="tilemap">Tilemap引用</param>
    /// <returns>cell边界</returns>
    public static BoundsInt GetTilemapCellBounds(Tilemap tilemap)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap未赋值，无法获取边界");
            return new BoundsInt();
        }
        
        return tilemap.cellBounds;
    }

    #endregion

    #region 地图验证

    /// <summary>
    /// 验证地图配置是否有效
    /// </summary>
    /// <param name="mapWidth">地图宽度</param>
    /// <param name="mapHeight">地图高度</param>
    /// <param name="cellSize">格子大小</param>
    /// <returns>是否有效</returns>
    public static bool ValidateMapConfig(int mapWidth, int mapHeight, float cellSize)
    {
        if (mapWidth <= 0)
        {
            Debug.LogError("地图宽度必须大于0");
            return false;
        }
        
        if (mapHeight <= 0)
        {
            Debug.LogError("地图高度必须大于0");
            return false;
        }
        
        if (cellSize <= 0)
        {
            Debug.LogError("格子大小必须大于0");
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// 验证位置是否在地图范围内
    /// </summary>
    /// <param name="position">要验证的位置</param>
    /// <param name="mapWidth">地图宽度</param>
    /// <param name="mapHeight">地图高度</param>
    /// <returns>是否在地图范围内</returns>
    public static bool IsPositionInMapBounds(Vector3Int position, int mapWidth, int mapHeight)
    {
        return position.x >= 0 && position.x < mapWidth &&
               position.y >= 0 && position.y < mapHeight;
    }

    #endregion

    #region 地图操作

    /// <summary>
    /// 获取指定位置的瓦片
    /// </summary>
    /// <param name="tilemap">Tilemap引用</param>
    /// <param name="position">位置</param>
    /// <returns>瓦片</returns>
    public static TileBase GetTileAt(Tilemap tilemap, Vector3Int position)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap未赋值，无法获取瓦片");
            return null;
        }
        
        return tilemap.GetTile(position);
    }

    /// <summary>
    /// 设置指定位置的瓦片
    /// </summary>
    /// <param name="tilemap">Tilemap引用</param>
    /// <param name="position">位置</param>
    /// <param name="tile">瓦片</param>
    public static void SetTileAt(Tilemap tilemap, Vector3Int position, TileBase tile)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap未赋值，无法设置瓦片");
            return;
        }
        
        tilemap.SetTile(position, tile);
    }

    #endregion
} 