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
        
        // 注意：不再订阅RoundCompletedEventArgs，因为我们在Round完成之前就检查胜利条件
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
            CreateDefaultVictoryConfig();
        }
        
        Debug.Log("胜利条件检查器初始化完成");
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
        Debug.Log("VictoryConditionChecker: 开始检查胜利条件");
        
        // 注意：在Round完成之前检查胜利条件时，允许重复检查
        // 只有在已经触发胜利事件后才阻止重复检查
        if (hasCheckedVictory)
        {
            Debug.Log("VictoryConditionChecker: 已经检查过胜利条件，跳过");
            return;
        }
            
        Dictionary<string, object> statistics = CollectGameStatistics();
        
        // 首先检查失败条件（中心塔血量 ≤ 0）
        if (CheckDefeatCondition(statistics))
        {
            Debug.Log("VictoryConditionChecker: 触发失败条件");
            TriggerDefeat(statistics);
            return;
        }
        
        // 检查Round胜利条件（所有敌人被消灭 或 达到时间上限）
        if (CheckRoundVictory(statistics))
        {
            Debug.Log("VictoryConditionChecker: Round胜利条件满足，检查最终胜利");
            
            // 检查是否达到最终胜利条件
            if (CheckFinalVictory(statistics))
            {
                Debug.Log("VictoryConditionChecker: 触发最终胜利");
                TriggerVictory(VictoryType.FinalVictory, statistics);
            }
            else
            {
                Debug.Log("VictoryConditionChecker: 触发Round胜利");
                TriggerVictory(VictoryType.RoundVictory, statistics);
            }
            return;
        }
        
        Debug.Log("VictoryConditionChecker: 没有满足任何胜利条件");
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
        Debug.Log($"VictoryConditionChecker: 开始检查Round胜利条件");
        Debug.Log($"VictoryConditionChecker: RoundManager为null? {RoundManager == null}");
        
        if (RoundManager == null)
        {
            Debug.Log("VictoryConditionChecker: RoundManager为null，无法检查Round胜利");
            return false;
        }
        
        Debug.Log($"VictoryConditionChecker: IsRoundInProgress = {RoundManager.IsRoundInProgress}");
        
        if (!RoundManager.IsRoundInProgress)
        {
            Debug.Log("VictoryConditionChecker: Round不在进行中，无法检查Round胜利");
            return false;
        }
            
        // 使用RoundManager检查敌人状态，而不是直接查找GameObject
        bool allEnemiesDefeated = false;
        int remainingEnemies = 0;
        
        var enemySpawner = RoundManager.GetEnemySpawner();
        Debug.Log($"VictoryConditionChecker: EnemySpawner为null? {enemySpawner == null}");
        
        if (enemySpawner != null)
        {
            remainingEnemies = enemySpawner.GetCurrentEnemyCount();
            allEnemiesDefeated = enemySpawner.AreAllEnemiesDefeated();
            Debug.Log($"VictoryConditionChecker: EnemySpawner检查 - 剩余敌人: {remainingEnemies}, 全部消灭: {allEnemiesDefeated}");
        }
        else
        {
            // 备用方案：直接查找敌人
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            remainingEnemies = enemies.Length;
            allEnemiesDefeated = enemies.Length == 0;
            Debug.Log($"VictoryConditionChecker: 直接查找敌人 - 剩余敌人: {remainingEnemies}, 全部消灭: {allEnemiesDefeated}");
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
            Debug.Log($"VictoryConditionChecker: 时间检查 - 经过时间: {elapsedTime:F1}s, 时间限制: {victoryConfig.roundTimeLimit}s, 达到限制: {timeLimitReached}");
        }
        else
        {
            Debug.Log("VictoryConditionChecker: 没有设置时间限制或时间限制为0");
        }
        
        statistics["AllEnemiesDefeated"] = allEnemiesDefeated;
        statistics["TimeLimitReached"] = timeLimitReached;
        statistics["RemainingEnemies"] = remainingEnemies;
        
        bool victoryCondition = allEnemiesDefeated || timeLimitReached;
        Debug.Log($"VictoryConditionChecker: Round胜利条件检查结果 - 全部消灭: {allEnemiesDefeated}, 时间限制: {timeLimitReached}, 最终结果: {victoryCondition}");
        
        return victoryCondition;
    }
    
    /// <summary>
    /// 检查最终胜利条件
    /// </summary>
    private bool CheckFinalVictory(Dictionary<string, object> statistics)
    {
        if (RoundManager == null || victoryConfig == null)
            return false;
            
        int completedRounds = RoundManager.CurrentRoundNumber;
        bool centerTowerAlive = CheckCenterTowerAlive();
        
        bool roundCondition = completedRounds >= victoryConfig.finalVictoryRound;
        bool towerCondition = !victoryConfig.requireCenterTowerAlive || centerTowerAlive;
        
        statistics["CompletedRounds"] = completedRounds;
        statistics["FinalVictoryRounds"] = victoryConfig.finalVictoryRound;
        statistics["CenterTowerAlive"] = centerTowerAlive;
        
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
    /// 处理Round完成事件（已废弃，现在在Round完成之前检查胜利条件）
    /// </summary>
    private void OnRoundCompleted(RoundCompletedEventArgs e)
    {
        Debug.Log($"VictoryConditionChecker: 收到Round完成事件 - Round {e.RoundNumber}, 奖励金钱: {e.RewardMoney}");
        // 不再在这里检查胜利条件，因为已经在Round完成之前检查过了
    }
    
    /// <summary>
    /// 重置胜利检查器
    /// </summary>
    public void Reset()
    {
        hasCheckedVictory = false;
        gameStartTime = Time.time;
        roundStartTime = Time.time;
        Debug.Log("胜利条件检查器已重置");
    }
    
    /// <summary>
    /// 重置Round胜利状态（用于开始新Round时）
    /// </summary>
    public void ResetRoundVictory()
    {
        hasCheckedVictory = false;
        roundStartTime = Time.time;
        Debug.Log("Round胜利状态已重置");
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
    
    private void OnDestroy()
    {
        // 不再需要取消订阅事件，因为我们不再订阅RoundCompletedEventArgs
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