using UnityEngine;

/// <summary>
/// 敌人生成器 - 管理敌人的生成和配置
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("生成设置")]
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxEnemies = 10;
    
    [Header("调试")]
    [SerializeField] private bool autoSpawn = true;
    [SerializeField] private bool showSpawnPoint = true;
    
    private float lastSpawnTime;
    private int currentEnemyCount;
    
    private void Start()
    {
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
        
        lastSpawnTime = -spawnInterval; // 允许立即生成
    }
    
    private void Update()
    {
        if (autoSpawn && currentEnemyCount < maxEnemies)
        {
            if (Time.time - lastSpawnTime >= spawnInterval)
            {
                SpawnEnemy();
            }
        }
    }
    
    /// <summary>
    /// 生成敌人
    /// </summary>
    public void SpawnEnemy()
    {
        if (enemyData == null)
        {
            Debug.LogError("未设置敌人数据！");
            return;
        }
        
        if (currentEnemyCount >= maxEnemies)
        {
            Debug.Log("已达到最大敌人数限制");
            return;
        }
        
        GameObject enemyObject;
        
        if (enemyData.EnemyPrefab != null)
        {
            // 使用预制体生成
            enemyObject = Instantiate(enemyData.EnemyPrefab, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            // 创建基础敌人对象
            enemyObject = CreateBasicEnemy();
        }
        
        // 配置敌人
        ConfigureEnemy(enemyObject);
        
        currentEnemyCount++;
        lastSpawnTime = Time.time;
        
        Debug.Log($"生成敌人: {enemyObject.name} (当前敌人数: {currentEnemyCount})");
    }
    
    /// <summary>
    /// 创建基础敌人对象
    /// </summary>
    /// <returns>敌人GameObject</returns>
    private GameObject CreateBasicEnemy()
    {
        GameObject enemyObject = new GameObject($"Enemy_{currentEnemyCount}");
        enemyObject.transform.position = spawnPoint.position;
        
        // 添加必要的组件
        SpriteRenderer spriteRenderer = enemyObject.AddComponent<SpriteRenderer>();
        if (enemyData.EnemySprite != null)
        {
            spriteRenderer.sprite = enemyData.EnemySprite;
        }
        
        // 添加碰撞器
        CircleCollider2D collider = enemyObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        
        // 添加敌人控制器
        EnemyController controller = enemyObject.AddComponent<EnemyController>();
        
        return enemyObject;
    }
    
    /// <summary>
    /// 配置敌人属性
    /// </summary>
    /// <param name="enemyObject">敌人对象</param>
    private void ConfigureEnemy(GameObject enemyObject)
    {
        EnemyController controller = enemyObject.GetComponent<EnemyController>();
        if (controller != null)
        {
            // 通过反射设置私有字段（如果需要的话）
            // 这里可以通过公共方法或属性来设置
        }
        
        // 设置标签
        enemyObject.tag = "Enemy";
        
        // 设置名称
        enemyObject.name = $"{enemyData.EnemyName}_{currentEnemyCount}";
    }
    
    /// <summary>
    /// 手动生成敌人
    /// </summary>
    [ContextMenu("生成敌人")]
    public void SpawnEnemyManual()
    {
        SpawnEnemy();
    }
    
    /// <summary>
    /// 清除所有敌人
    /// </summary>
    [ContextMenu("清除所有敌人")]
    public void ClearAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            DestroyImmediate(enemy);
        }
        currentEnemyCount = 0;
        Debug.Log("已清除所有敌人");
    }
    
    /// <summary>
    /// 敌人死亡时调用
    /// </summary>
    public void OnEnemyDeath()
    {
        currentEnemyCount--;
        if (currentEnemyCount < 0) currentEnemyCount = 0;
    }
    
    private void OnDrawGizmos()
    {
        if (!showSpawnPoint) return;
        
        // 绘制生成点
        Gizmos.color = Color.green;
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        Gizmos.DrawWireSphere(spawnPos, 0.5f);
        
        // 绘制生成方向
        Gizmos.color = Color.yellow;
        Vector3 direction = spawnPoint != null ? spawnPoint.right : transform.right;
        Gizmos.DrawRay(spawnPos, direction * 1f);
    }
} 