using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class BlockPlacementManager : MonoBehaviour
{
    [Header("管理器引用")]
    [SerializeField] private GameMap gameMap;
    [SerializeField] private Camera mainCamera;
    
    [Header("方块配置")]
    [SerializeField] private GameObject blockPrefab; // 方块预制体
    [SerializeField] private string currentShapeName = "LINE2H"; // 当前选择的形状
    [SerializeField] private TowerData currentTowerData; // 当前选择的塔数据
    
    [Header("放置状态")]
    [SerializeField] private bool isPlacing = false; // 是否正在放置模式
    [SerializeField] private GameObject previewBlock; // 预览方块
    [SerializeField] private Vector3Int previewPosition; // 预览位置
    
    // 公共属性
    public bool IsPlacing => isPlacing;
    public string CurrentShapeName => currentShapeName;
    
    private void Start()
    {
        // 自动获取组件引用
        if (gameMap == null)
            gameMap = FindFirstObjectByType<GameMap>();
            
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        Debug.Log("方块放置管理器初始化完成");
    }
    
    private void Update()
    {
        if (isPlacing)
        {
            HandlePlacementInput();
        }
    }
    
    /// <summary>
    /// 开始放置模式
    /// </summary>
    /// <param name="shapeName">方块形状名称</param>
    /// <param name="towerData">塔数据</param>
    public void StartPlacement(string shapeName, TowerData towerData = null)
    {
        if (gameMap == null)
        {
            Debug.LogError("GameMap未找到，无法开始放置");
            return;
        }
        
        currentShapeName = shapeName;
        currentTowerData = towerData;
        isPlacing = true;
        
        // 创建预览方块
        CreatePreviewBlock();
        
        Debug.Log($"开始放置模式，形状: {shapeName}");
    }
    
    /// <summary>
    /// 停止放置模式
    /// </summary>
    public void StopPlacement()
    {
        isPlacing = false;
        
        // 销毁预览方块
        if (previewBlock != null)
        {
            Destroy(previewBlock);
            previewBlock = null;
        }
        
        Debug.Log("停止放置模式");
    }
    
    /// <summary>
    /// 处理放置输入
    /// </summary>
    private void HandlePlacementInput()
    {
        if (mainCamera == null) return;
        
        // 获取鼠标世界坐标
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        // 转换为地图格子坐标
        Vector3Int gridPos = gameMap.WorldToGridPosition(mouseWorldPos);
        
        // 更新预览位置
        if (previewPosition != gridPos)
        {
            previewPosition = gridPos;
            UpdatePreviewBlock();
        }
        
        // 鼠标左键点击放置方块
        if (Input.GetMouseButtonDown(0))
        {
            PlaceBlockAtPosition(gridPos);
        }
        
        // 鼠标右键或ESC键取消放置
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            StopPlacement();
        }
    }
    
    /// <summary>
    /// 创建预览方块
    /// </summary>
    private void CreatePreviewBlock()
    {
        if (previewBlock != null)
        {
            Destroy(previewBlock);
        }
        
        // 创建预览方块GameObject
        previewBlock = new GameObject($"PreviewBlock_{currentShapeName}");
        previewBlock.transform.SetParent(transform);
        
        // 添加Block组件
        Block block = previewBlock.AddComponent<Block>();
        block.Init(currentShapeName);
        
        // 设置预览材质（半透明）
        Renderer renderer = previewBlock.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material previewMaterial = new Material(renderer.material);
            previewMaterial.color = new Color(0, 1, 0, 0.5f); // 半透明绿色
            renderer.material = previewMaterial;
        }
        
        UpdatePreviewBlock();
    }
    
    /// <summary>
    /// 更新预览方块位置和状态
    /// </summary>
    private void UpdatePreviewBlock()
    {
        if (previewBlock == null) return;
        
        Block block = previewBlock.GetComponent<Block>();
        if (block == null) return;
        
        // 设置预览位置
        Vector3 worldPos = gameMap.GridToWorldPosition(previewPosition);
        previewBlock.transform.position = worldPos;
        
        // 检查是否可以放置
        bool canPlace = gameMap.CanPlaceBlock(previewPosition, block.Config);
        
        // 更新预览颜色
        Renderer renderer = previewBlock.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color previewColor = canPlace ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
            renderer.material.color = previewColor;
        }
    }
    
    /// <summary>
    /// 在指定位置放置方块
    /// </summary>
    /// <param name="position">地图格子坐标</param>
    private void PlaceBlockAtPosition(Vector3Int position)
    {
        if (gameMap == null) return;
        
        // 创建实际的方块
        GameObject blockObject = new GameObject($"Block_{currentShapeName}_{position.x}_{position.y}");
        Block block = blockObject.AddComponent<Block>();
        
        // 初始化方块
        block.Init(currentShapeName);
        
        // 检查是否可以放置
        if (!gameMap.CanPlaceBlock(position, block.Config))
        {
            Debug.LogWarning($"无法在位置 ({position.x}, {position.y}) 放置方块");
            Destroy(blockObject);
            return;
        }
        
        // 放置方块到地图
        if (gameMap.PlaceBlock(position, block))
        {
            Debug.Log($"方块成功放置到位置 ({position.x}, {position.y})");
            
            // 如果提供了塔数据，为每个格子生成塔
            if (currentTowerData != null)
            {
                GenerateTowersForBlock(block, currentTowerData);
            }
            
            // 停止放置模式
            StopPlacement();
        }
        else
        {
            Debug.LogError($"放置方块失败");
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
        
        Debug.Log($"为方块生成了 {block.Config.Coordinates.Length} 座塔");
    }
    
    /// <summary>
    /// 测试生成方块（用于调试）
    /// </summary>
    /// <param name="shapeName">形状名称</param>
    /// <param name="position">位置</param>
    public void TestGenerateBlock(string shapeName, Vector2Int position)
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
        if (gameMap.PlaceBlock(new Vector3Int(position.x, position.y, 0), block))
        {
            Debug.Log($"测试方块放置成功: {shapeName} 在位置 ({position.x}, {position.y})");
            
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
        var idField = type.GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nameField = type.GetField("towerName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var healthField = type.GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var attackField = type.GetField("physicAttack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (idField != null) idField.SetValue(testData, 1);
        if (nameField != null) nameField.SetValue(testData, "测试塔");
        if (healthField != null) healthField.SetValue(testData, 100f);
        if (attackField != null) attackField.SetValue(testData, 25f);
        
        return testData;
    }
    
    /// <summary>
    /// 通用塔组建造方法：在指定格子组建造塔组
    /// </summary>
    /// <param name="cells">塔组所有格子坐标（世界cell坐标）</param>
    /// <param name="config">塔组配置</param>
    /// <param name="towerDatas">每个塔的数据</param>
    /// <param name="parent">父物体（可选）</param>
    public void PlaceTowerGroupAtPositions(List<Vector3Int> cells, BlockGenerationConfig config, List<TowerData> towerDatas, Transform parent = null)
    {
        if (gameMap == null || config == null || cells == null || towerDatas == null || cells.Count != towerDatas.Count)
        {
            Debug.LogError("参数无效，无法建造塔组");
            return;
        }
        // 生成Block对象
        GameObject blockObj = new GameObject($"BlockGroup_{cells[0].x}_{cells[0].y}");
        if (parent != null) blockObj.transform.SetParent(parent);
        Block block = blockObj.AddComponent<Block>();
        block.Init(config);
        // 依次生成塔
        Tilemap tilemap = gameMap.GetTilemap();
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int cell = cells[i];
            TowerData data = towerDatas[i];
            GameObject towerPrefab = Resources.Load<GameObject>("Prefab/Tower/Tower");
            GameObject towerObj = Instantiate(towerPrefab, blockObj.transform);
            // 设置塔位置
            Vector3 worldPos = tilemap.GetCellCenterWorld(cell);
            towerObj.transform.position = worldPos;
            // 恢复正常颜色
            SpriteRenderer[] renderers = towerObj.GetComponentsInChildren<SpriteRenderer>(true);
            Tower towerComponent = towerObj.GetComponent<Tower>();
            const int BaseOrder = 1000;
            const int VerticalOffsetMultiplier = 10;
            int verticalOffset = Mathf.RoundToInt(-worldPos.y * VerticalOffsetMultiplier);
            int finalOrder = BaseOrder + verticalOffset;
            towerComponent.Initialize(data,  new Vector2Int(cell.x, cell.y));
            towerComponent.SetOrder(finalOrder);
            block.SetTower(new Vector2Int(cell.x, cell.y), towerComponent);
            if (renderers != null && renderers.Length > 0)
            {
                foreach (var sr in renderers)
                {
                    sr.color = new Color(1, 1, 1, 1);
                }
            }
        }
        // 注册占用格子
        foreach (var cell in cells)
        {
            // 直接注册到GameMap
            // 只能通过PlaceBlock注册整体Block，或直接操作occupiedCells（如需更细粒度可扩展）
        }
        Debug.Log($"塔组建造完成，格子数：{cells.Count}");
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
    
} 