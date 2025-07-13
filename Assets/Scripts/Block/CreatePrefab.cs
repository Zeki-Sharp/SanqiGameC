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
    
    [Header("摄像头")]
    [SerializeField] private Camera camera;
    
    [SerializeField] private bool hasClick;
    
    // 公共属性
    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;
    public float CellSize => cellSize;

    private void Awake()
    {
        InitializeMap();
    }
    public void ClickBuildingButton()
    {
        hasClick = true;
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
    /// 根据当前Tilemap范围计算中心点
    /// </summary>
    /// <returns>Tilemap中心点坐标</returns>
    public Vector3Int CalculateTilemapCenter()
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap未赋值，无法计算中心点");
            return Vector3Int.zero;
        }
        
        // 获取Tilemap的边界信息
        BoundsInt bounds = tilemap.cellBounds;
        
        // 计算中心点（整数坐标）
        int centerX = bounds.xMin + (bounds.size.x / 2);
        int centerY = bounds.yMin + (bounds.size.y / 2);
        
        Debug.Log($"Tilemap中心点坐标: ({centerX}, {centerY})");
        return new Vector3Int(centerX, centerY);
    }
     /// <summary>
    /// 预览区域方块生成（自动将塔对齐到Tilemap格子中心）
    /// </summary>
    /// <param name="blockPrefab">方块预制体</param>
    /// <param name="towerDatas">塔数据列表</param>
    /// <param name="positions">方块覆盖的格子坐标</param>
    /// <param name="config">生成配置</param>
    public static BlockGenerationConfig lastPreviewConfig;
    public static List<TowerData> lastPreviewTowerDatas;
    public static Vector2Int lastPreviewAnchorOffset; // 记录原始左下角坐标
    public static Vector2Int[] lastPreviewOriginalPositions; // 记录原始相对坐标
    public static Vector2Int[] lastPreviewAdjustedPositions; // 记录调整后的坐标
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
        
        // 根据原始坐标调整Tilemap大小
        // AdjustTilemapSize(positions);
        
        // 计算Tilemap实际中心位置
        Vector3Int tilemapCenter = CalculateTilemapCenter();
        
        // 替换positions数组为相对于Tilemap中心的新坐标
        Vector2Int[] adjustedPositions = AdjustPositionsToTilemapCenter(positions, tilemapCenter);
        
        // 记录原始左下角
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var pos in positions)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.y < minY) minY = pos.y;
        }
        lastPreviewAnchorOffset = new Vector2Int(minX, minY);
        lastPreviewOriginalPositions = positions;
        lastPreviewAdjustedPositions = adjustedPositions;
        
        // 记录父物体的初始位置
        Vector3 initialParentPosition = transform.position;
        
        // 实例化方块
        GameObject blockObj = Instantiate(blockPrefab, prefabShowArea.transform);
        Block block = blockObj.GetComponent<Block>();
       

        if (block == null)
        {
            Debug.LogError($"方块预制体缺少Block组件，对象：{blockObj.name}");
            Destroy(blockObj);
            return;
        }

        // 使用调整后的坐标生成塔
        if (towerDatas == null || towerDatas.Count == 0)
        {
            Debug.LogWarning("塔数据为空，仅生成基础方块");
            block.GenerateTowers(adjustedPositions, new TowerData[0], tilemap);
        }
        else
        {
            block.GenerateTowers(adjustedPositions, towerDatas.ToArray(), tilemap);
        } 
        
        // 对齐到Tilemap中心格子
        AlignObjectToTile(block, tilemapCenter, initialParentPosition);
        block.transform.parent.position = block.transform.parent.position + (Vector3)config.offset;
        // 注册到地图
        if (GameMap.instance == null)
        {
            Debug.LogError("GameMap单例未初始化");
            return;
        }
        // 缓存当前showarea塔组配置
        lastPreviewConfig = config;
        lastPreviewTowerDatas = towerDatas != null ? new List<TowerData>(towerDatas) : null;
    }
    
    /// <summary>
    /// 将原始坐标转换为相对于Tilemap中心的新坐标
    /// </summary>
    private Vector2Int[] AdjustPositionsToTilemapCenter(Vector2Int[] originalPositions, Vector3Int tilemapCenter)
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
        
        // 计算偏移量以使几何中心与Tilemap中心对齐
        int offsetX = tilemapCenter.x - ((maxX + minX) / 2);
        int offsetY = tilemapCenter.y - ((maxY + minY) / 2);
        
        // 创建新的坐标数组
        Vector2Int[] adjustedPositions = new Vector2Int[originalPositions.Length];
        for (int i = 0; i < originalPositions.Length; i++)
        {
            adjustedPositions[i] = new Vector2Int(originalPositions[i].x + offsetX, originalPositions[i].y + offsetY);
        }
        
        Debug.Log($"坐标调整完成: 原始范围({minX},{minY})-({maxX},{maxY}), 新中心({tilemapCenter.x},{tilemapCenter.y})");
        return adjustedPositions;
    }
    /// <summary>
    /// 将物体对齐到指定Tile的中心（精确计算）
    /// </summary>
    void AlignObjectToTile(Block block, Vector3Int targetTilePosition, Vector3 initialParentPosition)
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap未赋值，无法对齐物体");
            return;
        }

        // 获取目标格子的世界中心坐标
        Vector3 targetTileWorldPos = tilemap.GetCellCenterWorld(targetTilePosition);

        // 计算物体本地坐标系中心到pivot的偏移量
        Vector3 pivotOffset = CalculatePivotOffset(block);
        
        // 应用偏移量后的最终位置（增加额外精度补偿）
        Vector3 finalPosition = targetTileWorldPos - (pivotOffset + Vector3.up * 0.01f ); // 微调避免Z轴对齐问题
        
        // 设置物体位置
        block.transform.position = finalPosition;
        

        // 输出完整的调试信息
        Debug.Log($"物体已对齐到格子位置 {targetTilePosition}，世界坐标：{finalPosition}，应用Pivot偏移：{pivotOffset}" +
                  $"\n目标Tile世界中心：{targetTileWorldPos}" +
                  $"\n物体局部中心：{CalculatePivotOffset(block) + block.transform.localPosition}");
    }
    /// <summary>
    /// 计算物体本地坐标系中心到pivot的偏移量（支持无渲染器的父物体）
    /// </summary>
    private Vector3 CalculatePivotOffset(Block block)
    {
        // 获取物体及其所有子物体的渲染器组件
        Renderer[] renderers = block.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length > 0)
        {
            // 计算合并包围盒
            Bounds combinedBounds = new Bounds();
            bool boundsInitialized = false;
            
            foreach (Renderer renderer in renderers)
            {
                if (!boundsInitialized)
                {
                    combinedBounds = renderer.bounds;
                    boundsInitialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }
            
            // 计算本地坐标系中心
            Vector3 localCenter = block.transform.InverseTransformPoint(combinedBounds.center);
            
            // 计算pivot偏移量
            Vector3 pivotOffset = localCenter - block.transform.localPosition;
            
            return pivotOffset;
        }
        
        // 如果没有渲染器组件，返回零偏移
        Debug.LogWarning($"无法获取物体{block.name}的渲染器组件，使用默认pivot偏移");
        return Vector3.zero;
    }
}
