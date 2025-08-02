using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;

/// <summary>
/// 子弹管理器 - 使用Unity官方ObjectPool统一管理所有子弹的对象池和配置
/// </summary>
public class BulletManager : MonoBehaviour
{
    [Header("调试信息")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 对象池管理 - 使用Unity官方ObjectPool
    private Dictionary<string, ObjectPool<BulletBase>> bulletPools = new Dictionary<string, ObjectPool<BulletBase>>();
    private Dictionary<string, BulletConfig> bulletConfigs = new Dictionary<string, BulletConfig>();
    
    // 性能统计
    private Dictionary<string, int> poolUsageStats = new Dictionary<string, int>();

    [SerializeField]private GameObject poolManager;

    public GameObject GetPoolManager()
    {
        return poolManager;
    }
    private void Awake()
    {
        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSystem(this);
        }
        
        InitializeManager();
    }
    
    /// <summary>
    /// 初始化管理器
    /// </summary>
    private void InitializeManager()
    {
        Debug.Log("BulletManager 初始化完成");
        LoadBulletConfigs();
    }
    
    /// <summary>
    /// 加载子弹配置
    /// </summary>
    private void LoadBulletConfigs()
    {
        BulletConfig[] configs = Resources.LoadAll<BulletConfig>("Data/Bullet");
        foreach (var config in configs)
        {
            RegisterBulletConfig(config);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"BulletManager: 加载了 {bulletConfigs.Count} 个子弹配置");
        }
    }
    
    /// <summary>
    /// 注册子弹配置
    /// </summary>
    public void RegisterBulletConfig(BulletConfig config)
    {
        if (config == null || string.IsNullOrEmpty(config.BulletName))
        {
            Debug.LogError("BulletManager: 无效的子弹配置");
            return;
        }
        
        bulletConfigs[config.BulletName] = config;
        
        if (showDebugInfo)
        {
            Debug.Log($"BulletManager: 注册子弹配置 - {config.BulletName}");
        }
    }
    
    /// <summary>
    /// 获取子弹（从对象池）
    /// </summary>
    public GameObject GetBullet(string bulletName, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrEmpty(bulletName))
        {
            Debug.LogError("BulletManager: 子弹名称为空");
            return null;
        }
        
        // 确保对象池存在
        EnsurePoolExists(bulletName);
        
        // 从对象池获取
        BulletBase bullet = bulletPools[bulletName].Get();
        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        
        // 设置对象池标识
        bullet.SetFromPool(bulletName);
        
        // 更新统计
        UpdateUsageStats(bulletName, 1);
        
        if (showDebugInfo)
        {
            Debug.Log($"BulletManager: 获取子弹 - {bulletName} at {position}");
        }
        
        return bullet.gameObject;
    }
    
    /// <summary>
    /// 返回子弹到对象池
    /// </summary>
    public void ReturnBullet(BulletBase bullet)
    {
        if (bullet == null || string.IsNullOrEmpty(bullet.PoolKey))
        {
            Debug.LogWarning("BulletManager: 无效的子弹返回请求");
            return;
        }
        
        string poolKey = bullet.PoolKey;
        
        if (bulletPools.ContainsKey(poolKey))
        {
            // 返回对象池（不在这里重置，让OnBulletRelease处理）
            bulletPools[poolKey].Release(bullet);
            
            if (showDebugInfo)
            {
                Debug.Log($"BulletManager: 返回子弹 - {poolKey}");
            }
        }
        else
        {
            Debug.LogWarning($"BulletManager: 对象池不存在 - {poolKey}");
            Destroy(bullet.gameObject);
        }
    }
    
    /// <summary>
    /// 确保对象池存在
    /// </summary>
    private void EnsurePoolExists(string bulletName)
    {
        if (bulletPools.ContainsKey(bulletName))
        {
            return;
        }
        
        if (!bulletConfigs.ContainsKey(bulletName))
        {
            Debug.LogError($"BulletManager: 子弹配置不存在 - {bulletName}");
            return;
        }
        
        BulletConfig config = bulletConfigs[bulletName];
        CreatePool(bulletName, config);
    }
    
    /// <summary>
    /// 创建对象池
    /// </summary>
    private void CreatePool(string bulletName, BulletConfig config)
    {
        if (config.BulletPrefab == null)
        {
            Debug.LogError($"BulletManager: 子弹预制体为空 - {bulletName}");
            return;
        }
        
        // 创建Unity官方对象池
        ObjectPool<BulletBase> pool = new ObjectPool<BulletBase>(
            createFunc: () => CreateBulletInstance(config.BulletPrefab, config),
            actionOnGet: (obj) => OnBulletGet(obj),
            actionOnRelease: (obj) => OnBulletRelease(obj),
            actionOnDestroy: (obj) => OnBulletDestroy(obj),
            collectionCheck: false,
            defaultCapacity: config.InitialPoolSize,
            maxSize: config.MaxPoolSize
        );
        
        bulletPools[bulletName] = pool;
        
        // 初始化统计
        poolUsageStats[bulletName] = 0;
        
        if (showDebugInfo)
        {
            Debug.Log($"BulletManager: 创建对象池 - {bulletName} (初始大小: {config.InitialPoolSize}, 最大大小: {config.MaxPoolSize})");
        }
    }
    
    /// <summary>
    /// 创建子弹实例
    /// </summary>
    private BulletBase CreateBulletInstance(GameObject prefab)
    {
        GameObject instance = Instantiate(prefab);
        instance.SetActive(false);
        
        // 确保子弹保持预制体的原始缩放
        BulletBase bullet = instance.GetComponent<BulletBase>();
        if (bullet != null)
        {
            // 在SetFromPool时会保存原始缩放
        }
        
        return bullet;
    }
    
    /// <summary>
    /// 创建子弹实例（带配置）
    /// </summary>
    private BulletBase CreateBulletInstance(GameObject prefab, BulletConfig config)
    {
        GameObject instance = Instantiate(prefab);
        instance.SetActive(false);
        
        BulletBase bullet = instance.GetComponent<BulletBase>();
        if (bullet != null)
        {
            // 设置bulletConfig
            bullet.SetBulletConfig(config);
        }
        
        return bullet;
    }
    
    /// <summary>
    /// 子弹获取时的处理
    /// </summary>
    private void OnBulletGet(BulletBase bullet)
    {
        bullet.gameObject.SetActive(true);
        
        // 确保子弹缩放正确
        if (bullet.OriginalScale != Vector3.zero)
        {
            bullet.transform.localScale = bullet.OriginalScale;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"BulletManager: 激活子弹 {bullet.name}，位置: {bullet.transform.position}");
        }
    }
    
    /// <summary>
    /// 子弹释放时的处理
    /// </summary>
    private void OnBulletRelease(BulletBase bullet)
    {
        // 重置子弹状态
        bullet.Reset();
        bullet.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 子弹销毁时的处理
    /// </summary>
    private void OnBulletDestroy(BulletBase bullet)
    {
        if (bullet != null && bullet.gameObject != null)
        {
            Destroy(bullet.gameObject);
        }
    }
    
    /// <summary>
    /// 更新使用统计
    /// </summary>
    private void UpdateUsageStats(string bulletName, int delta)
    {
        if (poolUsageStats.ContainsKey(bulletName))
        {
            poolUsageStats[bulletName] += delta;
        }
    }
    
    /// <summary>
    /// 获取子弹配置
    /// </summary>
    public BulletConfig GetBulletConfig(string bulletName)
    {
        bulletConfigs.TryGetValue(bulletName, out BulletConfig config);
        return config;
    }
    
    /// <summary>
    /// 获取所有子弹配置
    /// </summary>
    public Dictionary<string, BulletConfig> GetAllBulletConfigs()
    {
        return new Dictionary<string, BulletConfig>(bulletConfigs);
    }
    
    /// <summary>
    /// 获取对象池统计信息
    /// </summary>
    public Dictionary<string, object> GetPoolStats()
    {
        var stats = new Dictionary<string, object>();
        
        foreach (var kvp in bulletPools)
        {
            string bulletName = kvp.Key;
            ObjectPool<BulletBase> pool = kvp.Value;
            
            stats[bulletName] = new
            {
                ActiveCount = pool.CountActive,
                InactiveCount = pool.CountInactive,
                TotalCount = pool.CountAll,
                UsageCount = poolUsageStats.ContainsKey(bulletName) ? poolUsageStats[bulletName] : 0
            };
        }
        
        return stats;
    }
    
    /// <summary>
    /// 清理所有对象池
    /// </summary>
    public void ClearAllPools()
    {
        bulletPools.Clear();
        poolUsageStats.Clear();
        
        if (showDebugInfo)
        {
            Debug.Log("BulletManager: 清理所有对象池");
        }
    }
    
    /// <summary>
    /// 预热对象池
    /// </summary>
    public void WarmupPool(string bulletName, int count)
    {
        if (!bulletPools.ContainsKey(bulletName))
        {
            Debug.LogWarning($"BulletManager: 对象池不存在，无法预热 - {bulletName}");
            return;
        }
        
        ObjectPool<BulletBase> pool = bulletPools[bulletName];
        List<BulletBase> tempList = new List<BulletBase>();
        
        // 预热指定数量的对象
        for (int i = 0; i < count; i++)
        {
            tempList.Add(pool.Get());
        }
        
        // 立即返回
        foreach (var obj in tempList)
        {
            pool.Release(obj);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"BulletManager: 预热对象池 - {bulletName} ({count} 个对象)");
        }
    }
    
    private void OnDestroy()
    {
        ClearAllPools();
    }
    
    /// <summary>
    /// 显示调试信息
    /// </summary>
    private void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("BulletManager 调试信息", GUI.skin.box);
        
        foreach (var kvp in bulletPools)
        {
            string bulletName = kvp.Key;
            ObjectPool<BulletBase> pool = kvp.Value;
            
            GUILayout.Label($"{bulletName}: 活跃 {pool.CountActive} / 空闲 {pool.CountInactive} / 总计 {pool.CountAll}");
        }
        
        GUILayout.EndArea();
    }
} 