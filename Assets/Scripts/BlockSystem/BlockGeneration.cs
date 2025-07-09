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
     //   BlockShape blockShape = blockGenerationSettings.GetRandomShape();
     //   GameObject tower  = new GameObject();
     // for (int i = 0; i < blockShape.Coordinates.Length; i++)
     // {
     //     Vector2Int coord = blockShape.Coordinates[i];
     //     GameObject towerPrefab = blockGenerationSettings.GetRandomTower();
     //     
     // }
     //  
       
    }
}
