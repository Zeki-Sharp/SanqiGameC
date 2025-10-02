using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Plugins.RaycastPro.Demo.Scripts;
using RaycastPro;
using RaycastPro.Bullets;
using RaycastPro.Bullets2D;
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
    
    [Header("升级特效设置")]
    [SerializeField] private GameObject upgradeEffectPrefab; // 升级特效预制体
    [SerializeField] private Vector3 upgradeEffectOffset = Vector3.up * 0.5f; // 特效偏移

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

    // 治疗效果控制器
    private EffectController effectController;
    
    private void Start() 
    { 
        try
        {
            if (rangeDetector != null)
            {
                rangeDetector.onDetectCollider.AddListener(OnDetectCollider);
                Debug.Log($"塔 {this.name} 已注册攻击检测监听器");
                
                // 优化RangeDetector2D配置
                OptimizeRangeDetector();
            }
            else
            {
                Debug.LogError($"塔 {this.name} 的 RangeDetector 为空");
            }
            
            towerLayerMask = LayerMask.GetMask("Tower");
            
            // 启动治疗效果（如果是治疗塔）
            StartHealEffect();
            
            Debug.Log($"塔 {this.name} 初始化完成，攻击范围: {AttackRange:F1}，攻击间隔: {AttackInterval:F2}，攻击力: {AttackDamage:F1}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"塔 {this.name} 在 Start 中发生错误: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// 启动治疗效果（如果是治疗塔）
    /// </summary>
    private void StartHealEffect()
    {
        if (effectController == null || towerData == null) return;
        
        // 检查是否为治疗塔（有治疗配置的塔）
        float healAmount = towerData.GetHealAmount(level);
        if (healAmount > 0)
        {
            // 先移除可能存在的同名效果，避免重复
            effectController.RemoveEffect("Heal");
            
            // 创建治疗效果数据
            var healEffectData = new EffectData
            {
                effectName = "Heal",
                healAmount = healAmount,
                healInterval = towerData.GetHealInterval(level),
                healRangeType = towerData.GetHealRangeType(level),
                healEffectType = towerData.GetHealEffectType(level),
                duration = -1f, // 永久效果
                healEffectPrefab = towerData.GetHealEffectPrefab(level),
                healEffectOffset = towerData.GetHealEffectOffset(level)
            };
            
            // 应用治疗效果
            effectController.AddEffect(healEffectData);
            
            Debug.Log($"治疗效果激活：治疗量={healAmount}，间隔={healEffectData.healInterval}，范围类型={healEffectData.healRangeType}，效果类型={healEffectData.healEffectType}，持续时间为永久");
        }
    }
    void OnBullet(Bullet bullet) =>  damageTaker.TakeDamage(bullet.damage);
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
        try
        {
            // 展示区域的塔不进行游戏逻辑
            if (isShowAreaTower)
            {
                Debug.Log($"塔 {this.name} 是展示区域塔，跳过攻击逻辑");
                return;
            }

            if (towerData == null)
            {
                Debug.LogWarning($"塔 {this.name} 的 TowerData 为空");
                return;
            }

            if (bulletCaster == null)
            {
                Debug.LogWarning($"塔 {this.name} 的 BulletCaster 为空");
                return;
            }

            if (IsCenterTowerDestroyed())
            {
                Debug.Log($"塔 {this.name} 检测到中心塔已被摧毁，停止攻击");
                return;
            }

            // 检查攻击冷却
            if (!IsAttackCooldownReady())
            {
                return;
            }

            // 使用FindNearestEnemyInRange找到最近的目标
            GameObject nearestEnemy = FindNearestEnemyInRange();
            if (nearestEnemy == null)
            {
                Debug.Log($"塔 {this.name} 没有找到有效目标");
                return;
            }

            // 检查是否需要切换目标
            bool shouldSwitchTarget = targetCache.ShouldSwitchTarget(nearestEnemy, targetCache.CurrentTargetDistance);
            if (shouldSwitchTarget)
            {
                // 更新当前目标
                currentTarget = nearestEnemy;
                Debug.Log($"塔 {this.name} 切换目标到: {nearestEnemy.name} (距离: {targetCache.CurrentTargetDistance:F2})");
            }

            // 执行攻击
            ExecuteAttack(nearestEnemy);

            // 重置攻击冷却
            ResetAttackCooldown();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"塔 {this.name} 在处理攻击逻辑时发生错误: {e.Message}\n{e.StackTrace}");
        }
    }
    public void SetBulletDamage()
    {
        if (bulletCaster == null || bulletCaster.bullets == null)
        {
            Debug.LogWarning($"[DEBUG] {this.name} bulletCaster或bullets为空");
            return;
        }
        
        for (int i = 0; i < bulletCaster.bullets.Length; i++)
        {
            if (bulletCaster.bullets[i] == null)
                continue;
            
            bulletCaster.bullets[i].damage = towerData?.GetPhysicAttack(level) ?? 10f;
            bulletCaster.bullets[i].caster = bulletCaster; // 确保caster被正确设置
            
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
        effectController = GetComponent<EffectController>();
        
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
            
            // 订阅伤害和死亡事件
            damageTaker.onTakeDamage += OnTakeDamage;
            damageTaker.onDeath += OnTowerDeath;
            
            cachedAttackSpeed = towerData.GetAttackSpeed(level);
            cachedAttackSpeed = cachedAttackSpeed > 0 ? cachedAttackSpeed : 1f;
            
            cachedAttackRange = towerData.GetAttackRange(level);
            
            bulletCaster.ammo.reloadTime = cachedAttackSpeed;
            SetBulletDamage();
        }
        
        SetInitialAttackRange();
        
        // 注意：中心塔的层级现在由SceneLayerManager统一管理
        // 不再需要手动设置
        
        // 订阅敌人死亡事件
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Subscribe<EnemyDeathEventArgs>(OnEnemyDeath);
        }
    }
    
    private void OnTakeDamage(float damage)
    {
        Debug.Log($"塔 {this.name} 受到 {damage} 点伤害，剩余生命值: {damageTaker.currentHealth}");
    }
    
    private void OnTowerDeath()
    {
        Debug.Log($"塔 {this.name} 被摧毁");
        
        // 从Block中移除塔的引用
        if (cachedBlock != null)
        {
            Vector3Int localCoord = cachedBlock.GetTowerLocalCoord(this);
            cachedBlock.RemoveTower(localCoord);
        }
        else
        {
            // 如果找不到Block，直接销毁游戏对象
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // 取消事件订阅
        if (rangeDetector != null)
            rangeDetector.onDetectCollider.RemoveListener(OnDetectCollider);
            
        if (damageTaker != null)
        {
            damageTaker.onTakeDamage -= OnTakeDamage;
            damageTaker.onDeath -= OnTowerDeath;
        }
        
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<EnemyDeathEventArgs>(OnEnemyDeath);
        }
        
        Debug.Log($"塔 {this.name} 已销毁");
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

    // 注意：SetOrder和SetCenterTowerOrder方法已被删除
    // 现在由SceneLayerManager统一管理层级
    
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
    try
    {
        Debug.Log($"开始初始化塔 {this.name}，位置: {pos}，是否预览: {isShowArea}");
        
        // 检查关键组件是否存在
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError($"塔 {this.name} 的 SpriteRenderer 未找到，初始化失败");
                return;
            }
        }

        if (data == null)
        {
            Debug.LogError($"塔 {this.name} 的 TowerData 为 null，初始化失败");
            return;
        }

        towerData = data;
        cellPosition = pos;
        currentHealth = data.GetHealth(level);

        // 设置塔的名称
        this.name = $"{(isShowArea ? "ShowArea_" : "")}{data.TowerName}_{pos.x}_{pos.y}";

        // 设置 Sprite 和渲染器
        if (data.TowerSprite != null)
        {
            Sprite targetSprite = data.GetTowerSprite(level);
            Debug.Log($"[Tower Debug] 塔 {this.name} 初始化 - 等级: {level}, 目标图片: {targetSprite?.name ?? "null"}, 主渲染器: {spriteRenderer?.name ?? "null"}");
            
            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
                Debug.Log($"[Tower Debug] 塔 {this.name} 图片设置成功: {spriteRenderer.sprite?.name ?? "null"}");
            }
            else
            {
                Debug.LogError($"[Tower Debug] 塔 {this.name} 等级 {level} 的图片为空！");
            }
            
            spriteRenderer.enabled = true;
            Debug.Log($"塔 {this.name} 的主渲染器已启用");
        }
        else
        {
            Debug.LogWarning($"塔 {this.name} 的 Sprite 为空");
        }

        // 确保所有子渲染器都是启用的
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
                Debug.Log($"塔 {this.name} 的子渲染器已启用");
            }
        }

        // 获取父级 Block
        block = GetComponentInParent<Block>();
        if (block == null)
        {
            Debug.LogWarning($"塔 {this.name} 没有找到父级 Block");
        }

        // 设置是否为展示区域塔
        SetAsShowAreaTower(isShowArea);

        // 检查塔的操作类型（升级/替换）
        if (hasCheck && !isShowArea)  // 预览塔不需要检查
        {
            var towerCheckResult = DetectTowerAction(cellPosition, towerData);
            this.tag = "Tower";

            switch (towerCheckResult)
            {
                case TowerCheckResult.ShouldUpdate:
                    Debug.Log($"塔 {this.name} 需要升级，将删除新创建的塔");
                    Destroy(this.gameObject);
                    return;
                case TowerCheckResult.ShouldDelete:
                    Debug.Log($"塔 {this.name} 需要替换现有塔");
                    break;
                case TowerCheckResult.None:
                    Debug.Log($"塔 {this.name} 是新建塔");
                    break;
            }
        }

        // 设置战斗相关属性
        if (!isShowArea && towerData != null)
        {
            if (damageTaker != null)
            {
                damageTaker.maxHealth = towerData.GetHealth(level);
                damageTaker.currentHealth = towerData.GetHealth(level);
                Debug.Log($"塔 {this.name} 的生命值已设置: {damageTaker.currentHealth}/{damageTaker.maxHealth}");
            }

            if (bulletCaster != null)
            {
                float attackSpeed = towerData.GetAttackSpeed(level);
                attackSpeed = attackSpeed > 0 ? attackSpeed : 1f;
                bulletCaster.ammo.reloadTime = attackSpeed;
                Debug.Log($"塔 {this.name} 的攻击速度已设置: {attackSpeed}");
            }

            if (rangeDetector != null)
            {
                rangeDetector.Radius = towerData.GetAttackRange(level);
                Debug.Log($"塔 {this.name} 的攻击范围已设置: {rangeDetector.Radius}");
            }
        }

        Debug.Log($"塔 {this.name} 初始化完成");
    }
    catch (System.Exception e)
    {
        Debug.LogError($"塔 {this.name} 初始化时发生错误: {e.Message}\n{e.StackTrace}");
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
            
            // 修复：跳过自己，避免误删
            if (this.gameObject == collider.gameObject)
            {
                Debug.Log("跳过自己，避免误删");
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
                //播放升级音效
                AudioManager.Instance.PlayReplaceSound();
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
            // 修复：计算正确的localCoord
            Vector3Int localCoord = tower.CellPosition - block.CellPosition;
            block.RemoveTower(localCoord);
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
            Sprite targetSprite = towerData.GetTowerSprite(level);
            Debug.Log($"[Tower Debug] 塔 {this.name} 升级后 - 等级: {level}, 目标图片: {targetSprite?.name ?? "null"}, 主渲染器: {spriteRenderer?.name ?? "null"}");
            
            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
                Debug.Log($"[Tower Debug] 塔 {this.name} 升级后图片设置成功: {spriteRenderer.sprite?.name ?? "null"}");
            }
            else
            {
                Debug.LogError($"[Tower Debug] 塔 {this.name} 升级后等级 {level} 的图片为空！");
            }
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
        
        // 播放升级特效
        PlayUpgradeEffect();
        //播放升级音效
        AudioManager.Instance.PlayLevelUpSound();
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
        
        // 使用 Time.time 来计算冷却，这样会受到游戏暂停的影响
        float cooldownTime = 1f / cachedAttackSpeed;
        float timeSinceLastAttack = Time.time - lastAttackTime;
        bool isReady = timeSinceLastAttack >= cooldownTime;
        
        if (isReady)
        {
            Debug.Log($"塔 {this.name} 攻击冷却就绪 - 间隔: {timeSinceLastAttack:F2}s，冷却时间: {cooldownTime:F2}s");
        }
        
        return isReady;
    }
    
    /// <summary>
    /// 重置攻击冷却时间
    /// </summary>
    private void ResetAttackCooldown()
    {
        // 使用 Time.time 来重置冷却时间
        lastAttackTime = Time.time;
        Debug.Log($"塔 {this.name} 重置攻击冷却时间: {lastAttackTime:F2}");
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
            // 强制触发路径更新
            bezierRay.enabled = false;
            bezierRay.enabled = true;
            
            // 等待一帧确保路径更新完成
            StartCoroutine(UpdateRayPathAndAttack());
            return;
        }
        
        // 设置子弹伤害
        SetBulletDamage();
        AudioManager.Instance.PlayDamageSound(gameObject);
        // 使用RaycastPro系统发射子弹
        bulletCaster.Cast(0);
    }
    
    /// <summary>
    /// 更新射线路径并执行攻击的协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator UpdateRayPathAndAttack()
    {
        // 等待一帧确保路径更新完成
        yield return null;
        
        // 验证贝塞尔射线路径是否有效
        if (raySensor is BezierRay2D bezierRay)
        {
            if (bezierRay.PathPoints == null || bezierRay.PathPoints.Count < 2)
            {
                Debug.LogWarning($"{this.name} 贝塞尔射线路径无效，跳过攻击。路径点数: {bezierRay.PathPoints?.Count ?? 0}");
                yield break;
            }
            
            // 计算路径长度
            float pathLength = 0f;
            for (int i = 0; i < bezierRay.PathPoints.Count - 1; i++)
            {
                pathLength += Vector2.Distance(bezierRay.PathPoints[i], bezierRay.PathPoints[i + 1]);
            }
            
            Debug.Log($"{this.name} 贝塞尔射线路径有效，路径点数: {bezierRay.PathPoints.Count}，路径长度: {pathLength:F2}");
            
            // 如果路径长度过短，跳过攻击
            if (pathLength < 0.1f)
            {
                Debug.LogWarning($"{this.name} 贝塞尔射线路径长度过短: {pathLength:F2}，跳过攻击");
                yield break;
            }
        }
        
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
            $"塔名: {towerData.TowerName}\n等级: {level :F0}/{ towerData.MaxLevel:F0}\n 生命值: {currentHealth:F0}/{towerData.GetHealth(level):F0}\n攻击力: {towerData.GetPhysicAttack(level):F0}\n攻击间隔: {towerData.GetAttackInterval(level):F2}");
        #endif
    }
    //塔link 添加
    public void TowerLinkAdd()
    {
        
    }
    //塔link 添加
    public void TowerLinkDelete()
    {
        
    }
    /// <summary>
    /// 播放升级特效
    /// </summary>
    private void PlayUpgradeEffect()
    {
        if (upgradeEffectPrefab == null)
        {
            Debug.LogWarning($"塔 {this.name} 的升级特效预制体为空，跳过特效播放");
            return;
        }
        
        try
        {
            // 计算特效播放位置
            Vector3 effectPosition = transform.position + upgradeEffectOffset;
            
            // 实例化特效
            GameObject effect = Instantiate(upgradeEffectPrefab, effectPosition, Quaternion.identity);
            
            // 设置特效到Effect层级
            SetEffectToEffectLayer(effect);
            
            // 控制粒子系统只播放一次
            ControlParticleSystemPlayOnce(effect);
            
            Debug.Log($"塔 {this.name} 升级特效播放成功");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"塔 {this.name} 播放升级特效时发生异常: {e.Message}");
        }
    }
    
    /// <summary>
    /// 控制粒子系统只播放一次
    /// </summary>
    private void ControlParticleSystemPlayOnce(GameObject effect)
    {
        // 获取所有粒子系统组件
        var particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
        
        foreach (var ps in particleSystems)
        {
            // 设置粒子系统只播放一次
            var main = ps.main;
            main.loop = false; // 关闭循环
            main.playOnAwake = true; // 确保自动播放
            
            // 设置停止行为为销毁
            main.stopAction = ParticleSystemStopAction.Destroy;
            
            // 播放粒子系统
            ps.Play();
        }
        
        // 如果特效没有自动销毁，设置一个定时器来销毁它
        var monoBehaviour = effect.GetComponent<MonoBehaviour>();
        if (monoBehaviour == null)
        {
            // 如果没有MonoBehaviour组件，添加一个临时的
            var tempMono = effect.AddComponent<TempMonoBehaviour>();
            tempMono.StartCoroutine(DestroyEffectAfterDelay(effect, 0.5f));
        }
        else
        {
            monoBehaviour.StartCoroutine(DestroyEffectAfterDelay(effect, 0.5f));
        }
    }
    
    /// <summary>
    /// 延迟销毁特效
    /// </summary>
    private System.Collections.IEnumerator DestroyEffectAfterDelay(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (effect != null)
        {
            Destroy(effect);
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
}