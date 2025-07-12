using UnityEngine;

public class StraightBullet : MonoBehaviour, IBullet
{
    [Header("可调参数")]
    [SerializeField] private float defaultSpeed = 10f;
    [SerializeField] private float defaultDamage = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private string[] targetTags = new string[] { "Enemy" };

    private Vector3 direction;
    private float speed;
    private float damage;
    private float spawnTime;
    private GameObject owner;

    public void Initialize(Vector3 direction, float speed, float damage, GameObject owner, GameObject target = null, string[] targetTags = null)
    {
        this.direction = direction.normalized;
        this.speed = defaultSpeed;
        this.damage = damage > 0 ? damage : defaultDamage;
        this.owner = owner;
        this.spawnTime = Time.time;
        if (targetTags != null && targetTags.Length > 0)
            this.targetTags = targetTags;
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
        foreach (var tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                var taker = other.GetComponent<DamageTaker>();
                if (taker != null)
                    taker.TakeDamage(damage);
                Destroy(gameObject);
                break;
            }
        }
    }
} 