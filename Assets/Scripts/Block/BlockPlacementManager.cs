using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class BlockPlacementManager : MonoBehaviour
{
    [Header("管理器引用")] [SerializeField] private GameMap gameMap;
    [SerializeField] private Camera mainCamera;

    [Header("方块配置")] [SerializeField] private GameObject blockPrefab; // 方块预制体
    [SerializeField] private string currentShapeName = "LINE2H"; // 当前选择的形状
    [SerializeField] private TowerData currentTowerData; // 当前选择的塔数据

    // 新建塔组预览系统数据
    [SerializeField] private BlockGenerationConfig currentBlockConfig;
    [SerializeField] private List<TowerData> currentTowerDatas = new List<TowerData>();
    [SerializeField] private List<GameObject> towerPreviewObjects = new List<GameObject>();
    [SerializeField] private List<Vector3Int> currentCells = new List<Vector3Int>();

    [Header("放置状态")] [SerializeField] private bool isPlacing = false; // 是否正在放置模式
    [SerializeField] private GameObject previewBlock; // 预览方块
    [SerializeField] private Vector3Int previewPosition; // 预览位置

    [Header("颜色配置")] [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color previewColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color canPlaceColor = Color.green;
    [SerializeField] private Color cannotPlaceColor = Color.red;
    [SerializeField] private Color canReplaceColor = Color.yellow;

    // 公共属性
    public bool IsPlacing => isPlacing;
    public string CurrentShapeName => currentShapeName;

    private GameObject towerPrefabCache;

    [Header("输入处理器")]
    [SerializeField] private BlockPlacementInputHandler inputHandler;

    [Header("预览系统")]
    [SerializeField] private BlockPreviewSystem previewSystem;

    private void Awake()
    {
        // 事件订阅必须在Awake中完成，确保在BlockTestManager.Start()执行前就准备好
        EventBus.Instance.Subscribe<BuildPreviewEventArgs>(OnBuildPreviewRequested);
    }

    private void Start()
    {
        // 自动获取组件引用
        if (gameMap == null)
            gameMap = FindFirstObjectByType<GameMap>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        // 缓存塔预制体
        towerPrefabCache = Resources.Load<GameObject>("Prefab/Tower/Tower");

        if (inputHandler == null)
            inputHandler = FindFirstObjectByType<BlockPlacementInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.OnPlaceBlockRequested += PlaceBlockAtPosition;
            inputHandler.OnCancelPlacementRequested += StopPlacement;
            inputHandler.OnPreviewPositionChanged += OnPreviewPositionChanged;
        }

        if (previewSystem == null)
            previewSystem = FindFirstObjectByType<BlockPreviewSystem>();
        if (previewSystem != null && gameMap != null && towerPrefabCache != null)
            previewSystem.Init(gameMap, towerPrefabCache);
    }

    private void OnDestroy()
    {
        if (inputHandler != null)
        {
            inputHandler.OnPlaceBlockRequested -= PlaceBlockAtPosition;
            inputHandler.OnCancelPlacementRequested -= StopPlacement;
            inputHandler.OnPreviewPositionChanged -= OnPreviewPositionChanged;
        }
        EventBus.Instance.Unsubscribe<BuildPreviewEventArgs>(OnBuildPreviewRequested);
    }

    private void OnBuildPreviewRequested(BuildPreviewEventArgs args)
    {
        PlaceTowerGroupAtPositions(args.Positions, args.Config, args.TowerDatas, args.Parent, args.Tilemap);
    }

    /// <summary>
    /// 开始放置模式
    /// </summary>
    /// <param name="config">塔组配置</param>
    /// <param name="towerDatas">塔数据列表</param>
    public void StartPlacement(BlockGenerationConfig config, List<TowerData> towerDatas)
    {
        if (gameMap == null)
        {
            Debug.LogError("GameMap未找到，无法开始放置");
            return;
        }

        isPlacing = true;
        currentBlockConfig = config;
        currentTowerDatas = towerDatas;
        if (inputHandler != null)
            inputHandler.StartPlacement();
        if (previewSystem != null)
            previewSystem.ShowPreview(config, towerDatas);
    }

    /// <summary>
    /// 停止放置模式
    /// </summary>
    public void StopPlacement()
    {
        isPlacing = false;
        if (inputHandler != null)
            inputHandler.StopPlacement();
        if (previewSystem != null)
            previewSystem.ClearPreview();
        FindFirstObjectByType<Preview_Click>()?.ResetClickState(); // 重置建造状态

        // 清理预览塔对象
        // foreach (var obj in towerPreviewObjects)
        // {
        //     if (obj != null)
        //     {
        //         Destroy(obj);
        //     }
        // }

        // towerPreviewObjects.Clear();

        // 销毁预览方块
        if (previewBlock != null)
        {
            Destroy(previewBlock);
            previewBlock = null;
        }
    }

    /// <summary>
    /// 更新塔组预览对象跟随鼠标
    /// </summary>
    // private void UpdatePreviewTowerGroupFollowMouse()
    // {
    //     if (!isPlacing || currentBlockConfig == null || currentTowerDatas == null) return;
    //     if (currentTowerDatas.Count != currentBlockConfig.Coordinates.Length) return;

    //     // Debug.Log("更新塔组预览对象跟随鼠标");
    //     GeneratePreviewTowers();

    //     Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
    //     mouseWorldPos.z = 0;
    //     Vector3Int baseGridPos = gameMap.WorldToGridPosition(mouseWorldPos);

    //     UpdatePreviewTowerPositions(baseGridPos);
    // }

    /// <summary>
    /// 改变预览塔的颜色
    /// </summary>
    /// <param name="color">目标颜色</param>
    /// <param name="single">单个对象</param>
    // public void ChangePreviewTowersColor(Color color, GameObject single = null)
    // {
    //     if (single != null)
    //     {
    //         SpriteRenderer sr = single.GetComponentInChildren<SpriteRenderer>();
    //         if (sr != null)
    //         {
    //             sr.color = color;
    //         }
    //         return;
    //     }

    //     foreach (var obj in towerPreviewObjects)
    //     {
    //         if (obj != null)
    //         {
    //             SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
    //             if (sr != null)
    //             {
    //                 sr.color = color;
    //             }
    //         }
    //     }
    // }

    /// <summary>
    /// 确保预览塔组生成
    /// </summary>
    // private void GeneratePreviewTowers()
    // {
    //     if (towerPreviewObjects.Count > 0 || towerPrefabCache == null) return;

    //     for (int i = 0; i < currentBlockConfig.Coordinates.Length; i++)
    //     {
    //         GameObject towerObj = Instantiate(towerPrefabCache, transform);
    //         towerObj.name = $"PreviewTower_{i}";
    //         towerObj.tag = "PreviewTower";
    //         Tower towerComponent = towerObj.GetComponent<Tower>();
    //         if (towerComponent != null)
    //         {
    //             towerComponent.Initialize(currentTowerDatas[i], currentBlockConfig.Coordinates[i]);
    //         }

    //         SpriteRenderer sr = towerObj.GetComponentInChildren<SpriteRenderer>();
    //         if (sr != null)
    //         {
    //             sr.color = previewColor;
    //             sr.sortingOrder = 1000;
    //         }

    //         towerPreviewObjects.Add(towerObj);
    //     }
    // }

    /// <summary>
    /// 更新预览塔组位置
    /// </summary>
    /// <param name="baseGridPos">基础cell位置</param>
    // private void UpdatePreviewTowerPositions(Vector3Int baseGridPos)
    // {
    //     if (gameMap.CanPlaceBlock(baseGridPos, currentBlockConfig))
    //     {
    //         ChangePreviewTowersColor(canPlaceColor);
    //     }
    //     else
    //     {
    //         ChangePreviewTowersColor(cannotPlaceColor);
    //     }

    //     for (int i = 0; i < currentBlockConfig.Coordinates.Length; i++)
    //     {
    //         Vector3Int offset = currentBlockConfig.Coordinates[i];
    //         Vector3Int cellPos = baseGridPos + new Vector3Int(offset.x, offset.y, 0);
    //         Vector3 worldPos = gameMap.GridToWorldPosition(cellPos);
    //         towerPreviewObjects[i].transform.position = worldPos;

    //         CheckPreviewTowerGroupBuildingStatus(worldPos, towerPreviewObjects[i]);
    //     }
    // }

    /// <summary>
    /// 获取塔组预览坐标的锚点（左下角 → 中心）
    /// </summary>
    private Vector3Int GetPreviewAnchorOffset()
    {
        if (PreviewAreaController.lastPreviewAdjustedPositions == null || PreviewAreaController.lastPreviewAdjustedPositions.Length == 0)
            return Vector3Int.zero;

        // 计算最小 x/y
        int minX = int.MaxValue;
        int minY = int.MaxValue;

        foreach (var pos in PreviewAreaController.lastPreviewAdjustedPositions)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.y < minY) minY = pos.y;
        }

        return new Vector3Int(minX, minY, 0);
    }

    /// <summary>
    /// 在指定位置放置方块
    /// </summary>
    /// <param name="position">地图格子坐标</param>
    private void PlaceBlockAtPosition(Vector3Int position)
    {
        if (gameMap == null) return;

        // 生成实际方块对象
        GameObject blockObject = new GameObject($"Block_{position.x}_{position.y}");
        Block block = blockObject.AddComponent<Block>();
        block.tag = "Block";
        // 初始化配置
        block.Init(currentBlockConfig);

        // 检查是否可以放置
        if (!gameMap.CanPlaceBlock(position, currentBlockConfig))
        {
            Debug.LogWarning($"无法在位置 ({position.x}, {position.y}) 放置方块");
            Destroy(blockObject);
            return;
        }

        // 放置方块到地图
        if (gameMap.PlaceBlock(position, block))
        {
            // Debug.Log($"方块成功放置到位置 ({position.x}, {position.y})");

            // 生成塔
            Tilemap tilemap = gameMap.GetTilemap();
            // Debug.Log($"生成塔: 配置坐标数量={currentBlockConfig.Coordinates.Length}, 塔数据数量={currentTowerDatas.Count}");
            block.GenerateTowers(currentBlockConfig.Coordinates, currentTowerDatas.ToArray(), tilemap ,true);

            // 停止放置模式并清除预览
            // foreach (var obj in towerPreviewObjects)
            // {
            //     Destroy(obj);
            // }

            // towerPreviewObjects.Clear();

            // 只在这里刷新 ShowArea
            if (PreviewAreaController.instance != null)
            {
                PreviewAreaController.instance.RefreshShowArea();
                FindFirstObjectByType<Preview_Click>()?.ResetClickState(); // 重置建造状态
            }

            StopPlacement();
        }
        else
        {
            Debug.LogError("放置失败！");
            Destroy(blockObject);
        }
    }

    /// <summary>
    /// 为方块的所有格子生成塔
    /// </summary>
    /// <param name="block">方块</param>
    /// <param name="towerData">塔数据</param>
    private void GenerateTowersForBlock(Block block, TowerData towerData)
    {
        if (block.Config == null) return;

        // 获取Tilemap引用
        Tilemap tilemap = gameMap != null ? gameMap.GetTilemap() : null;

        foreach (Vector2Int coord in block.Config.Coordinates)
        {
            // block.GenerateTower(coord, towerData, tilemap);
        }
    }

    /// <summary>
    /// 测试生成方块（用于调试）
    /// </summary>
    /// <param name="shapeName">形状名称</param>
    /// <param name="position">cell坐标位置</param>
    public void TestGenerateBlock(string shapeName, Vector3Int position)
    {
        if (gameMap == null)
        {
            Debug.LogError("GameMap未找到");
            return;
        }

        // 创建测试方块
        GameObject blockObject = new GameObject($"TestBlock_{shapeName}");
        Block block = blockObject.AddComponent<Block>();

        // 初始化方块
        block.Init(shapeName);

        // 放置方块
        if (gameMap.PlaceBlock(position, block))
        {
            // 生成测试塔数据
            TowerData testTowerData = CreateTestTowerData();

            // 为方块生成塔
            GenerateTowersForBlock(block, testTowerData);
        }
        else
        {
            Debug.LogError($"测试方块放置失败");
            Destroy(blockObject);
        }
    }

    /// <summary>
    /// 创建测试塔数据
    /// </summary>
    /// <returns>测试塔数据</returns>
    private TowerData CreateTestTowerData()
    {
        // 创建ScriptableObject实例
        TowerData testData = ScriptableObject.CreateInstance<TowerData>();

        // 通过反射设置私有字段（仅用于测试）
        var type = typeof(TowerData);
        var idField = type.GetField("id",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nameField = type.GetField("towerName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var healthField = type.GetField("health",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var attackField = type.GetField("physicAttack",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (idField != null) idField.SetValue(testData, 1);
        if (nameField != null) nameField.SetValue(testData, "测试塔");
        if (healthField != null) healthField.SetValue(testData, 100f);
        if (attackField != null) attackField.SetValue(testData, 25f);

        return testData;
    }

    [SerializeField] private LayerMask TowerLayerMask = 1 << 8;

    /// <summary>
    /// 检测预览塔组建造时的情况，并用颜色来表示
    /// </summary>
    /// <returns></returns>
    private void CheckPreviewTowerGroupBuildingStatus(Vector3 cellCenter, GameObject towerObject)
    {
        Collider2D[] towers = Physics2D.OverlapPointAll(cellCenter, TowerLayerMask);
        bool shouldUpdate = false;

        if (towers.Length > 0)
        {
            foreach (var collider in towers)
            {
                if (collider == null) continue;
                if (collider.CompareTag("PreviewTower")) continue;
                if (collider.TryGetComponent<Tower>(out var tower))
                {
                    if (towerObject.GetComponent<Tower>() == null || !tower.CompareTag("Tower")) continue;
                    if (collider.transform.position == towerObject.transform.position)
                    {
                        if (tower.TowerData != null &&
                            tower.TowerData.TowerName ==
                            towerObject.GetComponent<Tower>().TowerData.TowerName)
                        {
                            shouldUpdate = true;
                        }

                        if (shouldUpdate)
                        {
                            previewSystem.SetPreviewColor(canReplaceColor);
                        }
                        else
                        {
                            previewSystem.SetPreviewColor(canPlaceColor);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 通用塔组建造方法：在指定格子组建造塔组
    /// </summary>
    /// <param name="cells">塔组所有格子坐标（世界cell坐标）</param>
    /// <param name="config">塔组配置</param>
    /// <param name="towerDatas">每个塔的数据</param>
    /// <param name="parent">父物体（可选）</param>
    public void PlaceTowerGroupAtPositions(List<Vector3Int> cells, BlockGenerationConfig config,
        List<TowerData> towerDatas, Transform parent = null, Tilemap tilemap = null,bool hasCheck = false)
    {
        if (gameMap == null || config == null || cells == null || towerDatas == null || cells.Count != towerDatas.Count)
        {
            Debug.LogError("参数无效，无法建造塔组");
            return;
        }

        // 生成Block对象
        GameObject blockObj = new GameObject($"BlockGroup_{cells[0].x}_{cells[0].y}");
        if (parent != null) blockObj.transform.SetParent(parent, false); // 保证本地坐标不变
        blockObj.SetActive(true); // 强制激活
        Block block = blockObj.AddComponent<Block>();
        block.Init(config);
        block.ClearTower();
        block.tag = "Block";
        // 依次生成塔
        tilemap = tilemap != null ? tilemap : (gameMap != null ? gameMap.GetTilemap() : null);
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int cell = cells[i];
            TowerData data = towerDatas[i];
            GameObject towerPrefab = towerPrefabCache ?? Resources.Load<GameObject>("Prefab/Tower/Tower");
            if (towerPrefab == null)
            {
                Debug.LogError("塔预制体未找到");
                continue;
            }

            GameObject towerObj = Instantiate(towerPrefab, blockObj.transform);
            towerObj.SetActive(true); // 强制激活
            // 设置塔位置
            Vector3 worldPos = tilemap != null ? tilemap.GetCellCenterWorld(cell) : new Vector3(cell.x, cell.y, 0);
            towerObj.transform.position = worldPos;
            towerObj.tag = parent != null ? "PreviewTower" : "Tower";
            // 恢复正常颜色
            SpriteRenderer[] renderers = towerObj.GetComponentsInChildren<SpriteRenderer>(true);
            Tower towerComponent = towerObj.GetComponent<Tower>();
            const int BaseOrder = 1000;
            const int VerticalOffsetMultiplier = 10;
            int verticalOffset = Mathf.RoundToInt(-worldPos.y * VerticalOffsetMultiplier);
            int finalOrder = BaseOrder + verticalOffset;
            towerComponent.Initialize(data, new Vector3Int(cell.x, cell.y), hasCheck);
            towerComponent.SetOrder(finalOrder);

            block.SetTower(new Vector3Int(cell.x, cell.y), towerComponent);
            if (renderers != null && renderers.Length > 0)
            {
                foreach (var sr in renderers)
                {
                    sr.color = normalColor;
                    // 主动刷新SpriteRenderer，防止首次渲染丢失
                    sr.enabled = false;
                    sr.enabled = true;
                }
            }
        }
    }

    /// <summary>
    /// 清空地图（用于测试）
    /// </summary>
    public void ClearMap()
    {
        if (gameMap != null)
        {
            gameMap.ClearMap();
        }
    }

    // 新增：预览位置变化时刷新预览塔
    private void OnPreviewPositionChanged(Vector3 mouseWorldPos)
    {
        if (isPlacing && previewSystem != null && currentBlockConfig != null)
        {
            Vector3Int gridPos = TileMapUtility.WorldToCellPosition(gameMap.GetTilemap(), mouseWorldPos);
            previewSystem.UpdatePreview(gridPos);
        }
    }
}

