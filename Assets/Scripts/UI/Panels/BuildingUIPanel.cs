using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 建设阶段UI面板
/// </summary>
public class BuildingUIPanel : UIPanel
{
    [Header("建设阶段UI组件")]
    [SerializeField] private Button startCombatButton;
    [SerializeField] private Button refreshItemsButton;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI roundInfoText;
    [SerializeField] private GameObject buildingUI;
    [SerializeField] private GameObject shopUI;

    [SerializeField] private HoverTextChanger buyButton;
    [SerializeField] private HoverTextChanger refreshButton;

    [Header("敌人预览")]
    [SerializeField] private GameObject enemyPreviewContainer;
    [SerializeField] private GameObject enemyPreviewCardPrefab;
    [SerializeField] private Transform enemyPreviewCardsParent;
    [SerializeField] private TextMeshProUGUI enemyPreviewTitleText;

    // 敌人预览卡片列表
    private List<GameObject> enemyPreviewCards = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();
        
        // 自动获取组件引用
        if (startCombatButton == null)
            startCombatButton = GetComponentInChildren<Button>();
        if (moneyText == null)
            moneyText = GetComponentInChildren<TextMeshProUGUI>();

     
        Debug.Log($"BuildingUIPanel Awake - startCombatButton: {startCombatButton}");
    }

    private void Start()
    {
       
    }

    protected override void OnShow()
    {
        base.OnShow();
        
        // 绑定按钮事件
        BindButtonEvents();
        
        // 更新UI显示
        UpdateUI();
        
        // 显示建设相关UI
        ShowBuildingUI();
        
        // 生成敌人预览
        GenerateEnemyPreview();
    }

    protected override void OnHide()
    {
        base.OnHide();
        
        // 解绑按钮事件
        UnbindButtonEvents();
        
        // 隐藏建设相关UI
        HideBuildingUI();
        
        // 清理敌人预览
        ClearEnemyPreview();
    }

    protected override void OnReset()
    {
        base.OnReset();
        
        // 重置按钮状态
        if (startCombatButton != null)
            startCombatButton.interactable = true;
        if (refreshItemsButton != null)
            refreshItemsButton.interactable = true;
    }

    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtonEvents()
    {
        Debug.Log($"绑定按钮事件 - startCombatButton: {startCombatButton}");
        
        if (startCombatButton != null)
        {
            startCombatButton.onClick.RemoveAllListeners();
            startCombatButton.onClick.AddListener(OnStartCombatClicked);
            Debug.Log("开始战斗按钮事件已绑定");
        }
        else
        {
            Debug.LogWarning("startCombatButton为null，无法绑定事件");
        }

        if (refreshItemsButton != null)
        {
            refreshItemsButton.onClick.RemoveAllListeners();
            refreshItemsButton.onClick.AddListener(OnRefreshItemsClicked);
            Debug.Log("刷新物品按钮事件已绑定");
        }
        else
        {
            Debug.LogWarning("refreshItemsButton为null，无法绑定事件");
        }
    }

    /// <summary>
    /// 解绑按钮事件
    /// </summary>
    private void UnbindButtonEvents()
    {
        if (startCombatButton != null)
            startCombatButton.onClick.RemoveAllListeners();
        if (refreshItemsButton != null)
            refreshItemsButton.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// 开始战斗按钮点击事件
    /// </summary>
    private void OnStartCombatClicked()
    {
        Debug.Log("开始战斗按钮被点击");
        
        // 通过GameManager切换到战斗阶段
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SwitchGamePhase(GamePhase.CombatPhase);
        }
        else
        {
            Debug.LogError("GameManager未找到，无法切换游戏阶段");
        }
    }

    /// <summary>
    /// 刷新物品按钮点击事件
    /// </summary>
    private void OnRefreshItemsClicked()
    {
        Debug.Log("刷新物品按钮被点击");
        
        // 通过GameManager获取ItemManage并刷新物品
        if (GameManager.Instance != null)
        {
            var itemManage = GameManager.Instance.GetSystem<ItemManage>();
            if (itemManage != null)
            {
                itemManage.ShowItem();
            }
        }
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (GameManager.Instance == null) return;
        if (buyButton == null)
            buyButton = GameObject.Find("Buy_Button").GetComponent<HoverTextChanger>();
        buyButton.Initialize("", GameManager.Instance.GetSystem<GameMap>().GetMapData().BlockBuildMoney.ToString(), false);
        
        if (refreshButton == null)
            refreshButton = GameObject.Find("Reduce_Button").GetComponent<HoverTextChanger>();
        refreshButton.Initialize("刷新", GameManager.Instance.GetSystem<GameMap>().GetMapData().BlockBuildMoney.ToString(), true);
        // 更新金钱显示
        var shopSystem = GameManager.Instance.GetSystem<ShopSystem>();
        if (shopSystem != null && moneyText != null)
        {
            moneyText.text = $"金钱: {shopSystem.Money}";
        }

        // 更新回合信息
        var roundManager = GameManager.Instance.GetSystem<RoundManager>();
        if (roundManager != null && roundInfoText != null)
        {
            roundInfoText.text = $"回合: {roundManager.CurrentRoundNumber}";
        }
    }

    /// <summary>
    /// 显示建设相关UI
    /// </summary>
    private void ShowBuildingUI()
    {
        if (buildingUI != null)
            buildingUI.SetActive(true);
        if (shopUI != null)
            shopUI.SetActive(true);
    }

    /// <summary>
    /// 隐藏建设相关UI
    /// </summary>
    private void HideBuildingUI()
    {
        if (buildingUI != null)
            buildingUI.SetActive(false);
        if (shopUI != null)
            shopUI.SetActive(false);
    }

    /// <summary>
    /// 生成敌人预览
    /// </summary>
    private void GenerateEnemyPreview()
    {
        if (enemyPreviewCardPrefab == null || enemyPreviewCardsParent == null)
        {
            Debug.LogWarning("敌人预览预制体或父容器未设置");
            return;
        }

        // 清理现有预览
        ClearEnemyPreview();

        // 获取当前关卡敌人信息
        var currentRoundEnemies = GetCurrentRoundEnemies();
        if (currentRoundEnemies == null || currentRoundEnemies.Count == 0)
        {
            Debug.Log("没有当前关卡敌人信息");
            return;
        }

        // 生成敌人预览卡片
        foreach (var enemyInfo in currentRoundEnemies)
        {
            if (enemyInfo.enemyData != null)
            {
                CreateEnemyPreviewCard(enemyInfo);
            }
        }

        // 更新预览标题
        UpdateEnemyPreviewTitle();
    }

    /// <summary>
    /// 创建单个敌人预览卡片
    /// </summary>
    private void CreateEnemyPreviewCard(EnemySpawnInfo enemyInfo)
    {
        GameObject cardObject = Instantiate(enemyPreviewCardPrefab, enemyPreviewCardsParent);
        enemyPreviewCards.Add(cardObject);

        // 设置敌人图像
        var enemyImage = cardObject.transform.Find("EnemyImage")?.GetComponent<Image>();
        if (enemyImage != null && enemyInfo.enemyData.EnemySprite != null)
        {
            enemyImage.sprite = enemyInfo.enemyData.EnemySprite;
            enemyImage.preserveAspect = true;
            enemyImage.type = Image.Type.Simple;
        }

        // 设置敌人名称
        var enemyNameText = cardObject.transform.Find("EnemyName")?.GetComponent<TextMeshProUGUI>();
        if (enemyNameText != null)
        {
            enemyNameText.text = enemyInfo.enemyData.EnemyName;
        }

        // 设置敌人描述
        var enemyDescText = cardObject.transform.Find("EnemyDesc")?.GetComponent<TextMeshProUGUI>();
        if (enemyDescText != null)
        {
            enemyDescText.text = enemyInfo.enemyData.Description;
        }

        // 设置敌人数目
        var enemyAmountText = cardObject.transform.Find("EnemyAmount")?.GetComponent<TextMeshProUGUI>();
        if (enemyAmountText != null)
        {
            enemyAmountText.text = $"x{enemyInfo.count}";
        }
    }

    /// <summary>
    /// 获取当前关卡敌人信息
    /// </summary>
    private List<EnemySpawnInfo> GetCurrentRoundEnemies()
    {
        if (GameManager.Instance == null) return null;

        var roundManager = GameManager.Instance.GetSystem<RoundManager>();
        if (roundManager == null) return null;

        // 获取当前关卡配置
        int currentRoundNumber = roundManager.CurrentRoundNumber;
        
        // 通过反射获取私有字段roundConfigs
        var roundConfigsField = roundManager.GetType().GetField("roundConfigs", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (roundConfigsField != null)
        {
            var roundConfigs = roundConfigsField.GetValue(roundManager) as List<RoundConfig>;
            if (roundConfigs != null && currentRoundNumber <= roundConfigs.Count)
            {
                var currentRoundConfig = roundConfigs[currentRoundNumber - 1];
                if (currentRoundConfig != null && currentRoundConfig.waves != null && currentRoundConfig.waves.Count > 0)
                {
                    // 合并所有Wave中的敌人信息
                    List<EnemySpawnInfo> allEnemies = new List<EnemySpawnInfo>();
                    foreach (var wave in currentRoundConfig.waves)
                    {
                        if (wave.enemies != null)
                        {
                            allEnemies.AddRange(wave.enemies);
                        }
                    }
                    return allEnemies;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 更新敌人预览标题
    /// </summary>
    private void UpdateEnemyPreviewTitle()
    {
        if (enemyPreviewTitleText != null)
        {
            var roundManager = GameManager.Instance?.GetSystem<RoundManager>();
            if (roundManager != null)
            {
                int currentRoundNumber = roundManager.CurrentRoundNumber;
                enemyPreviewTitleText.text = $"第{currentRoundNumber}关敌人预览";
            }
            else
            {
                enemyPreviewTitleText.text = "当前关卡敌人预览";
            }
        }
    }

    /// <summary>
    /// 清理敌人预览
    /// </summary>
    private void ClearEnemyPreview()
    {
        foreach (var card in enemyPreviewCards)
        {
            if (card != null)
            {
                DestroyImmediate(card);
            }
        }
        enemyPreviewCards.Clear();
    }

    /// <summary>
    /// 公共方法：更新UI（供外部调用）
    /// </summary>
    public void UpdateUIDisplay()
    {
        if (isVisible)
        {
            UpdateUI();
            GenerateEnemyPreview(); // 同时更新敌人预览
        }
    }

    /// <summary>
    /// 公共方法：设置开始战斗按钮状态
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetStartCombatButtonInteractable(bool interactable)
    {
        if (startCombatButton != null)
        {
            startCombatButton.interactable = interactable;
        }
    }
} 