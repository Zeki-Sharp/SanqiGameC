using System;
using System.Text;
using TMPro;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("塔数据")] [SerializeField] private TowerData towerData;

    [Header("状态")] [SerializeField] private float currentHealth;
    [SerializeField] private int level;
    [SerializeField] private Vector3Int cellPosition; // 塔在Tilemap中的cell坐标位置

    [Header("绑定")] [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TextMeshPro text;
    [SerializeField] private Block block;

    [Header("攻击相关")] 
    // 移除旧系统，只使用新的子弹配置系统

    [SerializeField] private LayerMask TowerLayerMask = 1 << 8;
    
    [Header("展示区域设置")]
    [SerializeField] private bool isShowAreaTower = false; // 是否为展示区域的塔

    // 公共属性
    public TowerData TowerData => towerData;
    public float CurrentHealth => currentHealth;
    public Vector3Int CellPosition => cellPosition;

    private DamageTaker damageTaker;

    public float AttackRange => towerData != null ? towerData.GetAttackRange(level) : 3f;
    public float AttackInterval => towerData != null ? towerData.GetAttackInterval(level) : 1f;
    public float BulletSpeed => 10f; // 可根据塔数据扩展
    public float AttackDamage => towerData != null ? towerData.GetPhysicAttack(level) : 10f;
    public int Level => level > towerData.MaxLevel ? towerData.MaxLevel : level;
    public bool IsShowAreaTower => isShowAreaTower;

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
    
    /// <summary>
    /// 设置为展示区域塔
    /// </summary>
    public void SetAsShowAreaTower(bool isShowArea)
    {
        isShowAreaTower = isShowArea;
        
        // 展示区域的塔可以禁用一些组件来节省性能
        if (isShowArea)
        {
            // 禁用DamageTaker组件（展示区域不需要伤害处理）
            var damageTaker = GetComponent<DamageTaker>();
            if (damageTaker != null)
            {
                damageTaker.enabled = false;
            }
        }
        else
        {
            // 重新启用DamageTaker组件
            var damageTaker = GetComponent<DamageTaker>();
            if (damageTaker != null)
            {
                damageTaker.enabled = true;
            }
        }
    }

   public enum TowerCheckResult
{
    None,
    ShouldUpdate,
    ShouldDelete
}

    public void Initialize(TowerData data, Vector3Int pos, bool hasCheck = false, bool isShowArea = false)
{
    // try
    // {
        // 检查关键组件是否存在
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("SpriteRenderer 未找到，初始化失败");
                return;
            }
        }

        if (text == null)
        {
            text = GetComponentInChildren<TextMeshPro>();
            if (text == null)
            {
                Debug.LogError("TextMeshPro 未找到，初始化失败");
                return;
            }
        }

        if (data == null)
        {
            Debug.LogError("传入的 TowerData 为 null，初始化失败");
            return;
        }

        towerData = data;
        cellPosition = pos;
        currentHealth = data.GetHealth(level);

        // 优化的 Sprite 赋值
        if (data.TowerSprite != null)
        {
            spriteRenderer.sprite = data.TowerSprite;
        }
        else
        {
            Debug.LogWarning($"塔 {data.TowerName} 的 Sprite 为空");
        }
        block = GetComponentInParent<Block>();
        // 优化的字符串拼接
        text.text = $"塔名：{data.TowerName} \n 等级：{level / (float)data.MaxLevel}";
        
        // 设置是否为展示区域塔
        SetAsShowAreaTower(isShowArea);

        if (hasCheck)
        {
            // 检查 GameMap 是否有效
            var gameMap = GameManager.Instance?.GetSystem<GameMap>();
            if (gameMap == null)
            {
                Debug.LogError("GameMap 未初始化，跳过碰撞检测");
                return;
            }

            // 优化的碰撞检测
            Vector3 cellCenter = CoordinateUtility.CellToWorldPosition(gameMap.GetTilemap(), new Vector3Int(pos.x, pos.y, 0));
            Collider2D[] towers = Physics2D.OverlapPointAll(cellCenter, TowerLayerMask);

            TowerCheckResult checkResult = TowerCheckResult.None;
            GameObject firstTower = null;

            if (towers.Length > 0)
            {
                foreach (var tower in towers)
                {
                    if (tower == null ||/*|| !tower.CompareTag("Tower") || !this.CompareTag("PreviewTower") ||*/ tower.gameObject == this.gameObject) continue;

                    Tower towerComponent = tower.GetComponent<Tower>();
                    if (towerComponent == null || towerComponent.block == null) continue;
 
                    if ((towerComponent.cellPosition + towerComponent.block.CellPosition) == (this.cellPosition+block.CellPosition))
                    {
                        if (towerComponent.TowerData != null  &&
                            towerComponent.TowerData.TowerName == data.TowerName)
                        {
                            DeleteOldTower(tower.gameObject);
                            checkResult = TowerCheckResult.ShouldUpdate;
                            firstTower = tower.gameObject;
                            break;
                        }
                        else
                        {
                            DeleteOldTower(tower.gameObject);
                            checkResult = TowerCheckResult.ShouldDelete;
                            firstTower = tower.gameObject;
                            break;
                        }
                    }
                }
            }

            switch (checkResult)
            {
                case TowerCheckResult.ShouldUpdate:
                    UpdateTower();
                    break;
                case TowerCheckResult.ShouldDelete:
                    // DeleteOldTower(firstTower); // 已在 DeleteOldTower 中处理
                    break;
            }
            this.tag = "Tower";
        }
    // }
    // catch (System.Exception ex)
    // {
    //     Debug.LogError($"[Tower] 塔 {data?.TowerName ?? "Unknown"} 初始化时发生异常: {ex.Message}\n{ex.StackTrace}");
    // }


}


    private void DeleteOldTower(GameObject oldTower)
    {
        if (oldTower == null) return;

        Tower tower = oldTower.GetComponent<Tower>();
        if (tower == null)
        {
            Debug.LogWarning("尝试删除的对象没有 Tower 组件");
            return;
        }

        Transform parent = oldTower.transform.parent;
        if (parent == null)
        {
            Debug.LogWarning("尝试删除的 Tower 没有父对象（Block）");
            return;
        }

        Block block = parent.GetComponent<Block>();
        if (block == null)
        {
            Debug.LogWarning("Tower 的父对象没有 Block 组件");
            return;
        }

        try
        {
            block.RemoveTower(tower.CellPosition);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"移除格子 {tower.CellPosition} 的塔时发生异常: {ex.Message}");
        }
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
        // 展示区域的塔不进行游戏逻辑
        if (isShowAreaTower) return;
        
        if (towerData == null) return;
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
        // 使用新的子弹系统
        var bulletConfig = towerData?.GetBulletConfig();
        if (bulletConfig != null)
        {
            var bulletManager = GameManager.Instance?.GetSystem<BulletManager>();
            if (bulletManager != null)
            {
                GameObject bullet = bulletManager.GetBullet(bulletConfig.BulletName, transform.position, Quaternion.identity);
                var bulletScript = bullet.GetComponent<IBullet>();
                if (bulletScript != null)
                {
                    Vector3 direction = (target.transform.position - transform.position).normalized;
                    bulletScript.Initialize(direction, 0, gameObject, target, bulletConfig.TargetTags, towerData.GetPhysicAttack(level));
                }
                else
                {
                    Debug.LogWarning("子弹预制体未挂载IBullet实现脚本！");
                }
            }
            else
            {
                Debug.LogWarning("BulletManager未找到！");
            }
        }
        else
        {
            Debug.LogWarning("塔没有配置子弹配置！请在TowerData中设置BulletConfig。");
        }
    }
}