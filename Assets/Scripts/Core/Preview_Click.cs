using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class Preview_Click : MonoBehaviour
{
    [SerializeField] private string previewShowName;
    [SerializeField] private bool hasClick = false;
    [SerializeField] private GameMap gameMap;
    // 放置管理器 - 通过GameManager自动获取
    private BlockPlacementManager BlockPlacementManager => GameManager.Instance?.GetSystem<BlockPlacementManager>();

    [SerializeField] private List<TowerData> previewTowerDatas;
    [SerializeField] private BlockGenerationConfig previewConfig;

    void Start()
    {
        // 引用通过属性自动获取，无需手动查找
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !hasClick)
        {
            var shopSystem = GameManager.Instance?.GetSystem<ShopSystem>();
            var gameMap = GameManager.Instance?.GetSystem<GameMap>();
            
            if (shopSystem == null || gameMap == null)
            {
                Debug.LogError("系统未初始化");
                return;
            }

            GameObject obj = UIInteractionUtility.GetFirstPickGameObject(Input.mousePosition);
            if (obj != null && obj.name == previewShowName)
            {
                previewConfig = PreviewAreaController.lastPreviewConfig;
                previewTowerDatas = PreviewAreaController.lastPreviewTowerDatas;

                if (previewConfig != null && previewTowerDatas != null)
                {
                    // 计算Block的价格（根据塔位数量）
                    int towerSlots = previewConfig.GetCellCount();
                    int cost = gameMap.GetMapData().CalculateBlockCost(towerSlots);

                    // 检查是否有足够的金币
                    if (!shopSystem.CanAfford(cost))
                    {
                        Debug.LogError($"建造失败：金币不足，需要 {cost} 金币");
                        return;
                    }

                    // 先扣除金币
                    shopSystem.SpendMoney(cost);
                    Debug.Log($"扣除金币：{cost}");

                    // 开始放置
                    if (BlockPlacementManager != null)
                    {
                        BlockPlacementManager.StartPlacement(previewConfig, previewTowerDatas);
                        hasClick = true;
                        Debug.Log($"开始放置Block");
                    }
                    else
                    {
                        // 如果放置失败，退还金币
                        shopSystem.AddMoney(cost);
                        Debug.LogError("放置失败：BlockPlacementManager未初始化");
                    }
                }
            }
        }
    }

    public void ResetClickState()
    {
        hasClick = false;
    }

}