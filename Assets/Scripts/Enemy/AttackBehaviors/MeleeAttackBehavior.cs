using UnityEngine;

/// <summary>
/// 近战攻击行为 - 直接接触目标造成伤害
/// </summary>
[CreateAssetMenu(fileName = "Melee Attack", menuName = "Tower Defense/Attack Behaviors/Melee Attack")]
public class MeleeAttackBehavior : ScriptableObject, IAttackBehavior
{
    [Header("近战攻击配置")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private string attackAnimationTrigger = "Attack";
    
    [Header("特效配置")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private AudioClip attackSound;
    
    public void PerformAttack(EnemyController attacker, GameObject target)
    {
        if (!CanAttack(attacker, target))
            return;
            
        Debug.Log($"{attacker.name} 执行近战攻击，目标: {target.name}");
        
        // 对目标造成伤害
        DamageTaker targetDamageTaker = target.GetComponent<DamageTaker>();
        if (targetDamageTaker != null)
        {
            targetDamageTaker.TakeDamage(damage);
            Debug.Log($"{attacker.name} 对 {target.name} 造成 {damage} 点伤害");
        }
        
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
        
        // 生成命中特效
        if (hitEffectPrefab != null)
        {
            Vector3 hitPosition = target.transform.position;
            GameObject hitEffect = Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
            Destroy(hitEffect, 2f); // 2秒后销毁特效
        }
    }
    
    public bool CanAttack(EnemyController attacker, GameObject target)
    {
        if (attacker == null || target == null)
            return false;
            
        // 检查距离
        float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
        return distance <= attackRange;
    }
    
    public float GetAttackCooldown()
    {
        return attackCooldown;
    }
    
    // 公共属性，用于在Inspector中查看
    public float Damage => damage;
    public float AttackRange => attackRange;
} 