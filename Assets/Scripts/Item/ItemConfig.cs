using Sirenix.OdinInspector;
using UnityEngine;


public abstract class ItemConfig : ScriptableObject
{
    public string ItemName;
    public Sprite ItemSprite;
    [MultiLineProperty]
    public string Description;
    public int Price;

    public abstract void Use();

    public virtual bool ValidateConfig()
    {
        if (string.IsNullOrEmpty(ItemName))
        {
            Debug.LogError("ItemName不能为空");
            return false;
        }

        if (Price <= 0)
        {
            Debug.LogError("Price不能小于或等于0");
            Price = 0;
        }
        return true;
    }
    
}

public enum ItemType
{
    Permanent,
    Temporary
}
