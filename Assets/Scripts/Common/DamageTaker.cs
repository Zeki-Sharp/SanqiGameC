using RaycastPro.Bullets;
using Sirenix.OdinInspector;
using UnityEngine;

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

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="amount">伤害值</param>
    public virtual void TakeDamage(float amount)
    {
        currentHealth -= amount;
        onTakeDamage?.Invoke(amount);
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
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
        onDeath?.Invoke();
        // 默认销毁对象，可重写
        Destroy(gameObject);
    }
} 