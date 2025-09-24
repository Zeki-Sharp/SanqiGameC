using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 敌人预警系统测试器 - 用于测试预警系统的基本功能
/// </summary>
public class EnemyWarningSystemTester : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private bool enableTesting = true;
    [SerializeField] private KeyCode testShowKey = KeyCode.T;
    [SerializeField] private KeyCode testHideKey = KeyCode.Y;
    
    [Header("测试信息")]
    [SerializeField, ReadOnly] private bool systemFound = false;
    [SerializeField, ReadOnly] private string systemStatus = "未初始化";
    
    private EnemyWarningSystem warningSystem;
    
    private void Start()
    {
        if (!enableTesting) return;
        
        Debug.Log("EnemyWarningSystemTester: Start方法被调用，开始查找预警系统");
        
        // 查找预警系统
        warningSystem = FindFirstObjectByType<EnemyWarningSystem>();
        Debug.Log($"EnemyWarningSystemTester: FindFirstObjectByType结果: {(warningSystem != null ? "成功" : "失败")}");
        
        if (warningSystem != null)
        {
            systemFound = true;
            systemStatus = "已找到预警系统";
            Debug.Log("EnemyWarningSystemTester: 找到预警系统，测试功能已启用");
            Debug.Log($"EnemyWarningSystemTester: systemFound = {systemFound}");
            
            // 验证系统组件
            if (warningSystem.GetComponent<EnemyWarningSystem>() != null)
            {
                Debug.Log("EnemyWarningSystemTester: 预警系统组件验证成功");
            }
            else
            {
                Debug.LogError("EnemyWarningSystemTester: 预警系统组件验证失败");
            }
        }
        else
        {
            systemFound = false;
            systemStatus = "未找到预警系统";
            Debug.LogWarning("EnemyWarningSystemTester: 未找到预警系统，请确保场景中有EnemyWarningSystem组件");
            Debug.Log($"EnemyWarningSystemTester: systemFound = {systemFound}");
            
            // 尝试查找所有EnemyWarningSystem类型的对象
            var allSystems = FindObjectsByType<EnemyWarningSystem>(FindObjectsSortMode.None);
            Debug.Log($"EnemyWarningSystemTester: 场景中找到 {allSystems.Length} 个EnemyWarningSystem对象");
            
            // 如果找到了对象，尝试使用第一个
            if (allSystems.Length > 0)
            {
                warningSystem = allSystems[0];
                systemFound = true;
                systemStatus = "使用FindObjectsOfType找到预警系统";
                Debug.Log("EnemyWarningSystemTester: 使用FindObjectsOfType找到预警系统，测试功能已启用");
                Debug.Log($"EnemyWarningSystemTester: systemFound = {systemFound}");
            }
        }
    }
    
    private void Update()
    {
        // 添加调试信息
        if (Time.frameCount % 300 == 0) // 每300帧输出一次状态
        {
            Debug.Log($"EnemyWarningSystemTester: Update检查 - enableTesting: {enableTesting}, systemFound: {systemFound}");
        }
        
        if (!enableTesting || !systemFound) 
        {
            if (Time.frameCount % 300 == 0) // 每300帧输出一次状态
            {
                Debug.Log($"EnemyWarningSystemTester: Update被跳过 - enableTesting: {enableTesting}, systemFound: {systemFound}");
            }
            return;
        }
        
        // 测试显示预警
        if (Input.GetKeyDown(testShowKey))
        {
            Debug.Log($"EnemyWarningSystemTester: 检测到按键 {testShowKey}，调用测试显示预警");
            TestShowWarning();
        }
        
        // 测试隐藏预警
        if (Input.GetKeyDown(testHideKey))
        {
            Debug.Log($"EnemyWarningSystemTester: 检测到按键 {testHideKey}，调用测试隐藏预警");
            TestHideWarning();
        }
        
        // 添加更多调试信息
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("EnemyWarningSystemTester: 检测到空格键，执行系统状态检查");
            CheckSystemStatus();
        }
    }
    
    /// <summary>
    /// 测试显示预警
    /// </summary>
    [ContextMenu("测试显示预警")]
    public void TestShowWarning()
    {
        Debug.Log("EnemyWarningSystemTester: TestShowWarning 被调用");
        
        if (warningSystem == null)
        {
            Debug.LogError("EnemyWarningSystemTester: 预警系统未找到");
            return;
        }
        
        Debug.Log("EnemyWarningSystemTester: 预警系统已找到，准备调用ManualShowWarning");
        
        try
        {
            warningSystem.ManualShowWarning();
            Debug.Log("EnemyWarningSystemTester: ManualShowWarning 调用成功");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"EnemyWarningSystemTester: 调用ManualShowWarning时发生错误: {e.Message}");
        }
    }
    
    /// <summary>
    /// 测试隐藏预警
    /// </summary>
    [ContextMenu("测试隐藏预警")]
    public void TestHideWarning()
    {
        if (warningSystem == null)
        {
            Debug.LogError("EnemyWarningSystemTester: 预警系统未找到");
            return;
        }
        
        Debug.Log("EnemyWarningSystemTester: 测试隐藏预警");
        warningSystem.ManualHideWarning();
    }
    
    /// <summary>
    /// 检查系统状态
    /// </summary>
    [ContextMenu("检查系统状态")]
    public void CheckSystemStatus()
    {
        var gameManager = GameManager.Instance;
        var roundManager = GameManager.Instance?.GetSystem<RoundManager>();
        var enemySpawner = GameManager.Instance?.GetSystem<EnemySpawner>();
        
        Debug.Log("=== 系统状态检查 ===");
        Debug.Log($"GameManager: {(gameManager != null ? "已找到" : "未找到")}");
        Debug.Log($"RoundManager: {(roundManager != null ? "已找到" : "未找到")}");
        Debug.Log($"EnemySpawner: {(enemySpawner != null ? "已找到" : "未找到")}");
        Debug.Log($"EnemyWarningSystem: {(warningSystem != null ? "已找到" : "未找到")}");
        
        if (enemySpawner != null)
        {
            Debug.Log($"生成区域数量: {enemySpawner.spawnAreas.Count}");
        }
        
        if (roundManager != null)
        {
            Debug.Log($"当前Round: {roundManager.CurrentRoundNumber}");
            Debug.Log($"Round进行中: {roundManager.IsRoundInProgress}");
        }
        
        Debug.Log("==================");
    }
    
    private void OnGUI()
    {
        if (!enableTesting) return;
        
        // 显示测试信息
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("敌人预警系统测试器", GUI.skin.box);
        GUILayout.Label($"系统状态: {systemStatus}");
        GUILayout.Label($"按 {testShowKey} 测试显示预警");
        GUILayout.Label($"按 {testHideKey} 测试隐藏预警");
        
        if (GUILayout.Button("检查系统状态"))
        {
            CheckSystemStatus();
        }
        
        if (GUILayout.Button("测试显示预警"))
        {
            TestShowWarning();
        }
        
        if (GUILayout.Button("测试隐藏预警"))
        {
            TestHideWarning();
        }
        
        GUILayout.EndArea();
    }
}
