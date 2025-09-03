using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using RaycastPro.Detectors2D;
using RaycastPro.Casters2D;

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


    [Header("放置状态")] [SerializeField] private bool isPlacing = false; // 是否正在放置模式
    [SerializeField] private GameObject previewBlock; // 预览方块
    [SerializeField] private Vector3Int previewPosition; // 预览位置

    [Header("颜色配置")] 
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color previewColor = new Color(1f, 1f, 1f, 0.5f);
    // 移除未使用的颜色配置，预览颜色管理已由BlockPreviewSystem负责

    // 公共属性
    public bool IsPlacing => isPlacing;
    public string CurrentShapeName => currentShapeName;

    private GameObject towerPrefabCache;

    [Header("输入处理器")]
    [SerializeField] private BlockPlacementInputHandler inputHandler;

    // 预览系统 - 通过GameManager自动获取
    private BlockPreviewSystem PreviewSystem => GameManager.Instance?.GetSystem<BlockPreviewSystem>();

    private void Awake()
    {
       
        EventBus.Instance.Subscribe<BuildPreviewEventArgs>(OnBuildPreviewRequested);
    }

    private void Start()
    {
        InitializeComponents();
        LoadTowerPrefab();
        SetupInputHandler();
        InitializePreviewSystem();
    }

    private void InitializeComponents()
    {
        // 自动获取组件引用
        if (gameMap == null)
        {
            gameMap = FindFirstObjectByType<GameMap>();
            if (gameMap == null)
                Debug.LogError("找不到GameMap组件");
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                Debug.LogError("找不到主相机");
        }
    }

    private void LoadTowerPrefab()
    {
        try
        {
            if (towerPrefabCache != null)
            {
                Debug.Log($"塔预制体已加载: {towerPrefabCache.name}");
                return;
            }

            // 尝试多个可能的预制体路径
            string[] possiblePaths = new string[]
            {
                "Prefabs/Tower/Tower",
                "Prefab/Tower/Tower",
                "Tower/Tower",
                "Towers/Tower",
                "Tower",
                "Prefabs/Tower",
                "Prefab/Tower",
                "Towers/BasicTower",
                "Tower/BasicTower",
                "Prefabs/Towers/BasicTower",
                "Prefab/Towers/BasicTower"
            };
            
            Debug.Log("开始搜索塔预制体，可能的路径：\n" + string.Join("\n", possiblePaths));

            // 首先检查是否已经有引用
            if (blockPrefab != null && blockPrefab.GetComponent<Tower>() != null)
            {
                towerPrefabCache = blockPrefab;
                Debug.Log($"使用已有的塔预制体引用: {blockPrefab.name}");
                return;
            }

            // 然后尝试从Resources加载
            foreach (string path in possiblePaths)
            {
                Debug.Log($"尝试从路径加载：{path}");
                var prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    var tower = prefab.GetComponent<Tower>();
                    if (tower != null)
                    {
                        towerPrefabCache = prefab;
                        Debug.Log($"成功加载塔预制体：{path}");
                        
                        // 验证预制体的关键组件
                        var spriteRenderer = prefab.GetComponentInChildren<SpriteRenderer>();
                        var rangeDetector = prefab.GetComponentInChildren<RangeDetector2D>();
                        var bulletCaster = prefab.GetComponent<BasicCaster2D>();
                        
                        Debug.Log($"预制体组件检查：\n" +
                                $"- SpriteRenderer: {(spriteRenderer != null ? "存在" : "缺失")}\n" +
                                $"- RangeDetector2D: {(rangeDetector != null ? "存在" : "缺失")}\n" +
                                $"- BasicCaster2D: {(bulletCaster != null ? "存在" : "缺失")}");
                        
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"在路径 {path} 找到预制体，但缺少Tower组件");
                    }
                }
            }

            string errorMsg = "无法加载塔预制体！\n" +
                "请检查以下内容：\n" +
                "1. 确保塔预制体已经放在Resources文件夹下的某个位置\n" +
                "2. 预制体的名字是否正确（可能的路径）：\n" + 
                string.Join("\n", possiblePaths) + "\n" +
                "3. 预制体上是否有Tower组件\n" +
                "4. 如果路径或名字不同，请修改BlockPlacementManager中的possiblePaths数组";
            
            Debug.LogError(errorMsg);
            
    #if UNITY_EDITOR
            // 在编辑器中显示更详细的信息
            UnityEditor.EditorUtility.DisplayDialog(
                "错误：找不到塔预制体",
                errorMsg,
                "确定");
    #endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载塔预制体时发生错误: {e.Message}\n{e.StackTrace}");
        }
        // 只在这里刷新 ShowArea
    }

    private void SetupInputHandler()
    {
        if (inputHandler == null)
        {
            inputHandler = FindFirstObjectByType<BlockPlacementInputHandler>();
            if (inputHandler == null)
            {
                Debug.LogError("找不到BlockPlacementInputHandler组件");
                return;
            }
        }

        inputHandler.OnPlaceBlockRequested += PlaceBlockAtPosition;
        inputHandler.OnCancelPlacementRequested += CallPlacement;
        inputHandler.OnPreviewPositionChanged += OnPreviewPositionChanged;
    }

    private void InitializePreviewSystem()
    {
        if (PreviewSystem == null)
        {
            Debug.LogError("找不到BlockPreviewSystem");
            return;
        }

        if (gameMap == null)
        {
            Debug.LogError("GameMap未初始化，无法初始化预览系统");
            return;
        }

        if (towerPrefabCache == null)
        {
            Debug.LogError("塔预制体未加载，无法初始化预览系统");
            return;
        }

        PreviewSystem.Init(gameMap, towerPrefabCache);
        Debug.Log("预览系统初始化完成");
    }

    private void OnDestroy()
    {
        if (inputHandler != null)
        {
            inputHandler.OnPlaceBlockRequested -= PlaceBlockAtPosition;
            inputHandler.OnCancelPlacementRequested -= CallPlacement;
            inputHandler.OnPreviewPositionChanged -= OnPreviewPositionChanged;
        }
        EventBus.Instance.Unsubscribe<BuildPreviewEventArgs>(OnBuildPreviewRequested);
    }

    private void OnBuildPreviewRequested(BuildPreviewEventArgs args)
    {
        PlaceTowerGroupAtPositions(args.Positions, args.Config, args.TowerDatas, args.Parent, args.Tilemap,false);
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
        if (PreviewSystem != null)
            PreviewSystem.ShowPreview(config, towerDatas);
    }

    /// <summary>
    /// 停止放置模式
    /// </summary>
    public void StopPlacement()
    {
        isPlacing = false;
        if (inputHandler != null)
            inputHandler.StopPlacement();
        if (PreviewSystem != null)
            PreviewSystem.ClearPreview();
        FindFirstObjectByType<Preview_Click>()?.ResetClickState(); // 重置建造状态

        // 销毁预览方块
        if (previewBlock != null)
        {
            Destroy(previewBlock);
            previewBlock = null;
        }
    }
    public void CallPlacement()
    {
        isPlacing = false;
        if (inputHandler != null)
            inputHandler.StopPlacement();
        if (PreviewSystem != null)
            PreviewSystem.ClearPreview();
        FindFirstObjectByType<Preview_Click>()?.ResetClickState(true); // 重置建造状态

        // 销毁预览方块
        if (previewBlock != null)
        {
            Destroy(previewBlock);
            previewBlock = null;
        }
    }
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
        Tilemap tilemap = gameMap.GetTilemap();
        // 生成实际方块对象
        GameObject blockObject = new GameObject($"Block_{position.x}_{position.y}");
        Block block = blockObject.AddComponent<Block>();
        block.tag = "Block";
        // 初始化配置
        block.Init(currentBlockConfig);

        // 检查是否可以放置
        if (!gameMap.CanPlaceBlock(position, currentBlockConfig,tilemap))
        {
            Debug.LogWarning($"无法在位置 ({position.x}, {position.y}) 放置方块");
            Destroy(blockObject);
            return;
        }

        // 放置方块到地图
        if (gameMap.PlaceBlock(position, block,tilemap))
        {
            // Debug.Log($"方块成功放置到位置 ({position.x}, {position.y})");
            
            // Debug.Log($"生成塔: 配置坐标数量={currentBlockConfig.Coordinates.Length}, 塔数据数量={currentTowerDatas.Count}");
            // 恢复：保持hasCheck为true，确保升级替换机制正常工作
            block.GenerateTowers(currentBlockConfig.Coordinates, currentTowerDatas.ToArray(), tilemap, true);

            // 停止放置模式并清除预览（已由BlockPreviewSystem处理）

            // 只在这里刷新 ShowArea
            var previewAreaController = GameManager.Instance?.GetSystem<PreviewAreaController>();
            if (previewAreaController != null)
            {
                previewAreaController.RefreshShowArea();
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
    // public void TestGenerateBlock(string shapeName, Vector3Int position)
    // {
    //     if (gameMap == null)
    //     {
    //         Debug.LogError("GameMap未找到");
    //         return;
    //     }
    //
    //     // 创建测试方块
    //     GameObject blockObject = new GameObject($"TestBlock_{shapeName}");
    //     Block block = blockObject.AddComponent<Block>();
    //
    //     // 初始化方块
    //     block.Init(shapeName);
    //
    //     // 放置方块
    //     if (gameMap.PlaceBlock(position, block,))
    //     {
    //         // 生成测试塔数据
    //         TowerData testTowerData = CreateTestTowerData();
    //
    //         // 为方块生成塔
    //         GenerateTowersForBlock(block, testTowerData);
    //     }
    //     else
    //     {
    //         Debug.LogError($"测试方块放置失败");
    //         Destroy(blockObject);
    //     }
    // }

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

    // 已移除CheckPreviewTowerGroupBuildingStatus方法，功能已集成到BlockPreviewSystem中

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
        block.tag = "Block";
        block.Init(config);  // 初始化会自动清理towers字典
        
        tilemap = tilemap != null ? tilemap : (gameMap != null ? gameMap.GetTilemap() : null);
        
        // 计算Block的cellPosition（使用第一个cell作为基准）
        Vector3Int blockCellPos = cells[0];
        
        // 通过GameMap.PlaceBlock()正确注册Block到地图系统
        if (!gameMap.PlaceBlock(blockCellPos, block,tilemap))
        {
            Debug.LogError($"无法在位置 {blockCellPos} 放置Block {blockObj.name}");
            Destroy(blockObj);
            return;
        }
        
        Debug.Log($"初始化Block: {blockObj.name}, 位置: {blockCellPos}, 配置: {config.name}");
        
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int cell = cells[i];
            TowerData data = towerDatas[i];
            
            // 检查预制体
            if (towerPrefabCache == null)
            {
                Debug.LogError("塔预制体未加载，无法生成塔");
                continue;
            }

            // 判断是否为showarea（父物体名包含showarea）
            bool isShowArea = parent != null && parent.name.ToLower().Contains("showarea");
            
            // 计算相对坐标
            Vector3Int localCoord = cell - blockCellPos;
            Debug.Log($"生成塔 - 全局坐标: {cell}, 相对坐标: {localCoord}, 数据: {data.TowerName}");
            
            // 使用Block的GenerateTower方法来生成塔
            Tower tower = block.GenerateTower(
                localCoord,
                data,
                tilemap,
                hasCheck
            );
            
            if (tower != null)
            {
                // 设置颜色
                var renderers = tower.GetComponentsInChildren<SpriteRenderer>(true);
                Color targetColor = isShowArea ? normalColor : (parent != null ? previewColor : normalColor);
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                    {
                        renderer.color = targetColor;
                        renderer.enabled = true;
                    }
                }
                
                // 确保展示区域的塔被正确标记
                if (isShowArea)
                {
                    tower.SetAsShowAreaTower(true);
                }
                
                Debug.Log($"塔生成成功: {tower.name}, 位置: {localCoord}");
            }
            else
            {
                Debug.LogError($"塔生成失败 - 位置: {localCoord}");
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
        if (isPlacing && PreviewSystem != null && currentBlockConfig != null && GameStateManager.Instance.IsInBuildingPhase)
        {
            Vector3Int gridPos = CoordinateUtility.WorldToCellPosition(gameMap.GetTilemap(), mouseWorldPos);
            PreviewSystem.UpdatePreview(gridPos);
        }
        else
        {
            FindFirstObjectByType<Preview_Click>()?.ResetClickState(true); // 重置建造状态
            StopPlacement();
        }
    }
}

