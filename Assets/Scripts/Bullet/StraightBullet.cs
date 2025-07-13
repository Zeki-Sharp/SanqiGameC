using UnityEngine;
using System.Collections.Generic;

public class StraightBullet : MonoBehaviour, IBullet
{
    public enum TargetType { Single, Aoe }

    [Header("可调参数")]
    [SerializeField] private float defaultSpeed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private string[] targetTags = new string[] { "Enemy" };
    [Header("命中类型")]
    public TargetType targetType = TargetType.Single;
    [Header("Aoe参数")]
    public float aoeRadius = 1.5f;
    public LayerMask aoeLayer;

    private Vector3 direction;
    private float speed;
    private float spawnTime;
    private GameObject owner;
    private float damage;

    public void Initialize(Vector3 direction, float speed, GameObject owner, GameObject target = null, string[] targetTags = null, float damage = 0f)
    {
        this.direction = direction.normalized;
        this.speed = defaultSpeed;
        this.owner = owner;
        this.spawnTime = Time.time;
        if (targetTags != null && targetTags.Length > 0)
            this.targetTags = targetTags;
        this.damage = damage;
        transform.right = direction;
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        if (Time.time - spawnTime > lifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner) return;
        List<GameObject> targets = new List<GameObject>();
        if (targetType == TargetType.Single)
        {
            foreach (var tag in targetTags)
            {
                if (other.CompareTag(tag))
                {
                    targets.Add(other.gameObject);
                    break;
                }
            }
        }
        else if (targetType == TargetType.Aoe)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius, aoeLayer);
            foreach (var hit in hits)
            {
                if (hit.gameObject == owner) continue;
                foreach (var tag in targetTags)
                {
                    if (hit.CompareTag(tag))
                    {
                        targets.Add(hit.gameObject);
                        break;
                    }
                }
            }
        }
        if (targets.Count > 0)
        {
            foreach (var target in targets)
            {
                // 1. 先造成伤害
                var taker = target.GetComponent<DamageTaker>();
                if (taker != null)
                    taker.TakeDamage(this.damage);
                // 2. 再分发所有效果
                var effectControllers = GetComponents<IBulletEffectDispatcher>();
                foreach (var dispatcher in effectControllers)
                {
                    dispatcher.DispatchEffect(target, owner);
                }
            }
            Destroy(gameObject);
        }
    }
} 