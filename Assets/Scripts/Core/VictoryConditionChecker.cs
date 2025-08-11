using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 胜利条件检查器 - 检查各种胜利条件，支持多种胜利模式，触发胜利事件
/// </summary>
public class VictoryConditionChecker : MonoBehaviour
{
    [Header("胜利条件配置")]
    [SerializeField] private VictoryConfig victoryConfig;
    [SerializeField] private bool enableTimeVictory = true;
    [SerializeField] private bool enableRoundVictory = true;
    [SerializeField] private bool enableFinalVictory = true;
    
    // 管理器引用 - 通过GameManager自动获取
    private RoundManager RoundManager => GameManager.Instance?.GetSystem<RoundManager>();
    private ShopSystem ShopSystem => GameManager.Instance?.GetSystem<ShopSystem>();
    
    // 私有变量
    private float gameStartTime;
    private float roundStartTime;
    private bool hasCheckedVictory = false;
    
    // 单例模式
    private static VictoryConditionChecker instance;
    public static VictoryConditionChecker Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<VictoryConditionChecker>();
                if (instance == null)
                {
                    GameObject go = new GameObject("VictoryConditionChecker");
                    instance = go.AddComponent<VictoryConditionChecker>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Debug.LogWarning("重复的VictoryConditionChecker实例，正在销毁新的实例");
            Destroy(gameObject);
            return;
        }
        
        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSystem(this);
        }
        
    }
    
    private void Start()
    {
        // 初始化胜利配置，引用通过属性自动获取
        InitializeVictoryConfig();
        
        // 记录游戏开始时间
        gameStartTime = Time.time;
    }
    
    /// <summary>
    /// 初始化胜利配置
    /// </summary>
    private void InitializeVictoryConfig()
    {
        if (victoryConfig == null)
        {
            // 尝试从Resources加载VictoryConfig
            victoryConfig = Resources.Load<VictoryConfig>("Data/Victory/New Victory Config");
            
            if (victoryConfig == null)
            {
                Debug.LogWarning("未找到VictoryConfig资源文件，创建默认配置");
                CreateDefaultVictoryConfig();
            }
        }
    }
    
    /// <summary>
    /// 创建默认胜利配置
    /// </summary>
    private void CreateDefaultVictoryConfig()
    {
        victoryConfig = ScriptableObject.CreateInstance<VictoryConfig>();
        victoryConfig.roundTimeLimit = 300f; // 每个Round 5分钟时间限制
        victoryConfig.finalVictoryRound = 10; // 完成10个Round获得最终胜利
        victoryConfig.requireCenterTowerAlive = true; // 要求中心塔存活
    }
    
    /// <summary>
    /// 检查胜利条件
    /// </summary>
    public void CheckVictoryConditions()
    {
        if (hasCheckedVictory)
            return;
            
        Dictionary<string, object> statistics = CollectGameStatistics();
        
        // 首先检查失败条件（中心塔血量 ≤ 0）
        if (CheckDefeatCondition(statistics))
        {
            TriggerDefeat(statistics);
            return;
        }
        
        // 检查Round胜利条件（所有敌人被消灭 或 达到时间上限）
        if (CheckRoundVictory(statistics))
        {
            // 检查是否达到最终胜利条件
            if (CheckFinalVictory(statistics))
            {
                TriggerVictory(VictoryType.FinalVictory, statistics);
            }
            else
            {
                TriggerVictory(VictoryType.RoundVictory, statistics);
            }
            return;
        }
        
        // 如果没有满足任何条件，不进行状态切换，等待其他条件
    }
    
    /// <summary>
    /// 检查失败条件（中心塔血量 ≤ 0）
    /// </summary>
    private bool CheckDefeatCondition(Dictionary<string, object> statistics)
    {
        bool centerTowerDead = !CheckCenterTowerAlive();
        statistics["CenterTowerDead"] = centerTowerDead;
        return centerTowerDead;
    }
    
    /// <summary>
    /// 检查Round胜利条件（所有敌人被消灭 或 达到时间上限）
    /// </summary>
    private bool CheckRoundVictory(Dictionary<string, object> statistics)
    {
        if (RoundManager == null || !RoundManager.IsRoundInProgress)
            return false;
            
        // 使用RoundManager检查敌人状态，而不是直接查找GameObject
        bool allEnemiesDefeated = false;
        int remainingEnemies = 0;
        
        var enemySpawner = RoundManager.GetEnemySpawner();
        if (enemySpawner != null)
        {
            remainingEnemies = enemySpawner.GetCurrentEnemyCount();
            allEnemiesDefeated = enemySpawner.AreAllEnemiesDefeated();
        }
        else
        {
            // 备用方案：直接查找敌人
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            remainingEnemies = enemies.Length;
            allEnemiesDefeated = enemies.Length == 0;
        }
        
        // 检查是否达到时间上限
        bool timeLimitReached = false;
        if (victoryConfig != null && victoryConfig.roundTimeLimit > 0)
        {
            float roundStartTime = GetRoundStartTime();
            float elapsedTime = Time.time - roundStartTime;
            timeLimitReached = elapsedTime >= victoryConfig.roundTimeLimit;
            statistics["RoundElapsedTime"] = elapsedTime;
            statistics["RoundTimeLimit"] = victoryConfig.roundTimeLimit;
        }
        
        statistics["AllEnemiesDefeated"] = allEnemiesDefeated;
        statistics["TimeLimitReached"] = timeLimitReached;
        statistics["RemainingEnemies"] = remainingEnemies;
        
        return allEnemiesDefeated || timeLimitReached;
    }
    
    /// <summary>
    /// 检查最终胜利条件
    /// </summary>
    private bool CheckFinalVictory(Dictionary<string, object> statistics)
    {
        if (RoundManager == null || victoryConfig == null)
            return false;
            
        int currentRound = RoundManager.CurrentRoundNumber;
        bool centerTowerAlive = CheckCenterTowerAlive();
        
        // 检查是否完成了所有可用的Round
        // 获取RoundManager中的配置总数
        int totalRounds = RoundManager.GetTotalRoundCount();
        bool roundCondition = currentRound >= totalRounds;
        bool towerCondition = !victoryConfig.requireCenterTowerAlive || centerTowerAlive;
        
        statistics["CompletedRounds"] = currentRound;
        statistics["TotalRounds"] = totalRounds;
        statistics["CenterTowerAlive"] = centerTowerAlive;
        
        Debug.Log($"CheckFinalVictory: 当前回合 {currentRound}, 总回合数 {totalRounds}, 回合条件 {roundCondition}, 塔条件 {towerCondition}");
        
        return roundCondition && towerCondition;
    }
    
    /// <summary>
    /// 检查中心塔是否存活
    /// </summary>
    private bool CheckCenterTowerAlive()
    {
        GameObject[] centerTowers = GameObject.FindGameObjectsWithTag("CenterTower");
        if (centerTowers.Length == 0)
        {
            // 如果没有找到CenterTower，检查Tower标签
            GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
            return towers.Length > 0;
        }
        
        foreach (GameObject tower in centerTowers)
        {
            DamageTaker damageTaker = tower.GetComponent<DamageTaker>();
            if (damageTaker != null && damageTaker.currentHealth > 0)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 收集游戏统计数据
    /// </summary>
    private Dictionary<string, object> CollectGameStatistics()
    {
        Dictionary<string, object> statistics = new Dictionary<string, object>();
        
        // 基础统计
        statistics["GameTime"] = Time.time - gameStartTime;
        statistics["CurrentRound"] = RoundManager != null ? RoundManager.CurrentRoundNumber : 0;
        statistics["PlayerMoney"] = ShopSystem != null ? ShopSystem.Money : 0;
        
        // 敌人统计
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        statistics["RemainingEnemies"] = enemies.Length;
        
        // 塔统计
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        statistics["TotalTowers"] = towers.Length;
        
        return statistics;
    }
    
    /// <summary>
    /// 获取Round开始时间
    /// </summary>
    private float GetRoundStartTime()
    {
        return roundStartTime;
    }
    
    /// <summary>
    /// 设置Round开始时间
    /// </summary>
    public void SetRoundStartTime(float startTime)
    {
        roundStartTime = startTime;
    }
    
    /// <summary>
    /// 触发胜利
    /// </summary>
    private void TriggerVictory(VictoryType victoryType, Dictionary<string, object> statistics)
    {
        hasCheckedVictory = true;
        
        // 发布胜利事件
        EventBus.Instance.Publish(new VictoryConditionMetEventArgs 
        { 
            VictoryType = victoryType,
            Statistics = statistics
        });
        
        Debug.Log($"胜利条件满足：{victoryType}");
    }
    
    /// <summary>
    /// 触发失败
    /// </summary>
    private void TriggerDefeat(Dictionary<string, object> statistics)
    {
        hasCheckedVictory = true;
        
        // 发布失败事件
        EventBus.Instance.Publish(new DefeatConditionMetEventArgs 
        { 
            Statistics = statistics
        });
        
        Debug.Log("游戏失败：中心塔被摧毁");
    }
    

    
    /// <summary>
    /// 重置胜利检查器
    /// </summary>
    public void Reset()
    {
        hasCheckedVictory = false;
        gameStartTime = Time.time;
        roundStartTime = Time.time;
    }
    
    /// <summary>
    /// 重置Round胜利状态（用于开始新Round时）
    /// </summary>
    public void ResetRoundVictory()
    {
        hasCheckedVictory = false;  // 重置胜利检查标志
        roundStartTime = Time.time;
        Debug.Log("VictoryConditionChecker: Round胜利状态已重置");
    }
    
    /// <summary>
    /// 手动检查胜利条件（用于调试）
    /// </summary>
    [ContextMenu("手动检查胜利条件")]
    public void ManualCheckVictory()
    {
        hasCheckedVictory = false;
        CheckVictoryConditions();
    }
    

}

/// <summary>
/// 胜利类型枚举
/// </summary>
public enum VictoryType
{
    RoundVictory,    // Round胜利
    TimeVictory,     // 时间胜利
    FinalVictory     // 最终胜利
}

/// <summary>
/// 胜利配置 - ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "New Victory Config", menuName = "Tower Defense/Victory Config")]
public class VictoryConfig : ScriptableObject
{
    [Header("Round时间限制")]
    public float roundTimeLimit = 300f; // 每个Round的时间限制（秒），0表示无限制
    
    [Header("最终胜利条件")]
    public int finalVictoryRound = 10; // 完成指定Round数量获得最终胜利
    
    [Header("通用条件")]
    public bool requireCenterTowerAlive = true; // 是否要求中心塔存活
}

/// <summary>
/// 胜利条件满足事件参数
/// </summary>
public class VictoryConditionMetEventArgs : EventArgs
{
    public VictoryType VictoryType;
    public Dictionary<string, object> Statistics;
}

/// <summary>
/// 失败条件满足事件参数
/// </summary>
public class DefeatConditionMetEventArgs : EventArgs
{
    public Dictionary<string, object> Statistics;
} 