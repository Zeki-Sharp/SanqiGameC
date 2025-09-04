using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 效果项UI组件 - 用于在UI中显示激活的物品效果
/// </summary>
public class EffectItemUI : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private Image effectIcon;           // 效果图标
    // [SerializeField] private TextMeshProUGUI effectName; // 效果名称
    // [SerializeField] private TextMeshProUGUI effectDesc; // 效果描述
    [SerializeField] private string effectName;
    [SerializeField] private string effectDesc;
    [SerializeField] private TextMeshProUGUI effectDuration; // 效果持续时间（回合数）
    
    /// <summary>
    /// 设置效果数据显示
    /// </summary>
    /// <param name="name">效果名称</param>
    /// <param name="description">效果描述</param>
    /// <param name="icon">效果图标</param>
    public void SetEffectData(string name, string description, Sprite icon)
    {
        if (effectName != null)
            effectName = name;
            
        if (effectDesc != null)
            effectDesc = description;
            
        if (effectIcon != null && icon != null)
            effectIcon.sprite = icon;
    }
    
    /// <summary>
    /// 设置临时效果的回合数显示
    /// </summary>
    /// <param name="currentRound">当前回合数</param>
    /// <param name="maxRound">最大回合数</param>
    public void SetDurationText(int currentRound, int maxRound)
    {
        if (effectDuration != null)
        {
            effectDuration.text = $"{currentRound}/{maxRound}";
            effectDuration.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// 隐藏回合数显示（用于永久物品）
    /// </summary>
    public void HideDurationText()
    {
        if (effectDuration != null)
        {
            effectDuration.gameObject.SetActive(false);
        }
    }
}