using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class Preview_Click : MonoBehaviour
{
    [SerializeField]private string previewShowName;
    [SerializeField]private GameObject previewArea;
    [SerializeField]private bool hasClick = false;
    [SerializeField]private GameMap gameMap;
    
    private GameObject previewBlockObj; // 预览塔组对象
    private Block previewBlock;
    private List<TowerData> previewTowerDatas;
    private BlockGenerationConfig previewConfig;
    private Vector2Int[] previewRelativeShape; // 预览塔组的相对形状
    private List<GameObject> previewTowers = new List<GameObject>();
    
    void Start()
    {
        gameMap = GameObject.Find("GameMap").GetComponent<GameMap>();
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
                    if (previewConfig != null && previewTowerDatas != null && previewTowerDatas.Count > 0 && CreatePrefab.lastPreviewAdjustedPositions != null)
                    {
                        // 1. 计算adjustedPositions的左下角
                        Vector2Int[] adjusted = CreatePrefab.lastPreviewAdjustedPositions;
                        int minX = int.MaxValue, minY = int.MaxValue;
                        foreach (var pos in adjusted) {
                            if (pos.x < minX) minX = pos.x;
                            if (pos.y < minY) minY = pos.y;
                        }
                        Vector2Int anchor = new Vector2Int(minX, minY);
                        // 2. 生成“相对形状”坐标
                        previewRelativeShape = new Vector2Int[adjusted.Length];
                        for (int i = 0; i < adjusted.Length; i++) {
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
                        for (int i = 0; i < previewRelativeShape.Length; i++) {
                            GameObject towerPrefab = Resources.Load<GameObject>("Prefab/Tower/Tower");
                            GameObject towerObj = Instantiate(towerPrefab, previewBlockObj.transform);
                            // 设置为半透明并提升渲染层级
                            var sr = towerObj.GetComponent<SpriteRenderer>();
                            if (sr != null) {
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
                Vector2Int gridPos = gameMap.WorldToGridPosition(mouseWorldPos);
                // 让预览塔组的左下角锚点对齐到鼠标所在格子
                for (int i = 0; i < previewRelativeShape.Length; i++) {
                    Vector2Int towerGrid = gridPos + previewRelativeShape[i];
                    Vector3 worldPos = gameMap.GridToWorldPosition(towerGrid);
                    previewTowers[i].transform.position = worldPos;
                }
            }
        }
    }
}