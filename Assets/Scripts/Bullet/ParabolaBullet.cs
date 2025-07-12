using UnityEngine;

public class ParabolaBullet : MonoBehaviour, IBullet
{
    [Header("可调参数")]
    [SerializeField] private float defaultSpeed = 10f;
    [SerializeField] private float defaultDamage = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float height = 2f;
    [SerializeField] private string[] targetTags = new string[] { "Enemy" };

    private Vector3 start;
    private Vector3 end;
    private float speed;
    private float damage;
    private float t;
    private float totalTime;
    private GameObject target;
    private float spawnTime;
    private GameObject owner;

    public void Initialize(Vector3 direction, float speed, float damage, GameObject owner, GameObject target = null, string[] targetTags = null)
    {
        this.start = transform.position;
        this.end = target != null ? target.transform.position : (transform.position + direction * 5f);
        this.speed = defaultSpeed;
        this.damage = damage > 0 ? damage : defaultDamage;
        this.target = target;
        this.owner = owner;
        if (targetTags != null && targetTags.Length > 0)
            this.targetTags = targetTags;
        float distance = Vector3.Distance(start, end);
        totalTime = distance / this.speed;
        t = 0f;
        spawnTime = Time.time;
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