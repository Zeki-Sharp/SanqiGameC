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
    [SerializeField] private MapData mapData;
    
    [Header("地图状态")]
    [SerializeField] private bool[,] occupiedCells; // 记录哪些格子被占用
    [SerializeField] private Dictionary<Vector2Int, Block> placedBlocks = new Dictionary<Vector2Int, Block>();
    
    [Header("Tilemap可视化")]
    public Tilemap tilemap; // 拖拽赋值
    public TileBase groundTile; // 拖拽你的grass瓦片
    
    [Header("预制体生成区域")]
    [SerializeField,LabelText("塔的生成区域物体名")]
    private string towerAreaName = "TowerArea";
    private Transform towerArea;
    
    
    // 公共属性
    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;
    public float CellSize => cellSize;
    
    
    private void Awake()
    {
        InitializeScene();
        InitializeMap();
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
        }
        towerArea = GetTowerArea();
    }
#endif
    private void InitializeScene()
    { 
        towerArea = GetTowerArea();
        if (towerArea == null)
        {
            Debug.LogError("未找到塔生成区域");
        }
    }
    public Transform GetTowerArea()
    {
        if (towerArea == null)
        {
            towerArea = transform.Find(towerAreaName);
        }
        return towerArea;
    }
    
    /// <summary>
    /// 初始化地图
    /// </summary>
    private void InitializeMap()
    {
        occupiedCells = new bool[mapWidth, mapHeight];
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
        
        
        //创建中心塔
        GameObject centerTower = Instantiate(mapData.centerTower,towerArea);
        PlaceBlock(new Vector2Int(mapWidth / 2, mapHeight / 2), centerTower.GetComponent<Block>());
        Debug.Log($"地图初始化完成: {mapWidth}x{mapHeight}, 格子大小: {cellSize}");
    }
    // /// <summary>
    // /// 创建塔
    // /// </summary>
    // private void CreateTower(Vector3 position,GameObject prefab)
    // {
    //     
    // }
    /// <summary>
    /// 检查指定位置是否可以放置方块
    /// </summary>
    /// <param name="position">方块左下角的世界坐标</param>
    /// <param name="shape">方块形状</param>
    /// <returns>是否可以放置</returns>
    public bool CanPlaceBlock(Vector2Int position, BlockGenerationConfig config)
    {
        if (config == null || config.CellCount<=0)
            return false;
            
        foreach (Vector2Int coord in config.Coordinates)
        {
            Vector2Int worldCoord = position + coord;
            
            // 检查是否超出地图边界
            if (worldCoord.x < 0 || worldCoord.x >= mapWidth || 
                worldCoord.y < 0 || worldCoord.y >= mapHeight)
            {
                return false;
            }
            
            // 检查格子是否已被占用
            if (occupiedCells[worldCoord.x, worldCoord.y])
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
    public bool PlaceBlock(Vector2Int position, Block block)
    {
        if (block == null || block.Config == null)
        {
            Debug.LogError("方块或形状为空，无法放置");
            return false;
        }
        
        if (!CanPlaceBlock(position, block.Config))
        {
            Debug.LogWarning($"无法在位置 ({position.x}, {position.y}) 放置方块");
            return false;
        }
        
        // 标记格子为已占用
        foreach (Vector2Int coord in block.Config.Coordinates)
        {
            Vector2Int worldCoord = position + coord;
            occupiedCells[worldCoord.x, worldCoord.y] = true;
        }
        
        // 设置方块位置并添加到地图
        block.SetWorldPosition(position, tilemap);
        placedBlocks[position] = block;
        
        Debug.Log($"方块成功放置到位置 ({position.x}, {position.y})");
        return true;
    }
    
    /// <summary>
    /// 移除方块
    /// </summary>
    /// <param name="position">方块左下角的世界坐标</param>
    /// <returns>是否移除成功</returns>
    public bool RemoveBlock(Vector2Int position)
    {
        if (!placedBlocks.ContainsKey(position))
        {
            Debug.LogWarning($"位置 ({position.x}, {position.y}) 没有方块");
            return false;
        }
        
        Block block = placedBlocks[position];
        if (block.Config != null)
        {
            // 取消标记格子占用
            foreach (Vector2Int coord in block.Config.Coordinates)
            {
                Vector2Int worldCoord = position + coord;
                if (worldCoord.x >= 0 && worldCoord.x < mapWidth && 
                    worldCoord.y >= 0 && worldCoord.y < mapHeight)
                {
                    occupiedCells[worldCoord.x, worldCoord.y] = false;
                }
            }
        }
        
        // 销毁方块GameObject
        if (block.gameObject != null)
        {
            Destroy(block.gameObject);
        }
        
        placedBlocks.Remove(position);
        Debug.Log($"方块从位置 ({position.x}, {position.y}) 移除");
        return true;
    }
    
    /// <summary>
    /// 获取指定位置的方块
    /// </summary>
    /// <param name="position">世界坐标</param>
    /// <returns>方块实例，如果没有则返回null</returns>
    public Block GetBlockAt(Vector2Int position)
    {
        return placedBlocks.ContainsKey(position) ? placedBlocks[position] : null;
    }
    
    /// <summary>
    /// 检查指定位置是否被占用
    /// </summary>
    /// <param name="position">世界坐标</param>
    /// <returns>是否被占用</returns>
    public bool IsCellOccupied(Vector2Int position)
    {
        if (position.x < 0 || position.x >= mapWidth || 
            position.y < 0 || position.y >= mapHeight)
        {
            return true; // 超出边界视为被占用
        }
        
        return occupiedCells[position.x, position.y];
    }
    
    /// <summary>
    /// 将世界坐标转换为地图格子坐标
    /// </summary>
    /// <param name="worldPosition">世界坐标</param>
    /// <returns>地图格子坐标</returns>
    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        Vector3Int cell = tilemap.WorldToCell(worldPosition);
        return new Vector2Int(cell.x, cell.y);
    }
    
    /// <summary>
    /// 将地图格子坐标转换为世界坐标
    /// </summary>
    /// <param name="gridPosition">地图格子坐标</param>
    /// <returns>世界坐标</returns>
    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        // Tilemap的CellToWorld返回的是格子左下角，通常需要加上tilemap.cellSize/2才能到中心
        Vector3 cellOrigin = tilemap.CellToWorld(new Vector3Int(gridPosition.x, gridPosition.y, 0));
        return cellOrigin + tilemap.cellSize / 2f;
    }
    
    /// <summary>
    /// 获取地图上所有已放置的方块
    /// </summary>
    /// <returns>方块字典</returns>
    public Dictionary<Vector2Int, Block> GetAllPlacedBlocks()
    {
        return new Dictionary<Vector2Int, Block>(placedBlocks);
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
        if (tilemap == null) return;
        
        // 绘制地图边界
        Gizmos.color = Color.yellow;
        Vector3 min = tilemap.CellToWorld(new Vector3Int(0, 0, 0));
        Vector3 max = tilemap.CellToWorld(new Vector3Int(mapWidth, mapHeight, 0));
        Vector3 center = (min + max) / 2f;
        Vector3 size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), 0.1f);
        Gizmos.DrawWireCube(center, size);
        
        // 绘制被占用的格子
        Gizmos.color = Color.red;
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (occupiedCells != null && occupiedCells[x, y])
                {
                    Vector3 cellCenter = GridToWorldPosition(new Vector2Int(x, y));
                    Gizmos.DrawWireCube(cellCenter, tilemap.cellSize * 0.8f);
                }
            }
        }
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