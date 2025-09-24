using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockGenerationSettings", menuName = "Tower Defense/BlockGeneration/BlockGenerationSettings"),Serializable]
public class BlockGenerationSettings : ScriptableObject
{
    [Serializable]
    public class TowerProbability
    {
        public TowerData Config;
        [Unit(Units.Percent)]
        public float Value;
    }
    // 随机几率
    [Header("Tower Setting")]
    [SerializeField,Tooltip("概率,总和为100%")]
    private List<TowerProbability> TowerProbabilities = new List<TowerProbability>();
    [SerializeField,ReadOnly]
    private float Towerprobability = 1f;
    [Button("塔概率均分")]
    public void TowerEqualizeProbability()
    {
        float probability = (100f) / TowerProbabilities.Count;
        for (int i = 0; i < TowerProbabilities.Count; i++)
        {
            TowerProbabilities[i].Value = probability;
        }
        float sum = 0;
        foreach (var item in TowerProbabilities)
        {
            sum += item.Value;
        }
        Towerprobability = sum;
    }
    
    [Serializable]
    public class BlockProbability
    {
        public BlockGenerationConfig Config;
        [Unit(Units.Percent)]
        public float Value;
    }
    // 随机几率
    [Header("Block Setting")]
    [SerializeField,Tooltip("概率,总和为100%")]
    private List<BlockProbability> BlockProbabilities = new List<BlockProbability>();
    [SerializeField,ReadOnly]
    private float Blockprobability = 1f;
    
    [Button("方块概率均分")]
    public void BlockEqualizeProbability()
    {
        float probability = (100f) / BlockProbabilities.Count;
        for (int i = 0; i < BlockProbabilities.Count; i++)
        {
            BlockProbabilities[i].Value = probability;
        }
        float sum = 0;
        foreach (var item in BlockProbabilities)
        {
            sum += item.Value;
        }
        Blockprobability = sum;
    }
  
    private void OnValidate()
    {
       
        float sum = 0;
        foreach (var item in BlockProbabilities)
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
        Blockprobability = sum;
        sum = 0;
        foreach (var item in TowerProbabilities)
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
        Towerprobability = sum;
       
    }
    public BlockGenerationConfig GetRandomShape()
    {
        float total = 0;
        float random = UnityEngine.Random.Range(0, 100);
        foreach (var item in BlockProbabilities)
        {
            total += item.Value;
            if (random <= total)
            {
                return item.Config;
            }
        }
        return  BlockProbabilities[0].Config;
    }
    public TowerData GetRandomTower()
    {
        if (TowerProbabilities.Count == 0)
        {
            return null;
        }
        float total = 0;
        float random = UnityEngine.Random.Range(0, 100);
        int index = 0;
        foreach (var item in TowerProbabilities)
        {
            total += item.Value;
            if (random <= total)
            {
                return item.Config;
            }
            index++;
        }
        return TowerProbabilities[0].Config;
    }

    
}
