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
        
        if (shopSystem != null && gameMap != null && shopSystem.CanAfford(gameMap.GetMapData().BlockBuildMoney))
            {
                   GameObject obj = UIInteractionUtility.GetFirstPickGameObject(Input.mousePosition);
                            if (obj != null && obj.name == previewShowName)
                            {
                                previewConfig = PreviewAreaController.lastPreviewConfig;
                                previewTowerDatas = PreviewAreaController.lastPreviewTowerDatas;
                
                                if (previewConfig != null && previewTowerDatas != null)
                                {
                                    BlockPlacementManager.StartPlacement(previewConfig, previewTowerDatas);
                                    shopSystem.SpendMoney(gameMap.GetMapData().BlockBuildMoney);
                                    
                                    hasClick = true;
                                    Debug.Log("触发建造：调用 StartPlacement");
                                }
                            }
            }
            else
            {
                Debug.LogError("建造失败：没有足够的钱");
            }
         
        }
    }

    public void ResetClickState()
    {
        hasClick = false;
    }

}