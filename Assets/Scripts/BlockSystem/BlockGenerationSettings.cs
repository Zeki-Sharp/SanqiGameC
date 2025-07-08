using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockGenerationSettings", menuName = "Scriptable Objects/BlockGenerationSettings"),Serializable]
public class BlockGenerationSettings : ScriptableObject
{
    [Header("Tower Setting")]
    [ShowInInspector] public List<GameObject> TowerPrefabs = new List<GameObject>();
    [ShowInInspector] public List<float> TowerProbability = new List<float>();

    [Serializable]
    public struct BlockProbability
    {
        public BlockGenerationConfig Key;
        [Unit(Units.Percent)]
        public float Value;
    }
    // 随机几率
    [Header("Block Setting")]
    [ShowInInspector,Tooltip("概率,总和为100%")]
    public List<BlockProbability> BlockProbabilities = new List<BlockProbability>();


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
        if (sum > 1f)
        {
            Debug.LogError("BlockProbability 总和需在100%或以内");
        }
        if (TowerPrefabs.Count != TowerProbability.Count)
        {
            Debug.LogError("TowerPrefabs 与 TowerProbability 长度不一致");
        }
        
    }
    public BlockGenerationConfig GetRandomShape()
    {
        float total = 0;
        float random = UnityEngine.Random.Range(0, 1);
        foreach (var item in BlockProbabilities)
        {
            total += item.Value;
            if (random <= total)
            {
                return item.Key;
            }
        }
        return  BlockProbabilities[0].Key;
    }
    public GameObject GetRandomTower()
    {
        float total = 0;
        float random = UnityEngine.Random.Range(0, 1);
        int index = 0;
        foreach (var item in TowerProbability)
        {
            total += item;
            if (random <= total)
            {
                return TowerPrefabs[index];
            }
            index++;
        }
        return TowerPrefabs[0];
    }

    
}
