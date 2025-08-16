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
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private GameMap gameMap;
    

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
}

public class MoneyChangedEventArgs : EventArgs
{
    public int NewAmount;
}