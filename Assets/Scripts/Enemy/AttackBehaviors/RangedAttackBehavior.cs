using UnityEngine;

/// <summary>
/// 远程攻击行为 - 发射子弹攻击目标
/// </summary>
[CreateAssetMenu(fileName = "Ranged Attack", menuName = "Tower Defense/Attack Behaviors/Ranged Attack")]
public class RangedAttackBehavior : ScriptableObject, IAttackBehavior
{
    [Header("远程攻击配置")]
    [SerializeField] private float damage = 15f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float bulletSpeed = 8f;
    [SerializeField] private string attackAnimationTrigger = "Attack";
    
    [Header("子弹配置")]
    [SerializeField] private BulletConfig bulletConfig; // 子弹配置
    
    [Header("特效配置")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip bulletHitSound;
    
    public void PerformAttack(EnemyController attacker, GameObject target)
    {
        if (!CanAttack(attacker, target))
            return;
        
        Debug.Log($"{attacker.name} 执行远程攻击，目标: {target.name}");
        
        // 播放攻击动画
        Animator animator = attacker.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger(attackAnimationTrigger);
        }
        
        // 播放攻击音效
        AudioSource audioSource = attacker.GetComponent<AudioSource>();
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
        
        // 查找FirePoint
        Transform firePoint = attacker.transform.Find("FirePoint");
        Vector3 firePosition = firePoint != null ? firePoint.position : attacker.transform.position;
        Vector3 direction = (target.transform.position - firePosition).normalized;
        
        // 使用新的子弹系统
        if (bulletConfig != null)
        {
            var bulletManager = GameManager.Instance?.GetSystem<BulletManager>();
            if (bulletManager != null)
            {
                GameObject bullet = bulletManager.GetBullet(bulletConfig.BulletName, firePosition, Quaternion.identity);
                var bulletScript = bullet.GetComponent<IBullet>();
                if (bulletScript != null)
                {
                    bulletScript.Initialize(direction, bulletSpeed, attacker.gameObject, target, bulletConfig.TargetTags, damage);
                }
                else
                {
                    Debug.LogWarning("子弹预制体未挂载IBullet实现脚本！");
                }
            }
            else
            {
                Debug.LogWarning("BulletManager未找到！");
            }
        }
        else
        {
            Debug.LogWarning("远程攻击行为没有配置子弹配置！请在RangedAttackBehavior中设置BulletConfig。");
        }
    }
    
    public bool CanAttack(EnemyController attacker, GameObject target)
    {
        if (attacker == null || target == null)
            return false;
        float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
        return distance <= attackRange;
    }
    
    public float GetAttackCooldown()
    {
        return attackCooldown;
    }
    
    public float Damage => damage;
    public float AttackRange => attackRange;
    public float BulletSpeed => bulletSpeed;
} 