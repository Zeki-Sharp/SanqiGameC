using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Plugins.RaycastPro.Demo.Scripts;
using RaycastPro;
using RaycastPro.Casters2D;
using RaycastPro.Detectors;
using RaycastPro.Detectors2D;
using RaycastPro.RaySensors2D;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("塔数据")] [SerializeField] private TowerData towerData;

    [Header("状态")] [SerializeField] private float currentHealth;
    [SerializeField] private int level;
    [SerializeField] private Vector3Int cellPosition; // 塔在Tilemap中的cell坐标位置
    [Header("绑定")] 
    [SerializeField] private SpriteRenderer spriteRenderer;
    // [SerializeField] private TextMeshPro text;
    [SerializeField] private Block block;

    [Header("攻击相关")] 
    // 移除旧系统，只使用新的子弹配置系统

    [SerializeField] private LayerMask towerLayerMask;
    
    [Header("展示区域设置")]
    [SerializeField] private bool isShowAreaTower = false; // 是否为展示区域的塔

    // 公共属性
    public TowerData TowerData => towerData;
    public float CurrentHealth => currentHealth;
    public Vector3Int CellPosition => cellPosition;

    private DamageTaker damageTaker;
    
    // 缓存常用组件
    private SpriteRenderer cachedSpriteRenderer;
    // private TextMeshPro cachedText;
    private Block cachedBlock;
    private BulletManager cachedBulletManager;

    public float AttackRange => towerData != null ? towerData.GetAttackRange(level) : 3f;
    public float AttackInterval => towerData != null ? towerData.GetAttackInterval(level) : 1f;
    public float BulletSpeed => 10f; // 可根据塔数据扩展
    public float AttackDamage => towerData != null ? towerData.GetPhysicAttack(level) : 10f;
    public int Level => level > towerData.MaxLevel ? towerData.MaxLevel : level;
    public bool IsShowAreaTower => isShowAreaTower;

    [SerializeField] private RangeDetector2D rangeDetector;
    [SerializeField] public BezierRay2D raySensor;
    [SerializeField] private BasicCaster2D bulletCaster;
    
    private float cachedAttackSpeed = 1f;
    private float cachedAttackRange = 3f;
    private void Start() 
    { 
        rangeDetector.onDetectCollider.AddListener(OnDetectCollider); 
        towerLayerMask = LayerMask.GetMask("Tower");
        
        // 优化RangeDetector2D配置
        OptimizeRangeDetector();
    }
    
    /// <summary>
    /// 优化RangeDetector2D配置
    /// </summary>
    private void OptimizeRangeDetector()
    {
        if (rangeDetector == null) return;
        
        // 设置检测频率（每0.05秒检测一次，提高响应速度）
        rangeDetector.pulseTime = 0.05f;
        
        // 设置检测层为敌人层
        rangeDetector.detectLayer = LayerMask.GetMask("Enemy");
        
        // 设置最小检测半径（避免检测太近的敌人）
        rangeDetector.minRadius = 0.3f;
        
        // 设置最大检测数量限制（避免检测过多敌人）
        rangeDetector.Limited = true;
        rangeDetector.LimitCount = 15;
        
        Debug.Log($"{this.name} RangeDetector2D 优化配置完成");
    }
    
    /// <summary>
    /// 动态调整RangeDetector2D的检测范围
    /// </summary>
    private void UpdateRangeDetectorRadius()
    {
        if (rangeDetector == null) return;
        
        float attackRange = towerData?.GetAttackRange(level) ?? 3f;
        
        // 只有当攻击范围发生变化时才更新
        if (Mathf.Abs(rangeDetector.Radius - attackRange) > 0.1f)
        {
            rangeDetector.Radius = attackRange;
            Debug.Log($"{this.name} 更新检测范围到: {attackRange:F1}");
        }
    } 
    
    protected virtual void OnDetectCollider(Collider2D collider) 
    {
        // 展示区域的塔不进行游戏逻辑
        if (isShowAreaTower) return;
        if (towerData == null || bulletCaster == null) return;
        if (IsCenterTowerDestroyed()) return;
        
        // 检查攻击冷却
        if (!IsAttackCooldownReady()) return;
        
        // 使用FindNearestEnemyInRange找到最近的目标
        GameObject nearestEnemy = FindNearestEnemyInRange();
        if (nearestEnemy == null) return;
        
        // 检查是否需要切换目标
        bool shouldSwitchTarget = targetCache.ShouldSwitchTarget(nearestEnemy, targetCache.CurrentTargetDistance);
        
        if (shouldSwitchTarget)
        {
            // 更新当前目标
            currentTarget = nearestEnemy;
            Debug.Log($"{this.name} 切换目标到: {nearestEnemy.name} (距离: {targetCache.CurrentTargetDistance:F2})");
        }
        
        // 执行攻击
        ExecuteAttack(nearestEnemy);
        
        // 重置攻击冷却
        ResetAttackCooldown();
    }
    public void SetBulletDamage()
    {
        if (bulletCaster == null || bulletCaster.bullets == null)
            return;
        
        for (int i = 0; i < bulletCaster.bullets.Length; i++)
        {
            if (bulletCaster.bullets[i] == null)
                continue;
            
            bulletCaster.bullets[i].damage = towerData?.GetPhysicAttack(level) ?? 10f;
            var bulletCollide = bulletCaster.bullets[i].GetComponent<BulletCollide>();
            if (bulletCollide != null)
            {
                 bulletCollide.Initial(towerData?.GetBulletConfig(), this.gameObject);
            }
               
            
        }
    }
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
    public void OnLostAgent(GameObject data) 
    { 
        Debug.Log("Lost Agent!"); 
    }
    private void Awake()
    {
        damageTaker = GetComponent<DamageTaker>();
        rangeDetector = GetComponentInChildren<RangeDetector2D>();
        raySensor = GetComponentInChildren<BezierRay2D>();
        bulletCaster = GetComponent<BasicCaster2D>();
        cachedSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        // cachedText = GetComponentInChildren<TextMeshPro>();
        cachedBlock = GetComponentInParent<Block>();
        cachedBulletManager = GameManager.Instance?.GetSystem<BulletManager>();
        
        if (damageTaker == null)
            Debug.LogError($"DamageTaker component missing on {name}");
        if (rangeDetector == null)
            Debug.LogError($"RangeDetector2D component missing on {name}");
        if (raySensor == null)
            Debug.LogError($"BezierRay2D component missing on {name}");
        if (bulletCaster == null)
            Debug.LogError($"BasicCaster2D component missing on {name}");
        
        if (towerData != null)
        {
            damageTaker.maxHealth = towerData.GetHealth(level);
            damageTaker.currentHealth = towerData.GetHealth(level);
            
            cachedAttackSpeed = towerData.GetAttackSpeed(level);
            cachedAttackSpeed = cachedAttackSpeed > 0 ? cachedAttackSpeed : 1f;
            
            cachedAttackRange = towerData.GetAttackRange(level);
            
            bulletCaster.ammo.reloadTime = cachedAttackSpeed;
            SetBulletDamage();
        }
        
        SetInitialAttackRange();
        
        // 如果是中心塔，设置正确的层级
        if (CompareTag("CenterTower"))
        {
            SetCenterTowerOrder();
        }
        
        // 订阅敌人死亡事件
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Subscribe<EnemyDeathEventArgs>(OnEnemyDeath);
        }
    }
    
    private void OnDestroy()
    {
        if (rangeDetector != null)
            rangeDetector.onDetectCollider.RemoveListener(OnDetectCollider);
        
        // 取消订阅敌人死亡事件
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<EnemyDeathEventArgs>(OnEnemyDeath);
        }
    }
    
    /// <summary>
    /// 处理敌人死亡事件
    /// </summary>
    /// <param name="e">敌人死亡事件参数</param>
    private void OnEnemyDeath(EnemyDeathEventArgs e)
    {
        // 如果当前目标死亡，强制重新选择目标
        if (currentTarget == e.Enemy || targetCache.CurrentTarget == e.Enemy)
        {
            currentTarget = null;
            targetCache.ClearTarget();
            Debug.Log($"{this.name} 的当前目标 {e.EnemyName} 已死亡，将重新选择目标");
        }
    }
    
    private void SetInitialAttackRange()
    {
        if (rangeDetector != null)
            rangeDetector.Radius = towerData?.GetAttackRange(level) ?? 3f;
    }

    public void SetOrder(int order)
    {
        // 设置所有渲染器的层级
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.sortingOrder = order;
        }
        
        // 保持向后兼容
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = order;
        }
    }
    
    /// <summary>
    /// 设置中心塔的层级，使其与其他塔采用相同的层级遮挡关系
    /// </summary>
    public void SetCenterTowerOrder()
    {
        if (CompareTag("CenterTower"))
        {
            const int BaseOrder = 1000;
            const int VerticalOffsetMultiplier = 10;
            int verticalOffset = Mathf.RoundToInt(-transform.position.y * VerticalOffsetMultiplier);
            int finalOrder = BaseOrder + verticalOffset;
            
            // 设置所有渲染器的层级
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.sortingOrder = finalOrder;
            }
            
            Debug.Log($"中心塔层级设置为: {finalOrder} (位置: {transform.position})");
        }
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

    // if (text == null)
    // {
    //     text = GetComponentInChildren<TextMeshPro>();
    //     if (text == null)
    //     {
    //         Debug.LogError("TextMeshPro 未找到，初始化失败");
    //         return;
    //     }
    // }

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
        spriteRenderer.sprite = data.GetTowerSprite(level);
    }
    else
    {
        Debug.LogWarning($"塔 {data.TowerName} 的 Sprite 为空");
    }
    block = GetComponentInParent<Block>();
    // 优化的字符串拼接
    // text.text = $"塔名：{towerData.TowerName} \n 等级：{level+1}/{towerData.MaxLevel}";
    // 设置是否为展示区域塔
    SetAsShowAreaTower(isShowArea);

 
    if (hasCheck)
    {  
       
        // Vector3Int towerCellPos = new Vector3Int(cellPosition.x + block.CellPosition.x, cellPosition.y +block.CellPosition.y, 0);

        var towerCheckResult = DetectTowerAction(cellPosition,towerData); 
        this.tag = "Tower";

        switch (towerCheckResult)
        {
            case TowerCheckResult.ShouldUpdate:
                // 升级情况下，删除新创建的塔，让现有塔升级
                Debug.Log($"升级情况：删除新创建的塔 {this.name}");
                Destroy(this.gameObject);
                return; // 直接返回，不执行后续初始化
            case TowerCheckResult.ShouldDelete:
                // 替换情况下，继续初始化新塔
                Debug.Log($"替换情况：继续初始化新塔 {this.name}");
                break;
            case TowerCheckResult.None:
                // 新建情况，继续初始化
                Debug.Log($"新建情况：继续初始化新塔 {this.name}");
                break;
        }
    }
    
    
     
        if (towerData != null && damageTaker != null)
        {
            damageTaker.maxHealth = towerData.GetHealth(level);
            damageTaker.currentHealth = towerData.GetHealth(level);
            float attackSpeed = towerData.GetAttackSpeed(level) > 0 ? towerData.GetAttackSpeed(level) : 1f;

            bulletCaster.ammo.reloadTime = attackSpeed;
            rangeDetector.Radius = towerData.GetAttackRange(level);
        }

   
}
/// <summary>
    /// 检测单个位置的塔操作类型
    /// </summary>
    private TowerCheckResult DetectTowerAction(Vector3Int cellPos, TowerData newTowerData)
    {
        var gameMap = GameManager.Instance?.GetSystem<GameMap>();
        if (gameMap == null || newTowerData == null) return TowerCheckResult.None;
        
        // Vector3 worldPos = CoordinateUtility.CellToWorldPosition(gameMap.GetTilemap(), cellPos);
        //
        // 使用更精确的点检测，避免检测到附近位置的塔
        Collider2D[] allColliders = Physics2D.OverlapPointAll(this.transform.position);
        
        Debug.Log($"=== 检测位置 {cellPos} (世界坐标: {this.transform.position}) ===");
        Debug.Log($"找到 {allColliders.Length} 个碰撞体");
        
        // 遍历所有碰撞体，详细分析
        foreach (var collider in allColliders)
        {
            if (collider == null)
            {
                Debug.Log("跳过空碰撞体");
                continue;
            }
            
            if (this.gameObject == collider.gameObject)
            {
                Debug.Log("跳过空碰撞体");
                continue;
            }
            
            // Debug.Log($"碰撞体: {collider.name}, Tag: {collider.tag}, Layer: {collider.gameObject.layer}");
            // if (collider.name.Contains("PreviewTower"))
            // {
            //     Debug.Log($"跳过预览塔: {collider.name}");
            //     continue;
            // }
            // 跳过预览塔
            if (collider.CompareTag("PreviewTower"))
            {
                Debug.Log($"跳过预览塔: {collider.name}");
                continue;
            }
            //
            // // 检查是否在正确的层级
            // if (((1 << collider.gameObject.layer) & towerLayerMask) == 0)
            // {
            //     Debug.Log($"跳过非塔层级物体: {collider.name} (层级: {collider.gameObject.layer})");
            //     continue;
            // }
            
            // 检查是否有Tower组件
            Tower existingTower = collider.GetComponent<Tower>();
            if (existingTower == null)
            {
                Debug.Log($"跳过无Tower组件的物体: {collider.name}");
                continue;
            }
            
            if (existingTower.TowerData == null)
            {
                Debug.Log($"跳过无TowerData的塔: {collider.name}");
                continue;
            }
            
            // // 验证塔的位置是否真的在这个cell
            Vector3Int towerCellPos = existingTower.CellPosition;
            if (towerCellPos != cellPos)
            {
                Debug.Log($"跳过位置不匹配的塔: {collider.name} (塔位置: {towerCellPos}, 检测位置: {cellPos})");
                continue;
            }
            
            Debug.Log($"找到匹配的塔: {collider.name}, 类型: {existingTower.TowerData.TowerName}, 位置: {towerCellPos}");
            Debug.Log($"比较塔类型: 现有={existingTower.TowerData.TowerName}, 新塔={newTowerData.TowerName}");
      
            // 比较塔类型
            if (existingTower.TowerData.TowerName == newTowerData.TowerName)
            {    
                // 升级现有塔，删除新创建的塔
                existingTower.UpdateTower();
                Debug.Log($"检测到升级: {newTowerData.TowerName}，删除新创建的塔");
                return TowerCheckResult.ShouldUpdate;
            }
            else
            {
                // 替换现有塔
                DeleteOldTower(existingTower.gameObject);
                Debug.Log($"检测到替换: {newTowerData.TowerName} -> {existingTower.TowerData.TowerName}");
                return TowerCheckResult.ShouldDelete;
            }
        }
        
        Debug.Log($"位置 {cellPos} 无操作（空地新建）");
        return TowerCheckResult.None;
    }
   
    public LayerMask TowerLayerMask { get; set; }


    private void DeleteOldTower(GameObject oldTower)
    {
        Debug.Log("删除");
        if (oldTower == null)
        {
            Debug.LogError("删除物为空");
        }

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



    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    private float lastAttackTime;
    
    // 当前攻击目标
    private GameObject currentTarget;
    
    // 目标缓存系统
    private class TowerTargetCache
    {
        public GameObject CurrentTarget { get; set; }
        public float CurrentTargetDistance { get; set; }
        public bool IsTargetValid { get; set; }
        
        public void UpdateTarget(GameObject newTarget, float distance)
        {
            CurrentTarget = newTarget;
            CurrentTargetDistance = distance;
            IsTargetValid = newTarget != null;
        }
        
        public bool ShouldSwitchTarget(GameObject newTarget, float newDistance)
        {
            // 如果当前没有目标，或者新目标更近，则切换
            return CurrentTarget == null || newDistance < CurrentTargetDistance;
        }
        
        public void ClearTarget()
        {
            CurrentTarget = null;
            CurrentTargetDistance = float.MaxValue;
            IsTargetValid = false;
        }
    }
    
    private TowerTargetCache targetCache = new TowerTargetCache();

    private void Update()
    {
        // 展示区域的塔不进行游戏逻辑
        if (isShowAreaTower || towerData == null || rangeDetector == null)
            return;
        
        // 更新缓存的攻击速度和范围
        if (cachedAttackSpeed <= 0f)
            cachedAttackSpeed = towerData.GetAttackSpeed(level) > 0 ? towerData.GetAttackSpeed(level) : 1f;
        
        if (cachedAttackRange <= 0f)
            cachedAttackRange = towerData.GetAttackRange(level);
        
        // 动态更新检测范围
        UpdateRangeDetectorRadius();
    }

    //塔更新
    public void UpdateTower()
    {
        Debug.Log($"{this.name} 塔升级到{level}级");
        // 修复升级逻辑：确保能升级到最高等级
        if (level < towerData?.MaxLevel - 1)
        {
            level++;
            Debug.Log($"{this.name} 成功升级到{level + 1}级，最大等级为{towerData.MaxLevel}");
        }
        else
        {
            Debug.LogWarning($"{this.name} 已达到最大等级{towerData.MaxLevel}，无法继续升级");
        }
        // if (cachedText != null && towerData != null)
        // {
        //     cachedText.text = $"塔名：{towerData.TowerName} \n 等级：{level+1}/{towerData.MaxLevel}";
        // }
        if (towerData.TowerSprite != null)
        {
            spriteRenderer.sprite = towerData.GetTowerSprite(level);
        }
        else
        {
            Debug.LogWarning($"塔 {towerData.TowerName} 的 Sprite 为空");
        }
        if (towerData != null && damageTaker != null)
        {
            damageTaker.maxHealth = towerData.GetHealth(level);
            damageTaker.currentHealth = towerData.GetHealth(level);
            
            cachedAttackSpeed = towerData.GetAttackSpeed(level);
            cachedAttackSpeed = cachedAttackSpeed > 0 ? cachedAttackSpeed : 1f;
            
            cachedAttackRange = towerData.GetAttackRange(level);
        }
    }

    private GameObject FindNearestEnemyInRange()
    {
        // 1. 获取检测到的所有敌人（不重新执行Cast，避免无限循环）
        var detectedColliders = rangeDetector.DetectedColliders;
        if (detectedColliders == null || detectedColliders.Count == 0)
        {
            targetCache.ClearTarget();
            return null;
        }
        
        // 2. 计算距离并选择最近的敌人
        GameObject nearestEnemy = null;
        float minDistance = float.MaxValue;
        float attackRange = towerData?.GetAttackRange(level) ?? 3f;
        
        Debug.Log($"{this.name} 检测到 {detectedColliders.Count} 个敌人，攻击范围: {attackRange:F1}");
        
        foreach (var collider in detectedColliders)
        {
            // 检查是否为有效的敌人
            if (collider == null || !IsValidTarget(collider.gameObject))
            {
                Debug.Log($"{this.name} 跳过无效敌人: {collider?.name ?? "null"}");
                continue;
            }
            
            // 计算距离
            float distance = Vector3.Distance(transform.position, collider.transform.position);
            
            Debug.Log($"{this.name} 检查敌人 {collider.gameObject.name}，距离: {distance:F2}");
            
            // 检查是否在攻击范围内
            if (distance <= attackRange && distance < minDistance)
            {
                minDistance = distance;
                nearestEnemy = collider.gameObject;
                Debug.Log($"{this.name} 更新最近敌人: {nearestEnemy.name}，距离: {minDistance:F2}");
            }
        }
        
        // 3. 更新目标缓存
        if (nearestEnemy != null)
        {
            targetCache.UpdateTarget(nearestEnemy, minDistance);
            Debug.Log($"{this.name} 最终选择敌人: {nearestEnemy.name}，距离: {minDistance:F2}");
        }
        else
        {
            targetCache.ClearTarget();
            Debug.Log($"{this.name} 没有找到有效敌人");
        }
        
        return nearestEnemy;
    }
    
    /// <summary>
    /// 检查目标是否为有效的敌人
    /// </summary>
    /// <param name="target">要检查的目标</param>
    /// <returns>是否为有效目标</returns>
    private bool IsValidTarget(GameObject target)
    {
        if (target == null)
        {
            return false;
        }
        
        // 检查是否为敌人标签
        if (!target.CompareTag("Enemy"))
        {
            return false;
        }
        
        // 检查敌人是否还活着
        var damageTaker = target.GetComponent<DamageTaker>();
        if (damageTaker != null && damageTaker.currentHealth <= 0)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 检查攻击冷却是否就绪
    /// </summary>
    /// <returns>是否可以攻击</returns>
    private bool IsAttackCooldownReady()
    {
        if (cachedAttackSpeed <= 0f)
        {
            cachedAttackSpeed = towerData?.GetAttackSpeed(level) ?? 1f;
            cachedAttackSpeed = cachedAttackSpeed > 0 ? cachedAttackSpeed : 1f;
        }
        
        return Time.unscaledTime - lastAttackTime >= 1f / cachedAttackSpeed;
    }
    
    /// <summary>
    /// 重置攻击冷却时间
    /// </summary>
    private void ResetAttackCooldown()
    {
        lastAttackTime = Time.unscaledTime;
    }
    
    /// <summary>
    /// 执行攻击
    /// </summary>
    /// <param name="target">攻击目标</param>
    private void ExecuteAttack(GameObject target)
    {
        if (target == null || bulletCaster == null || raySensor == null)
        {
            return;
        }
        
        // 设置攻击范围
        float attackRange = towerData?.GetAttackRange(level) ?? 3f;
        rangeDetector.Radius = attackRange;
        
        // 重新设置射线传感器指向目标（确保路径更新）
        raySensor.SetStartEnd(this.transform, target.transform);
        
        // 强制更新贝塞尔射线的路径
        if (raySensor is BezierRay2D bezierRay)
        {
            // 手动触发路径更新
            bezierRay.enabled = false;
            bezierRay.enabled = true;
            
            // 等待一帧确保路径更新完成
            StartCoroutine(UpdateRayPathAndAttack());
            return;
        }
        
        // 设置子弹伤害
        SetBulletDamage();
        
        // 使用RaycastPro系统发射子弹
        bulletCaster.Cast(0);
        
        Debug.Log($"{this.name} 攻击目标: {target.name}");
    }
    
    /// <summary>
    /// 更新射线路径并执行攻击的协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator UpdateRayPathAndAttack()
    {
        // 等待一帧确保路径更新完成
        yield return null;
        
        // 设置子弹伤害
        SetBulletDamage();
        
        // 使用RaycastPro系统发射子弹
        bulletCaster.Cast(0);
        
        Debug.Log($"{this.name} 贝塞尔射线攻击目标完成");
    }

    private void OnDrawGizmos()
    {
        // 在Scene视图中显示调试信息
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
            $"塔名: {towerData.TowerName}\n等级: {level :F0}/{ towerData.MaxLevel:F0}");
        #endif
    }
}