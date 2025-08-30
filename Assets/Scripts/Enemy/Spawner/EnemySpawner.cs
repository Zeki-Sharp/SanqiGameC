using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RaycastPro.Casters2D;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 敌人生成器 - 多波次多类型敌人生成，支持多个手动框选区域
/// 配置完全由RoundConfig管理，不再支持Inspector面板配置
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("生成设置")]
    [SerializeField] private float unitSpawnDelay = 1f;
    
    [Header("生成区域 (可多选)")]
    public List<SpawnArea> spawnAreas = new List<SpawnArea>();

    [Header("调试")]
    public bool showSpawnAreas = true;
    public bool debugSpawnInfo = true;

    // 私有字段，不再在Inspector中显示
    private List<Wave> waves = new List<Wave>();
    private int currentWaveIndex = 0;
    private int currentEnemyCount = 0;
    private Coroutine spawnRoutine;
    
    private BulletManager bulletManager;
    private void Start()
    {
        bulletManager = GameManager.Instance.GetSystem<BulletManager>();
        
        // 订阅敌人死亡事件，用于更新敌人计数
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Subscribe<EnemyDeathEventArgs>(OnEnemyDeath);
        }
        
        // 不再自动开始，完全由RoundManager控制
        Debug.Log("EnemySpawner初始化完成，等待RoundManager调用");
    }

    public void StartWaves()
    {
        if (waves == null || waves.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: 没有配置waves，无法开始生成");
            return;
        }
        
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnWavesCoroutine());
    }

    private IEnumerator SpawnWavesCoroutine()
    {
        Debug.Log($"EnemySpawner: 开始生成 {waves.Count} 个Wave");
        
        for (currentWaveIndex = 0; currentWaveIndex < waves.Count; currentWaveIndex++)
        {
            Wave wave = waves[currentWaveIndex];
            if (wave.delayBeforeWave > 0)
            {
                Debug.Log($"EnemySpawner: Wave {currentWaveIndex + 1} 延迟 {wave.delayBeforeWave} 秒");
                yield return new WaitForSeconds(wave.delayBeforeWave);
            }
            
            Debug.Log($"EnemySpawner: 开始生成Wave {currentWaveIndex + 1}，包含 {wave.enemies.Count} 种敌人");
            
            // 创建随机打乱的敌人生成序列
            List<EnemySpawnInfo> shuffledEnemySequence = CreateShuffledEnemySequence(wave);
            
            // 按照打乱后的顺序生成敌人
            foreach (var enemyInfo in shuffledEnemySequence)
            {
                SpawnEnemy(wave, enemyInfo.enemyData);
                yield return new WaitForSeconds(unitSpawnDelay);
            }
        }
        
        Debug.Log("EnemySpawner: 所有Wave生成完成");
    }

    /// <summary>
    /// 创建随机打乱的敌人生成序列
    /// 将同一wave中所有类型的敌人混合在一起，随机打乱顺序
    /// </summary>
    private List<EnemySpawnInfo> CreateShuffledEnemySequence(Wave wave)
    {
        List<EnemySpawnInfo> shuffledSequence = new List<EnemySpawnInfo>();
        
        // 为每种敌人类型创建对应数量的EnemySpawnInfo
        foreach (var enemyInfo in wave.enemies)
        {
            for (int i = 0; i < enemyInfo.count; i++)
            {
                // 创建新的EnemySpawnInfo，避免修改原始配置
                EnemySpawnInfo newEnemyInfo = new EnemySpawnInfo
                {
                    enemyData = enemyInfo.enemyData,
                    count = 1 // 每个都是1个，用于随机打乱
                };
                shuffledSequence.Add(newEnemyInfo);
            }
        }
        
        // 使用Fisher-Yates洗牌算法随机打乱顺序
        for (int i = shuffledSequence.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            var temp = shuffledSequence[i];
            shuffledSequence[i] = shuffledSequence[randomIndex];
            shuffledSequence[randomIndex] = temp;
        }
        
        if (debugSpawnInfo)
        {
            Debug.Log($"Wave {currentWaveIndex + 1}: 创建了 {shuffledSequence.Count} 个敌人的随机生成序列");
        }
        
        return shuffledSequence;
    }

    /// <summary>
    /// 生成单个敌人
    /// </summary>
    public void SpawnEnemy(Wave wave, EnemyData enemyData)
    {
        if (enemyData == null)
        {
            Debug.LogError("EnemySpawner: 未设置敌人Data！");
            return;
        }
        
        if (enemyData.EnemyPrefab == null)
        {
            Debug.LogError($"EnemySpawner: EnemyData '{enemyData.EnemyName}' 的enemyPrefab为空，无法生成敌人！");
            return;
        }
        
        Vector3 spawnPosition = CalculateSpawnPositionInAreas();
        GameObject enemyPrefab = enemyData.EnemyPrefab;
        GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        enemyObject.GetComponent<BasicCaster2D>().poolManager = bulletManager.GetPoolManager();
        enemyObject.GetComponent<EnemyController>().SetEnemyData(enemyData);
        enemyObject.name = enemyData.EnemyName + FindObjectsByType<EnemyController>(sortMode: FindObjectsSortMode.InstanceID).Length;
        if (enemyData.EnemySprite != null)
        {
            enemyObject.GetComponent<SpriteRenderer>().sprite = enemyData.EnemySprite;
        }
        // 设置敌人朝向
        SetEnemyDirection(enemyObject, spawnPosition);
        
        currentEnemyCount++;
        
        if (debugSpawnInfo)
        {
            Debug.Log($"EnemySpawner: 生成敌人 {enemyPrefab.name} 在位置 {spawnPosition}，当前敌人总数: {currentEnemyCount}");
        }
    }

    /// <summary>
    /// 在所有区域中随机选择一个区域，并在其中随机生成点
    /// </summary>
    private Vector3 CalculateSpawnPositionInAreas()
    {
        if (spawnAreas == null || spawnAreas.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: 未设置生成区域，使用(0,0,0)");
            return Vector3.zero;
        }
        int areaIndex = Random.Range(0, spawnAreas.Count);
        SpawnArea area = spawnAreas[areaIndex];
        float x = Random.Range(area.min.x, area.max.x);
        float y = Random.Range(area.min.y, area.max.y);
        return new Vector3(x, y, 0f);
    }

    [ContextMenu("清除所有敌人")]
    public void ClearAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }
        currentEnemyCount = 0;
        Debug.Log($"EnemySpawner: 清除所有敌人，共清除 {enemies.Length} 个敌人");
    }
    
    /// <summary>
    /// 处理敌人死亡事件，减少敌人计数
    /// </summary>
    private void OnEnemyDeath(EnemyDeathEventArgs e)
    {
        if (currentEnemyCount > 0)
        {
            currentEnemyCount--;
            if (debugSpawnInfo)
            {
                Debug.Log($"EnemySpawner: 敌人 {e.EnemyName} 死亡，当前敌人总数: {currentEnemyCount}");
            }
        }
    }
    
    /// <summary>
    /// 清理事件订阅
    /// </summary>
    private void OnDestroy()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<EnemyDeathEventArgs>(OnEnemyDeath);
        }
    }
    
    /// <summary>
    /// 设置Wave配置（由RoundManager调用）
    /// </summary>
    /// <param name="newWaves">新的Wave列表</param>
    public void SetWaves(List<Wave> newWaves)
    {
        waves = newWaves;
        currentEnemyCount = 0; // 重置敌人计数，准备新回合
        Debug.Log($"EnemySpawner: 设置Wave配置，共 {waves?.Count ?? 0} 个Wave，敌人计数已重置");
    }
    
    /// <summary>
    /// 获取当前敌人数量
    /// </summary>
    public int GetCurrentEnemyCount()
    {
        return currentEnemyCount;
    }
    
    /// <summary>
    /// 检查是否所有敌人都被消灭
    /// </summary>
    public bool AreAllEnemiesDefeated()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        return enemies.Length == 0;
    }

    /// <summary>
    /// 恢复Wave生成
    /// </summary>
    public void ResumeWaves()
    {
        Debug.Log("EnemySpawner: 恢复Wave生成");
        
        if (spawnRoutine == null && waves != null && waves.Count > 0)
        {
            // 如果协程被暂停，重新开始生成
            spawnRoutine = StartCoroutine(SpawnWavesCoroutine());
        }
        else if (spawnRoutine != null)
        {
            Debug.Log("EnemySpawner: Wave生成协程正在运行中");
        }
        else
        {
            Debug.LogWarning("EnemySpawner: 没有配置waves或waves已结束，无法恢复");
        }
    }
    
    /// <summary>
    /// 获取中心塔位置
    /// </summary>
    private Vector3 GetCenterTowerPosition()
    {
        GameObject centerTower = GameObject.FindGameObjectWithTag("CenterTower");
        if (centerTower == null)
        {
            Debug.LogWarning("未找到中心塔，使用默认位置(0,0,0)");
            return Vector3.zero;
        }
        return centerTower.transform.position;
    }
    
    /// <summary>
    /// 根据生成位置设置敌人朝向
    /// </summary>
    private void SetEnemyDirection(GameObject enemy, Vector3 spawnPosition)
    {
        Vector3 centerTowerPosition = GetCenterTowerPosition();
        bool isOnLeftSide = spawnPosition.x < centerTowerPosition.x;
        
        // 确保Transform旋转为0
        enemy.transform.rotation = Quaternion.identity;
        
        // 重置所有子对象的Transform（只影响敌人，不影响塔）
        Transform[] children = enemy.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            // 确保只影响敌人及其子对象，不影响塔
            if (child.rotation != Quaternion.identity && 
                (child.CompareTag("Enemy") || child.parent != null))
            {
                child.rotation = Quaternion.identity;
            }
        }
        
        SpriteRenderer spriteRenderer = enemy.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // 左侧生成朝右，右侧生成朝左
            spriteRenderer.flipX = !isOnLeftSide;
            // 确保Y轴不翻转
            spriteRenderer.flipY = false;
        }
        
        // 调用控制器的SetDirection方法确保状态同步
        var controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
        {
            Vector3 direction = (centerTowerPosition - spawnPosition).normalized;
            controller.SetDirection(direction);
        }
    }

    private void OnDrawGizmos()
    {
        if (showSpawnAreas && spawnAreas != null)
        {
            for (int i = 0; i < spawnAreas.Count; i++)
            {
                var area = spawnAreas[i];
                Vector3 center = new Vector3((area.min.x + area.max.x) / 2, (area.min.y + area.max.y) / 2, 0f);
                Vector3 size = new Vector3(Mathf.Abs(area.max.x - area.min.x), Mathf.Abs(area.max.y - area.min.y), 0.1f);
                Gizmos.color = new Color(0, 1, 0, 0.15f);
                Gizmos.DrawCube(center, size);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(center, size);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (showSpawnAreas && spawnAreas != null)
        {
            for (int i = 0; i < spawnAreas.Count; i++)
            {
                var area = spawnAreas[i];
                Handles.color = Color.yellow;
                Vector3 p0 = new Vector3(area.min.x, area.min.y, 0f);
                Vector3 p2 = new Vector3(area.max.x, area.max.y, 0f);
                EditorGUI.BeginChangeCheck();
                Vector3 newMin = Handles.FreeMoveHandle(p0, 0.15f, Vector3.zero, Handles.SphereHandleCap);
                Vector3 newMax = Handles.FreeMoveHandle(p2, 0.15f, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Move Spawn Area Corner");
                    area.min = new Vector2(Mathf.Min(newMin.x, newMax.x), Mathf.Min(newMin.y, newMax.y));
                    area.max = new Vector2(Mathf.Max(newMin.x, newMax.x), Mathf.Max(newMin.y, newMax.y));
                    EditorUtility.SetDirty(this);
                }
                // 可视化区域
                Handles.color = Color.yellow;
                Vector3 p1 = new Vector3(area.max.x, area.min.y, 0f);
                Vector3 p3 = new Vector3(area.min.x, area.max.y, 0f);
                Handles.DrawLine(p0, p1);
                Handles.DrawLine(p1, p2);
                Handles.DrawLine(p2, p3);
                Handles.DrawLine(p3, p0);
            }
        }
    }
#endif
} 