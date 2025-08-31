using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 特效组合预设 - 包含所有可用的特效组合
/// </summary>
[CreateAssetMenu(fileName = "Effect Combination Preset", menuName = "Tower Defense/Effect Combination Preset")]
public class EffectCombinationPreset : ScriptableObject
{
    [Header("特效组合列表")]
    [SerializeField] private List<EffectCombination> combinations = new List<EffectCombination>();
    
    /// <summary>
    /// 根据名称获取特效组合
    /// </summary>
    public EffectCombination GetCombination(string combinationName)
    {
        foreach (var combination in combinations)
        {
            if (combination != null && combination.combinationName == combinationName)
            {
                return combination;
            }
        }
        
        Debug.LogWarning($"未找到名为 '{combinationName}' 的特效组合");
        return null;
    }
    
    /// <summary>
    /// 获取所有特效组合名称（用于调试）
    /// </summary>
    public List<string> GetAllCombinationNames()
    {
        var names = new List<string>();
        foreach (var combination in combinations)
        {
            if (combination != null && !string.IsNullOrEmpty(combination.combinationName))
            {
                names.Add(combination.combinationName);
            }
        }
        return names;
    }
    
    /// <summary>
    /// 添加新的特效组合
    /// </summary>
    public void AddCombination(EffectCombination combination)
    {
        if (combination != null && !string.IsNullOrEmpty(combination.combinationName))
        {
            combinations.Add(combination);
        }
    }
    
    /// <summary>
    /// 移除特效组合
    /// </summary>
    public void RemoveCombination(string combinationName)
    {
        for (int i = combinations.Count - 1; i >= 0; i--)
        {
            if (combinations[i] != null && combinations[i].combinationName == combinationName)
            {
                combinations.RemoveAt(i);
            }
        }
    }
}
