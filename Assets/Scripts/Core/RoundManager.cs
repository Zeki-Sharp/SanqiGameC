using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 大波次管理器 - 管理Round计数和配置，控制战斗开始/结束，处理Round完成奖励
/// </summary>
public class RoundManager : MonoBehaviour
{
    [Header("Round配置")]
    [SerializeField] private int currentRoundNumber = 1; // 从第一回合开始
    [SerializeField] private bool isRoundInProgress = false;
    [ListDrawerSettings]
    [SerializeField] private List<RoundConfig> roundConfigs = new List<RoundConfig>();

    // 管理器引用 - 通过GameManager自动获取
    private EnemySpawner EnemySpawner => GameManager.Instance?.GetSystem<EnemySpawner>();
    private ShopSystem ShopSystem => GameManager.Instance?.GetSystem<ShopSystem>();
    private VictoryConditionChecker VictoryChecker => GameManager.Instance?.GetSystem<VictoryConditionChecker>();

    // 公共属性
    public int CurrentRoundNumber => currentRoundNumber;
    public bool IsRoundInProgress => isRoundInProgress;
    public RoundConfig CurrentRoundConfig => GetCurrentRoundConfig();

    // 单例模式
    private static RoundManager instance;
    public static RoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<RoundManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("RoundManager");
                    instance = go.AddComponent<RoundManager>();
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
            Debug.LogWarning("重复的RoundManager实例，正在销毁新的实例");
            Destroy(gameObject);
            return;
        }

        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSystem(this);
        }

        // 订阅事件
        EventBus.Instance.Subscribe<EnemyDeathEventArgs>(OnEnemyDeath);
    }

    private void Start()
    {
        Debug.Log("RoundManager: Start() 开始执行");
        
        // 初始化Round配置，引用通过属性自动获取
        InitializeRoundConfigs();
        
        Debug.Log($"RoundManager Start() 完成，当前回合: {currentRoundNumber}, 配置总数: {roundConfigs.Count}");
    }

    /// <summary>
    /// 初始化Round配置
    /// </summary>
    private void InitializeRoundConfigs()
    {
        Debug.Log($"RoundManager初始化：Inspector中有{roundConfigs.Count}个Round配置");
        
        // 优先从Resources加载Round配置
        List<RoundConfig> loadedConfigs = LoadRoundConfigsFromResources();
        if (loadedConfigs != null && loadedConfigs.Count > 0)
        {
            roundConfigs = loadedConfigs;
            Debug.Log($"从Resources加载了{roundConfigs.Count}个Round配置");
            
            // 详细记录每个配置
            for (int i = 0; i < roundConfigs.Count; i++)
            {
                var config = roundConfigs[i];
                Debug.Log($"配置 {i + 1}: {config.name}, Round {config.roundNumber}, {config.waves?.Count ?? 0} 个Wave");
            }
        }
        else if (roundConfigs.Count == 0)
        {
            // 如果没有找到配置文件，创建默认配置
            Debug.LogWarning("没有找到Round配置文件，创建默认配置");
            roundConfigs = CreateDefaultRoundConfigs();
        }
        
        // 验证配置有效性
        ValidateRoundConfigs();
        
        Debug.Log($"RoundManager初始化完成，共有{roundConfigs.Count}个Round配置");
    }
    
    /// <summary>
    /// 从Resources加载Round配置
    /// </summary>
    private List<RoundConfig> LoadRoundConfigsFromResources()
    {
        List<RoundConfig> configs = new List<RoundConfig>();
        
        // 尝试加载所有Round配置
        RoundConfig[] loadedConfigs = Resources.LoadAll<RoundConfig>("Data/Round");
        
        if (loadedConfigs != null && loadedConfigs.Length > 0)
        {
            // 按Round编号排序
            List<RoundConfig> sortedConfigs = new List<RoundConfig>(loadedConfigs);
            sortedConfigs.Sort((a, b) => a.roundNumber.CompareTo(b.roundNumber));
            
            foreach (var config in sortedConfigs)
            {
                if (config != null && config.waves != null && config.waves.Count > 0)
                {
                    configs.Add(config);
                    Debug.Log($"加载Round配置: {config.name}, Round {config.roundNumber}, {config.waves.Count}个Wave");
                }
                else
                {
                    Debug.LogWarning($"Round配置 {config?.name ?? "null"} 无效，跳过");
                }
            }
        }
        else
        {
            Debug.LogWarning("Resources/Data/Round文件夹中没有找到Round配置");
        }
        
        return configs;
    }
    
    /// <summary>
    /// 验证Round配置的有效性
    /// </summary>
    private void ValidateRoundConfigs()
    {
        if (roundConfigs == null || roundConfigs.Count == 0)
        {
            Debug.LogWarning("Round配置列表为空");
            return;
        }
        
        for (int i = 0; i < roundConfigs.Count; i++)
        {
            var config = roundConfigs[i];
            if (config != null)
            {
                Debug.Log($"Round {config.roundNumber} 配置: {config.waves?.Count ?? 0} 个Wave");
                if (config.waves != null)
                {
                    for (int j = 0; j < config.waves.Count; j++)
                    {
                        var wave = config.waves[j];
                        Debug.Log($"  Wave {j + 1}: {wave.enemies?.Count ?? 0} 种敌人");
                        if (wave.enemies != null)
                        {
                            for (int k = 0; k < wave.enemies.Count; k++)
                            {
                                var enemy = wave.enemies[k];
                                Debug.Log($"    敌人 {k + 1}: {enemy.enemyData?.EnemyName ?? "null"} x {enemy.count}");
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"Round {i + 1} 配置为null");
            }
        }
    }

    /// <summary>
    /// 创建默认Round配置
    /// </summary>
    private List<RoundConfig> CreateDefaultRoundConfigs()
    {
        List<RoundConfig> defaultConfigs = new List<RoundConfig>();
        // 创建前10个Round的默认配置
        for (int i = 1; i <= 10; i++)
        {
            RoundConfig config = ScriptableObject.CreateInstance<RoundConfig>();
            config.roundNumber = i;
            config.waves = CreateDefaultWaves(i);
            config.rewardMoney = 50 + i * 10;
            defaultConfigs.Add(config);
        }
        return defaultConfigs;
    }

    /// <summary>
    /// 创建默认Wave配置
    /// </summary>
    private List<Wave> CreateDefaultWaves(int roundNumber)
    {
        List<Wave> waves = new List<Wave>();

        // 根据Round数量创建不同数量的Wave
        int waveCount = Mathf.Min(3 + roundNumber / 3, 8); // 最多8个Wave

        for (int i = 0; i < waveCount; i++)
        {
            Wave wave = new Wave();
            wave.delayBeforeWave = 2f + i * 1f;
            
            // 获取可用的EnemyData（只使用有预制体的）
            EnemyData[] enemyDatas = GetDefaultEnemyPrefabFromData();
            List<EnemyData> validEnemyDatas = new List<EnemyData>();
            
            foreach (var enemyData in enemyDatas)
            {
                if (enemyData != null && enemyData.EnemyPrefab != null)
                {
                    validEnemyDatas.Add(enemyData);
                }
            }
            
            // 如果没有有效的EnemyData，创建一个默认的Wave
            if (validEnemyDatas.Count == 0)
            {
                Debug.LogWarning($"Round {roundNumber} Wave {i + 1}: 没有找到有效的EnemyData，跳过此Wave");
                continue;
            }
            
            // 为每个有效的EnemyData创建敌人信息
            foreach (var enemyData in validEnemyDatas)
            {
                EnemySpawnInfo enemyInfo = new EnemySpawnInfo();
                enemyInfo.enemyData = enemyData;
                enemyInfo.count = 3 + roundNumber + i * 2;
                wave.enemies.Add(enemyInfo);
            }
            
            waves.Add(wave);
        }

        return waves;
    }


    public EnemyData[] GetDefaultEnemyPrefabFromData()
    {
        EnemyData[] enemyData = Resources.LoadAll<EnemyData>("Data/Enemy");
        
        if (enemyData == null || enemyData.Length == 0)
        {
            Debug.LogWarning("没有找到任何EnemyData文件");
            return new EnemyData[0];
        }
        
        Debug.Log($"找到 {enemyData.Length} 个EnemyData文件");
        
        // 检查每个EnemyData的有效性
        int validCount = 0;
        foreach (var data in enemyData)
        {
            if (data != null && data.EnemyPrefab != null)
            {
                validCount++;
            }
            else
            {
                Debug.LogWarning($"EnemyData '{data?.name ?? "null"}' 的enemyPrefab为空");
            }
        }
        
        Debug.Log($"其中 {validCount} 个EnemyData有有效的enemyPrefab");
        
        return enemyData;
    }
    /// <summary>
    /// 开始当前回合的战斗
    /// </summary>
    public void StartCurrentRound()
    {
        if (isRoundInProgress)
        {
            Debug.LogWarning("当前Round正在进行中，无法重新开始");
            return;
        }

        isRoundInProgress = true;
        Debug.Log($"开始第 {currentRoundNumber} 回合");
        StartRoundInternal();
    }

    /// <summary>
    /// 准备下一个回合（增加回合数，但不开始战斗）
    /// </summary>
    public void PrepareNextRound()
    {
        if (isRoundInProgress)
        {
            Debug.LogWarning("当前Round正在进行中，无法准备新Round");
            return;
        }

        currentRoundNumber++;
        Debug.Log($"准备第 {currentRoundNumber} 回合");
    }

    /// <summary>
    /// 开始下一个Round
    /// </summary>
    public void StartNextRound()
    {
        if (isRoundInProgress)
        {
            Debug.LogWarning("当前Round正在进行中，无法开始新Round");
            return;
        }

        isRoundInProgress = true;
        Debug.Log($"开始第 {currentRoundNumber} 回合");
        StartRoundInternal();
    }

    /// <summary>
    /// 启动回合的内部逻辑
    /// </summary>
    private void StartRoundInternal()
    {
        Debug.Log($"RoundManager: 开始Round {currentRoundNumber}，当前配置总数: {roundConfigs.Count}");

        RoundConfig config = GetCurrentRoundConfig();
        if (config == null)
        {
            Debug.LogError($"未找到Round {currentRoundNumber} 的配置");
            return;
        }

        Debug.Log($"RoundManager: 使用配置 {config.name}，包含 {config.waves?.Count ?? 0} 个Wave");

        // 重置胜利检查器的Round状态
        if (VictoryChecker != null)
        {
            VictoryChecker.ResetRoundVictory();
        }

        // 发布Round开始事件
        EventBus.Instance.Publish(new RoundStartedEventArgs
        {
            RoundNumber = currentRoundNumber,
            Waves = config.waves
        });

        // 开始生成敌人
        if (EnemySpawner != null)
        {
            EnemySpawner.SetWaves(config.waves);
            EnemySpawner.StartWaves();
        }
        else
        {
            Debug.LogError("EnemySpawner为null，无法开始生成敌人");
        }

        // 设置Round开始时间
        if (VictoryChecker != null)
        {
            VictoryChecker.SetRoundStartTime(Time.time);
        }
    }

    /// <summary>
    /// 完成当前Round
    /// </summary>
    public void CompleteCurrentRound()
    {
        if (!isRoundInProgress)
        {
            Debug.LogWarning("当前没有Round在进行中");
            return;
        }

        isRoundInProgress = false;

        RoundConfig config = GetCurrentRoundConfig();
        if (config == null)
        {
            Debug.LogError($"未找到Round {currentRoundNumber} 的配置");
            return;
        }

        // 给予奖励
        if (ShopSystem != null)
        {
            ShopSystem.AddMoney(config.rewardMoney);
        }

        // 发布Round完成事件
        EventBus.Instance.Publish(new RoundCompletedEventArgs
        {
            RoundNumber = currentRoundNumber,
            RewardMoney = config.rewardMoney
        });
    }

    /// <summary>
    /// 获取当前Round配置
    /// </summary>
    private RoundConfig GetCurrentRoundConfig()
    {
        Debug.Log($"GetCurrentRoundConfig: 当前回合 {currentRoundNumber}，配置总数 {roundConfigs.Count}");
        
        if (currentRoundNumber <= 0 || currentRoundNumber > roundConfigs.Count)
        {
            Debug.LogError($"Round编号超出范围: {currentRoundNumber}，配置总数: {roundConfigs.Count}");
            return null;
        }

        var config = roundConfigs[currentRoundNumber - 1];
        Debug.Log($"获取配置: {config?.name ?? "null"}，Round {config?.roundNumber ?? 0}");
        return config;
    }

    /// <summary>
    /// 获取EnemySpawner（供VictoryConditionChecker使用）
    /// </summary>
    public EnemySpawner GetEnemySpawner()
    {
        return EnemySpawner;
    }
    
    /// <summary>
    /// 获取总Round数量（供VictoryConditionChecker使用）
    /// </summary>
    public int GetTotalRoundCount()
    {
        return roundConfigs.Count;
    }

    /// <summary>
    /// 处理敌人死亡事件
    /// </summary>
    private void OnEnemyDeath(EnemyDeathEventArgs e)
    {
        // 检查是否所有敌人都被消灭
        if (isRoundInProgress && EnemySpawner != null)
        {
            // 使用EnemySpawner提供的方法检查Round完成
            StartCoroutine(CheckRoundCompletion());
        }
    }

    /// <summary>
    /// 检查Round是否完成
    /// </summary>
    private System.Collections.IEnumerator CheckRoundCompletion()
    {
        yield return new WaitForSeconds(0.5f); // 等待一段时间确保所有敌人都被处理

        // 使用EnemySpawner提供的方法检查是否所有敌人都被消灭
        if (EnemySpawner.AreAllEnemiesDefeated() && isRoundInProgress)
        {
            // 在完成Round之前先检查胜利条件
            if (VictoryChecker != null)
            {
                VictoryChecker.CheckVictoryConditions();
            }

            // 然后完成Round
            CompleteCurrentRound();
        }
    }

    /// <summary>
    /// 重置Round管理器
    /// </summary>
    public void Reset()
    {
        Debug.Log($"RoundManager: 重置前 - 当前回合: {currentRoundNumber}, 进行中: {isRoundInProgress}");
        
        currentRoundNumber = 1; // 重置为第一回合
        isRoundInProgress = false;

        // 清除所有敌人
        if (EnemySpawner != null)
        {
            EnemySpawner.ClearAllEnemies();
        }

        Debug.Log("Round管理器已重置");
    }

    /// <summary>
    /// 恢复Round状态
    /// </summary>
    public void ResumeRound()
    {
        Debug.Log($"RoundManager: 恢复Round {currentRoundNumber}");

        if (isRoundInProgress)
        {
            // 恢复EnemySpawner的状态
            if (EnemySpawner != null)
            {
                EnemySpawner.ResumeWaves();
            }

            Debug.Log($"Round {currentRoundNumber} 已恢复");
        }
        else
        {
            Debug.LogWarning("当前没有Round在进行中，无法恢复");
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<EnemyDeathEventArgs>(OnEnemyDeath);
        }
    }
}



/// <summary>
/// Round开始事件参数
/// </summary>
public class RoundStartedEventArgs : EventArgs
{
    public int RoundNumber;
    public List<Wave> Waves;
}

/// <summary>
/// Round完成事件参数
/// </summary>
public class RoundCompletedEventArgs : EventArgs
{
    public int RoundNumber;
    public int RewardMoney;
}