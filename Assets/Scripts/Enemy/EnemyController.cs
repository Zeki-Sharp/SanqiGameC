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
    [SerializeField]private EnemyState currentState;
    private float currentHealth;
    private SpriteRenderer spriteRenderer;
    private float moveSpeedOverride = -1f;
    private float difDistance;
    
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
        difDistance = Random.Range(0.1f, 1f);
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
        
        // 调试信息
        Debug.Log($"敌人 {name} 初始化完成，攻击行为: {AttackBehavior?.GetType().Name ?? "null"}");
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
    /// 检查攻击范围内是否有塔（centerTower或tower标签），排除ShowArea塔
    /// </summary>
    /// <returns>是否有塔在攻击范围内</returns>
    public bool IsTowerInAttackRange()
    {
        // 优先检查中心塔
        GameObject centerTower = GameObject.FindGameObjectWithTag("CenterTower");
        if (centerTower != null && !IsShowAreaTower(centerTower))
        {
            float distance = Vector3.Distance(transform.position, centerTower.transform.position);
            if ((distance+ difDistance ) <= AttackRange)
            {
                Debug.Log($"{name} 在攻击范围内找到中心塔，距离: {distance:F2}");
                return true;
            }
        }
        
        // 检查普通塔
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        Debug.Log($"{name} 找到 {towers.Length} 个普通塔");
        
        foreach (GameObject tower in towers)
        {
            // 过滤掉ShowArea塔
            if (IsShowAreaTower(tower))
            {
                Debug.Log($"{name} 跳过ShowArea塔: {tower.name}");
                continue;
            }
                
            float distance = Vector3.Distance(transform.position, tower.transform.position);
            if ((distance+ difDistance ) <= AttackRange )
            {
                Debug.Log($"{name} 在攻击范围内找到塔 {tower.name}，距离: {distance:F2}");
                return true;
            }
        }
        
        Debug.Log($"{name} 没有在攻击范围内找到塔");
        return false;
    }
    
    /// <summary>
    /// 检查中心塔是否被摧毁
    /// </summary>
    /// <returns>如果中心塔不存在或被摧毁则返回true</returns>
    public bool IsCenterTowerDestroyed()
    {
        GameObject centerTower = GameObject.FindGameObjectWithTag("CenterTower");
        if (centerTower == null)
        {
            return true; // 中心塔不存在
        }
        
        // 检查中心塔是否被摧毁（通过DamageTaker组件）
        var damageTaker = centerTower.GetComponent<DamageTaker>();
        if (damageTaker != null && damageTaker.currentHealth <= 0)
        {
            return true; // 中心塔生命值为0
        }
        
        // 检查中心塔是否被禁用
        if (!centerTower.activeInHierarchy)
        {
            return true; // 中心塔被禁用
        }
        
        return false; // 中心塔仍然存在且健康
    }
    
    /// <summary>
    /// 检查是否为ShowArea塔
    /// </summary>
    private bool IsShowAreaTower(GameObject tower)
    {
        if (tower == null) return false;
        
        // 检查父物体名称是否包含"showarea"
        Transform parent = tower.transform.parent;
        while (parent != null)
        {
            if (parent.name.ToLower().Contains("showarea"))
            {
                return true;
            }
            parent = parent.parent;
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