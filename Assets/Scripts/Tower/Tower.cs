using System;
using System.Text;
using TMPro;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("塔数据")] [SerializeField] private TowerData towerData;

    [Header("状态")] [SerializeField] private float currentHealth;
    [SerializeField] private int level;
    [SerializeField] private Vector2Int position;

    [Header("绑定")] [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TextMeshPro text;

    [Header("攻击相关")] [SerializeField] private GameObject bulletPrefab;

    [SerializeField] private LayerMask TowerLayerMask = 1 << 8;

    // 公共属性
    public TowerData TowerData => towerData;
    public float CurrentHealth => currentHealth;
    public Vector2Int Position => position;

    private DamageTaker damageTaker;

    public float AttackRange => towerData != null ? towerData.GetAttackRange(level) : 3f;
    public float AttackInterval => towerData != null ? towerData.GetAttackInterval(level) : 1f;
    public float BulletSpeed => 10f; // 可根据塔数据扩展
    public float AttackDamage => towerData != null ? towerData.GetPhysicAttack(level) : 10f;
    public int Level => level > towerData.MaxLevel ? towerData.MaxLevel : level;

    private void Awake()
    {
        damageTaker = GetComponent<DamageTaker>();
        if (towerData != null && damageTaker != null)
        {
            damageTaker.maxHealth = towerData.GetHealth(level);
            damageTaker.currentHealth = towerData.GetHealth(level);
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
        currentHealth = data.GetHealth(level);

        // 优化的Sprite赋值
        if (data.TowerSprite != null)
        {
            spriteRenderer.sprite = data.TowerSprite;
        }

        // 优化的字符串拼接
        var sb = new StringBuilder();
        sb.Append("塔名：").Append(data.TowerName).Append(" \n 等级：").Append(level / data.MaxLevel);
        text.text = sb.ToString();

        // 优化的碰撞检测
        Vector3 cellCenter = GameMap.instance.GridToWorldPosition(new Vector3Int(pos.x, pos.y, 0));
        Collider2D[] towers = Physics2D.OverlapPointAll(cellCenter, TowerLayerMask);

        // 单一出口优化
        bool shouldUpdate = false;

        if (towers.Length > 0)
        {
            foreach (var tower in towers)
            {
                if (tower == null) continue;

                Tower towerComponent = tower.GetComponent<Tower>();
                if (towerComponent == null) continue;
                if (towerComponent.transform.position == transform.position)
                {
                    if (towerComponent.TowerData != null &&
                        towerComponent.TowerData.TowerName == data.TowerName)
                    {
                        DeleteOldTower(tower.gameObject);
                        shouldUpdate = true;
                        Debug.Log("应该更新");
                    }
                    else
                    {
                        DeleteOldTower(tower.gameObject);
                        Debug.Log("应该删除");
                    }
                }
            }
        }

        if (shouldUpdate)
        {
            UpdateTower();
        }

#if UNITY_EDITOR
        Debug.Log($"塔初始化完成: {data.TowerName}");
#endif
    }

    private void DeleteOldTower(GameObject oldTower)
    {
        Tower tower = oldTower.GetComponent<Tower>();
        oldTower.transform.parent.GetComponent<Block>().RemoveTower(tower.position);
    }

    // private void ReplaceTower(TowerData data)
    // {
    //     throw new NotImplementedException();
    // }


    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    private float lastAttackTime;

    private void Update()
    {
        if (towerData == null || bulletPrefab == null) return;
        float attackSpeed = towerData.GetAttackSpeed(level) > 0 ? towerData.GetAttackSpeed(level) : 1f;
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

    //塔更新
    public void UpdateTower()
    {
        if (towerData != null && damageTaker != null)
        {
            damageTaker.maxHealth = towerData.GetHealth(level);
            // damageTaker.maxHealth = towerData.GetHealth(level);
        }

        // spriteRenderer.sprite = towerData.TowerSprite;
        level += 1;
        text.text = $"塔名：{towerData.TowerName} \n 等级：{level / towerData.MaxLevel}";
    }

    private GameObject FindNearestEnemyInRange()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float minDist = float.MaxValue;
        GameObject closest = null;
        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist <= towerData.GetAttackRange(level) && dist < minDist)
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
            bulletScript.Initialize((target.transform.position - transform.position).normalized, speed, gameObject,
                target, new string[] { "Enemy" }, towerData.GetPhysicAttack(level));
        }
        else
        {
            Debug.LogWarning("塔的子弹预制体未挂载IBullet实现脚本！");
        }
    }
}