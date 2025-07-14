using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItemConfig", menuName = "Tower Defense/Item/ItemConfig")]
public class ItemConfig : ScriptableObject
{
    public string ItemName;
    public Sprite ItemSprite;
    [MultiLineProperty]
    public string Description;
    public int Price;
    
}