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
    [SerializeField] private int currentRoundNumber = 0;
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
        // 初始化Round配置，引用通过属性自动获取
        InitializeRoundConfigs();
    }

    /// <summary>
    /// 初始化Round配置
    /// </summary>
    private void InitializeRoundConfigs()
    {
        Debug.Log($"RoundManager初始化：Inspector中有{roundConfigs.Count}个Round配置");

        // 如果没有配置，创建默认配置
        if (roundConfigs.Count == 0)
        {
            Debug.Log("Inspector中没有Round配置，创建默认配置");
            CreateDefaultRoundConfigs();
        }
        else
        {
            // 验证现有配置
            for (int i = 0; i < roundConfigs.Count; i++)
            {
                var config = roundConfigs[i];
                if (config != null)
                {
                    Debug.Log($"Round {i + 1} 配置: {config.waves.Count} 个Wave");
                    for (int j = 0; j < config.waves.Count; j++)
                    {
                        var wave = config.waves[j];
                        if (roundConfigs[i].enemyPrefab != null)
                        {
                            config.waves[j].enemyPrefab = roundConfigs[i].enemyPrefab;
                        }
                        Debug.Log($"  Wave {j + 1}: {wave.enemies.Count} 种敌人");
                        for (int k = 0; k < wave.enemies.Count; k++)
                        {
                            var enemy = wave.enemies[k];
                            Debug.Log($"    敌人 {k + 1}: {enemy.enemyData?.EnemyName ?? "null"} x {enemy.count}");
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Round {i + 1} 配置为null");
                }
            }
        }

        Debug.Log($"Round配置初始化完成，共{roundConfigs.Count}个Round配置");
    }

    /// <summary>
    /// 创建默认Round配置
    /// </summary>
    private void CreateDefaultRoundConfigs()
    {
        // 创建前10个Round的默认配置
        for (int i = 1; i <= 10; i++)
        {
            RoundConfig config = ScriptableObject.CreateInstance<RoundConfig>();
            config.roundNumber = i;
            config.waves = CreateDefaultWaves(i);
            config.rewardMoney = 50 + i * 10;
            config.rewardExperience = 10 + i * 2;
            roundConfigs.Add(config);
        }
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
            EnemyData[] enemyDatas = GetDefaultEnemyPrefabFromData();
            foreach (var enemyData in enemyDatas)
            {
                // 创建敌人信息
                EnemySpawnInfo enemyInfo = new EnemySpawnInfo();
                enemyInfo.enemyData = enemyData;
                enemyInfo.count = 3 + roundNumber + i * 2;
                wave.enemies.Add(enemyInfo);
            }
            wave.enemyPrefab = GetDefaultEnemyPrefab();
            waves.Add(wave);
        }

        return waves;
    }

    /// <summary>
    /// 获取默认敌人预制体
    /// </summary>
    private GameObject GetDefaultEnemyPrefab()
    {
        // 从Resources加载默认敌人预制体
        GameObject enemyPrefab = Resources.Load<GameObject>("Prefab/Enemy/Enemy_test");
        if (enemyPrefab == null)
        {
            // 尝试加载备用预制体
            enemyPrefab = Resources.Load<GameObject>("Prefab/Enemy/Enemy_test_2");
        }
        if (enemyPrefab == null)
        {
            Debug.LogError("未找到任何敌人预制体，请检查Resources/Prefab/Enemy/目录");
        }
        else
        {
            Debug.Log($"成功加载敌人预制体: {enemyPrefab.name}");
        }
        return enemyPrefab;
    }
    public EnemyData[] GetDefaultEnemyPrefabFromData()
    {
        EnemyData[] enemyData = Resources.LoadAll<EnemyData>("Data/Enemy");
        return enemyData;
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

        currentRoundNumber++;
        isRoundInProgress = true;

        RoundConfig config = GetCurrentRoundConfig();
        if (config == null)
        {
            Debug.LogError($"未找到Round {currentRoundNumber} 的配置");
            return;
        }

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
            RewardMoney = config.rewardMoney,
            RewardExperience = config.rewardExperience
        });
    }

    /// <summary>
    /// 获取当前Round配置
    /// </summary>
    private RoundConfig GetCurrentRoundConfig()
    {
        if (currentRoundNumber <= 0 || currentRoundNumber > roundConfigs.Count)
            return null;

        return roundConfigs[currentRoundNumber - 1];
    }

    /// <summary>
    /// 获取EnemySpawner（供VictoryConditionChecker使用）
    /// </summary>
    public EnemySpawner GetEnemySpawner()
    {
        return EnemySpawner;
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
        currentRoundNumber = 0;
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
/// Round配置 - ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "New Round Config", menuName = "Tower Defense/Round Config")] [Serializable]
public class RoundConfig : ScriptableObject
{
    [Header("Round信息")]
    public int roundNumber;
    public List<Wave> waves = new List<Wave>();

    [Header("配置")]
    public GameObject enemyPrefab;

    [Header("奖励")]
    public int rewardMoney = 100;
    public int rewardExperience = 20;
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
    public int RewardExperience;
}