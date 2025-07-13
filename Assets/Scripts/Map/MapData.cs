using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "Tower Defense/Map/MapData")]
public class MapData : ScriptableObject
{
    public GameObject centerTower;
    public GameObject BlockPrefab;
    public BlockGenerationSettings BlockGenerationSettings;
    
}
