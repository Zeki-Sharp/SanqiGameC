using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 负责建造预览塔的生成、刷新、销毁和高亮显示。
/// 只管理预览对象的生命周期和表现，不涉及建造落地。
/// </summary>
public class BlockPreviewSystem : MonoBehaviour
{
    // 操作类型枚举
    public enum TowerActionType
    {
        None,       // 无操作（空地新建）
        Upgrade,    // 升级（同类型塔）
        Replace     // 替换（不同类型塔）
    }

    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private Color previewColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color canPlaceColor = Color.green;
    [SerializeField] private Color cannotPlaceColor = Color.red;
    [SerializeField] private Color canReplaceColor = Color.yellow;
    [SerializeField] private Color canUpgradeColor = Color.blue; // 新增：升级颜色

    [SerializeField] private LayerMask TowerLayerMask = 1 << 8; // 修正：统一使用第8层

    private List<GameObject> previewTowers = new List<GameObject>();
    private BlockGenerationConfig currentBlockConfig;
    private List<TowerData> currentTowerDatas;
    private GameMap gameMap;

    public void Init(GameMap map, GameObject towerPrefabRef)
    {
        gameMap = map;
        towerPrefab = towerPrefabRef;
    }

    public void ShowPreview(BlockGenerationConfig config, List<TowerData> towerDatas)
    {
        currentBlockConfig = config;
        currentTowerDatas = towerDatas;
        GeneratePreviewTowers();
    }

    /// <summary>
    /// 更新预览位置
    /// </summary>
    /// <param name="baseGridPos">基础cell位置</param>
    public void UpdatePreview(Vector3Int baseGridPos)
    {
        if (currentBlockConfig == null || currentTowerDatas == null) return;
        if (previewTowers.Count != currentBlockConfig.Coordinates.Length)
        {
            ClearPreview();
            GeneratePreviewTowers();
        }
        
        // 判断是否可放置
        if (gameMap != null && gameMap.CanPlaceBlock(baseGridPos, currentBlockConfig))
        {
            // Block可建造，检查每个塔的升级/替换状态
            UpdateIndividualTowerColors(baseGridPos);
        }
        else
        {
            // Block不可建造，所有塔显示红色
            SetPreviewColor(cannotPlaceColor);
        }
        
        // 更新预览塔位置
        for (int i = 0; i < currentBlockConfig.Coordinates.Length; i++)
        {
            Vector3Int offset = currentBlockConfig.Coordinates[i];
            Vector3Int cellPos = baseGridPos + offset;
            Vector3 worldPos = CoordinateUtility.CellToWorldPosition(gameMap.GetTilemap(), cellPos);
            previewTowers[i].transform.position = worldPos;
            previewTowers[i].gameObject.tag = "PreviewTower";
        }
    }

    /// <summary>
    /// 更新单个塔的颜色（仅在block可建造时调用）
    /// </summary>
    private void UpdateIndividualTowerColors(Vector3Int baseGridPos)
    {
        // 检查数组长度匹配
        if (currentTowerDatas.Count != currentBlockConfig.Coordinates.Length)
        {
            Debug.LogError($"塔数据数量({currentTowerDatas.Count})与坐标数量({currentBlockConfig.Coordinates.Length})不匹配！");
            return;
        }
        
        for (int i = 0; i < currentBlockConfig.Coordinates.Length; i++)
        {
            Vector3Int offset = currentBlockConfig.Coordinates[i];
            Vector3Int cellPos = baseGridPos + offset;
            
            // 检查索引有效性
            if (i >= currentTowerDatas.Count)
            {
                Debug.LogError($"索引 {i} 超出塔数据数组范围！");
                continue;
            }
            
            // Debug.Log($"处理预览塔 {i}: 位置={cellPos}, 塔类型={currentTowerDatas[i]?.TowerName ?? "null"}");
            
            // 检测该位置的操作类型
            TowerActionType actionType = DetectTowerAction(cellPos, currentTowerDatas[i]);
            
            // 根据操作类型设置颜色
            Color towerColor = GetColorForActionType(actionType);
            
            // 设置单个塔的颜色
            SetIndividualTowerColor(i, towerColor);
        }
    }

    /// <summary>
    /// 检测单个位置的塔操作类型
    /// </summary>
    private TowerActionType DetectTowerAction(Vector3Int cellPos, TowerData newTowerData)
    {
        if (gameMap == null || newTowerData == null) return TowerActionType.None;
        
        Vector3 worldPos = CoordinateUtility.CellToWorldPosition(gameMap.GetTilemap(), cellPos);
        
        // 使用更精确的点检测，避免检测到附近位置的塔
        Collider2D[] allColliders = Physics2D.OverlapPointAll(worldPos);
        
        Debug.Log($"=== 检测位置 {cellPos} (世界坐标: {worldPos}) ===");
        Debug.Log($"找到 {allColliders.Length} 个碰撞体");
        
        // 遍历所有碰撞体，详细分析
        foreach (var collider in allColliders)
        {
            if (collider == null)
            {
                Debug.Log("跳过空碰撞体");
                continue;
            }
            
            // Debug.Log($"碰撞体: {collider.name}, Tag: {collider.tag}, Layer: {collider.gameObject.layer}");
            if (this.gameObject == collider.gameObject)
            {
                Debug.Log($"跳过自身: {collider.name}");
                continue;
            }
            // 跳过预览塔
            if (collider.CompareTag("PreviewTower"))
            {
                Debug.Log($"跳过预览塔: {collider.name}");
                continue;
            }
            
            // 检查是否在正确的层级
            if (((1 << collider.gameObject.layer) & TowerLayerMask) == 0)
            {
                Debug.Log($"跳过非塔层级物体: {collider.name} (层级: {collider.gameObject.layer})");
                continue;
            }
            
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
            
            // 验证塔的位置是否真的在这个cell
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
                Debug.Log($"检测到升级: {newTowerData.TowerName}");
                return TowerActionType.Upgrade;
            }
            else
            {
                Debug.Log($"检测到替换: {existingTower.TowerData.TowerName} -> {newTowerData.TowerName}");
                return TowerActionType.Replace;
            }
        }
        
        // Debug.Log($"位置 {cellPos} 无操作（空地新建）");
        return TowerActionType.None;
    }

    /// <summary>
    /// 根据操作类型获取颜色
    /// </summary>
    private Color GetColorForActionType(TowerActionType actionType)
    {
        switch (actionType)
        {
            case TowerActionType.Upgrade:
                return canUpgradeColor;
            case TowerActionType.Replace:
                return canReplaceColor;
            case TowerActionType.None:
            default:
                return canPlaceColor; // 空地新建保持绿色
        }
    }

    /// <summary>
    /// 设置单个预览塔的颜色
    /// </summary>
    private void SetIndividualTowerColor(int towerIndex, Color color)
    {
        if (towerIndex >= 0 && towerIndex < previewTowers.Count)
        {
            GameObject towerObj = previewTowers[towerIndex];
            if (towerObj != null)
            {
                SpriteRenderer sr = towerObj.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = color;
                }
            }
        }
    }

    public void SetPreviewColor(Color color)
    {
        foreach (var obj in previewTowers)
        {
            if (obj != null)
            {
                SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = color;
                }
            }
        }
    }

    public void ClearPreview()
    {
        foreach (var obj in previewTowers)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        previewTowers.Clear();
    }

    private void GeneratePreviewTowers()
    {
        if (towerPrefab == null || currentBlockConfig == null || currentTowerDatas == null) return;
        for (int i = 0; i < currentBlockConfig.Coordinates.Length; i++)
        {
            var towerObj = TowerBuildUtility.GenerateTower(
                this.transform,
                towerPrefab,
                currentBlockConfig.Coordinates[i],
                gameMap != null ? gameMap.GetTilemap() : null,
                i < currentTowerDatas.Count ? currentTowerDatas[i] : null,
                true, // 预览模式
                previewColor,
                false
            );
            if (towerObj != null)
            {
                towerObj.gameObject.name = $"PreviewTower_{i}";
                towerObj.gameObject.tag = "PreviewTower";
                previewTowers.Add(towerObj.gameObject);
            }
        }
    }
} 