
using System.Collections.Generic;
using UnityEngine;

public class BlockTestManager : MonoBehaviour
{
    [Header("测试配置")]
    [SerializeField] private GameMap gameMap;
    [SerializeField] private CreatePrefab createPrefab;
    [SerializeField] private bool runTestsOnStart = true;
    [SerializeField] private string prefabShowName = "PrefabArea";
    [SerializeField] private MapConfig mapConfig;

    public static BlockTestManager instance;
    private void Start()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (gameMap == null)
            gameMap = FindFirstObjectByType<GameMap>();
        if (mapConfig == null)
            mapConfig = gameMap.GetMapData();
        if (runTestsOnStart)
        {
            RunAllTests();
        }
    }
    
    private void Update()
    {
        // 按空格键运行所有测试
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RunAllTests();
        }
    }
    
    /// <summary>
    /// 运行所有测试
    /// </summary>
    public void RunAllTests()
    {
        Debug.Log("=== 开始运行方块塔绑定测试 ===");
        Test_01();
        // TestBlockShapeCreation();
        // TestBlockInitialization();
        // TestTowerGeneration();
        // TestMapPlacement();
        
        Debug.Log("=== 测试完成 ===");
    }

    public void Test_01()
    {
        Debug.Log("--- 测试方块进入待建造池 ---");
        BlockGenerationConfig config =  mapConfig.blockGenerationSettings.GetRandomShape();
        List<TowerData> towerDatas = new List<TowerData>();
        foreach (var vector2 in config.GetCellCoords(config.CellCount))
        {
            towerDatas.Add(mapConfig.blockGenerationSettings.GetRandomTower());
            // Debug.Log($"方块初始化中，方块坐标: {vector2}，替换塔{towerDatas[towerDatas.Count-1].name}");
        }

        List<Vector3Int> positions =   BaseUtility.Vector2IntArrayToVector3IntList(config.GetCellCoords(config.CellCount));
        createPrefab.CreateBlock(mapConfig.blockPrefab, towerDatas,positions , config);
        // Debug.Log($"方块完成，形状: {config.name}，包含 {config.GetCellCount(out int count)} 个格子");
        
        
    }
    /// <summary>
    /// 测试方块形状创建
    /// </summary>
    private void TestBlockShapeCreation()
    {
           // string[] testShapes = { "LINE2H", "L3", "SQUARE2", "SINGLE", "LINE3H" };
        //
        // foreach (string shapeName in testShapes)
        // {
        //     BlockShape shape = BlockShape.GetShape(shapeName);
        //     
        //     if (shape != null && shape.Coordinates != null)
        //     {
        //         Debug.Log($"✓ {shapeName}: 创建成功，包含 {shape.Coordinates.Length} 个格子");
        //         
        //         // 输出格子坐标
        //         string coords = "";
        //         foreach (Vector2Int coord in shape.Coordinates)
        //         {
        //             coords += $"({coord.x},{coord.y}) ";
        //         }
        //         Debug.Log($"  坐标: {coords}");
        //         
        //         // 输出形状大小
        //         Vector2Int size = shape.GetSize();
        //         Debug.Log($"  大小: {size.x}x{size.y}");
        //     }
        //     else
        //     {
        //         Debug.LogError($"✗ {shapeName}: 创建失败");
        //     }
        // }
    }
    
    /// <summary>
    /// 测试方块初始化
    /// </summary>
    private void TestBlockInitialization()
    {
        // Debug.Log("--- 测试方块初始化 ---");
        //
        // string[] testShapes = { "LINE2H", "L3", "SQUARE2" };
        //
        // foreach (string shapeName in testShapes)
        // {
        //     GameObject blockObject = new GameObject($"TestBlock_{shapeName}");
        //     Block block = blockObject.AddComponent<Block>();
        //     
        //     // 初始化方块
        //     block.Init(shapeName);
        //     
        //     // 验证初始化结果
        //     if (block.Config != null && block.Config.Coordinates != null)
        //     {
        //         Debug.Log($"✓ {shapeName}: 初始化成功");
        //         Debug.Log($"  格子数: {block.GetTotalCellCount()}");
        //         Debug.Log($"  塔数: {block.GetTowerCount()}");
        //         
        //         // 检查每个格子是否已预留
        //         foreach (Vector2Int coord in block.Config.Coordinates)
        //         {
        //             if (block.IsCellEmpty(coord))
        //             {
        //                 Debug.Log($"  ✓ 格子 ({coord.x},{coord.y}) 已预留");
        //             }
        //             else
        //             {
        //                 Debug.LogError($"  ✗ 格子 ({coord.x},{coord.y}) 未正确预留");
        //             }
        //         }
        //     }
        //     else
        //     {
        //         Debug.LogError($"✗ {shapeName}: 初始化失败");
        //     }
        //     
        //     // 清理测试对象
        //     Destroy(blockObject);
        // }
    }
    
    /// <summary>
    /// 测试塔生成
    /// </summary>
    private void TestTowerGeneration()
    {
        // Debug.Log("--- 测试塔生成 ---");
        //
        // // 创建测试塔数据
        // TowerData testTowerData = CreateTestTowerData();
        //
        // string[] testShapes = { "LINE2H", "L3", "SQUARE2" };
        //
        // foreach (string shapeName in testShapes)
        // {
        //     GameObject blockObject = new GameObject($"TestBlock_{shapeName}");
        //     Block block = blockObject.AddComponent<Block>();
        //     
        //     // 初始化方块
        //     block.Init(shapeName);
        //     
        //     // 为每个格子生成塔
        //     int generatedTowers = 0;
        //     foreach (Vector2Int coord in block.Config.Coordinates)
        //     {
        //         Tower tower = block.GenerateTower(coord, testTowerData);
        //         if (tower != null)
        //         {
        //             generatedTowers++;
        //             Debug.Log($"  ✓ 格子 ({coord.x},{coord.y}) 塔生成成功");
        //         }
        //         else
        //         {
        //             Debug.Log($"  - 格子 ({coord.x},{coord.y}) 塔生成（当前为Debug.Log）");
        //         }
        //     }
        //     
        //     Debug.Log($"✓ {shapeName}: 塔生成测试完成，预期 {block.Config.Coordinates.Length} 座塔");
        //     
        //     // 清理测试对象
        //     Destroy(blockObject);
        // }
    }
    
    /// <summary>
    /// 测试地图放置
    /// </summary>
    private void TestMapPlacement()
    {
        // Debug.Log("--- 测试地图放置 ---");
        //
        // if (gameMap == null)
        // {
        //     Debug.LogError("✗ GameMap未找到，跳过地图放置测试");
        //     return;
        // }
        //
        // // 清空地图
        // gameMap.ClearMap();
        //
        // string[] testShapes = { "LINE2H", "L3", "SQUARE2" };
        // Vector2Int[] testPositions = { new Vector2Int(2, 2), new Vector2Int(5, 2), new Vector2Int(8, 2) };
        //
        // for (int i = 0; i < testShapes.Length && i < testPositions.Length; i++)
        // {
        //     string shapeName = testShapes[i];
        //     Vector2Int position = testPositions[i];
        //     
        //     // 创建方块
        //     GameObject blockObject = new GameObject($"TestBlock_{shapeName}");
        //     Block block = blockObject.AddComponent<Block>();
        //     block.Init(shapeName);
        //     
        //     // 检查是否可以放置
        //     bool canPlace = gameMap.CanPlaceBlock(position, block.Config);
        //     Debug.Log($"✓ {shapeName} 在位置 ({position.x},{position.y}) 可放置: {canPlace}");
        //     
        //     if (canPlace)
        //     {
        //         // 放置方块
        //         bool placed = gameMap.PlaceBlock(position, block);
        //         Debug.Log($"  ✓ 放置结果: {placed}");
        //         
        //         if (placed)
        //         {
        //             // 验证放置结果
        //             Block placedBlock = gameMap.GetBlockAt(position);
        //             if (placedBlock != null)
        //             {
        //                 Debug.Log($"  ✓ 验证成功: 方块已正确放置");
        //             }
        //             else
        //             {
        //                 Debug.LogError($"  ✗ 验证失败: 无法获取放置的方块");
        //             }
        //         }
        //     }
        //     else
        //     {
        //         // 清理未放置的方块
        //         Destroy(blockObject);
        //     }
        // }
        //
        // // 测试重叠放置
        // Debug.Log("--- 测试重叠放置 ---");
        // GameObject overlapBlock = new GameObject("OverlapBlock");
        // Block overlapBlockComponent = overlapBlock.AddComponent<Block>();
        // overlapBlockComponent.Init("LINE2H");
        //
        // bool canOverlap = gameMap.CanPlaceBlock(new Vector2Int(2, 2), overlapBlockComponent.Config);
        // Debug.Log($"✓ 重叠放置测试: {!canOverlap} (应该为false)");
        //
        // Destroy(overlapBlock);
    }
    
    /// <summary>
    /// 创建测试塔数据
    /// </summary>
    /// <returns>测试塔数据</returns>
    private TowerData CreateTestTowerData()
    {
        TowerData testData = ScriptableObject.CreateInstance<TowerData>();
        
        // 通过反射设置私有字段
        var type = typeof(TowerData);
        var idField = type.GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nameField = type.GetField("towerName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var healthField = type.GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var attackField = type.GetField("physicAttack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rangeField = type.GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var intervalField = type.GetField("attackInterval", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (idField != null) idField.SetValue(testData, 1);
        if (nameField != null) nameField.SetValue(testData, "测试塔");
        if (healthField != null) healthField.SetValue(testData, 100f);
        if (attackField != null) attackField.SetValue(testData, 25f);
        if (rangeField != null) rangeField.SetValue(testData, 3f);
        if (intervalField != null) intervalField.SetValue(testData, 1f);
        
        return testData;
    }
    
    /// <summary>
    /// 在Scene视图中显示测试信息
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // 绘制测试区域
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(5, 2, 0), new Vector3(10, 2, 0.1f));
        
        // 绘制测试位置
        Gizmos.color = Color.yellow;
        Vector2Int[] testPositions = { new Vector2Int(2, 2), new Vector2Int(5, 2), new Vector2Int(8, 2) };
        
        foreach (Vector2Int pos in testPositions)
        {
            Vector3 worldPos = new Vector3(pos.x, pos.y, 0);
            Gizmos.DrawWireSphere(worldPos, 0.3f);
        }
    }
} 