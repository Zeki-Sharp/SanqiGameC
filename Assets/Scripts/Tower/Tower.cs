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

    // 公共属性
    public TowerData TowerData => towerData;
    public float CurrentHealth => currentHealth;
    public Vector2Int Position => position;

    private DamageTaker damageTaker;

    public float AttackRange => towerData != null ? towerData.AttackRange : 3f;
    public float AttackInterval => towerData != null ? towerData.AttackInterval : 1f;
    public float BulletSpeed => 10f; // 可根据塔数据扩展
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

    public void Initialize(TowerData data, Vector2Int pos)
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        towerData = data;
        position = pos;
        currentHealth = data.Health;
        spriteRenderer.sprite = data.TowerSprite;
        text.text = data.TowerName;
        Debug.Log($"塔初始化完成: {data.TowerName} 在位置 ({pos.x}, {pos.y})");
    }


    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    private float lastAttackTime;

    private void Update()
    {
        if (towerData == null || bulletPrefab == null) return;
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
        var bulletScript = bullet.GetComponent<IBullet>();
        if (bulletScript != null)
        {
            float speed = 0; // 让子弹用自己的Inspector速度
            bulletScript.Initialize((target.transform.position - transform.position).normalized, speed, gameObject, target, new string[] { "Enemy" }, towerData.PhysicAttack);
        }
        else
        {
            Debug.LogWarning("塔的子弹预制体未挂载IBullet实现脚本！");
        }
    }
}