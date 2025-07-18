using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Tilemaps;

public class GameMap : MonoBehaviour
{
    [Header("地图配置")]
    [SerializeField] private int mapWidth = 20;
    [SerializeField] private int mapHeight = 15;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private MapConfig mapConfig;
    [SerializeField] private DifficultyLevel difficultyLevel = DifficultyLevel.Easy;
    // 移除tilemapOrigin
    // 所有API、判定、放置、Gizmos等全部用Tilemap的cell坐标(Vector3Int/x,y,z)
    // occupiedCells、placedBlocks等用cell坐标为key
    // WorldToGridPosition/ GridToWorldPosition直接用Tilemap的cell坐标

    // 以cell坐标为key的占用字典
  [ShowInInspector]  private OccupiedCellSet occupiedCells = new OccupiedCellSet();
  [ShowInInspector]  private Dictionary<Vector3Int, Block> placedBlocks = new Dictionary<Vector3Int, Block>();

    [Header("Tilemap可视化")]
    public Tilemap tilemap; // 拖拽赋值
    public TileBase groundTile; // 拖拽你的grass瓦片

    [Header("预制体生成区域")]
    [SerializeField, LabelText("塔的生成区域物体名")]
    private string towerAreaName = "TowerArea";
    private Transform towerArea;

    public static GameMap instance;
    private static readonly object lockObj = new object();

    // 公共属性
    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;
    public float CellSize => cellSize;

    private void Awake()
    {
        lock (lockObj)
        {
            if (instance == null)
                instance = this;
        }

        if (!InitializeScene())
            return;

        InitializeMap();
        Debug.Log(tilemap.cellSize);

        BoundsInt bounds = tilemap.cellBounds;
        Vector3 worldMin = tilemap.CellToWorld(bounds.min);
        Vector3 worldMax = tilemap.CellToWorld(bounds.max);
        // Debug.Log($"世界坐标范围: Min = {worldMin}，Max = {worldMax}");
    }

    public MapConfig GetMapConfig()
    {
        return mapConfig;
    }
    public MapData GetMapData()
    {
        return mapConfig.GetMapData(difficultyLevel);
    }

#if UNITY_EDITOR
    [Button("自动化生成/查找物体")]
    public void InitializeAndCreate()
    {
        towerArea = GetTowerArea();
        if (towerArea == null)
        {
            GameObject obj = new GameObject(towerAreaName);
            obj.transform.SetParent(this.transform.parent);
            towerArea = obj.transform;
        }
    }
#endif

    private bool InitializeScene()
    {
        towerArea = GetTowerArea();
        if (towerArea == null)
        {
            Debug.LogError("未找到塔生成区域");
            return false;
        }
        return true;
    }

    public Transform GetTowerArea()
    {
        if (towerArea == null)
        {
            towerArea = GameObject.Find(towerAreaName)?.transform;
        }
        return towerArea;
    }

    /// <summary>
    /// 初始化地图
    /// </summary>
    private void InitializeMap()
    {
        occupiedCells.Clear();
        placedBlocks.Clear();

        // 清空Tilemap
        if (tilemap != null)
            tilemap.ClearAllTiles();

        // 填充Tilemap
        if (tilemap != null && groundTile != null)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
                }
            }
        }

        Debug.Log($"地图初始化完成: {mapWidth}x{mapHeight}, 格子大小: {cellSize}");

        // 创建中心塔
        GameObject centerTower = Instantiate(mapConfig.centerTower, towerArea);
        Vector3Int centerCell = BaseUtility.GetCenterCell(mapWidth, mapHeight); // 新增方法，返回cell坐标
    // Debug.Log($"中心塔已创建,位置 {centerCell}");
        PlaceBlock(centerCell, centerTower.GetComponent<Block>());
        // Debug.Log($"中心塔已创建,位置 {centerCell}");
    }

    /// <summary>
    /// 检查指定位置是否可以放置方块
    /// </summary>
    /// <param name="position">方块左下角的世界坐标</param>
    /// <param name="shape">方块形状</param>
    /// <returns>是否可以放置</returns>
    public bool CanPlaceBlock(Vector3Int cellPos, BlockGenerationConfig config)
    {
        if (config == null || config.CellCount <= 0)
            return false;

        foreach (Vector2Int coord in config.Coordinates)
        {
            Vector3Int cell = cellPos + new Vector3Int(coord.x, coord.y, 0);

            // 检查是否超出地图边界
            if (!tilemap.HasTile(cell))
            {
                return false;
            }
            // Debug.Log($"  检查格子 ({cell.x},{cell.y}) 是否可放置 {!occupiedCells.Contains(cell)}");
            // 检查格子是否已被占用
            if (occupiedCells.Contains(cell))
            {
                return false;
            }
            
        }

        return true;
    }

    /// <summary>
    /// 放置方块到地图上
    /// </summary>
    /// <param name="position">方块左下角的世界坐标</param>
    /// <param name="block">要放置的方块</param>
    /// <returns>是否放置成功</returns>
    public bool PlaceBlock(Vector3Int cellPos, Block block)
    {
        if (block == null || block.Config == null)
        {
            Debug.LogError("方块或形状为空，无法放置");
            return false;
        }

        if (!CanPlaceBlock(cellPos, block.Config))
        {
            Debug.LogWarning($"无法在位置 ({cellPos.x}, {cellPos.y}) 放置方块");
            return false;
        }
        block.Config.GetCellCount(out var count);
        var coordinates = block.Config.GetCellCoords(count);
        // Debug.Log($"方块坐标: {coordinates}");
        // Debug.Log($"坐标大小: {coordinates.Length}");
        // 标记格子为已占用
        foreach (Vector2Int coord in coordinates)
        {
            Vector3Int cell = cellPos + new Vector3Int(coord.x, coord.y, 0);  // 修复：使用正确的相对坐标
            // Debug.Log($"占用格子: {cell}");
            occupiedCells.Add(new OccupiedCell(cell,!block.CanBeOverridden));
        }

        // 设置方块位置并添加到地图
        block.SetWorldPosition(cellPos, tilemap);
        placedBlocks[cellPos] = block;

        // Debug.Log($"方块成功放置到cell位置 {cellPos}");
        return true;
    }
  
    /// <summary>
    /// 放置方块到地图上
    /// </summary>
    /// <param name="position">方块左下角的世界坐标</param>
    /// <param name="block">要放置的方块</param>
    /// <returns>是否放置成功</returns>
    public bool PlaceBlock(Vector3Int cellPos, Block block,Tilemap tilemap)
    {
        if (block == null || block.Config == null)
        {
            Debug.LogError("方块或形状为空，无法放置");
            return false;
        }

        // if (!CanPlaceBlock(position, block.Config))
        // {
        //     Debug.LogWarning($"无法在位置 ({position.x}, {position.y}) 放置方块");
        //     return false;
        // }

        // 标记格子为已占用
        foreach (Vector2Int coord in block.Config.Coordinates)
        {
            Vector3Int cell = cellPos + new Vector3Int(coord.x, coord.y, 0);  // 修复：使用正确的相对坐标
            occupiedCells.Add(new OccupiedCell(cell,!block.CanBeOverridden));
        }

        // 设置方块位置并添加到地图
        block.SetWorldPosition(cellPos, tilemap);
        placedBlocks[cellPos] = block;

        Debug.Log($"方块成功放置到cell位置 {cellPos}");
        return true;
    }
    /// <summary>
    /// 移除方块
    /// </summary>
    /// <param name="position">方块左下角的世界坐标</param>
    /// <returns>是否移除成功</returns>
    public bool RemoveBlock(Vector3Int cellPos)
    {
        if (!placedBlocks.ContainsKey(cellPos))
        {
            Debug.LogWarning($"cell位置 {cellPos} 没有方块");
            return false;
        }

        Block block = placedBlocks[cellPos];
        if (block.Config != null && block.Config.Coordinates != null)
        {
            // 取消标记格子占用
            foreach (Vector2Int coord in block.Config.Coordinates)
            {
                Vector3Int cell = cellPos + new Vector3Int(coord.x, coord.y, 0);  // 修复：使用正确的相对坐标
                occupiedCells.Remove(cell);
            }
        }

        // 销毁方块GameObject
        if (block.gameObject != null && block.gameObject.scene.IsValid())
        {
            Destroy(block.gameObject);
        }

        placedBlocks.Remove(cellPos);
        Debug.Log($"方块从cell位置 {cellPos} 移除");
        return true;
    }


    /// <summary>
    /// 获取指定位置的方块
    /// </summary>
    /// <param name="position">世界坐标</param>
    /// <returns>方块实例，如果没有则返回null</returns>
    public Block GetBlockAt(Vector3Int cellPos)
    {
        return placedBlocks.ContainsKey(cellPos) ? placedBlocks[cellPos] : null;
    }

    /// <summary>
    /// 检查指定位置是否被占用
    /// </summary>
    /// <param name="position">世界坐标</param>
    /// <returns>是否被占用</returns>
    public bool IsCellOccupied(Vector3Int cellPos)
    {
        Debug.Log($"检查cell位置 {cellPos} 是否被占用");
        return occupiedCells.Contains(cellPos);
    }

    /// <summary>
    /// 将世界坐标转换为地图格子坐标
    /// </summary>
    /// <param name="worldPosition">世界坐标</param>
    /// <returns>地图格子坐标</returns>
    public Vector3Int WorldToGridPosition(Vector3 worldPosition)
    {
        return tilemap.WorldToCell(worldPosition);
    }

    /// <summary>
    /// 将地图格子坐标转换为世界坐标
    /// </summary>
    /// <param name="gridPosition">地图格子坐标</param>
    /// <returns>世界坐标</returns>
    public Vector3 GridToWorldPosition(Vector3Int cellPos)
    {
        return tilemap.GetCellCenterWorld(cellPos);
    }

    /// <summary>
    /// 获取地图上所有已放置的方块
    /// </summary>
    /// <returns>方块字典</returns>
    public Dictionary<Vector3Int, Block> GetAllPlacedBlocks()
    {
        return new Dictionary<Vector3Int, Block>(placedBlocks);
    }

    /// <summary>
    /// 清空地图
    /// </summary>
    public void ClearMap()
    {
        // 销毁所有方块
        foreach (var block in placedBlocks.Values)
        {
            if (block != null && block.gameObject != null)
            {
                Destroy(block.gameObject);
            }
        }

        // 重新初始化
        InitializeMap();
        Debug.Log("地图已清空");
    }

    /// <summary>
    /// 在Scene视图中绘制地图边界（仅用于调试）
    /// </summary>
    private void OnDrawGizmos()
    {
        // Gizmos.color = Color.red;
        // Gizmos.DrawWireCube(tilemap.GetCellCenterWorld(new Vector3Int(0, 0, 0)), tilemap.cellSize * 0.5f);
    }

    /// <summary>
    /// 获取Tilemap引用（供其他脚本使用）
    /// </summary>
    /// <returns>Tilemap组件</returns>
    public Tilemap GetTilemap()
    {
        return tilemap;
    }
}

[System.Serializable]
public class OccupiedCell
{
    public Vector3Int Position;
    public bool IsOccupied;

    public OccupiedCell()
    {
    }

    public OccupiedCell(Vector3Int position, bool isOccupied)
    {
        Position = position;
        IsOccupied = isOccupied;
    }

    public override bool Equals(object obj)
    {
        if (obj is OccupiedCell other)
        {
            return Position.Equals(other.Position);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Position.GetHashCode();
    }
}

public class OccupiedCellSet : HashSet<OccupiedCell>
{
    public bool Contains(Vector3Int position)
    {
        foreach (var cell in this)
        {
            if (cell.Position == position)
            {
                return cell.IsOccupied;
            }
        }
        return false;
    }

    public bool Remove(Vector3Int position)
    {
        foreach (var cell in this)
        {
            if (cell.Position == position)
            {
                return base.Remove(cell);
            }
        }
        return false;
    }
}
