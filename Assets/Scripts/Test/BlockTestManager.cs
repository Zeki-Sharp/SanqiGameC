
using System.Collections.Generic;
using UnityEngine;

public class BlockTestManager : MonoBehaviour
{
    [Header("测试配置")]
    [SerializeField] private GameMap gameMap;
    [SerializeField] private PreviewAreaController previewAreaController;
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
            mapConfig = gameMap.GetMapConfig();
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
        Test_01();
    }

    public void Test_01()
    {

        BlockGenerationConfig config =  mapConfig.blockGenerationSettings.GetRandomShape();
        List<TowerData> towerDatas = new List<TowerData>();
        
        // 使用原始配置生成塔数据（CreateBlock内部会重新生成旋转后的坐标）
        foreach (var vector2 in config.GetCellCoords())
        {
            towerDatas.Add(mapConfig.blockGenerationSettings.GetRandomTower());
            // Debug.Log($"方块初始化中，方块坐标: {vector2}，替换塔{towerDatas[towerDatas.Count-1].name}");
        }

        // 不再预先生成坐标列表，让CreateBlock内部使用旋转后的配置坐标
        previewAreaController.CreateShowAreaBlock(mapConfig.blockPrefab, towerDatas, config);
        // Debug.Log($"方块完成，形状: {config.name}，包含 {config.GetCellCount(out int count)} 个格子");
        
        
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