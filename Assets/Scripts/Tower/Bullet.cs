using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("子弹配置")]
    public BulletConfig bulletConfig;

    private Transform target;
    private float speed;
    private float damage;
    private float lifeTime;
    private float spawnTime;

    public void Init(Transform target, float damage, BulletConfig config)
    {
        this.target = target;
        this.damage = damage;
        this.bulletConfig = config;
        if (config != null)
        {
            speed = config.BulletSpeed;
            lifeTime = config.BulletLifeTime;
        }
        else
        {
            speed = 10f;
            lifeTime = 3f;
        }
        spawnTime = Time.time;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }
        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            var taker = target.GetComponent<DamageTaker>();
            if (taker != null)
                taker.TakeDamage(damage);
            if (bulletConfig != null && bulletConfig.hitEffect != null)
                Instantiate(bulletConfig.hitEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
        if (Time.time - spawnTime > lifeTime)
        {
            Destroy(gameObject);
        }
    }
} 