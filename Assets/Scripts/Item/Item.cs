using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Item : MonoBehaviour
{
    [ShowInInspector]private ItemConfig itemConfig;
    [ShowInInspector]private Image ItemSprite;
    [ShowInInspector]private TextMeshProUGUI ItemName;
    [ShowInInspector]  private TextMeshProUGUI ItemDescription;
    [ShowInInspector] private TextMeshProUGUI ItemPrice;
    [ShowInInspector]  private Button UseButton;
    public ItemConfig ItemConfig => itemConfig;
    private void Awake()
    {
        Transform spriteTransform = transform.Find("ItemSprite");
        if (spriteTransform != null)
        {
            ItemSprite = spriteTransform.GetComponent<Image>();
        }
        else
        {
            Debug.LogError("ItemSprite not found in Item prefab");
        }

        Transform nameTransform = transform.Find("ItemName");
        if (nameTransform != null)
        {
            ItemName = nameTransform.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogError("ItemName not found in Item prefab");
        }

        Transform descriptionTransform = transform.Find("ItemDescription");
        if (descriptionTransform != null)
        {
            ItemDescription = descriptionTransform.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogError("ItemDescription not found in Item prefab");
        }

        Transform priceTransform = transform.Find("ItemPrice");
        if (priceTransform != null)
        {
            ItemPrice = priceTransform.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogError("ItemPrice not found in Item prefab");
        }

        UseButton = GetComponent<Button>();
        UseButton?.onClick.AddListener(() =>
        {
            if (itemConfig != null)
            {
                if (GameManager.Instance.ShopSystem.CanAfford(itemConfig.Price))
                {
                    GameManager.Instance.ShopSystem.SpendMoney(itemConfig.Price);
                    
                    // 发布物品使用事件
                    EventBus.Instance.Publish(new ItemEvent(this));
                }
                else
                {
                    Debug.Log("金币不足，无法购买物品");
                    // 可选：发布购买失败事件
                    // EventBus.Instance.Publish(new ItemPurchaseFailedEvent(this, ItemPurchaseFailedEvent.Reason.InsufficientFunds));
                    return;
                }
                itemConfig.Use();
                GameManager.Instance.ItemManage.ShowItem();
            }
        });
    }
    
    public void SetItem(ItemConfig itemConfig)
    {  Transform spriteTransform = transform.Find("ItemSprite");
        if (spriteTransform != null)
        {
            ItemSprite = spriteTransform.GetComponent<Image>();
        }
        else
        {
            Debug.LogError("ItemSprite not found in Item prefab");
        }

        Transform nameTransform = transform.Find("ItemName");
        if (nameTransform != null)
        {
            ItemName = nameTransform.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogError("ItemName not found in Item prefab");
        }

        Transform descriptionTransform = transform.Find("ItemDescription");
        if (descriptionTransform != null)
        {
            ItemDescription = descriptionTransform.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogError("ItemDescription not found in Item prefab");
        }

        Transform priceTransform = transform.Find("ItemPrice");
        if (priceTransform != null)
        {
            ItemPrice = priceTransform.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogError("ItemPrice not found in Item prefab");
        }

        if (itemConfig != null)
        {
            if (ItemName != null)
                ItemName.text = itemConfig.ItemName;
            else
                Debug.LogError("ItemName is null when setting item");

            if (ItemDescription != null)
                ItemDescription.text = itemConfig.Description;
            else
                Debug.LogError("ItemDescription is null when setting item");

            if (ItemPrice != null)
                ItemPrice.text = $"售价：{itemConfig.Price.ToString()}";
            else
                Debug.LogError("ItemPrice is null when setting item");

            if (ItemSprite != null)
                ItemSprite.sprite = itemConfig.ItemSprite;
            else
                Debug.LogError("ItemSprite is null when setting item");

            this.itemConfig = itemConfig;
        }
    }
}