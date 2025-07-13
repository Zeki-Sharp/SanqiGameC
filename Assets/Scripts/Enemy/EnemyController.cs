using UnityEngine;

/// <summary>
/// 敌人控制器 - 管理敌人的状态机和行为
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("数据配置")]
    public EnemyData data; // 直接在Prefab Inspector拖拽

    private DamageTaker damageTaker;

    // 私有变量
    private EnemyState currentState;
    private float currentHealth;
    private SpriteRenderer spriteRenderer;
    private float moveSpeedOverride = -1f;
    
    // 公共属性
    public float AttackRange => data != null ? data.AttackRange : 1.5f;
    public float MoveSpeed {
        get {
            if (moveSpeedOverride >= 0f) return moveSpeedOverride;
            return data != null ? data.MoveSpeed : 2f;
        }
        set {
            moveSpeedOverride = value;
        }
    }
    public float CurrentHealth => currentHealth;
    public float MaxHealth => data != null ? data.MaxHealth : 100f;
    public IAttackBehavior AttackBehavior => data != null ? data.AttackBehavior : null;
    
    private void Awake()
    {
        damageTaker = GetComponent<DamageTaker>();
        if (data != null && damageTaker != null)
        {
            damageTaker.maxHealth = data.MaxHealth;
            damageTaker.currentHealth = data.MaxHealth;
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = MaxHealth;
        
        // 确保敌人有正确的标签
        if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag != "Enemy")
        {
            gameObject.tag = "Enemy";
        }
        
        // 确保Z轴位置正确
        Vector3 position = transform.position;
        if (position.z != 0f)
        {
            position.z = 0f;
            transform.position = position;
        }
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
    /// 检查攻击范围内是否有塔（centerTower或tower标签）
    /// </summary>
    /// <returns>是否有塔在攻击范围内</returns>
    public bool IsTowerInAttackRange()
    {
        string[] tags = { "CenterTower", "Tower" };
        foreach (string tag in tags)
        {
            GameObject[] towers = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject tower in towers)
            {
                float distance = Vector3.Distance(transform.position, tower.transform.position);
                if (distance <= AttackRange)
                {
                    return true;
                }
            }
        }
        return false;
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
        // 绘制攻击范围
        Gizmos.color = Color.red; // 使用默认颜色
        Gizmos.DrawWireSphere(transform.position, AttackRange);
        
        // 绘制朝向
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.right * 1f);
    }
    
    private void OnDrawGizmos()
    {
        // 在Scene视图中显示调试信息
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
            $"状态: {GetCurrentStateName()}\n生命值: {currentHealth:F0}/{MaxHealth:F0}");
        #endif
    }
} 