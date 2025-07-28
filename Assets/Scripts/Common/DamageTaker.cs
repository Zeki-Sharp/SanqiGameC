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