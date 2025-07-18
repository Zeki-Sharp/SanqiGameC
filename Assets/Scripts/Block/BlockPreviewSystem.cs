using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 负责建造预览塔的生成、刷新、销毁和高亮显示。
/// 只管理预览对象的生命周期和表现，不涉及建造落地。
/// </summary>
public class BlockPreviewSystem : MonoBehaviour
{
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private Color previewColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color canPlaceColor = Color.green;
    [SerializeField] private Color cannotPlaceColor = Color.red;
    [SerializeField] private Color canReplaceColor = Color.yellow;

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
            SetPreviewColor(canPlaceColor);
        }
        else
        {
            SetPreviewColor(cannotPlaceColor);
        }
        for (int i = 0; i < currentBlockConfig.Coordinates.Length; i++)
        {
            Vector3Int offset = currentBlockConfig.Coordinates[i];
            Vector3Int cellPos = baseGridPos + new Vector3Int(offset.x, offset.y, 0);
            Vector3 worldPos = TileMapUtility.CellToWorldPosition(gameMap.GetTilemap(), cellPos);
            previewTowers[i].transform.position = worldPos;
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
            GameObject towerObj = Instantiate(towerPrefab, transform);
            towerObj.name = $"PreviewTower_{i}";
            towerObj.tag = "PreviewTower";
            Tower towerComponent = towerObj.GetComponent<Tower>();
            if (towerComponent != null && i < currentTowerDatas.Count)
            {
                towerComponent.Initialize(currentTowerDatas[i], currentBlockConfig.Coordinates[i]);
            }
            SpriteRenderer sr = towerObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = previewColor;
                sr.sortingOrder = 1000;
            }
            previewTowers.Add(towerObj);
        }
    }
} 