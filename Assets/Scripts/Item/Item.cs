using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    private ItemConfig itemConfig;
    public Image ItemSprite;
    public TextMeshProUGUI ItemName;
    public TextMeshProUGUI ItemDescription;
    public TextMeshProUGUI ItemPrice;

    private void Awake()
    {
        if (itemConfig == null )
        {
            ItemSprite = GetComponentInChildren<Image>();
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
    }
    public void SetItem(ItemConfig itemConfig)
    {
        if (itemConfig != null)
        {
            ItemName.text = itemConfig.ItemName;
            ItemDescription.text = itemConfig.Description;
            ItemPrice.text = itemConfig.Price.ToString();
            ItemSprite.sprite = itemConfig.ItemSprite;
            this.itemConfig = itemConfig;
        }
    }
}
