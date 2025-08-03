using System;
using UnityEngine;

/// <summary>
/// 游戏状态管理器 - 管理游戏三大状态的切换和协调各系统间的交互
/// </summary>
public class GameStateManager : MonoBehaviour
{
    [Header("当前状态")]
    [SerializeField] private GamePhase currentPhase = GamePhase.BuildingPhase;
    
    // 管理器引用 - 通过GameManager自动获取
    private RoundManager RoundManager => GameManager.Instance?.GetSystem<RoundManager>();
    private VictoryConditionChecker VictoryChecker => GameManager.Instance?.GetSystem<VictoryConditionChecker>();
    private UIManager UIManager => GameManager.Instance?.GetSystem<UIManager>();
    
    // 公共属性
    public GamePhase CurrentPhase => currentPhase;
    public bool IsInCombatPhase => currentPhase == GamePhase.CombatPhase;
    public bool IsInBuildingPhase => currentPhase == GamePhase.BuildingPhase;
    public bool IsInPassPhase => currentPhase == GamePhase.PassPhase;
    public bool IsInVictoryPhase => currentPhase == GamePhase.VictoryPhase;
    
    // 单例模式
    private static GameStateManager instance;
    public static GameStateManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameStateManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameStateManager");
                    instance = go.AddComponent<GameStateManager>();
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
            Debug.LogWarning("重复的GameStateManager实例，正在销毁新的实例");
            Destroy(gameObject);
            return;
        }
        
        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSystem(this);
        }
        
        // 订阅事件
        EventBus.Instance.Subscribe<RoundCompletedEventArgs>(OnRoundCompleted);
        EventBus.Instance.Subscribe<VictoryConditionMetEventArgs>(OnVictoryConditionMet);
        EventBus.Instance.Subscribe<DefeatConditionMetEventArgs>(OnDefeatConditionMet);
    }
    
    private void Start()
    {
        // 延迟初始化，确保UIManager已经初始化完成
        StartCoroutine(DelayedInitialize());
    }
    
    private System.Collections.IEnumerator DelayedInitialize()
    {
        // 等待一帧，确保UIManager已经初始化
        yield return null;
        
        // 初始化游戏状态，引用通过属性自动获取
        InitializeGameState();
    }
    
    /// <summary>
    /// 初始化游戏状态
    /// </summary>
    private void InitializeGameState()
    {
        currentPhase = GamePhase.BuildingPhase;
        EventBus.Instance.Publish(new GamePhaseChangedEventArgs 
        { 
            OldPhase = GamePhase.BuildingPhase, 
            NewPhase = GamePhase.BuildingPhase 
        });
        
        // 确保UI显示正确的阶段
        if (UIManager != null)
        {
            UIManager.SwitchToPhase(GamePhase.BuildingPhase);
        }
        
        Debug.Log("游戏状态管理器初始化完成，当前状态：建设阶段");
    }
    
    /// <summary>
    /// 切换到建设阶段
    /// </summary>
    public void SwitchToBuildingPhase()
    {
        if (currentPhase == GamePhase.BuildingPhase)
            return;
            
        GamePhase oldPhase = currentPhase;
        currentPhase = GamePhase.BuildingPhase;
        
        // 发布状态变化事件
        EventBus.Instance.Publish(new GamePhaseChangedEventArgs 
        { 
            OldPhase = oldPhase, 
            NewPhase = GamePhase.BuildingPhase 
        });
        
        // 更新UI
        if (UIManager != null)
            UIManager.SwitchToPhase(GamePhase.BuildingPhase);
            
        Debug.Log($"游戏状态切换：{oldPhase} → 建设阶段");
    }
    
    /// <summary>
    /// 切换到战斗阶段
    /// </summary>
    public void SwitchToCombatPhase()
    {
        if (currentPhase == GamePhase.CombatPhase)
            return;
            
        GamePhase oldPhase = currentPhase;
        currentPhase = GamePhase.CombatPhase;
        
        // 发布状态变化事件
        EventBus.Instance.Publish(new GamePhaseChangedEventArgs 
        { 
            OldPhase = oldPhase, 
            NewPhase = GamePhase.CombatPhase 
        });
        
        // 开始新的Round
        if (RoundManager != null)
            RoundManager.StartNextRound();
            
        // 更新UI
        if (UIManager != null)
            UIManager.SwitchToPhase(GamePhase.CombatPhase);
            
        Debug.Log($"游戏状态切换：{oldPhase} → 战斗阶段");
    }
    
    /// <summary>
    /// 切换到通过阶段
    /// </summary>
    public void SwitchToPassPhase()
    {
        if (currentPhase == GamePhase.PassPhase)
            return;
            
        GamePhase oldPhase = currentPhase;
        currentPhase = GamePhase.PassPhase;
        
        // 发布状态变化事件
        EventBus.Instance.Publish(new GamePhaseChangedEventArgs 
        { 
            OldPhase = oldPhase, 
            NewPhase = GamePhase.PassPhase 
        });
        
        // 更新UI
        if (UIManager != null)
            UIManager.SwitchToPhase(GamePhase.PassPhase);
            
        Debug.Log($"游戏状态切换：{oldPhase} → 通过阶段");
    }
    
    /// <summary>
    /// 切换到胜利阶段
    /// </summary>
    public void SwitchToVictoryPhase()
    {
        if (currentPhase == GamePhase.VictoryPhase)
            return;
            
        GamePhase oldPhase = currentPhase;
        currentPhase = GamePhase.VictoryPhase;
        
        // 发布状态变化事件
        EventBus.Instance.Publish(new GamePhaseChangedEventArgs 
        { 
            OldPhase = oldPhase, 
            NewPhase = GamePhase.VictoryPhase 
        });
        
        // 更新UI
        if (UIManager != null)
            UIManager.SwitchToPhase(GamePhase.VictoryPhase);
            
        Debug.Log($"游戏状态切换：{oldPhase} → 胜利阶段");
    }
    
    /// <summary>
    /// 处理Round完成事件
    /// </summary>
    private void OnRoundCompleted(RoundCompletedEventArgs e)
    {
        // 检查是否满足胜利条件
        if (VictoryChecker != null)
        {
            VictoryChecker.CheckVictoryConditions();
        }
        else
        {
            // 如果没有胜利检查器，直接切换回建设阶段
            SwitchToBuildingPhase();
        }
    }
    
    /// <summary>
    /// 处理胜利条件满足事件
    /// </summary>
    private void OnVictoryConditionMet(VictoryConditionMetEventArgs e)
    {
        if (e.VictoryType == VictoryType.FinalVictory)
        {
            // 最终胜利，切换到胜利阶段
            SwitchToVictoryPhase();
        }
        else
        {
            // Round胜利，切换到通过阶段
            SwitchToPassPhase();
        }
    }
    
    /// <summary>
    /// 处理失败条件满足事件
    /// </summary>
    private void OnDefeatConditionMet(DefeatConditionMetEventArgs e)
    {
        Debug.Log("游戏失败：中心塔被摧毁");
        // TODO: 切换到失败界面或重新开始
        // 暂时重新开始游戏
        RestartGame();
    }
    
    /// <summary>
    /// 重新开始游戏
    /// </summary>
    private void RestartGame()
    {
        // 重置所有管理器
        if (RoundManager != null)
            RoundManager.Reset();
        if (VictoryChecker != null)
            VictoryChecker.Reset();
            
        // 切换回建设阶段
        SwitchToBuildingPhase();
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<RoundCompletedEventArgs>(OnRoundCompleted);
            EventBus.Instance.Unsubscribe<VictoryConditionMetEventArgs>(OnVictoryConditionMet);
            EventBus.Instance.Unsubscribe<DefeatConditionMetEventArgs>(OnDefeatConditionMet);
        }
    }
}

/// <summary>
/// 游戏阶段枚举
/// </summary>
public enum GamePhase
{
    BuildingPhase,  // 建设阶段
    CombatPhase,    // 战斗阶段
    PassPhase,      // 通过阶段（新增）
    VictoryPhase,   // 胜利阶段
    DefeatPhase     // 失败阶段
}

/// <summary>
/// 游戏状态变化事件参数
/// </summary>
public class GamePhaseChangedEventArgs : EventArgs
{
    public GamePhase OldPhase;
    public GamePhase NewPhase;
} 