using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine.Tilemaps;

public class Block : MonoBehaviour
{
    [Header("方块配置")] [SerializeField] private BlockGenerationConfig config;
    [SerializeField] private Vector3Int cellPosition; // 方块在Tilemap中的cell坐标位置
    [SerializeField] private bool canBeOverridden = true;
    [SerializeField] private GameObject towerPrefab;

    [Header("塔管理")] [ShowInInspector] private Dictionary<Vector3Int, Tower> towers = new Dictionary<Vector3Int, Tower>();

    // 公共属性（返回只读副本）
    public BlockGenerationConfig Config => config;
    public Vector3Int CellPosition => cellPosition;
    public IReadOnlyDictionary<Vector3Int, Tower> Towers => towers;
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

        foreach (Vector3Int coord in config.Coordinates)
        {
            towers[coord] = null;
            // 开发阶段调试日志，上线前可关闭
            // Debug.Log($"生成塔于格子 ({coord.x}, {coord.y})");
        }

        // Debug.Log($"方块初始化完成，形状: {config.name}，包含 {config.CellCount} 个格子");
    }

    /// <summary>
    /// 设置方块的cell坐标并根据地图大小调整尺寸
    /// </summary>
    /// <param name="cellPos">方块在Tilemap中的cell坐标</param>
    /// <param name="tilemap">Tilemap引用，用于计算世界坐标</param>
    public void SetCellPosition(Vector3Int cellPos, Tilemap tilemap = null)
    {
        cellPosition = new Vector3Int(cellPos.x, cellPos.y);
        if (tilemap != null)
        {
            Vector3 cellCenter = CoordinateUtility.CellToWorldPosition(tilemap, cellPos);
            transform.position = cellCenter;
        }
        else
        {
            transform.position = new Vector3(cellPos.x, cellPos.y, 0);
        }
    }

    public void GenerateTowers(Vector3Int[] localCoord, TowerData[] towerData, Tilemap tilemap = null,bool hasCheck = false)
    {
        for (int i = 0; i < localCoord.Length; i++)
        {
            if (i >= towerData.Length)
            {
                Debug.LogWarning($"索引 {i} 超出塔数据长度");
                break;
            }

            Tower tower = GenerateTower(localCoord[i], towerData[i], tilemap,hasCheck);
            towers[localCoord[i]] = tower;
        }
    }

    /// <summary>
    /// 在指定格子位置生成塔
    /// </summary>
    public Tower GenerateTower(Vector3Int localCoord, TowerData towerData, Tilemap tilemap = null,bool hasCheck = false)
    {
        if (towers.Count > 0)
        {
            if (towers.TryGetValue(localCoord, out Tower tower) && tower != null)
            {
                Debug.LogWarning($"格子 ({localCoord.x}, {localCoord.y}) 已经有塔了");
                return towers[localCoord];
            }
        }
        Vector3Int towerCellPos = new Vector3Int(cellPosition.x + localCoord.x, cellPosition.y + localCoord.y, 0);

        Tower towerComponent = TowerBuildUtility.GenerateTower(
            this.transform,
            towerPrefab,
            towerCellPos,
            tilemap,
            towerData,
            false,
            Color.white,
            hasCheck
        );
        towers[towerCellPos] = towerComponent;
        return towerComponent;
    }


    public bool IsCellEmpty(Vector3Int localCoord)
    {
        return towers.TryGetValue(localCoord, out var tower) && tower == null;
    }

    public Tower GetTower(Vector3Int localCoord)
    {
        towers.TryGetValue(localCoord, out var tower);
        return tower;
    }
    public Vector3Int GetTowerLocalCoord(Tower tower)
    {
        return towers.FirstOrDefault(kvp => kvp.Value == tower).Key;
    }

 public void RemoveTower(Vector3Int localCoord)
{
        // 检查 towers 字典是否已初始化
        if (towers == null)
        {
            Debug.LogError("Towers 字典未初始化");
            return;
        }

        // 检查 GameMap 是否有效
        var gameMap = GameManager.Instance?.GetSystem<GameMap>();
        if (gameMap == null)
        {
            string errorMessage = $"GameMap 未初始化，无法移除格子 ({localCoord.x}, {localCoord.y}) 的塔";
            Debug.LogError(errorMessage, this);
            // 可选：开发环境下触发断点
#if UNITY_EDITOR
            UnityEngine.Debug.Break();
#else
            // 或者抛出异常用于崩溃报告
            throw new System.Exception(errorMessage);
#endif
            return;
        }

        if (towers.TryGetValue(localCoord, out var tower))
        {
            // 检查 tower 是否有效
            if (tower == null)
            {
                Debug.LogWarning($"格子 ({localCoord.x}, {localCoord.y}) 的塔引用为空");
                towers.Remove(localCoord);
                return;
            }


            // 移除塔的引用
            towers.Remove(localCoord);

            // 销毁塔的游戏对象
            if (tower.gameObject != null)
            {
                Destroy(tower.gameObject);
            }
            else
            {
                Debug.LogWarning($"塔的游戏对象为空，无法销毁");
            }

            // 如果没有塔了，销毁当前游戏对象
            if (towers.Count == 0)
            {
                if (gameObject != null)
                {
                    // 移除地块
                    gameMap.RemoveBlock(localCoord);
                }
                else
                {
                    Debug.LogWarning("游戏对象为空，无法销毁");
                }
            }
        }
}


    public void  SetTower(Vector3Int localCoord, Tower tower)
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