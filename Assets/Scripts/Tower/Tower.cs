using System;
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
       
    } 
    
    protected virtual void OnDetectCollider(Collider2D collider) 
    {
        // 展示区域的塔不进行游戏逻辑
        if (isShowAreaTower) return;
        if (towerData == null || bulletCaster == null) return;
        if (IsCenterTowerDestroyed()) return;
        
        float attackSpeed = towerData?.GetAttackSpeed(level) ?? 1f;
        attackSpeed = attackSpeed > 0 ? attackSpeed : 1f;
        bulletCaster.ammo.reloadTime = attackSpeed;
        rangeDetector.Radius = towerData?.GetAttackRange(level) ?? 3f;
        
        raySensor.SetStartEnd(this.transform, collider.transform);
        SetBulletDamage();
        bulletCaster.Cast(0);
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
    }
    
    private void OnDestroy()
    {
        if (rangeDetector != null)
            rangeDetector.onDetectCollider.RemoveListener(OnDetectCollider);
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
        if (isShowAreaTower || towerData == null || rangeDetector == null)
            return;
        
        if (cachedAttackSpeed <= 0f)
            cachedAttackSpeed = towerData.GetAttackSpeed(level) > 0 ? towerData.GetAttackSpeed(level) : 1f;
        
        if (Time.unscaledTime - lastAttackTime >= 1f / cachedAttackSpeed)
        {
            rangeDetector.Cast();
            if (cachedAttackRange <= 0f)
                cachedAttackRange = towerData.GetAttackRange(level);

            rangeDetector.Radius = cachedAttackRange;
            lastAttackTime = Time.unscaledTime;
        }
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
        rangeDetector.Cast();
        // foreach (var enemy in rangeDetector.de )
        // {
        //     
        //     // var blockHit = rangeDetector.DetectedLOSHits[enemy]; 
        //     // // if (enemy.TryGetComponent(out GameObject obj)) 
        //     // // { 
        //     // //     float dist = Vector3.Distance(transform.position, enemy.transform.position);
        //     // //     return obj;
        //     // // }
        // }
        // GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        // float minDist = float.MaxValue;
        // GameObject closest = null;
        // foreach (var enemy in enemies)
        // {
        //     float dist = Vector3.Distance(transform.position, enemy.transform.position);
        //     if (dist <= towerData.GetAttackRange(level) && dist < minDist)
        //     {
        //         minDist = dist;
        //         closest = enemy;
        //     }
    // }

        return null;
    }

    private void FireAt(GameObject target)
    { 
        // // 使用新的子弹系统
        // var bulletConfig = towerData?.GetBulletConfig();
        // if (bulletConfig != null)
        // {
        //     var bulletManager = GameManager.Instance?.GetSystem<BulletManager>();
        //     if (bulletManager != null)
        //     {
        //         GameObject bullet = bulletManager.GetBullet(bulletConfig.BulletName, transform.position, Quaternion.identity);
        //         var bulletScript = bullet.GetComponent<IBullet>();
        //         if (bulletScript != null)
        //         {
        //             Vector3 direction = (target.transform.position - transform.position).normalized;
        //             bulletScript.Initialize(direction, 0, gameObject, target, bulletConfig.TargetTags, towerData.GetPhysicAttack(level));
        //         }
        //         else
        //         {
        //             Debug.LogWarning("子弹预制体未挂载IBullet实现脚本！");
        //         }
        //     }
        //     else
        //     {
        //         Debug.LogWarning("BulletManager未找到！");
        //     }
        // }
        // else
        // {
        //     Debug.LogWarning("塔没有配置子弹配置！请在TowerData中设置BulletConfig。");
        // }
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