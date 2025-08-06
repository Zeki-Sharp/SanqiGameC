using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Wave
{
    public float delayBeforeWave = 0f;
    public List<EnemySpawnInfo> enemies = new List<EnemySpawnInfo>();
    public GameObject enemyPrefab;
}