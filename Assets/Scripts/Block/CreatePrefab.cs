using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CreatePrefab : MonoBehaviour
{
    [Header("地图配置")]
    [SerializeField] private int mapWidth = 20;
    [SerializeField] private int mapHeight = 15;
    [SerializeField] private float cellSize = 1f;
    
    [Header("地图状态")]
    [SerializeField] private bool[,] occupiedCells; // 记录哪些格子被占用
    [SerializeField] private Dictionary<Vector2Int, Block> placedBlocks = new Dictionary<Vector2Int, Block>();

    
    [Header("Tilemap可视化")]
    public Tilemap tilemap; // 拖拽赋值
    public TileBase groundTile; // 拖拽你的grass瓦片
    
    [SerializeField] private GameObject prefabShowArea;
    
    // 公共属性
    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;
    public float CellSize => cellSize;

    private void Awake()
    {
        InitializeMap();
    }

    /// <summary>
    /// 初始化地图
    /// </summary>
    private void InitializeMap()
    {
        if (mapWidth <= 0 || mapHeight <= 0)
        {
            Debug.LogError("地图尺寸无效，宽度和高度必须大于0");
            return;
        }

        occupiedCells = new bool[mapWidth, mapHeight];
        placedBlocks.Clear();
        
        // 清空并填充Tilemap
        if (tilemap != null)
        {
            tilemap.ClearAllTiles();
            if (groundTile != null)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    for (int y = 0; y < mapHeight; y++)
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
                    }
                }
            }
            else
            {
                Debug.LogWarning("未分配地面瓦片，仅清空了Tilemap");
            }
        }
        else
        {
            Debug.LogWarning("Tilemap未赋值，无法初始化地图");
        }

        Debug.Log($"地图初始化完成: {mapWidth}x{mapHeight}, 格子大小: {cellSize}");
    }

    /// <summary>
    /// 预览区域方块生成
    /// </summary>
    /// <param name="blockPrefab">方块预制体</param>
    /// <param name="towerDatas">塔数据列表</param>
    /// <param name="positions">方块覆盖的格子坐标</param>
    /// <param name="config">生成配置</param>
    public void CreateBlock(GameObject blockPrefab, List<TowerData> towerDatas, Vector2Int[] positions, BlockGenerationConfig config)
    { 
        // 参数验证
        if (positions == null || positions.Length == 0)
        {
            Debug.LogError("无法生成方块：坐标数组为空或长度为0");
            return;
        }

        if (prefabShowArea == null)
        {
            Debug.LogError("预制体显示区域未赋值");
            return;
        }

        if (tilemap == null)
        {
            Debug.LogError("Tilemap未赋值，无法生成方块");
            return;
        }

        // 实例化方块
        GameObject blockObj = Instantiate(blockPrefab, prefabShowArea.transform);
        Block block = blockObj.GetComponent<Block>(); 

        if (block == null)
        {
            Debug.LogError($"方块预制体缺少Block组件，对象：{blockObj.name}");
            Destroy(blockObj);
            return;
        }

        // 计算几何中心
        float sumX = 0, sumY = 0;
        foreach (var pos in positions)
        {
            sumX += pos.x;
            sumY += pos.y;
        }

        Vector2Int center = new Vector2Int(Mathf.RoundToInt(sumX / positions.Length), Mathf.RoundToInt(sumY / positions.Length));
        // Vector3 vector3 = tilemap.GetCellCenterWorld(new Vector3Int(center.x, center.y, 0));
        // Debug.Log($"世界坐标: {vector3}");
        // 设置位置并初始化
    
        block.Init(config);

        // 生成塔
        if (towerDatas == null || towerDatas.Count == 0)
        {
            Debug.LogWarning("塔数据为空，仅生成基础方块");
            block.GenerateTowers(positions, new TowerData[0], tilemap);
        }
        else
        {
            block.GenerateTowers(positions, towerDatas.ToArray(), tilemap);
        }
    AlignObjectToTile(block,new Vector3Int(center.x, center.y, 0));
        // 注册到地图
        if (GameMap.instance == null)
        {
            Debug.LogError("GameMap单例未初始化");
            return;
        }

        // GameMap.instance.PlaceBlock(positions[positions.Length - 1], block,tilemap);
    }
    void AlignObjectToTile(Block block, Vector3Int targetTilePosition)
    {
        // 获取物体中心位置
        Vector3 objectCenter = transform.position;

        // 获取 Tilemap 第一格位置的世界坐标
        Vector3Int firstTilePosition = new Vector3Int(0, 0, 0); // 第一格（0,0）
        Vector3 firstTileWorldPos = tilemap.GetCellCenterWorld(firstTilePosition);

        // 计算偏移量
        Vector3 offset = objectCenter - firstTileWorldPos ;
        // offset.y += 0.5f;
        // 获取目标格子的世界坐标
        Vector3 targetTileWorldPos = tilemap.GetCellCenterWorld(targetTilePosition);

        // 计算物体的正确位置
        Vector3 correctedPosition = targetTileWorldPos + offset;

        // 设置物体的位置
        block.transform.position = correctedPosition;

        Debug.Log($"物体已对齐到格子位置 {targetTilePosition}，世界坐标：{correctedPosition}");
    }
}
