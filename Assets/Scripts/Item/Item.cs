using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    private ItemConfig itemConfig;
    private Image ItemSprite;
    private TextMeshProUGUI ItemName;
    private TextMeshProUGUI ItemDescription;
    private TextMeshProUGUI ItemPrice;
    private Button UseButton;
    private void Awake()
    {
        if (itemConfig == null )
        {
            ItemSprite = transform.Find("ItemSprite").GetComponent<Image>();
        }
        if (ItemName == null)
        {
            ItemName = transform.Find("ItemName").GetComponent<TextMeshProUGUI>();
        }
        if (ItemDescription == null)
        {
            ItemDescription = transform.Find("ItemDescription").GetComponent<TextMeshProUGUI>();
        }
        if (ItemPrice == null)
        {
            ItemPrice = transform.Find("ItemPrice").GetComponent<TextMeshProUGUI>();
        }
        UseButton = GetComponent<Button>();
        UseButton.onClick.AddListener(() =>
        {
            if (itemConfig != null)
            {
                itemConfig.Use();
                GameManager.Instance.ItemManage.ShowItem();
            }
        });
    }
    public void SetItem(ItemConfig itemConfig)
    {
        if (itemConfig != null)
        {
            ItemName.text = itemConfig.ItemName;
            ItemDescription.text = itemConfig.Description;
            ItemPrice.text = $"售价：{itemConfig.Price.ToString()}";
            ItemSprite.sprite = itemConfig.ItemSprite;
            this.itemConfig = itemConfig;
        }
    }
}
