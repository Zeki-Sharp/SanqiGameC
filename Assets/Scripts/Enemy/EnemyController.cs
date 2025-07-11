using UnityEngine;

/// <summary>
/// 敌人控制器 - 管理敌人的状态机和行为
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("敌人设置")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float maxHealth = 100f;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Color attackRangeColor = Color.red;
    
    // 私有变量
    private EnemyState currentState;
    private float currentHealth;
    private SpriteRenderer spriteRenderer;
    
    // 公共属性
    public float AttackRange => attackRange;
    public float MoveSpeed => moveSpeed;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
    }
    
    private void Start()
    {
        // 初始化为移动状态
        ChangeState(new EnemyMoveState(this));
    }
    
    private void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
            currentState.CheckTransitions();
        }
    }
    
    /// <summary>
    /// 切换状态
    /// </summary>
    /// <param name="newState">新状态</param>
    public void ChangeState(EnemyState newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }
        
        currentState = newState;
        
        if (currentState != null)
        {
            currentState.Enter();
        }
    }
    
    /// <summary>
    /// 检查攻击范围内是否有塔
    /// </summary>
    /// <returns>是否有塔在攻击范围内</returns>
    public bool IsTowerInAttackRange()
    {
        GameObject[] towers = GameObject.FindGameObjectsWithTag("centerTower");
        
        foreach (GameObject tower in towers)
        {
            float distance = Vector3.Distance(transform.position, tower.transform.position);
            if (distance <= attackRange)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            Debug.Log($"{name} 受到 {damage} 点伤害，剩余生命值: {currentHealth}");
        }
    }
    
    /// <summary>
    /// 敌人死亡
    /// </summary>
    private void Die()
    {
        Debug.Log($"{name} 死亡");
        // TODO: 播放死亡动画、音效等
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 获取当前状态名称
    /// </summary>
    /// <returns>状态名称</returns>
    public string GetCurrentStateName()
    {
        return currentState?.GetType().Name ?? "None";
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        // 绘制攻击范围
        Gizmos.color = attackRangeColor;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 绘制朝向
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.right * 1f);
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        // 在Scene视图中显示调试信息
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
            $"状态: {GetCurrentStateName()}\n生命值: {currentHealth:F0}/{maxHealth:F0}");
        #endif
    }
} 