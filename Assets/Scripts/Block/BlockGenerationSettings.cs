using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockGenerationSettings", menuName = "Scriptable Objects/BlockGenerationSettings"),Serializable]
public class BlockGenerationSettings : ScriptableObject
{
    [Header("Tower Setting")]
    [ShowInInspector] public List<TowerData> TowerDatas = new List<TowerData>();
    [ShowInInspector] public List<float> TowerProbability = new List<float>();

    [Serializable]
    public class BlockProbability
    {
        public BlockGenerationConfig Config;
        [Unit(Units.Percent)]
        public float Value;
    }
    // 随机几率
    [Header("Block Setting")]
    [ShowInInspector,Tooltip("概率,总和为100%")]
    public List<BlockProbability> BlockProbabilities = new List<BlockProbability>();
    [SerializeField,ReadOnly]
    public float probability = 1f;
    
    [Button("概率均分")]
    public void EqualizeProbability()
    {
        float probability = (100f-1) / BlockProbabilities.Count;
        for (int i = 0; i < BlockProbabilities.Count; i++)
        {
            BlockProbabilities[i].Value = probability;
        }
        float sum = 1;
        foreach (var item in BlockProbabilities)
        {
            sum += item.Value;
        }
        probability = sum;
    }
    private void OnValidate()
    {
       
        float sum = 1;
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
        if (TowerDatas.Count != TowerProbability.Count)
        {
            Debug.LogError("TowerPrefabs 与 TowerProbability 长度不一致");
        }
        probability = sum;
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
        if (TowerDatas.Count == 0)
        {
            return null;
        }
        float total = 0;
        float random = UnityEngine.Random.Range(0, 100);
        int index = 0;
        foreach (var item in TowerProbability)
        {
            total += item;
            if (random <= total)
            {
                return TowerDatas[index];
            }
            index++;
        }
        return TowerDatas[0];
    }

    
}
