using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class Block : MonoBehaviour
{
    [Header("方块配置")] [SerializeField] private BlockGenerationConfig config;
    [SerializeField] private Vector2Int worldPosition; // 方块在世界中的位置
    [SerializeField] private bool canBeOverridden = true;
    [SerializeField]private GameObject towerPrefab;
    [Header("塔管理")] [SerializeField] private Dictionary<Vector2Int, Tower> towers = new Dictionary<Vector2Int, Tower>();

    // 公共属性
    public BlockGenerationConfig Config => config;
    public Vector2Int WorldPosition => worldPosition;
    public Dictionary<Vector2Int, Tower> Towers => towers;

    public bool CanBeOverridden => canBeOverridden;

    /// <summary>
    /// 初始化方块
    /// </summary>
    /// <param name="shapeName">形状名称</param>
    public void Init(string shapeName)
    {
        if (towerPrefab!=null)
        // 在Start中加载Tower Prefab
        towerPrefab = Resources.Load<GameObject>("Prefab/Tower/Tower");

        if (towerPrefab == null)
        {
            Debug.LogError("Tower prefab not found at the specified path.");
            return;
        }
        // 获取对应的方块形状
        config = Resources.Load<BlockGenerationConfig>("Assets/Resources/Data/Blocks/" + shapeName);
        // config = BlockShape.GetShape(shapeName);

        if (config == null || config.BlockGrid == null)
        {
            Debug.LogError($"无法获取形状: {shapeName}");
            return;
        }

        // 清空现有的塔
        towers.Clear();

        // 遍历格子坐标，为每个格子预留塔的位置
        foreach (Vector2Int coord in config.Coordinates)
        {
            towers[coord] = null; // 初始化为null，表示还没有生成塔
            Debug.Log($"生成塔于格子 ({coord.x}, {coord.y})");
        }

        Debug.Log($"方块初始化完成，形状: {shapeName}，包含 {config.CellCount} 个格子");
    }

    /// <summary>
    /// 初始化方块
    /// </summary>
    /// <param name="shapeName">形状名称</param>
    public void Init(BlockGenerationConfig bGConfig)
    {
        config = bGConfig;

        if (config == null || config.BlockGrid == null)
        {
            Debug.LogError($"无法获取形状: {bGConfig}");
            return;
        }

        // 清空现有的塔
        towers.Clear();

        // 遍历格子坐标，为每个格子预留塔的位置
        foreach (Vector2Int coord in config.Coordinates)
        {
            towers[coord] = null; // 初始化为null，表示还没有生成塔
            Debug.Log($"生成塔于格子 ({coord.x}, {coord.y})");
        }

        Debug.Log($"方块初始化完成，形状: {bGConfig}，包含 {config.CellCount} 个格子");
    }

    /// <summary>
    /// 设置方块在世界中的位置
    /// </summary>
    /// <param name="position">格子坐标</param>
    /// <param name="tilemap">Tilemap引用</param>
    // public void SetWorldPosition(Vector2Int position, Tilemap tilemap = null)
    // {
    //     worldPosition = position;
    //     
    //     if (tilemap != null)
    //     {
    //         // 使用Tilemap的坐标转换
    //         Vector3 cellOrigin = tilemap.CellToWorld(new Vector3Int(position.x, position.y, 0));
    //         transform.position = cellOrigin + tilemap.cellSize / 2f;
    //     }
    //     else
    //     {
    //         // 备用方案：直接使用格子坐标
    //         transform.position = new Vector3(position.x, position.y, 0);
    //     }
    // }
    /// <summary>
    /// 设置方块的世界坐标并根据地图大小调整尺寸
    /// </summary>
    /// <param name="position">地图格子坐标</param>
    /// <param name="grid">使用的Grid组件（优先级高于Tilemap）</param>
    /// <param name="tilemap">使用的Tilemap组件（可选）</param>
    /// <param name="mapWidth">地图宽度（用于缩放适配）</param>
    /// <param name="mapHeight">地图高度（用于缩放适配）</param>
    public void SetWorldPosition(Vector2Int position, Tilemap tilemap = null, int mapWidth = 20, int mapHeight = 15)
    {
        worldPosition = position;

        // 默认缩放因子
        float baseScale = 1.5f;

        // 根据地图大小动态调整缩放
        float scaleFactor = Mathf.Min(1f, 10f / Mathf.Max(mapWidth, mapHeight));
        transform.localScale = Vector3.one * (baseScale * scaleFactor);

        if (tilemap != null)
        {
            Vector3 cellCenter = tilemap.CellToWorld(new Vector3Int(position.x, position.y, 0));
            Debug.Log($"cellCenter: {cellCenter}");
            transform.position = new Vector3(cellCenter.x, cellCenter.y - 0.4f, 0);
        }
        // else if (tilemap != null)
        // {
        //     Vector3 cellOrigin = tilemap.CellToWorld(new Vector3Int(position.x, position.y, 0));
        //     transform.position = new Vector3(
        //         cellOrigin.x + tilemap.cellSize.x / 2f,
        //         cellOrigin.y + tilemap.cellSize.y / 2f,
        //         0
        //     );
        // }
        else
        {
            transform.position = new Vector3(position.x, position.y, 0);
        }
    }

    public void GenerateTowers(Vector2Int[] localCoord, TowerData[] towerData, Tilemap tilemap = null)
    {
        for (int i = 0; i < localCoord.Length; i++)
        {
            Tower tower = GenerateTower(localCoord[i], towerData[i], tilemap);
            towers[localCoord[i]] = tower;
        }
    }

    /// <summary>
    /// 在指定格子位置生成塔
    /// </summary>
    /// <param name="localCoord">相对于方块的本地坐标</param>
    /// <param name="towerData">塔的数据</param>
    /// <param name="tilemap">Tilemap引用</param>
    /// <returns>生成的塔实例</returns>
    public Tower GenerateTower(Vector2Int localCoord, TowerData towerData, Tilemap tilemap = null)
    {
        if (!towers.ContainsKey(localCoord))
        {
            Debug.LogError($"格子 ({localCoord.x}, {localCoord.y}) 不在当前方块范围内");
            return null;
        }

        if (towers[localCoord] != null)
        {
            Debug.LogWarning($"格子 ({localCoord.x}, {localCoord.y}) 已经有塔了");
            return towers[localCoord];
        }

        // 计算塔的格子坐标
        Vector2Int towerGridPos = worldPosition + localCoord;

        // 计算塔的世界坐标
        Vector3 towerWorldPos;
        if (tilemap != null)
        {
            // 使用Tilemap的坐标转换
            Vector3 cellOrigin = tilemap.GetCellCenterWorld(new Vector3Int(towerGridPos.x, towerGridPos.y, 0));
            towerWorldPos = new Vector3(cellOrigin.x, cellOrigin.y, 0);
            // towerWorldPos = cellOrigin + tilemap.cellSize / 2f;
        }
        else
        {
            // 备用方案：直接使用格子坐标
            towerWorldPos = new Vector3(towerGridPos.x, towerGridPos.y, 0);
        }

        // 这里应该实例化塔的GameObject，暂时用Debug.Log代替
        // Debug.Log($"在格子 ({localCoord.x}, {localCoord.y}) 生成塔，格子坐标: ({towerGridPos.x}, {towerGridPos.y})，世界坐标: {towerWorldPos}");
        // Debug.Log($"塔数据: {towerData.TowerName}, 攻击力: {towerData.PhysicAttack}");

        // TODO: 实际生成塔的GameObject
        GameObject tower = Instantiate(towerPrefab, towerWorldPos, Quaternion.identity,this.transform);
        tower.GetComponent<Tower>().Initialize(towerData, towerGridPos);
        towers[localCoord] = tower.GetComponent<Tower>();


        return towers[localCoord]; // 暂时返回null，等塔类实现后再修改
    }

    /// <summary>
    /// 检查指定格子是否为空
    /// </summary>
    /// <param name="localCoord">相对于方块的本地坐标</param>
    /// <returns>是否为空</returns>
    public bool IsCellEmpty(Vector2Int localCoord)
    {
        return towers.ContainsKey(localCoord) && towers[localCoord] == null;
    }

    /// <summary>
    /// 获取指定格子的塔
    /// </summary>
    /// <param name="localCoord">相对于方块的本地坐标</param>
    /// <returns>塔实例，如果为空则返回null</returns>
    public Tower GetTower(Vector2Int localCoord)
    {
        return towers.ContainsKey(localCoord) ? towers[localCoord] : null;
    }

    /// <summary>
    /// 移除指定格子的塔
    /// </summary>
    /// <param name="localCoord">相对于方块的本地坐标</param>
    public void RemoveTower(Vector2Int localCoord)
    {
        if (towers.ContainsKey(localCoord) && towers[localCoord] != null)
        {
            Debug.Log($"移除格子 ({localCoord.x}, {localCoord.y}) 的塔");
            // TODO: 销毁塔的GameObject
            // Destroy(towers[localCoord].gameObject);
            towers[localCoord] = null;
        }
    }

    /// <summary>
    /// 获取方块中所有塔的数量
    /// </summary>
    /// <returns>塔的数量</returns>
    public int GetTowerCount()
    {
        int count = 0;
        foreach (var tower in towers.Values)
        {
            if (tower != null) count++;
        }

        return count;
    }

    /// <summary>
    /// 获取方块的总格子数
    /// </summary>
    /// <returns>总格子数</returns>
    public int GetTotalCellCount()
    {
        return config != null ? config.CellCount : 0;
    }
}