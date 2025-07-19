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
    [SerializeField] private BlockPlacementManager blockPlacementManager;

    [SerializeField] private List<TowerData> previewTowerDatas;
    [SerializeField] private BlockGenerationConfig previewConfig;

    void Start()
    {
        gameMap = GameObject.Find("GameMap").GetComponent<GameMap>();
        if (blockPlacementManager == null)
            blockPlacementManager = FindFirstObjectByType<BlockPlacementManager>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !hasClick)
        {
            if (GameManager.Instance.ShopSystem.CanAfford(GameMap.instance.GetMapData().BlockBuildMoney))
            {
                   GameObject obj = UIInteractionUtility.GetFirstPickGameObject(Input.mousePosition);
                            if (obj != null && obj.name == previewShowName)
                            {
                                previewConfig = PreviewAreaController.lastPreviewConfig;
                                previewTowerDatas = PreviewAreaController.lastPreviewTowerDatas;
                
                                if (previewConfig != null && previewTowerDatas != null)
                                {
                                    blockPlacementManager.StartPlacement(previewConfig, previewTowerDatas);
                                    GameManager.Instance.ShopSystem.SpendMoney(GameMap.instance.GetMapData().BlockBuildMoney);
                                    
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