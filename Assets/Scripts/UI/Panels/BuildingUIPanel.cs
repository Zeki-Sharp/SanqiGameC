using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    protected override void OnShow()
    {
        base.OnShow();
        
        // 绑定按钮事件
        BindButtonEvents();
        
        // 更新UI显示
        UpdateUI();
        
        // 显示建设相关UI
        ShowBuildingUI();
    }

    protected override void OnHide()
    {
        base.OnHide();
        
        // 解绑按钮事件
        UnbindButtonEvents();
        
        // 隐藏建设相关UI
        HideBuildingUI();
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
    /// 公共方法：更新UI（供外部调用）
    /// </summary>
    public void UpdateUIDisplay()
    {
        if (isVisible)
        {
            UpdateUI();
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