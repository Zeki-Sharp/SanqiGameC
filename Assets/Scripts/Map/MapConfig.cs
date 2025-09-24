using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "Tower Defense/ALL/MapData")]
public class MapConfig : ScriptableObject
{
    [Header("地图设置")]
    public GameObject centerTower;
    public GameObject blockPrefab;
    public BlockGenerationSettings blockGenerationSettings;
    public ItemGeneratorConfig itemGeneratorConfig;
    [Header("初始设置")]
    public List<MapData> MapDatas = new List<MapData>();
    
    public MapData GetMapData(DifficultyLevel level)
    {
        return MapDatas.Find(x => x.difficulty == level);
    }
}