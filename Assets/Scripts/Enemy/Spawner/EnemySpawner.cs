using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 敌人生成器 - 多波次多类型敌人生成，支持多个手动框选区域
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("生成设置")]
    public List<Wave> waves = new List<Wave>();
    public float unitSpawnDelay = 1f;

    [Header("生成区域 (可多选)")]
    public List<SpawnArea> spawnAreas = new List<SpawnArea>();

    [Header("调试")]
    public bool autoStart = true;
    public bool showSpawnAreas = true;
    public bool debugSpawnInfo = true;

    private int currentWaveIndex = 0;
    private int currentEnemyCount = 0;
    private Coroutine spawnRoutine;

    private void Start()
    {
        if (autoStart)
            StartWaves();
    }

    public void StartWaves()
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnWavesCoroutine());
    }

    private IEnumerator SpawnWavesCoroutine()
    {
        for (currentWaveIndex = 0; currentWaveIndex < waves.Count; currentWaveIndex++)
        {
            Wave wave = waves[currentWaveIndex];
            if (debugSpawnInfo)
                Debug.Log($"准备生成第{currentWaveIndex + 1}波，延迟{wave.delayBeforeWave}s");
            if (wave.delayBeforeWave > 0)
                yield return new WaitForSeconds(wave.delayBeforeWave);
            foreach (var enemyInfo in wave.enemies)
            {
                for (int i = 0; i < enemyInfo.count; i++)
                {
                    SpawnEnemy(enemyInfo.enemyPrefab);
                    yield return new WaitForSeconds(unitSpawnDelay);
                }
            }
        }
        if (debugSpawnInfo)
            Debug.Log("所有波次生成完毕");
    }

    /// <summary>
    /// 生成单个敌人
    /// </summary>
    public void SpawnEnemy(GameObject enemyPrefab)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("未设置敌人Prefab！");
            return;
        }
        Vector3 spawnPosition = CalculateSpawnPositionInAreas();
        GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        currentEnemyCount++;
        if (debugSpawnInfo)
        {
            Debug.Log($"生成敌人: {enemyObject.name} 在位置 {spawnPosition} (当前敌人数: {currentEnemyCount})");
        }
    }

    /// <summary>
    /// 在所有区域中随机选择一个区域，并在其中随机生成点
    /// </summary>
    private Vector3 CalculateSpawnPositionInAreas()
    {
        if (spawnAreas == null || spawnAreas.Count == 0)
        {
            Debug.LogWarning("未设置生成区域，使用(0,0,0)");
            return Vector3.zero;
        }
        int areaIndex = Random.Range(0, spawnAreas.Count);
        SpawnArea area = spawnAreas[areaIndex];
        float x = Random.Range(area.min.x, area.max.x);
        float y = Random.Range(area.min.y, area.max.y);
        return new Vector3(x, y, 0f);
    }

    [ContextMenu("开始波次生成")]
    public void StartWavesManual()
    {
        StartWaves();
    }

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