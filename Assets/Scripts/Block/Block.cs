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

        // 初始化所有格子位置
        foreach (Vector3Int coord in config.Coordinates)
        {
            towers[coord] = null;
            Debug.Log($"初始化格子位置: ({coord.x}, {coord.y})");
        }

        Debug.Log($"方块初始化完成，形状: {config.name}，包含 {config.CellCount} 个格子");
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
        }
    }

    /// <summary>
    /// 在指定格子位置生成塔
    /// </summary>
    public Tower GenerateTower(Vector3Int localCoord, TowerData towerData, Tilemap tilemap = null, bool hasCheck = false)
    {
        // 检查参数
        if (towerData == null)
        {
            Debug.LogError($"尝试在格子 ({localCoord.x}, {localCoord.y}) 生成塔时，TowerData为空");
            return null;
        }

        if (towerPrefab == null)
        {
            Debug.LogError("塔预制体为空，请确保已正确加载");
            return null;
        }

        // 检查格子是否已经有塔
        if (towers.TryGetValue(localCoord, out Tower existingTower) && existingTower != null)
        {
            Debug.LogWarning($"格子 ({localCoord.x}, {localCoord.y}) 已经有塔了");
            return existingTower;
        }

        // 计算世界坐标
        Vector3Int towerCellPos = new Vector3Int(cellPosition.x + localCoord.x, cellPosition.y + localCoord.y, 0);
        Debug.Log($"在位置 ({towerCellPos.x}, {towerCellPos.y}) 生成塔 {towerData.TowerName}");

        // 生成塔
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

        if (towerComponent == null)
        {
            Debug.LogError($"塔生成失败：{towerData.TowerName}");
            return null;
        }

        // 确保塔的渲染器是启用的
        var renderers = towerComponent.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
                Debug.Log($"启用塔 {towerData.TowerName} 的渲染器");
            }
        }

        // 保存塔的引用
        towers[localCoord] = towerComponent;
        Debug.Log($"塔 {towerData.TowerName} 生成完成，已保存到字典中");

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
Debug.Log($"格子 ({localCoord.x}, {localCoord.y}) 的塔将要移除");
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
                Debug.Log($"销毁塔({localCoord.x}, {localCoord.y})");
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
                    // 修复：使用正确的cellPosition而不是localCoord
                    gameMap.RemoveBlock(cellPosition,this);
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