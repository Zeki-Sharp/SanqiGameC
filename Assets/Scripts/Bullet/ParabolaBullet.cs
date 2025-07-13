using UnityEngine;
using System.Collections.Generic;

public class ParabolaBullet : MonoBehaviour, IBullet
{
    public enum TargetType { Single, Aoe }

    [Header("可调参数")]
    [SerializeField] private float defaultSpeed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float height = 2f;
    [SerializeField] private string[] targetTags = new string[] { "Enemy" };
    [Header("命中类型")]
    public TargetType targetType = TargetType.Single;
    [Header("Aoe参数")]
    public float aoeRadius = 1.5f;
    public LayerMask aoeLayer;

    private Vector3 start;
    private Vector3 end;
    private float speed;
    private float t;
    private float totalTime;
    private GameObject target;
    private float spawnTime;
    private GameObject owner;
    private float damage;

    public void Initialize(Vector3 direction, float speed, GameObject owner, GameObject target = null, string[] targetTags = null, float damage = 0f)
    {
        this.start = transform.position;
        this.end = target != null ? target.transform.position : (transform.position + direction * 5f);
        this.speed = defaultSpeed;
        this.target = target;
        this.owner = owner;
        if (targetTags != null && targetTags.Length > 0)
            this.targetTags = targetTags;
        float distance = Vector3.Distance(start, end);
        totalTime = distance / this.speed;
        t = 0f;
        spawnTime = Time.time;
        this.damage = damage;
    }

    private void Update()
    {
        t += Time.deltaTime / totalTime;
        if (t > 1f || Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
            return;
        }
        Vector3 pos = Vector3.Lerp(start, end, t);
        pos.y += height * 4 * (t - t * t);
        transform.position = pos;
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