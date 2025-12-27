using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 敌人预警系统 - 在战斗回合开始前，通过触手效果提示玩家下一回合中敌人的位置和强度信息
/// </summary>
public class EnemyWarningSystem : MonoBehaviour
{
    [Header("预警设置")]
    [SerializeField] private GameObject tentaclePrefab;
    [SerializeField] private float tentacleDisplayDuration = 3f;
    [SerializeField] private int maxTentaclesPerSpawnArea = 5;
    
    [Header("触手属性")]
    [SerializeField] private float minTentacleWidth = 0.1f;
    [SerializeField] private float maxTentacleWidth = 0.5f;
    [SerializeField] private float tentacleLengthMultiplier = 1.2f;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 私有字段
    private List<WarningTentacle> activeTentacles = new List<WarningTentacle>();
    private Coroutine warningCoroutine;
    private Vector3 centerTowerPosition;
    
    // 管理器引用
    private RoundManager roundManager;
    private EnemySpawner enemySpawner;
    
    private void Awake()
    {
        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSystem(this);
        }
    }
    
    private void Start()
    {
        // 获取管理器引用
        roundManager = GameManager.Instance?.GetSystem<RoundManager>();
        enemySpawner = GameManager.Instance?.GetSystem<EnemySpawner>();
        
        // 获取中心塔位置
        centerTowerPosition = GetCenterTowerPosition();
        
        // 订阅事件
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Subscribe<RoundStartedEventArgs>(OnRoundStarted);
            EventBus.Instance.Subscribe<RoundCompletedEventArgs>(OnRoundCompleted);
        }
        
        Debug.Log("EnemyWarningSystem初始化完成");
        
        // 延迟一帧后自动显示当前Round的预警
        StartCoroutine(AutoShowCurrentRoundWarning());
    }
    
    /// <summary>
    /// 自动显示当前Round的预警
    /// </summary>
    private IEnumerator AutoShowCurrentRoundWarning()
    {
        // 等待一帧，确保所有系统都已初始化
        yield return null;
        
        // 等待RoundManager初始化完成
        yield return new WaitUntil(() => roundManager != null && roundManager.CurrentRoundConfig != null);
        
        // 显示当前Round的预警
        var currentConfig = roundManager.CurrentRoundConfig;
        if (currentConfig != null)
        {
            Debug.Log("EnemyWarningSystem: 自动显示当前Round预警");
            ShowWarningTentacles(currentConfig);
        }
    }
    
    /// <summary>
    /// 当Round开始时隐藏所有触手
    /// </summary>
    private void OnRoundStarted(RoundStartedEventArgs e)
    {
        if (showDebugInfo)
            Debug.Log($"EnemyWarningSystem: Round {e.RoundNumber} 开始，隐藏预警触手");
        
        HideAllTentacles();
    }
    
    /// <summary>
    /// 当Round完成时显示下一Round的预警
    /// </summary>
    private void OnRoundCompleted(RoundCompletedEventArgs e)
    {
        if (showDebugInfo)
            Debug.Log($"EnemyWarningSystem: Round {e.RoundNumber} 完成，准备显示下一Round预警");
        
        // 延迟显示预警，让玩家看到Round完成的效果
        StartCoroutine(ShowNextRoundWarning());
    }
    
    /// <summary>
    /// 显示下一Round的预警信息
    /// </summary>
    private IEnumerator ShowNextRoundWarning()
    {
        // 等待一小段时间
        yield return new WaitForSeconds(0.5f);
        
        // 获取下一Round的配置
        var nextRoundConfig = GetNextRoundConfig();
        if (nextRoundConfig != null)
        {
            ShowWarningTentacles(nextRoundConfig);
        }
        else
        {
            Debug.LogWarning("EnemyWarningSystem: 无法获取下一Round配置，尝试使用当前Round配置");
            // 如果无法获取下一Round配置，尝试使用当前Round配置
            var currentConfig = roundManager?.CurrentRoundConfig;
            if (currentConfig != null)
            {
                ShowWarningTentacles(currentConfig);
            }
        }
    }
    
    /// <summary>
    /// 获取下一Round的配置
    /// </summary>
    private RoundConfig GetNextRoundConfig()
    {
        if (roundManager == null) return null;
        
        int nextRoundNumber = roundManager.CurrentRoundNumber + 1;
        
        // 通过反射获取私有字段roundConfigs
        var roundConfigsField = roundManager.GetType().GetField("roundConfigs", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (roundConfigsField != null)
        {
            var roundConfigs = roundConfigsField.GetValue(roundManager) as List<RoundConfig>;
            if (roundConfigs != null && nextRoundNumber <= roundConfigs.Count)
            {
                return roundConfigs[nextRoundNumber - 1];
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 显示预警触手
    /// </summary>
    private void ShowWarningTentacles(RoundConfig roundConfig)
    {
        Debug.Log("EnemyWarningSystem: ShowWarningTentacles开始执行");
        
        if (roundConfig == null || roundConfig.waves == null || roundConfig.waves.Count == 0)
        {
            Debug.LogWarning("EnemyWarningSystem: 无效的Round配置");
            return;
        }
        
        Debug.Log($"EnemyWarningSystem: 开始处理 {roundConfig.waves.Count} 个Wave");
        
        // 清除现有触手
        ClearAllTentacles();
        
        // 为每个Wave创建触手
        foreach (var wave in roundConfig.waves)
        {
            Debug.Log($"EnemyWarningSystem: 处理Wave，敌人数量: {wave.enemies?.Count ?? 0}");
            CreateTentaclesForWave(wave);
        }
        
        if (showDebugInfo)
            Debug.Log($"EnemyWarningSystem: 创建了 {activeTentacles.Count} 个预警触手");
        
        // 设置自动隐藏
        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(AutoHideTentacles());
        
        Debug.Log($"EnemyWarningSystem: ShowWarningTentacles执行完成，触手数量: {activeTentacles.Count}");
    }
    
    /// <summary>
    /// 为单个Wave创建触手
    /// </summary>
    private void CreateTentaclesForWave(Wave wave)
    {
        if (enemySpawner == null || enemySpawner.spawnAreas == null) 
        {
            Debug.LogError("EnemyWarningSystem: CreateTentaclesForWave - enemySpawner或spawnAreas为null");
            return;
        }
        
        Debug.Log($"EnemyWarningSystem: 为Wave创建触手，生成区域数量: {enemySpawner.spawnAreas.Count}");
        
        // 为每个生成区域创建触手，确保所有区域都有预警显示
        for (int areaIndex = 0; areaIndex < enemySpawner.spawnAreas.Count; areaIndex++)
        {
            var spawnArea = enemySpawner.spawnAreas[areaIndex];
            
            // 计算该生成区域的敌人强度
            float totalStrength = CalculateWaveStrengthInSpawnArea(wave, spawnArea);
            Debug.Log($"EnemyWarningSystem: 生成区域 {areaIndex} 的敌人强度: {totalStrength}");
            
            // 即使强度为0，也创建至少一个触手来显示该区域
            int tentacleCount = Mathf.Max(1, Mathf.Min(
                Mathf.RoundToInt(totalStrength / 100f), 
                maxTentaclesPerSpawnArea
            ));
            
            Debug.Log($"EnemyWarningSystem: 将为生成区域 {areaIndex} 创建 {tentacleCount} 个触手");
            
            // 创建触手
            for (int i = 0; i < tentacleCount; i++)
            {
                CreateTentacle(spawnArea, totalStrength);
            }
        }
    }
    
    /// <summary>
    /// 计算Wave在指定生成区域的强度
    /// </summary>
    private float CalculateWaveStrengthInSpawnArea(Wave wave, SpawnArea spawnArea)
    {
        float totalStrength = 0f;
        
        foreach (var enemyInfo in wave.enemies)
        {
            if (enemyInfo.enemyData != null)
            {
                // 基础强度 = 敌人数量 × 敌人血量
                float enemyStrength = enemyInfo.count * enemyInfo.enemyData.MaxHealth;
                totalStrength += enemyStrength;
            }
        }
        
        return totalStrength;
    }
    
    /// <summary>
    /// 创建单个触手
    /// </summary>
    private void CreateTentacle(SpawnArea spawnArea, float strength)
    {
        Debug.Log($"EnemyWarningSystem: 开始创建触手，强度: {strength}");
        
        if (tentaclePrefab == null)
        {
            Debug.LogError("EnemyWarningSystem: 触手预制体未设置");
            return;
        }
        
        // 计算触手起点（在生成区域内随机）
        Vector3 startPosition = GetRandomPositionInSpawnArea(spawnArea);
        Debug.Log($"EnemyWarningSystem: 触手起点位置: {startPosition}");
        
        // 计算触手终点（指向中心塔）
        Vector3 endPosition = centerTowerPosition;
        Debug.Log($"EnemyWarningSystem: 触手终点位置: {endPosition}");
        
        // 计算触手长度和粗细
        float distance = Vector3.Distance(startPosition, endPosition);
        float tentacleLength = distance * tentacleLengthMultiplier;
        float tentacleWidth = Mathf.Lerp(minTentacleWidth, maxTentacleWidth, strength / 1000f);
        
        Debug.Log($"EnemyWarningSystem: 触手长度: {tentacleLength}, 宽度: {tentacleWidth}");
        
        // 实例化触手
        GameObject tentacleObject = Instantiate(tentaclePrefab, startPosition, Quaternion.identity);
        WarningTentacle tentacle = tentacleObject.GetComponent<WarningTentacle>();
        
        if (tentacle != null)
        {
            Debug.Log("EnemyWarningSystem: 触手组件获取成功，开始初始化");
            tentacle.Initialize(startPosition, endPosition, tentacleLength, tentacleWidth, strength);
            activeTentacles.Add(tentacle);
            Debug.Log($"EnemyWarningSystem: 触手创建成功，当前触手总数: {activeTentacles.Count}");
        }
        else
        {
            Debug.LogError("EnemyWarningSystem: 触手预制体缺少WarningTentacle组件");
            Destroy(tentacleObject);
        }
    }
    
    /// <summary>
    /// 在生成区域内获取随机位置
    /// </summary>
    private Vector3 GetRandomPositionInSpawnArea(SpawnArea spawnArea)
    {
        float randomX = Random.Range(spawnArea.min.x, spawnArea.max.x);
        float randomY = Random.Range(spawnArea.min.y, spawnArea.max.y);
        return new Vector3(randomX, randomY, 0f);
    }
    
    /// <summary>
    /// 自动隐藏触手
    /// </summary>
    private IEnumerator AutoHideTentacles()
    {
        yield return new WaitForSeconds(tentacleDisplayDuration);
        HideAllTentacles();
    }
    
    /// <summary>
    /// 隐藏所有触手
    /// </summary>
    private void HideAllTentacles()
    {
        foreach (var tentacle in activeTentacles)
        {
            if (tentacle != null)
            {
                tentacle.Hide();
            }
        }
    }
    
    /// <summary>
    /// 清除所有触手
    /// </summary>
    private void ClearAllTentacles()
    {
        foreach (var tentacle in activeTentacles)
        {
            if (tentacle != null)
            {
                Destroy(tentacle.gameObject);
            }
        }
        activeTentacles.Clear();
    }
    
    /// <summary>
    /// 获取中心塔位置
    /// </summary>
    private Vector3 GetCenterTowerPosition()
    {
        GameObject centerTower = GameObject.FindGameObjectWithTag("CenterTower");
        if (centerTower == null)
        {
            Debug.LogWarning("EnemyWarningSystem: 未找到中心塔，使用默认位置(0,0,0)");
            return Vector3.zero;
        }
        return centerTower.transform.position;
    }
    
    /// <summary>
    /// 手动显示预警（用于测试）
    /// </summary>
    [ContextMenu("手动显示预警")]
    public void ManualShowWarning()
    {
        Debug.Log("EnemyWarningSystem: ManualShowWarning被调用");
        
        // 检查预制体
        if (tentaclePrefab == null)
        {
            Debug.LogError("EnemyWarningSystem: 触手预制体未设置！请在Inspector中设置tentaclePrefab");
            return;
        }
        
        // 检查RoundManager
        if (roundManager == null)
        {
            Debug.LogError("EnemyWarningSystem: RoundManager未找到！");
            return;
        }
        
        // 检查EnemySpawner
        if (enemySpawner == null)
        {
            Debug.LogError("EnemyWarningSystem: EnemySpawner未找到！");
            return;
        }
        
        // 检查生成区域
        if (enemySpawner.spawnAreas == null || enemySpawner.spawnAreas.Count == 0)
        {
            Debug.LogError("EnemyWarningSystem: 没有找到生成区域！");
            return;
        }
        
        Debug.Log($"EnemyWarningSystem: 找到 {enemySpawner.spawnAreas.Count} 个生成区域");
        
        // 获取当前Round配置
        var currentConfig = roundManager.CurrentRoundConfig;
        if (currentConfig == null)
        {
            Debug.LogWarning("EnemyWarningSystem: 当前Round配置为null，尝试获取下一Round配置");
            currentConfig = GetNextRoundConfig();
        }
        
        if (currentConfig == null)
        {
            Debug.LogError("EnemyWarningSystem: 无法获取有效的Round配置！");
            return;
        }
        
        Debug.Log($"EnemyWarningSystem: 使用Round配置: {currentConfig.name}");
        Debug.Log($"EnemyWarningSystem: Wave数量: {currentConfig.waves?.Count ?? 0}");
        
        if (currentConfig.waves != null)
        {
            for (int i = 0; i < currentConfig.waves.Count; i++)
            {
                var wave = currentConfig.waves[i];
                Debug.Log($"EnemyWarningSystem: Wave {i}: {wave.enemies?.Count ?? 0} 个敌人");
            }
        }
        
        // 显示预警触手
        ShowWarningTentacles(currentConfig);
    }
    
    /// <summary>
    /// 手动隐藏预警（用于测试）
    /// </summary>
    [ContextMenu("手动隐藏预警")]
    public void ManualHideWarning()
    {
        HideAllTentacles();
    }
    
    private void OnDestroy()
    {
        // 取消事件订阅
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<RoundStartedEventArgs>(OnRoundStarted);
            EventBus.Instance.Unsubscribe<RoundCompletedEventArgs>(OnRoundCompleted);
        }
        
        // 清理触手
        ClearAllTentacles();
    }
}
