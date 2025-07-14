using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItemGeneratorConfig", menuName = "Tower Defense/Item/ItemGeneratorConfig")]
public class ItemGeneratorConfig : ScriptableObject
{
    [Serializable]
    public class ItemProbability
    {
        public ItemConfig Config;
        [Unit(Units.Percent)]
        public float Value;
    }
    [SerializeField]
    public List<ItemProbability> ItemProbabilities = new List<ItemProbability>();
    private float Itemprobability = 1f;
    
    [Button("概率均分")]
    public void TowerEqualizeProbability()
    {
        float probability = (100f) / ItemProbabilities.Count;
        for (int i = 0; i < ItemProbabilities.Count; i++)
        {
            ItemProbabilities[i].Value = probability;
        }
        float sum = 0;
        foreach (var item in ItemProbabilities)
        {
            sum += item.Value;
        }
        probability = sum;
    }
    private void OnValidate()
    {
       
        float sum = 0;
        foreach (var item in ItemProbabilities)
        {
            sum += item.Value;
        }
        if (sum <= 0f)
        {
            Debug.LogError("BlockProbability 总和不能小于或等于0");
        }
        if (sum > 100f)
        {
            Debug.LogError("BlockProbability 总和需在100%或以内");
        }
        Itemprobability = sum;
       
    }
    
}