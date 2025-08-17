using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class ShopSystem : MonoBehaviour
{
    private int money;
    [ShowInInspector] public int Money
    {
        get { return money; }
        set 
        { 
            money = value;
            EventBus.Instance.Publish(new MoneyChangedEventArgs { NewAmount = money });
        }
    }
    
    public int InitialMoney
    {
        get 
        {
            if (gameMap != null)
            {
                return gameMap.GetMapData().StartingMoney;
            }
            return 0;
        }
    }

    [Header("商店系统")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private GameMap gameMap;

    // 刷新系统相关
    private int currentRefreshCount = 0;    // 当前轮次的刷新次数
    private int currentRoundNumber = 1;     // 当前轮次号
    
    [ShowInInspector]
    public int CurrentRefreshCost 
    { 
        get 
        {
            if (gameMap != null)
            {
                return gameMap.GetMapData().CalculateRefreshCost(currentRefreshCount);
            }
            return 3; // 默认值
        }
    }

    [ShowInInspector]
    public int RemainingRefreshes
    {
        get
        {
            if (gameMap != null)
            {
                return gameMap.GetMapData().MaxRefreshPerRound - currentRefreshCount;
            }
            return 0;
        }
    }

    // Block系统相关
    private int remainingBlockSlots;  // 剩余Block槽位数

    [ShowInInspector]
    public int CurrentBlockCost
    {
        get
        {
            if (gameMap != null)
            {
                return gameMap.GetMapData().CalculateBlockCost(remainingBlockSlots);
            }
            return 5; // 默认值
        }
    }
    

    public void Awake()
    {
        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSystem(this);
        }
        
        EventBus.Instance.Subscribe<MoneyChangedEventArgs>(OnMoneyChanged);
    }

    public void Initialize(MapConfig mapConfig, DifficultyLevel level)
    {
        MapData mapData = mapConfig.GetMapData(level);
        money = mapData.StartingMoney;
        EventBus.Instance.Publish(new MoneyChangedEventArgs { NewAmount = money });
    }

    private void Start()
    {
        gameMap = FindFirstObjectByType<GameMap>();
        money = gameMap.GetMapData().StartingMoney;
        EventBus.Instance.Publish(new MoneyChangedEventArgs { NewAmount = money });
    }

    public bool CanAfford(int amount)
    {
        return money >= amount;
    }

    public void SpendMoney(int amount)
    {
        money -= amount;
        EventBus.Instance.Publish(new MoneyChangedEventArgs { NewAmount = money });
    }

    public void AddMoney(int amount)
    {
        money += amount;
        EventBus.Instance.Publish(new MoneyChangedEventArgs { NewAmount = money });
    }

    private void OnMoneyChanged(MoneyChangedEventArgs e)
    {
        moneyText.text = e.NewAmount.ToString();
    }

    private void OnDestroy()
    {
        EventBus.Instance.Unsubscribe<MoneyChangedEventArgs>(OnMoneyChanged);
    }

    /// <summary>
    /// 尝试刷新商店
    /// </summary>
    /// <returns>是否刷新成功</returns>
    public bool TryRefresh()
    {
        // 检查是否还有刷新次数
        if (RemainingRefreshes <= 0)
        {
            Debug.Log("本轮刷新次数已用完");
            return false;
        }

        // 检查是否有足够的金币
        if (!CanAfford(CurrentRefreshCost))
        {
            Debug.Log($"金币不足，需要 {CurrentRefreshCost} 金币");
            return false;
        }

        // 扣除刷新费用
        SpendMoney(CurrentRefreshCost);
        
        // 增加刷新计数
        currentRefreshCount++;
        
        // 触发刷新事件
        EventBus.Instance.Publish(new ShopRefreshEventArgs { RefreshCount = currentRefreshCount });
        
        return true;
    }

    /// <summary>
    /// 尝试购买Block
    /// </summary>
    /// <returns>是否购买成功</returns>
    public bool TryBuyBlock(BlockGenerationConfig blockConfig)
    {
        if (blockConfig == null)
        {
            Debug.LogError("Block配置为空");
            return false;
        }

        // 获取Block包含的塔位数量
        int towerSlots = blockConfig.GetCellCount();
        
        // 计算价格
        int cost = gameMap.GetMapData().CalculateBlockCost(towerSlots);

        // 检查是否有足够的金币
        if (!CanAfford(cost))
        {
            Debug.Log($"金币不足，需要 {cost} 金币");
            return false;
        }

        // 扣除费用
        SpendMoney(cost);
        
        return true;
    }

    /// <summary>
    /// 进入新回合时重置刷新次数
    /// </summary>
    public void OnNewRound(int roundNumber)
    {
        currentRoundNumber = roundNumber;
        currentRefreshCount = 0;
        
        // 可以在这里添加回合开始时的其他逻辑
        EventBus.Instance.Publish(new ShopRefreshEventArgs { RefreshCount = 0 });
    }

    /// <summary>
    /// 重置商店系统
    /// </summary>
    public void Reset()
    {
        // 重置金币
        Money = InitialMoney;
        
        // 重置刷新次数
        currentRefreshCount = 0;
        currentRoundNumber = 1;
        
        // 重置Block槽位
        if (gameMap != null)
        {
            remainingBlockSlots = gameMap.GetMapData().MaxBlockSlots;
        }
        
        // 触发重置事件
        EventBus.Instance.Publish(new ShopRefreshEventArgs { RefreshCount = 0 });
    }
}

/// <summary>
/// 商店刷新事件参数
/// </summary>
public class ShopRefreshEventArgs : EventArgs
{
    public int RefreshCount;
}

public class MoneyChangedEventArgs : EventArgs
{
    public int NewAmount;
}