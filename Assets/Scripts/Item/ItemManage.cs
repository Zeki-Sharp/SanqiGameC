using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemManage : MonoBehaviour
{
    public GameMap map;
    public ItemGeneratorConfig itemGenerator;
    public GameObject itemPrefab;
    public GameObject itemArea;
    [SerializeField]private int itemLimitCount = 5;
    private List<Item> currentItemData = new List<Item>();
    private int duration;
    
    // 物品效果列表相关
    [Header("物品效果显示")]
    public Transform effectListContainer; // 效果列表容器
    public GameObject effectItemPrefab; // 效果项预制体
    private List<ActiveItemEffect> activeItemEffects = new List<ActiveItemEffect>();
    
    public void GenerateItem()
    {
        ItemConfig itemConfig = itemGenerator.GetRandomItem();
        if (itemConfig == null)
        {
            return;
        }

        if (itemArea != null)
        {
            GameObject item = Instantiate(itemPrefab, itemArea.transform);
            item.GetComponent<Item>().SetItem(itemConfig);
            itemConfig.Init();
        }
    }
    void Awake()
    {
        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSystem(this);
        }

        if (itemPrefab == null)
        {
            itemPrefab = Resources.Load<GameObject>("Prefab/Item/Item");
        }

        if (map == null)
        {
            map = GameManager.Instance?.GetSystem<GameMap>();
        }
        EventBus.Instance.SubscribeSimple("Game_NextRound", OnGame_NextRound);
        EventBus.Instance.Subscribe<ItemEvent>(UpdateEffectList);
    }

    private void OnGame_NextRound()
    {
        duration++;
        CheckItem();
        // 检查临时物品效果是否过期
        CheckTemporaryItemEffects();
    }

    public void CheckItem()
    {
        // 这个方法可能需要重构，因为逻辑上有些问题
        // 现有的逻辑是：如果duration大于等于durationMax，则移除物品
        // 但实际应该是：如果duration大于durationMax，则移除物品
        for (int i = currentItemData.Count - 1; i >= 0; i--)
        {
            var item = currentItemData[i];
            if (item.ItemConfig is TemmporaryItemConfig temmporaryItemConfig)
            {
                if (duration > temmporaryItemConfig.durationMax)
                {
                    currentItemData.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// 检查临时物品效果是否过期
    /// </summary>
    private void CheckTemporaryItemEffects()
    {
        for (int i = activeItemEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeItemEffects[i];
            if (effect.itemConfig is TemmporaryItemConfig tempConfig)
            {
                // 如果当前局数大于最大使用局数，删除效果
                if (duration > tempConfig.durationMax)
                {
                    activeItemEffects.RemoveAt(i);
                }
            }
        }
    }

    void Start()
    {
        itemGenerator = map.GetMapConfig().itemGeneratorConfig;
        itemLimitCount = map.GetMapData().itemLimitCount;
        ShowItem();
    }
    

    public void ShowItem(bool isRefse = false)
    {
        if (!isRefse)
        {
            for (int i = itemArea.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(itemArea.transform.GetChild(i).gameObject);
            }
            for (int i = 0; i < itemLimitCount; i++)
            {
                GenerateItem();
            }
        }
        else
        {
            for (int i = itemArea.transform.childCount; i < itemLimitCount; i++)
            {
                GenerateItem();
            }
        }
    }

    private void OnDestroy()
    {
        EventBus.Instance.UnsubscribeSimple("Game_NextRound", OnGame_NextRound);
        EventBus.Instance.Unsubscribe<ItemEvent>(UpdateEffectList);
    }
    
    /// <summary>
    /// 更新物品效果列表
    /// </summary>
    private void UpdateEffectList(ItemEvent itemEvent)
    {
        if (itemEvent == null || itemEvent.ItemConfig == null) return;
        
        // 创建新的激活效果
        ActiveItemEffect activeEffect = new ActiveItemEffect
        {
            itemName = itemEvent.ItemConfig.ItemName,
            itemDescription = itemEvent.ItemConfig.Description,
            itemSprite = itemEvent.ItemConfig.ItemSprite,
            itemConfig = itemEvent.ItemConfig
        };
        
        // 添加到激活效果列表
        activeItemEffects.Add(activeEffect);
        
        // 调用MainTowerHealthPanel中的DisplayActiveEffects方法更新UI显示
        MainTowerHealthPanel mainTowerPanel = UIManager.Instance?.GetPanel<MainTowerHealthPanel>();
        if (mainTowerPanel != null)
        {
            mainTowerPanel.DisplayActiveEffects();
        }
    }
    
    /// <summary>
    /// 获取当前激活的物品效果列表
    /// </summary>
    public List<ActiveItemEffect> GetActiveItemEffects()
    {
        return activeItemEffects;
    }
    
    /// <summary>
    /// 获取当前游戏回合数
    /// </summary>
    public int GetDuration()
    {
        return duration;
    }
}