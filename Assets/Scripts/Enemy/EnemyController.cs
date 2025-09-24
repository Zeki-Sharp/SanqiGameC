using System;
using RaycastPro.Bullets;
using RaycastPro.Bullets2D;
using RaycastPro.Casters2D;
using RaycastPro.Detectors2D;
using RaycastPro.RaySensors2D;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 敌人控制器 - 管理敌人的状态机和行为
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("数据配置")]
    public EnemyData data; // 在 Prefab Inspector 拖拽

    private DamageTaker damageTaker;

    [SerializeField] private RangeDetector2D rangeDetector;
    [SerializeField] public BasicRay2D raySensor;
    [SerializeField] private BasicCaster2D bulletCaster;

    [ShowInInspector] private EnemyState currentState;
    private float currentHealth;
    private SpriteRenderer spriteRenderer;
    private float moveSpeedOverride = -1f;
    private float difDistance;

    // 公共属性
    public float AttackRange => data != null ? data.AttackRange : 1.5f;
    public float MoveSpeed {
        get => (moveSpeedOverride >= 0f) ? moveSpeedOverride : (data != null ? data.MoveSpeed : 2f);
        set => moveSpeedOverride = value;
    }
    public float CurrentHealth => currentHealth;
    public float MaxHealth => data != null ? data.MaxHealth : 100f;
    public IAttackBehavior AttackBehavior => data != null ? data.AttackBehavior : null;

    private void Awake()
    {
        damageTaker = GetComponent<DamageTaker>();
        difDistance = UnityEngine.Random.Range(0.1f, 1f);

        // auto wire common components to avoid null refs
        if (rangeDetector == null) rangeDetector = GetComponentInChildren<RangeDetector2D>(true);
        if (bulletCaster == null)  bulletCaster  = GetComponentInChildren<BasicCaster2D>(true);
        if (raySensor == null)     raySensor     = GetComponentInChildren<BasicRay2D>(true);

        if (data != null && damageTaker != null)
        {
            damageTaker.maxHealth = data.MaxHealth;
            damageTaker.currentHealth = data.MaxHealth;
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = MaxHealth;

        // 确保标签
        if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag != "Enemy")
            gameObject.tag = "Enemy";

        // Z 轴归零
        Vector3 position = transform.position;
        if (Mathf.Abs(position.z) > 0.0001f)
            transform.position = new Vector3(position.x, position.y, 0f);

        // 订阅死亡
        if (damageTaker != null)
            damageTaker.onDeath += Die;
    }
    
    // 处理子弹碰撞
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.name.Contains("Bullet_Tower"))
        {
            PlayHitEffectManually(other.gameObject);
        }
    }
    
    /// <summary>
    /// 手动播放击中特效
    /// </summary>
    private void PlayHitEffectManually(GameObject bulletObject)
    {
        // 尝试从子弹对象获取塔的信息
        var bullet2D = bulletObject.GetComponent<RaycastPro.Bullets2D.Bullet2D>();
        if (bullet2D != null && bullet2D.caster != null)
        {
            var tower = bullet2D.caster.GetComponent<Tower>();
            if (tower != null && tower.TowerData != null)
            {
                var bulletConfig = tower.TowerData.GetBulletConfig();
                if (bulletConfig?.HitEffectPrefab != null)
                {
                    Vector3 effectPosition = transform.position + bulletConfig.HitEffectOffset;
                    GameObject effect = Instantiate(bulletConfig.HitEffectPrefab, effectPosition, Quaternion.identity);
                    
                    // 设置特效到Effect层级
                    SetEffectToEffectLayer(effect);
                    return;
                }
            }
        }
        
        // 如果无法从子弹获取塔信息，回退到原来的方法
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        foreach (GameObject tower in towers)
        {
            var towerComponent = tower.GetComponent<Tower>();
            if (towerComponent != null && towerComponent.TowerData != null)
            {
                var bulletConfig = towerComponent.TowerData.GetBulletConfig();
                if (bulletConfig?.HitEffectPrefab != null)
                {
                    Vector3 effectPosition = transform.position + bulletConfig.HitEffectOffset;
                    GameObject effect = Instantiate(bulletConfig.HitEffectPrefab, effectPosition, Quaternion.identity);
                    
                    // 设置特效到Effect层级
                    SetEffectToEffectLayer(effect);
                    return;
                }
            }
        }
    }
    
    /// <summary>
    /// 设置特效到Effect层级
    /// </summary>
    private void SetEffectToEffectLayer(GameObject effect)
    {
        // 设置GameObject的Layer
        effect.layer = LayerMask.NameToLayer("Effect");
        
        // 设置所有子对象的Layer
        Transform[] childTransforms = effect.GetComponentsInChildren<Transform>();
        foreach (Transform child in childTransforms)
        {
            child.gameObject.layer = LayerMask.NameToLayer("Effect");
        }
        
        // 设置渲染器的sortingLayer和sortingOrder
        var renderers = effect.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.sortingLayerName = "Effect";
            renderer.sortingOrder = 100; // 设置较高的渲染顺序
        }
    }


    private void Start()
    {
        ChangeState(new EnemyMoveState(this));
    }

    private void OnDestroy()
    {
        if (damageTaker != null)
            damageTaker.onDeath -= Die;
    }

    private void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
            currentState.CheckTransitions();
        }
    }

    public bool SetEnemyData(EnemyData newData)
    {
        if (newData == null)
        {
            Debug.LogError("EnemyData 不能为空");
            return false;
        }
        data = newData;
        if (damageTaker != null)
        {
            damageTaker.maxHealth = newData.MaxHealth;
            damageTaker.currentHealth = newData.MaxHealth;
        }
        
        // 设置动画控制器
        var animator = GetComponent<Animator>();
        if (animator != null && newData.AnimatorController != null)
        {
            animator.runtimeAnimatorController = newData.AnimatorController;
        }
        
        return true;
    }

    /// <summary> 切换状态 </summary>
    public void ChangeState(EnemyState newState)
    {
        if (currentState != null) currentState.Exit();
        currentState = newState;

        if (currentState != null)
        {
            if (newState is EnemyAttackState)
                currentState.Enter(rangeDetector, raySensor, bulletCaster);
            else
                currentState.Enter();
        }
    }

    /// <summary>
    /// 用 RangeDetector2D 判定攻距内是否有塔（标签/ShowArea/存活校验）
    /// </summary>
    public bool IsTowerInAttackRange()
    {
        if (rangeDetector == null) return false;

        // 如果你关闭了 detector 的自动更新，可在别处定时 Cast；也可在这里按需 Cast。
        // rangeDetector.Cast();

        var list = rangeDetector.DetectedColliders;
        if (list == null || list.Count == 0) return false;

        float r2 = AttackRange * AttackRange;
        Vector3 self = transform.position;

        for (int i = 0; i < list.Count; i++)
        {
            var col = list[i];
            if (col == null) continue;
            var go = col.gameObject;

            if (!IsValidTower(go)) continue;

            if ((go.transform.position - self).sqrMagnitude <= r2)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 统一的“择目标”方法：优先中心塔（若在攻距内且有效），否则从探测器集合里取最近的有效塔
    /// </summary>
    public GameObject AcquireBestTargetInRange(bool preferCenter = true)
    {
        float r2 = AttackRange * AttackRange;
        Vector3 self = transform.position;

        // 1) 先从 RangeDetector2D 里拿候选
        var candidates = new System.Collections.Generic.List<GameObject>();
        if (rangeDetector != null)
        {
            var list = rangeDetector.DetectedColliders;
            if (list != null && list.Count > 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var col = list[i];
                    if (col == null) continue;
                    var go = col.gameObject;
                    if (IsValidTower(go) && (go.transform.position - self).sqrMagnitude <= r2)
                        candidates.Add(go);
                }
            }
        }

        // 2) 探测器没拿到，就用物理圈查做兜底
        if (candidates.Count == 0)
        {
            int mask = LayerMask.GetMask("Tower"); // 确保这两个 Layer 存在
            var cols = Physics2D.OverlapCircleAll(self, AttackRange, mask);
            for (int i = 0; i < cols.Length; i++)
            {
                var c = cols[i];
                if (c == null) continue;
                var go = c.gameObject;
                if (IsValidTower(go))
                    candidates.Add(go);
            }
        }

        if (candidates.Count == 0) return null;

        // 3) 按需求优先中心塔（仅当它在候选里）
        if (preferCenter)
        {
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].CompareTag("CenterTower"))
                    return candidates[i];
        }

        // 4) 否则取最近的普通塔
        GameObject best = null;
        float bestD2 = float.MaxValue;
        for (int i = 0; i < candidates.Count; i++)
        {
            float d2 = (candidates[i].transform.position - self).sqrMagnitude;
            if (d2 < bestD2) { bestD2 = d2; best = candidates[i]; }
        }
        return best;
    }


    /// <summary> 中心塔是否被摧毁 </summary>
    public bool IsCenterTowerDestroyed()
    {
        GameObject centerTower = GameObject.FindGameObjectWithTag("CenterTower");
        if (centerTower == null) return true;

        var dt = centerTower.GetComponent<DamageTaker>();
        if (dt != null && dt.currentHealth <= 0) return true;

        if (!centerTower.activeInHierarchy) return true;

        return false;
    }

    /// <summary> 过滤 ShowArea </summary>
    private bool IsShowAreaTower(GameObject tower)
    {
        if (tower == null) return false;
        Transform parent = tower.transform.parent;
        while (parent != null)
        {
            // case-insensitive contains "ShowArea"
            if (parent.name.IndexOf("ShowArea", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            parent = parent.parent;
        }
        return false;
    }

    /// <summary> 统一的塔有效性校验：标签/ShowArea/存活 </summary>
    private bool IsValidTower(GameObject go)
    {
        if (go == null) return false;
        if (!(go.CompareTag("CenterTower") || go.CompareTag("Tower"))) return false;
        if (IsShowAreaTower(go)) return false;

        var dt = go.GetComponent<DamageTaker>();
        if (dt != null && dt.currentHealth <= 0) return false;

        return true;
    }

    /// <summary> 敌人死亡 </summary>
    private void Die()
    {
        Debug.Log($"{name} 死亡");

        if (EventBus.Instance != null)
        {
            int goldReward = data != null ? data.GoldReward : 10;
            EventBus.Instance.Publish(new EnemyDeathEventArgs(gameObject, goldReward, transform.position));
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// 设置敌人朝向并同步精灵翻转状态
    /// </summary>
    /// <param name="direction">朝向向量</param>
    public void SetDirection(Vector3 direction)
    {
        if (spriteRenderer == null) return;
        
        // 根据朝向设置精灵翻转，只在X轴有明显移动时翻转
        if (Mathf.Abs(direction.x) > 0.1f)
        {
            // 只有当朝向发生明显变化时才更新flipX
            bool shouldFlipX = direction.x < 0;
            if (spriteRenderer.flipX != shouldFlipX)
            {
                spriteRenderer.flipX = shouldFlipX;
                Debug.Log($"{name} SetDirection: 更新flipX为 {shouldFlipX}，方向: {direction}");
            }
        }
        
        // 确保Y轴不翻转
        if (spriteRenderer.flipY)
        {
            spriteRenderer.flipY = false;
            Debug.Log($"{name} SetDirection: 重置flipY为false");
        }
    }

    public string GetCurrentStateName() => currentState?.GetType().Name ?? "None";

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.right * 1f);
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (damageTaker != null)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1.5f,
                $"状态: {GetCurrentStateName()}\n生命值: {damageTaker.currentHealth:F0}/{damageTaker.maxHealth:F0}"
            );
        }
#endif
    }
}
