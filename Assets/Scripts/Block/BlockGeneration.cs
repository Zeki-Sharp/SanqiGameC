using System;
using UnityEngine;

public class BlockGeneration : MonoBehaviour
{
    public BlockGenerationSettings blockGenerationSettings;
    
    private void Awake()
    {
        blockGenerationSettings = Resources.Load<BlockGenerationSettings>("BlockGenerationSettings");
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.F5))
        {
            
        }
    }
    public void SetBlocks()
    {
        // 已迁移到BlockGenerationConfig体系
    }
}
