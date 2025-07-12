using System;
using TMPro;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("塔数据")]
    [SerializeField] private TowerData towerData;

    [Header("状态")]
    [SerializeField] private float currentHealth;
    [SerializeField] private Vector2Int position;

    [Header("绑定")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TextMeshPro text;

    [Header("攻击相关")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private BulletConfig bulletConfig;


    // 公共属性
    public TowerData TowerData => towerData;
    public float CurrentHealth => currentHealth;
    public Vector2Int Position => position;

    private DamageTaker damageTaker;

    public float AttackRange => towerData != null ? towerData.AttackRange : 3f;
    public float AttackInterval => towerData != null ? towerData.AttackInterval : 1f;
    public float BulletSpeed => bulletConfig != null ? bulletConfig.BulletSpeed : 10f;
    public float AttackDamage => towerData != null ? towerData.PhysicAttack : 10f;

    private void Awake()
    {
        damageTaker = GetComponent<DamageTaker>();
        if (towerData != null && damageTaker != null)
        {
            damageTaker.maxHealth = towerData.Health;
            damageTaker.currentHealth = towerData.Health;
        }
    }
    public void SetOrder(int order)
    {
        spriteRenderer.sortingOrder = order;
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        renderer.sortingOrder = order;
    }

    /// <summary>
    /// 初始化塔
    /// </summary>
    /// <param name="data">塔的数据</param>
    /// <param name="pos">塔的位置</param>
    public void Initialize(TowerData data, Vector2Int pos)
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // text = GetComponentInChildren<TextMeshProUGUI>();
        towerData = data;
        position = pos;
        currentHealth = data.Health;
        spriteRenderer.sprite = data.TowerSprite;
        text.text = data.TowerName;
        // 设置位置
        // transform.position = new Vector3(pos.x, pos.y, 0);

        Debug.Log($"塔初始化完成: {data.TowerName} 在位置 ({pos.x}, {pos.y})");
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
            Debug.Log($"塔 {towerData.TowerName} 被摧毁");
            // TODO: 播放摧毁效果
        }
        else
        {
            Debug.Log($"塔 {towerData.TowerName} 受到 {damage} 点伤害，剩余生命值: {currentHealth}");
        }
    }

    /// <summary>
    /// 攻击目标
    /// </summary>
    /// <param name="target">目标</param>
    public void Attack(GameObject target)
    {
        if (target == null) return;

        // TODO: 实现攻击逻辑
        Debug.Log($"塔 {towerData.TowerName} 攻击目标");
    }

    /// <summary>
    /// 检查是否存活
    /// </summary>
    /// <returns>是否存活</returns>
    public bool IsAlive()
    {
        return currentHealth > 0;
    }



    private float lastAttackTime;

    private void Update()
    {
        if (towerData == null || bulletPrefab == null || bulletConfig == null) return;
        float attackSpeed = towerData.AttackSpeed > 0 ? towerData.AttackSpeed : 1f;
        if (Time.time - lastAttackTime >= 1f / attackSpeed)
        {
            GameObject target = FindNearestEnemyInRange();
            if (target != null)
            {
                FireAt(target);
                lastAttackTime = Time.time;
            }
        }
    }

    private GameObject FindNearestEnemyInRange()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float minDist = float.MaxValue;
        GameObject closest = null;
        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist <= towerData.AttackRange && dist < minDist)
            {
                minDist = dist;
                closest = enemy;
            }
        }
        return closest;
    }

    private void FireAt(GameObject target)
    {
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            float damage = towerData.PhysicAttack;
            bulletScript.Init(target.transform, damage, bulletConfig);
        }
    }
}