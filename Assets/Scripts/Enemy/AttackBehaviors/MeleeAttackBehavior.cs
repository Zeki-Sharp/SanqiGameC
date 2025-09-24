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
        Debug.Log($"[Melee Debug] {attacker.name} 开始执行近战攻击，目标: {target.name}");
        
        if (!CanAttack(attacker, target))
        {
            Debug.LogWarning($"[Melee Debug] {attacker.name} 无法攻击 {target.name}，CanAttack 返回 false");
            return;
        }
            
        Debug.Log($"[Melee Debug] {attacker.name} CanAttack 检查通过，继续执行攻击");
        Debug.Log($"[Melee Debug] 攻击者位置: {attacker.transform.position}, 目标位置: {target.transform.position}");
        Debug.Log($"[Melee Debug] 两者距离: {Vector3.Distance(attacker.transform.position, target.transform.position):F3}");
            
        // 对目标造成伤害
        DamageTaker targetDamageTaker = target.GetComponent<DamageTaker>();
        if (targetDamageTaker != null)
        {
            Debug.Log($"[Melee Debug] 找到目标 DamageTaker，当前血量: {targetDamageTaker.currentHealth}/{targetDamageTaker.maxHealth}");
            float oldHealth = targetDamageTaker.currentHealth;
            targetDamageTaker.TakeDamage(damage);
            Debug.Log($"[Melee Debug] {attacker.name} 对 {target.name} 造成 {damage} 点伤害，血量变化: {oldHealth} -> {targetDamageTaker.currentHealth}");
        }
        else
        {
            Debug.LogError($"[Melee Debug] 目标 {target.name} 没有 DamageTaker 组件！");
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
        {
            Debug.LogWarning($"[Melee Debug] CanAttack 检查失败: attacker={attacker}, target={target}");
            return false;
        }
            
        // 检查距离
        float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
        bool inRange = distance <= attackRange;
        
        Debug.Log($"[Melee Debug] CanAttack 距离检查: {attacker.name} -> {target.name}, 距离={distance:F3}, 攻击范围={attackRange:F3}, 结果={inRange}");
        
        return inRange;
    }
    
    public float GetAttackCooldown()
    {
        return attackCooldown;
    }
    
    // 公共属性，用于在Inspector中查看
    public float Damage => damage;
    public float AttackRange => attackRange;
} 