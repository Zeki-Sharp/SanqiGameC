using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class Preview_Click : MonoBehaviour
{
    [SerializeField] private string previewShowName;
    [SerializeField] private GameObject previewArea;
    [SerializeField] private bool hasClick = false;
    [SerializeField] private GameMap gameMap;
    [SerializeField] private BlockPlacementManager blockPlacementManager;

    [SerializeField] private GameObject previewBlockObj; // 预览塔组对象
    [SerializeField] private Block previewBlock;
    [SerializeField] private List<TowerData> previewTowerDatas;
    [SerializeField] private BlockGenerationConfig previewConfig;
    [SerializeField] private Vector3Int[] previewRelativeShape; // 预览塔组的相对形状
    [SerializeField] private List<GameObject> previewTowers = new List<GameObject>();

    void Start()
    {
        gameMap = GameObject.Find("GameMap").GetComponent<GameMap>();
        if (blockPlacementManager == null)
            blockPlacementManager = FindFirstObjectByType<BlockPlacementManager>();
    }

    private void Update()
    {
        //点击检测
        if (Input.GetMouseButtonDown(0) && !hasClick)
        {
            Debug.Log("点击了");

            GameObject obj = BaseUtility.GetFirstPickGameObject(Input.mousePosition);
            if (obj != null)
            {
                if (obj.name == previewShowName && obj.TryGetComponent(out RawImageColorController rawImage))
                {
                    rawImage.OnPointerClick(new PointerEventData(EventSystem.current));
                    hasClick = true;

                    // 生成主地图上的预览塔组
                    if (previewBlockObj != null)
                        Destroy(previewBlockObj);
                    // 获取showarea当前塔组配置
                    previewConfig = CreatePrefab.lastPreviewConfig;
                    previewTowerDatas = CreatePrefab.lastPreviewTowerDatas;
                    if (previewConfig != null && previewTowerDatas != null && previewTowerDatas.Count > 0 &&
                        CreatePrefab.lastPreviewAdjustedPositions != null)
                    {
                        // 1. 计算adjustedPositions的左下角
                        Vector3Int[] adjusted = CreatePrefab.lastPreviewAdjustedPositions;
                        int minX = int.MaxValue, minY = int.MaxValue;
                        foreach (var pos in adjusted)
                        {
                            if (pos.x < minX) minX = pos.x;
                            if (pos.y < minY) minY = pos.y;
                        }

                        Vector3Int anchor = new Vector3Int(minX, minY, 0);
                        // 2. 生成“相对形状”坐标
                        previewRelativeShape = new Vector3Int[adjusted.Length];
                        for (int i = 0; i < adjusted.Length; i++)
                        {
                            previewRelativeShape[i] = adjusted[i] - anchor;
                        }

                        // 3. 生成预览塔组（只生成一次塔对象，后续只移动位置）
                        GameObject blockPrefab = Resources.Load<GameObject>("Prefab/Block/Block");
                        previewBlockObj = Instantiate(blockPrefab, null);
                        previewBlock = previewBlockObj.GetComponent<Block>();
                        previewBlock.Init(previewConfig);
                        // 清理旧的预览塔对象
                        previewTowers.Clear();
                        // 生成塔对象
                        for (int i = 0; i < previewRelativeShape.Length; i++)
                        {
                            GameObject towerPrefab = Resources.Load<GameObject>("Prefab/Tower/Tower");
                            GameObject towerObj = Instantiate(towerPrefab, previewBlockObj.transform);
                            // 设置为半透明并提升渲染层级
                            var sr = towerObj.GetComponent<SpriteRenderer>();
                            if (sr != null)
                            {
                                Color c = sr.color;
                                c.a = 0.5f;
                                sr.color = c;
                                sr.sortingOrder = 1000;
                            }

                            foreach (var r in towerObj.GetComponentsInChildren<Renderer>())
                            {
                                r.sortingOrder = 1000;
                            }

                            previewTowers.Add(towerObj);
                        }
                    }
                }
            }
        }

        // 预览塔组跟随鼠标移动
        if (hasClick && previewBlockObj != null && previewTowers.Count == previewRelativeShape.Length)
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePos);
            mouseWorldPos.z = 0;
            if (gameMap != null)
            {
                Vector3Int gridPos = gameMap.WorldToGridPosition(mouseWorldPos);
                bool isValid = true;
                Tilemap tilemap = gameMap.GetTilemap();
                // 收集所有预览塔的目标格子坐标
                List<Vector3Int> previewCells = new List<Vector3Int>();
                for (int i = 0; i < previewRelativeShape.Length; i++)
                {
                    Vector3Int towerGrid =
                        gridPos + new Vector3Int(previewRelativeShape[i].x, previewRelativeShape[i].y, 0);
                    previewCells.Add(towerGrid);
                    Vector3 worldPos = tilemap.GetCellCenterWorld(towerGrid);
                    previewTowers[i].transform.position = worldPos;
                }

                // 判定是否合法
                foreach (var cell in previewCells)
                {
                    if (!tilemap.HasTile(cell) || gameMap.IsCellOccupied(cell))
                    {
                        isValid = false;
                        break;
                    }
                }

                // 变色
                Color previewColor = isValid ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
                foreach (var tower in previewTowers)
                {
                    SpriteRenderer[] renderers = tower.GetComponentsInChildren<SpriteRenderer>(true);
                    if (renderers != null && renderers.Length > 0)
                    {
                        foreach (var sr in renderers)
                        {
                            sr.color = previewColor;
                        }
                    }
                }

                // 建造逻辑：合法且点击左键
                if (isValid && Input.GetMouseButtonDown(0))
                {
                    // 调用BlockPlacementManager通用建造方法
                    if (blockPlacementManager != null)
                    {
                        blockPlacementManager.PlaceTowerGroupAtPositions(previewCells, previewConfig, previewTowerDatas,
                            gameMap.GetTowerArea());
                    }
                    if (previewBlockObj != null)
                        Destroy(previewBlockObj);
                    hasClick = false; // 禁止连续建造
                    // 刷新showarea
                    CreatePrefab.instance.RefreshShowArea();
                }
            }
        }
    }
}