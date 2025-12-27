using System;
using UnityEngine;

public class BlockGeneration : MonoBehaviour
{
    public BlockGenerationSettings blockGenerationSettings;
    
    private void Awake()
    {
        blockGenerationSettings = Resources.Load<BlockGenerationSettings>("BlockGenerationSettings");
    }
}
