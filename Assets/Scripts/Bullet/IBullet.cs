using UnityEngine;

public interface IBullet
{
    void Initialize(Vector3 direction, float speed, float damage, GameObject owner, GameObject target = null, string[] targetTags = null);
} 