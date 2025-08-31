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

    [Header("视效系统")]
    [SerializeField] private VisualEffectController visualEffectController;

    private void Awake()
    {
        currentHealth = maxHealth;
        
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
        Debug.Log($"[DamageTaker] {gameObject.name} 尝试播放受击特效");
        
        if (visualEffectController == null)
        {
            Debug.LogError($"[DamageTaker] {gameObject.name} VisualEffectController为空！");
            return;
        }
        
        // 判断是否为敌人（通过Tag判断，可以根据需要调整）
        bool isEnemy = gameObject.CompareTag("Enemy");
        Debug.Log($"[DamageTaker] {gameObject.name} 是否为敌人: {isEnemy}, Tag: {gameObject.tag}");
        
        // 直接调用对应的特效文件，不需要在GameObject上配置
        if (isEnemy)
        {
            // 调用敌人受击特效文件
            Debug.Log($"[DamageTaker] {gameObject.name} 尝试加载敌人受击特效文件");
            var enemyHitPreset = Resources.Load<EffectCombinationPreset>("Data/Effect/EnemyHitEffect");
            
            if (enemyHitPreset != null)
            {
                Debug.Log($"[DamageTaker] {gameObject.name} 成功加载敌人受击特效文件，组合数量: {enemyHitPreset.GetAllCombinationNames().Count}");
                var combination = enemyHitPreset.GetCombination("EnemyHitEffect");
                if (combination != null)
                {
                    Debug.Log($"[DamageTaker] {gameObject.name} 找到EnemyHitEffect组合，特效数量: {combination.effects.Count}");
                    visualEffectController.PlayEffectFromPreset(enemyHitPreset, "EnemyHitEffect");
                }
                else
                {
                    Debug.LogError($"[DamageTaker] {gameObject.name} 未找到EnemyHitEffect组合！");
                }
            }
            else
            {
                Debug.LogError($"[DamageTaker] {gameObject.name} 无法加载敌人受击特效文件！");
            }
        }
        else
        {
            // 调用塔受击特效文件
            Debug.Log($"[DamageTaker] {gameObject.name} 尝试加载塔受击特效文件");
            var towerHitPreset = Resources.Load<EffectCombinationPreset>("Data/Effect/TowerHitEffect");
            
            if (towerHitPreset != null)
            {
                Debug.Log($"[DamageTaker] {gameObject.name} 成功加载塔受击特效文件，组合数量: {towerHitPreset.GetAllCombinationNames().Count}");
                var combination = towerHitPreset.GetCombination("TowerHitEffect");
                if (combination != null)
                {
                    Debug.Log($"[DamageTaker] {gameObject.name} 找到TowerHitEffect组合，特效数量: {combination.effects.Count}");
                    visualEffectController.PlayEffectFromPreset(towerHitPreset, "TowerHitEffect");
                }
                else
                {
                    Debug.LogError($"[DamageTaker] {gameObject.name} 未找到TowerHitEffect组合！");
                }
            }
            else
            {
                Debug.LogError($"[DamageTaker] {gameObject.name} 无法加载塔受击特效文件！");
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
        
        Debug.Log($"[DamageTaker Debug] {this.name} 治疗: {oldHealth:F1} -> {currentHealth:F1} (+{actualHeal:F1}), 治疗量={amount:F1}, 最大血量={maxHealth:F1}");
        
        if (actualHeal > 0)
        {
            onHeal?.Invoke(actualHeal);
            Debug.Log($"[DamageTaker Debug] {this.name} 触发治疗事件回调: +{actualHeal:F1}");
        }
        else
        {
            Debug.Log($"[DamageTaker Debug] {this.name} 治疗无效: 血量已满或治疗量为0");
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