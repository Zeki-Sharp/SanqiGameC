using UnityEngine;

/// <summary>
/// 敌人子弹 - 专门用于敌人的远程攻击
/// </summary>
public class EnemyBullet : MonoBehaviour
{
    [Header("子弹配置")]
    [SerializeField] private float bulletSpeed = 8f;
    [SerializeField] private float bulletLifetime = 5f;
    private float damage;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private AudioClip hitSound;
    
    private Vector3 direction;
    private float spawnTime;
    private GameObject owner; // 发射者
    
    /// <summary>
    /// 初始化子弹
    /// </summary>
    /// <param name="direction">发射方向</param>
    /// <param name="speed">子弹速度</param>
    /// <param name="damage">伤害值</param>
    /// <param name="owner">发射者</param>
    public void Initialize(Vector3 direction, float speed, float damage, GameObject owner)
    {
        this.direction = direction.normalized;
        this.bulletSpeed = speed;
        this.damage = damage;
        this.owner = owner;
        this.spawnTime = Time.time;
        
        // 设置子弹朝向
        if (direction != Vector3.zero)
        {
            transform.right = direction;
        }
    }
    
    private void Update()
    {
        // 移动子弹
        transform.position += direction * bulletSpeed * Time.deltaTime;
        
        // 检查生命周期
        if (Time.time - spawnTime > bulletLifetime)
        {
            Destroy(gameObject);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检查是否击中目标
        if (other.CompareTag("CenterTower") || other.CompareTag("Tower"))
        {
            // 对目标造成伤害
            DamageTaker targetDamageTaker = other.GetComponent<DamageTaker>();
            if (targetDamageTaker != null)
            {
                targetDamageTaker.TakeDamage(damage);
                Debug.Log($"敌人子弹击中 {other.name}，造成 {damage} 点伤害");
            }
            
            // 播放击中音效
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
            
            // 生成击中特效
            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                Destroy(hitEffect, 2f);
            }
            
            // 销毁子弹
            Destroy(gameObject);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // 绘制子弹轨迹
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, direction * 2f);
    }
} 