using UnityEngine;
using System.Collections.Generic;
using RaycastPro.Bullets2D;
using RaycastPro.Casters2D;
using Sirenix.OdinInspector;
using UnityEngine.Tilemaps;

public class GameMap : MonoBehaviour
{
    [Header("地图配置")] [SerializeField] private int mapWidth = 20;
    [SerializeField] private int mapHeight = 15;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private MapConfig mapConfig;

    [SerializeField] private DifficultyLevel difficultyLevel = DifficultyLevel.Easy;

    // 以cell坐标为key的占用字典
    [ShowInInspector] private OccupiedCellSet occupiedCells = new OccupiedCellSet();
    [ShowInInspector] private Dictionary<Vector3Int, Block> placedBlocks = new Dictionary<Vector3Int, Block>();

    [Header("Tilemap可视化")] public Tilemap tilemap; // 拖拽赋值
    public TileBase groundTile; // 拖拽你的grass瓦片
    [Header("交错地砖配置")]
    [SerializeField] private Tile groundTile1; // 地砖1
    [SerializeField] private Tile groundTile2; // 地砖2
    [SerializeField] private bool useAlternatingTiles = true; // 是否使用交错地砖

    [Header("预制体生成区域")] [SerializeField, LabelText("塔的生成区域物体名")]
    private string towerAreaName = "TowerArea";

    private Transform towerArea;
    private BulletManager bulletManager;

    // 移除传统单例模式，改为通过GameManager注册

    // 公共属性
    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;
    public float CellSize => cellSize;

    private void Awake()
    {
        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSystem(this);
        }
        bulletManager = GameManager.Instance.GetSystem<BulletManager>();
        if (!InitializeScene())
            return;

        InitializeMap();

        var (worldMin, worldMax) = MapUtility.GetTilemapWorldBounds(tilemap);
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
        if (tilemap != null)
        {
            if (useAlternatingTiles && groundTile1 != null && groundTile2 != null)
            {
                // 使用交错地砖模式
                FillMapWithAlternatingTiles();
            }
            else if (groundTile != null)
            {
                // 使用单一地砖模式（保持原有逻辑）
                FillMapWithSingleTile();
            }
            else
            {
                Debug.LogWarning("未分配地砖资源，无法初始化地图");
            }
        }

        // 创建中心塔
        GameObject centerTower = Instantiate(mapConfig.centerTower, towerArea);
        Vector3Int centerCell = CoordinateUtility.GetCenterCell(mapWidth, mapHeight); // 新增方法，返回cell坐标
        PlaceBlock(centerCell, centerTower.GetComponent<Block>());
        centerTower.GetComponent<BasicCaster2D>().poolManager = bulletManager.GetPoolManager();
        // 注意：中心塔的层级现在由SceneLayerManager统一管理
        // 不再需要手动设置
    }

    /// <summary>
    /// 使用单一地砖填充地图
    /// </summary>
    private void FillMapWithSingleTile()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
            }
        }
    }

    /// <summary>
    /// 使用交错地砖填充地图
    /// </summary>
    private void FillMapWithAlternatingTiles()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // 交错排列：棋盘格模式，相邻格子使用不同地砖
                TileBase tileToUse = ((x + y) % 2 == 0) ? groundTile1 : groundTile2;
                tilemap.SetTile(new Vector3Int(x, y, 0), tileToUse);
            }
        }
    }

    /// <summary>
    /// 检查指定位置是否可以放置方块
    /// </summary>
    /// <param name="cellPos">方块左下角的cell坐标</param>
    /// <param name="config">方块生成配置</param>
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
    /// <param name="cellPos">方块左下角的cell坐标</param>
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

        var coordinates = block.Config.GetCellCoords();
        
        if (coordinates == null || coordinates.Length == 0)
        {
            return false;
        }
        
        // 标记格子为已占用
        foreach (Vector2Int coord in coordinates)
        {
            Vector3Int cell = cellPos + new Vector3Int(coord.x, coord.y, 0);
            Debug.Log($"[GameMap Debug] 标记格子 {cell} 为已占用");
            

            var occupiedCell = new OccupiedCell(cell, !block.CanBeOverridden);
                    
            bool addResult = occupiedCells.Add(occupiedCell);
        }

        // 设置方块位置并添加到地图
        block.SetCellPosition(cellPos, tilemap);
        placedBlocks[cellPos] = block;

        Debug.Log($"[GameMap Debug] Block {block.name} 成功放置到cell位置 {cellPos}");
        Debug.Log($"[GameMap Debug] 当前occupiedCells数量: {occupiedCells.Count}");
        
        return true;
    }

    /// <summary>
    /// 移除方块
    /// </summary>
    /// <param name="cellPos">方块左下角的cell坐标</param>
    /// <param name="block">要移除的方块</param>
    /// <returns>是否移除成功</returns>
    public bool RemoveBlock(Vector3Int cellPos, Block block)
    {
        if (block == null || block.Config == null)
        {
            Debug.LogWarning($"方块或配置为空，无法移除");
            return false;
        }

        // 修复：使用GetCellCoords()而不是Coordinates
        var coordinates = block.Config.GetCellCoords();
        if (coordinates != null)
        {
            // 取消标记格子占用
            foreach (Vector2Int coord in coordinates)
            {
                Vector3Int cell = cellPos + new Vector3Int(coord.x, coord.y, 0);
                occupiedCells.Remove(cell);
            }
        }

        // 销毁方块GameObject
        if (block.gameObject != null && block.gameObject.scene.IsValid())
        {
            Destroy(block.gameObject);
        }

        placedBlocks.Remove(cellPos);
        return true;
    }

    /// <summary>
    /// 获取指定位置的方块
    /// </summary>
    /// <param name="cellPos">cell坐标</param>
    /// <returns>方块实例，如果没有则返回null</returns>
    public Block GetBlockAt(Vector3Int cellPos)
    {
        return placedBlocks.ContainsKey(cellPos) ? placedBlocks[cellPos] : null;
    }

    /// <summary>
    /// 检查指定位置是否被占用
    /// </summary>
    /// <param name="cellPos">cell坐标</param>
    /// <returns>是否被占用</returns>
    public bool IsCellOccupied(Vector3Int cellPos)
    {
        return occupiedCells.Contains(cellPos);
    }

    /// <summary>
    /// 将世界坐标转换为地图格子坐标
    /// </summary>
    /// <param name="worldPosition">世界坐标</param>
    /// <returns>地图格子坐标</returns>
    public Vector3Int WorldToCellPosition(Vector3 worldPosition)
    {
        return CoordinateUtility.WorldToCellPosition(tilemap, worldPosition);
    }

    /// <summary>
    /// 将地图格子坐标转换为世界坐标
    /// </summary>
    /// <param name="cellPos">cell坐标</param>
    /// <returns>世界坐标</returns>
    public Vector3 CellToWorldPosition(Vector3Int cellPos)
    {
        return CoordinateUtility.CellToWorldPosition(tilemap, cellPos);
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

    /// <summary>
    /// 刷新
    /// </summary>
    public void RefreshWithMoney()
    {
        var shopSystem = GameManager.Instance?.GetSystem<ShopSystem>();
        var itemManage = GameManager.Instance?.GetSystem<ItemManage>();
        
        if (shopSystem != null && itemManage != null && shopSystem.CanAfford(GetMapData().ItemRefreshMoney))
        {
            shopSystem.SpendMoney(GetMapData().ItemRefreshMoney);
            var previewAreaController = GameManager.Instance?.GetSystem<PreviewAreaController>();
            if (previewAreaController != null)
            {
                previewAreaController.RefreshShowArea();
            }
            itemManage.ShowItem();
        }
    }

    /// <summary>
    /// 重置地图到初始状态
    /// </summary>
    public void ResetMap()
    {
        // 清空并重新初始化地图
        ClearMap();
        
        // 重置预览区域
        var previewAreaController = GameManager.Instance?.GetSystem<PreviewAreaController>();
        if (previewAreaController != null)
        {
            previewAreaController.RefreshShowArea();
        }
        
        // 重置物品系统
        var itemManage = GameManager.Instance?.GetSystem<ItemManage>();
        if (itemManage != null)
        {
            itemManage.ShowItem(false);
        }
    }
}

[System.Serializable]
public class OccupiedCell
{
    public Vector3Int CellPosition;
    public bool IsOccupied;

    public OccupiedCell()
    {
    }

    public OccupiedCell(Vector3Int cellPosition, bool isOccupied)
    {
        CellPosition = cellPosition;
        IsOccupied = isOccupied;
    }

    public override bool Equals(object obj)
    {
        if (obj is OccupiedCell other)
        {
            return CellPosition.Equals(other.CellPosition);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return CellPosition.GetHashCode();
    }
}

public class OccupiedCellSet : HashSet<OccupiedCell>
{
    public new bool Add(OccupiedCell item)
    {
            
        bool result = base.Add(item);

        
        return result;
    }

    public bool Contains(Vector3Int position)
    {
        
        foreach (var cell in this)
        {
            if (cell.CellPosition == position)
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
            if (cell.CellPosition == position)
            {
                return base.Remove(cell);
            }
        }

        return false;
    }
}