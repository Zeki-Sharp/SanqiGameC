using UnityEngine;

/// <summary>
/// IBullet负责运动、命中检测和伤害，伤害由发射者直接传递。
/// </summary>
public interface IBullet
{
    void Initialize(Vector3 direction, float speed, GameObject owner, GameObject target = null, string[] targetTags = null, float damage = 0f);
} 