using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 游戏主管理器 - 唯一的全局访问点，统一管理所有子系统
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();
                if (instance == null)
                {
                    Debug.LogError("GameManager未找到！请在场景中添加GameManager组件。");
                }
            }
            return instance;
        }
    }

    // 子系统注册表
    private Dictionary<Type, object> systems = new Dictionary<Type, object>();
    
    // 向后兼容的属性
    public ShopSystem ShopSystem => GetSystem<ShopSystem>();
    public ItemManage ItemManage => GetSystem<ItemManage>();
    
    // 便捷访问属性
    public GameStateManager GameStateManager => GetSystem<GameStateManager>();
    public RoundManager RoundManager => GetSystem<RoundManager>();
    public VictoryConditionChecker VictoryConditionChecker => GetSystem<VictoryConditionChecker>();
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("GameManager已初始化");
        }
        else if (instance != this)
        {
            Debug.LogWarning("发现重复的GameManager，销毁当前实例");
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // 验证关键系统是否可用
        ValidateSystems();
    }

    /// <summary>
    /// 注册子系统
    /// </summary>
    /// <typeparam name="T">子系统类型</typeparam>
    /// <param name="system">子系统实例</param>
    public void RegisterSystem<T>(T system) where T : MonoBehaviour
    {
        if (system == null)
        {
            Debug.LogError($"尝试注册空的{typeof(T).Name}系统");
            return;
        }
        
        Type systemType = typeof(T);
        if (systems.ContainsKey(systemType))
        {
            Debug.LogWarning($"系统{systemType.Name}已经注册，将被覆盖");
        }
        
        systems[systemType] = system;
        Debug.Log($"系统{systemType.Name}已注册到GameManager");
    }

    /// <summary>
    /// 获取子系统
    /// </summary>
    /// <typeparam name="T">子系统类型</typeparam>
    /// <returns>子系统实例</returns>
    public T GetSystem<T>() where T : MonoBehaviour
    {
        Type systemType = typeof(T);
        if (systems.TryGetValue(systemType, out object system))
        {
            return system as T;
        }
        
        Debug.LogWarning($"系统{systemType.Name}未注册，尝试自动查找");
        // 尝试自动查找
        T foundSystem = FindFirstObjectByType<T>();
        if (foundSystem != null)
        {
            RegisterSystem(foundSystem);
            return foundSystem;
        }
        
        Debug.LogError($"无法找到系统{systemType.Name}");
        return null;
    }

    /// <summary>
    /// 初始化所有系统
    /// </summary>
    public void InitializeAllSystems()
    {
        Debug.Log("开始初始化所有系统...");
        
        // 按依赖顺序初始化系统
        var gameStateManager = GetSystem<GameStateManager>();
        var roundManager = GetSystem<RoundManager>();
        var victoryConditionChecker = GetSystem<VictoryConditionChecker>();
        var shopSystem = GetSystem<ShopSystem>();
        var itemManage = GetSystem<ItemManage>();
        
        Debug.Log("所有系统初始化完成");
    }

    /// <summary>
    /// 获取当前游戏状态
    /// </summary>
    /// <returns>当前游戏状态</returns>
    public GamePhase GetCurrentGamePhase()
    {
        var gameStateManager = GetSystem<GameStateManager>();
        return gameStateManager?.CurrentPhase ?? GamePhase.BuildingPhase;
    }

    /// <summary>
    /// 切换游戏阶段（便捷方法）
    /// </summary>
    /// <param name="newPhase">目标游戏阶段</param>
    public void SwitchGamePhase(GamePhase newPhase)
    {
        var gameStateManager = GetSystem<GameStateManager>();
        if (gameStateManager != null)
        {
            switch (newPhase)
            {
                case GamePhase.BuildingPhase:
                    gameStateManager.SwitchToBuildingPhase();
                    break;
                case GamePhase.CombatPhase:
                    gameStateManager.SwitchToCombatPhase();
                    break;
                case GamePhase.VictoryPhase:
                    gameStateManager.SwitchToVictoryPhase();
                    break;
            }
        }
        else
        {
            Debug.LogError("GameStateManager未找到，无法切换游戏阶段");
        }
    }

    /// <summary>
    /// 验证关键系统是否可用
    /// </summary>
    private void ValidateSystems()
    {
        var requiredSystems = new Type[]
        {
            typeof(ShopSystem),
            typeof(ItemManage),
            typeof(GameStateManager),
            typeof(RoundManager),
            typeof(VictoryConditionChecker)
        };

        foreach (var systemType in requiredSystems)
        {
            if (!systems.ContainsKey(systemType))
            {
                Debug.LogWarning($"关键系统{systemType.Name}未注册");
            }
        }
    }

    /// <summary>
    /// 获取所有已注册的系统
    /// </summary>
    /// <returns>系统类型列表</returns>
    public List<Type> GetRegisteredSystems()
    {
        return new List<Type>(systems.Keys);
    }

    /// <summary>
    /// 清理所有系统
    /// </summary>
    public void ClearAllSystems()
    {
        systems.Clear();
        Debug.Log("所有系统已清理");
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        Debug.Log("GameManager: 恢复游戏");
        
        // 恢复时间缩放
        Time.timeScale = 1f;
        
        // 通知相关系统恢复游戏
        var roundManager = GetSystem<RoundManager>();
        if (roundManager != null)
        {
            roundManager.ResumeRound();
        }
        
        // 确保UI显示正确的面板
        var uiManager = GetSystem<UIManager>();
        if (uiManager != null)
        {
            var currentPhase = GetCurrentGamePhase();
            uiManager.SwitchToPhase(currentPhase);
        }
    }
}
