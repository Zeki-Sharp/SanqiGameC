using RaycastPro.Bullets;
using Sirenix.OdinInspector;
using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 通用受伤处理组件，可用于塔和敌人
/// </summary>
public class DamageTaker : MonoBehaviour
{
    [ShowInInspector]public float maxHealth;
    [ShowInInspector] public float currentHealth;

   [ShowInInspector] public System.Action<float> onTakeDamage;
   [ShowInInspector]public System.Action onDeath;
   [ShowInInspector]public System.Action<float> onHeal; // 新增治疗事件回调

    [Header("视效系统")]
    [SerializeField] private VisualEffectController visualEffectController;
    
    [Header("死亡特效")]
    [SerializeField] private MMF_Player deathEffectPlayer; // MMF死亡特效播放器
    
    private bool isDead = false; // 防止重复触发死亡特效

    private void Awake()
    {
        currentHealth = maxHealth;
        
        // 自动获取MMF Player组件
        if (deathEffectPlayer == null)
        {
            deathEffectPlayer = GetComponent<MMF_Player>();
        }
        
        // 如果没有手动指定VisualEffectController，尝试自动获取
        if (visualEffectController == null)
        {
            visualEffectController = GetComponent<VisualEffectController>();
        }
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="amount">伤害值</param>
    public virtual void TakeDamage(float amount)
    {
        currentHealth -= amount;
        onTakeDamage?.Invoke(amount);
        
        // 播放受击特效
        PlayHitEffect();
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    /// <summary>
    /// 播放受击特效
    /// </summary>
    private void PlayHitEffect()
    {
        if (visualEffectController == null) return;
        
        if (gameObject.CompareTag("Enemy"))
        {
            var enemyHitPreset = Resources.Load<EffectCombinationPreset>("Data/Effect/EnemyHitEffect");
            if (enemyHitPreset != null)
            {
                visualEffectController.PlayEffectFromPreset(enemyHitPreset, "EnemyHitEffect");
            }
        }
        else
        {
            var towerHitPreset = Resources.Load<EffectCombinationPreset>("Data/Effect/TowerHitEffect");
            if (towerHitPreset != null)
            {
                visualEffectController.PlayEffectFromPreset(towerHitPreset, "TowerHitEffect");
            }
        }
    }

    /// <summary>
    /// 接受治疗
    /// </summary>
    /// <param name="amount">治疗量</param>
    public virtual void Heal(float amount)
    {
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        float actualHeal = currentHealth - oldHealth;
        
        if (actualHeal > 0)
        {
            onHeal?.Invoke(actualHeal);
        }
    }

    public void OnBullet(Bullet bullet)
    {
        TakeDamage(bullet.damage);
    }

    /// <summary>
    /// 死亡处理
    /// </summary>
    protected virtual void Die()
    {
        // 防止重复触发死亡
        if (isDead) return;
        isDead = true;
        
        // 禁用碰撞检测
        DisableCollision();
        
        onDeath?.Invoke();
        
        // 检查是否是主塔死亡
        if (gameObject.CompareTag("CenterTower"))
        {
            Debug.Log("主塔死亡，通知VictoryConditionChecker");
            var victoryChecker = GameManager.Instance?.GetSystem<VictoryConditionChecker>();
            if (victoryChecker != null)
            {
                victoryChecker.OnCenterTowerDestroyed();
            }
        }
        
        // 播放死亡特效（仅对敌人）
        if (gameObject.CompareTag("Enemy"))
        {
            PlayDeathEffect();
            // 延迟销毁，确保特效播放完成
            StartCoroutine(DestroyAfterEffect());
        }
        else
        {
            // 非敌人对象直接销毁
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 播放死亡爆炸特效
    /// </summary>
    private void PlayDeathEffect()
    {
        if (deathEffectPlayer != null)
        {
            deathEffectPlayer.PlayFeedbacks();
        }
    }
    
    /// <summary>
    /// 禁用碰撞检测
    /// </summary>
    private void DisableCollision()
    {
        // 禁用所有Collider组件
        var colliders = GetComponents<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
        
        // 禁用所有Collider2D组件
        var colliders2D = GetComponents<Collider2D>();
        foreach (var collider2D in colliders2D)
        {
            collider2D.enabled = false;
        }
        
        // 禁用所有子对象的Collider
        var childColliders = GetComponentsInChildren<Collider>();
        foreach (var collider in childColliders)
        {
            collider.enabled = false;
        }
        
        // 禁用所有子对象的Collider2D
        var childColliders2D = GetComponentsInChildren<Collider2D>();
        foreach (var collider2D in childColliders2D)
        {
            collider2D.enabled = false;
        }
    }
    
    /// <summary>
    /// 延迟销毁对象，确保特效播放完成
    /// </summary>
    private System.Collections.IEnumerator DestroyAfterEffect()
    {
        yield return new WaitForSeconds(0.4f);
        Destroy(gameObject);
    }
} 