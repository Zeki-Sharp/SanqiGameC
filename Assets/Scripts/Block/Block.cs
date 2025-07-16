using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Tilemaps;

public class Block : MonoBehaviour
{
    [Header("方块配置")] [SerializeField] private BlockGenerationConfig config;
    [SerializeField] private Vector2Int worldPosition; // 方块在世界中的位置
    [SerializeField] private bool canBeOverridden = true;
    [SerializeField] private GameObject towerPrefab;

    [Header("塔管理")] [ShowInInspector] private Dictionary<Vector2Int, Tower> towers = new Dictionary<Vector2Int, Tower>();

    // 公共属性（返回只读副本）
    public BlockGenerationConfig Config => config;
    public Vector2Int WorldPosition => worldPosition;
    public IReadOnlyDictionary<Vector2Int, Tower> Towers => towers;
    public bool CanBeOverridden => canBeOverridden;

    /// <summary>
    /// 初始化方块（通过形状名称）
    /// </summary>
    public void Init(string shapeName)
    {
        LoadTowerPrefab();

        if (towerPrefab == null)
        {
            Debug.LogError("Tower prefab not found at the specified path.");
            return;
        }

        // 加载配置
        config = Resources.Load<BlockGenerationConfig>("Data/Blocks/" + shapeName);

        InitInternal();
    }

    /// <summary>
    /// 初始化方块（通过配置对象）
    /// </summary>
    public void Init(BlockGenerationConfig bGConfig)
    {
        config = bGConfig;
        LoadTowerPrefab();

        InitInternal();
    }

    private void LoadTowerPrefab()
    {
        if (towerPrefab == null)
        {
            towerPrefab = Resources.Load<GameObject>("Prefab/Tower/Tower");
        }
    }

    private void InitInternal()
    {
        if (config == null || config.BlockGrid == null)
        {
            Debug.LogError($"无法获取形状: {config?.name ?? "null"}");
            return;
        }

        towers.Clear();

        foreach (Vector2Int coord in config.Coordinates)
        {
            towers[coord] = null;
            // 开发阶段调试日志，上线前可关闭
            // Debug.Log($"生成塔于格子 ({coord.x}, {coord.y})");
        }

        // Debug.Log($"方块初始化完成，形状: {config.name}，包含 {config.CellCount} 个格子");
    }

    /// <summary>
    /// 设置方块的世界坐标并根据地图大小调整尺寸
    /// </summary>
    public void SetWorldPosition(Vector3Int cellPos, Tilemap tilemap = null)
    {
        worldPosition = new Vector2Int(cellPos.x, cellPos.y);
        float baseScale = 1.5f;
        if (tilemap != null)
        {
            Vector3 cellCenter = tilemap.GetCellCenterWorld(cellPos);
            transform.position = cellCenter;
        }
        else
        {
            transform.position = new Vector3(cellPos.x, cellPos.y, 0);
        }
    }

    public void GenerateTowers(Vector2Int[] localCoord, TowerData[] towerData, Tilemap tilemap = null)
    {
        for (int i = 0; i < localCoord.Length; i++)
        {
            if (i >= towerData.Length)
            {
                Debug.LogWarning($"索引 {i} 超出塔数据长度");
                break;
            }

            Tower tower = GenerateTower(localCoord[i], towerData[i], tilemap);
            towers[localCoord[i]] = tower;
        }
    }

    /// <summary>
    /// 在指定格子位置生成塔
    /// </summary>
    public Tower GenerateTower(Vector2Int localCoord, TowerData towerData, Tilemap tilemap = null)
    {
        if (towers.Count > 0)
        {

            if (towers.TryGetValue(localCoord, out Tower tower) && tower != null)
            {
                Debug.LogWarning($"格子 ({localCoord.x}, {localCoord.y}) 已经有塔了");
                return towers[localCoord];
            }
        }

        Vector3Int towerCellPos = new Vector3Int(worldPosition.x + localCoord.x, worldPosition.y + localCoord.y, 0);
        Vector3 cellCenter = tilemap != null ? tilemap.GetCellCenterWorld(towerCellPos) : new Vector3(towerCellPos.x, towerCellPos.y, 0);
        if (towerPrefab == null)
        {
            Debug.LogError("Tower prefab is null when trying to instantiate.");
            return null;
        }
        GameObject go = Instantiate(towerPrefab, transform);
        
        // 直接使用cellCenter，不添加Y轴偏移，与预览保持一致
        go.transform.position = cellCenter;
        
        Tower towerComponent = go.GetComponent<Tower>();
        if (towerComponent == null)
        {
            Debug.LogError("Tower prefab does not have a Tower component.");
            return null;
        }
        const int BaseOrder = 1000;
        const int VerticalOffsetMultiplier = 10;
        int verticalOffset = Mathf.RoundToInt(-cellCenter.y * VerticalOffsetMultiplier);
        int finalOrder = BaseOrder + verticalOffset;
        towerComponent.Initialize(towerData,localCoord);
        towerComponent.SetOrder(finalOrder);
        towers[localCoord] = towerComponent;
        return towerComponent;
    }


    public bool IsCellEmpty(Vector2Int localCoord)
    {
        return towers.TryGetValue(localCoord, out var tower) && tower == null;
    }

    public Tower GetTower(Vector2Int localCoord)
    {
        towers.TryGetValue(localCoord, out var tower);
        return tower;
    }

    public void RemoveTower(Vector2Int localCoord)
    {
        if (towers.TryGetValue(localCoord, out var tower) && tower != null)
        {
#if UNITY_EDITOR
            Debug.Log($"移除格子 ({localCoord.x}, {localCoord.y}) 的塔");
#endif
            towers.Remove(localCoord);
            Destroy(tower.gameObject);
            if (towers.Count == 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public void  SetTower(Vector2Int localCoord, Tower tower)
    {
        towers[localCoord] = tower;
    }
    public void ClearTower()
    {
        towers.Clear();
    }

    public int GetTowerCount()
    {
        int count = 0;
        foreach (var tower in towers.Values)
        {
            if (tower != null) count++;
        }

        return count;
    }

    public int GetTotalCellCount()
    {
        return config?.CellCount ?? 0;
    }
}