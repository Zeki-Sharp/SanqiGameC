using System;
using UnityEngine;

/// <summary>
/// 敌人死亡事件参数
/// </summary>
public class EnemyDeathEventArgs : EventArgs
{
    public GameObject Enemy;
    public int GoldReward;
    public Vector3 DeathPosition;
    public string EnemyName;
    
    public EnemyDeathEventArgs(GameObject enemy, int goldReward, Vector3 deathPosition)
    {
        Enemy = enemy;
        GoldReward = goldReward;
        DeathPosition = deathPosition;
        EnemyName = enemy != null ? enemy.name : "Unknown";
    }
} 