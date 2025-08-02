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
            Debug.Log($"{attacker.name} 使用子弹配置: {bulletConfig.BulletName}");
            var bulletManager = GameManager.Instance?.GetSystem<BulletManager>();
            if (bulletManager != null)
            {
                GameObject bullet = bulletManager.GetBullet(bulletConfig.BulletName, firePosition, Quaternion.identity);
                if (bullet != null)
                {
                    Debug.Log($"{attacker.name} 成功获取子弹: {bullet.name}");
                    var bulletScript = bullet.GetComponent<IBullet>();
                    if (bulletScript != null)
                    {
                        Debug.Log($"{attacker.name} 初始化子弹，方向: {direction}, 速度: {bulletSpeed}, 伤害: {damage}");
                        bulletScript.Initialize(direction, bulletSpeed, attacker.gameObject, target, bulletConfig.TargetTags, damage);
                        Debug.Log($"{attacker.name} 子弹初始化完成");
                    }
                    else
                    {
                        Debug.LogWarning($"{attacker.name} 子弹预制体未挂载IBullet实现脚本！");
                    }
                }
                else
                {
                    Debug.LogError($"{attacker.name} 获取子弹失败！");
                }
            }
            else
            {
                Debug.LogWarning($"{attacker.name} BulletManager未找到！");
            }
        }
        else
        {
            Debug.LogWarning($"{attacker.name} 远程攻击行为没有配置子弹配置！请在RangedAttackBehavior中设置BulletConfig。");
        }
    }
    
    public bool CanAttack(EnemyController attacker, GameObject target)
    {
        if (attacker == null || target == null)
            return false;
        float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
        return distance <= attacker.AttackRange;
    }
    
    public float GetAttackCooldown()
    {
        return attackCooldown;
    }
    
    public float Damage => damage;
    public float BulletSpeed => bulletSpeed;
    
    // 攻击范围现在由EnemyData统一管理，此属性仅满足接口要求
    public float AttackRange => 0f;
} 