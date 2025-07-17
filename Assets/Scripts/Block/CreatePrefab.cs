using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private BlockPlacementManager blockPlacementManager;

    // 公共属性
    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;
    public float CellSize => cellSize;



    public static CreatePrefab instance;

    private void Awake()
    {
        InitializeMap();
        if (instance == null) instance = this;  
        else Destroy(gameObject);
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
        
        // 计算中心点（考虑奇偶性）
        int centerX, centerY;
        
        // 对于X轴：如果宽度是偶数，选择右侧作为中心
        int totalWidth = bounds.size.x;
        if (totalWidth % 2 == 0) // 偶数
        {
            centerX = bounds.xMin + (totalWidth / 2);
            Debug.Log($"Tilemap宽度为偶数({totalWidth})，选择右侧格子作为X轴中心");
        }
        else // 奇数
        {
            centerX = bounds.xMin + (totalWidth / 2);
            Debug.Log($"Tilemap宽度为奇数({totalWidth})，选择中间格子作为X轴中心");
        }
        
        // 对于Y轴：如果高度是偶数，选择上侧作为中心
        int totalHeight = bounds.size.y;
        if (totalHeight % 2 == 0) // 偶数
        {
            centerY = bounds.yMin + (totalHeight / 2);
            Debug.Log($"Tilemap高度为偶数({totalHeight})，选择上侧格子作为Y轴中心");
        }
        else // 奇数
        {
            centerY = bounds.yMin + (totalHeight / 2);
            Debug.Log($"Tilemap高度为奇数({totalHeight})，选择中间格子作为Y轴中心");
        }
        
        Debug.Log($"Tilemap中心点坐标: ({centerX}, {centerY})");
        return new Vector3Int(centerX, centerY);
    }

    /// <summary>
    /// 将原始坐标转换为相对于Tilemap中心的新坐标
    /// </summary>
    private Vector3Int[] AdjustPositionsToTilemapCenter(Vector3Int[] originalPositions, Vector3Int tilemapCenter)
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
            Debug.Log($"塔组宽度为偶数({originalWidth})，几何中心X坐标调整到右侧");
        }
        else // 奇数
        {
            geometricCenterX = (maxX + minX) / 2;
        }
        
        // 如果高度是偶数，几何中心在上侧
        if (originalHeight % 2 == 0)
        {
            geometricCenterY = (maxY + minY + 1) / 2;
            Debug.Log($"塔组高度为偶数({originalHeight})，几何中心Y坐标调整到上侧");
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
        
        Debug.Log($"坐标调整完成: 原始范围({minX},{minY})-({maxX},{maxY}), " +
                  $"新中心({tilemapCenter.x},{tilemapCenter.y}), " +
                  $"几何中心({geometricCenterX},{geometricCenterY}), " +
                  $"应用偏移({offsetX},{offsetY})");
        return adjustedPositions;
    }
     /// <summary>
    /// 预览区域方块生成（自动将塔对齐到Tilemap格子中心）
    /// </summary>
    /// <param name="blockPrefab">方块预制体</param>
    /// <param name="towerDatas">塔数据列表</param>
    /// <param name="config">生成配置</param>
    public static BlockGenerationConfig lastPreviewConfig;
    public static List<TowerData> lastPreviewTowerDatas;
    public static Vector2Int lastPreviewAnchorOffset; // 记录原始左下角坐标
    public static Vector3Int[] lastPreviewOriginalPositions; // 记录原始相对坐标
    public static Vector3Int[] lastPreviewAdjustedPositions; // 记录调整后的坐标
    
    public void CreateBlock(GameObject blockPrefab, List<TowerData> towerDatas, BlockGenerationConfig config)
    {
        // 获取随机旋转后的配置副本（不修改原配置）
        BlockGenerationConfig rotatedConfig = config.GetRandomRotatedCopy();
        Debug.Log($"使用旋转配置: {rotatedConfig.name}");

        // 使用旋转后的配置坐标重新生成位置列表
        Vector3Int[] rotatedCoords = rotatedConfig.GetCellCoords(rotatedConfig.CellCount);
        List<Vector3Int> rotatedPositions = new List<Vector3Int>();
        foreach (var coord in rotatedCoords)
        {
            rotatedPositions.Add(new Vector3Int(coord.x, coord.y, 0));
        }
        
        Debug.Log($"旋转后坐标数量: {rotatedPositions.Count}");
        foreach (var pos in rotatedPositions)
        {
            Debug.Log($"旋转后坐标: ({pos.x}, {pos.y})");
        }
        
        // 计算Tilemap实际中心位置
        Vector3Int tilemapCenter = CalculateTilemapCenter();
        
        // 使用旋转后的坐标进行位置调整
        Vector3Int[] adjustedPositions2 = AdjustPositionsToTilemapCenter(rotatedPositions.ToArray(), tilemapCenter);
        Vector3Int[] adjustedPositions = new Vector3Int[adjustedPositions2.Length];
        for (int i = 0; i < adjustedPositions2.Length; i++)
        {
            adjustedPositions[i] = new Vector3Int(adjustedPositions2[i].x, adjustedPositions2[i].y, 0);
            Debug.Log($"旋转后原始坐标: ({adjustedPositions2[i].x},{adjustedPositions2[i].y}) " +
                      $"转换后坐标: ({adjustedPositions[i].x},{adjustedPositions[i].y})");
        }
        
        if (blockPlacementManager != null)
        {
             blockPlacementManager.PlaceTowerGroupAtPositions(adjustedPositions.ToList(), rotatedConfig, towerDatas,
                        prefabShowArea.transform,tilemap);
        }
        
        // 记录旋转后的左下角
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var pos in rotatedPositions)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.y < minY) minY = pos.y;
        }
        lastPreviewAnchorOffset = new Vector2Int(minX, minY);
        lastPreviewOriginalPositions = rotatedPositions.ToArray(); // 使用旋转后的坐标
        lastPreviewAdjustedPositions = adjustedPositions;
        lastPreviewConfig = rotatedConfig; // 使用旋转后的配置
        lastPreviewTowerDatas = towerDatas != null ? new List<TowerData>(towerDatas) : null;
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


    public void ClearShowArea()
    {
        lastPreviewConfig = null;
        lastPreviewTowerDatas = null;
        lastPreviewOriginalPositions = null;
        lastPreviewAdjustedPositions = null;
        if (prefabShowArea != null)
        {
            for (int i = prefabShowArea.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(prefabShowArea.transform.GetChild(i).gameObject);
            }
        }
    }


    /// <summary>
    /// 刷新showarea：清除原有内容并用上一次参数重建block/tower组
    /// </summary>
    public void RefreshShowArea()
    {
        ClearShowArea();
        BlockTestManager.instance.Test_01();

    }
}
