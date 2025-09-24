using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 通过阶段UI面板 - 显示单关通过界面
/// </summary>
public class PassUIPanel : UIPanel
{
    [Header("通过阶段UI组件")]
    [SerializeField] private TextMeshProUGUI passTitleText;
    [SerializeField] private TextMeshProUGUI roundInfoText;
    [SerializeField] private Button startBuildingButton;

    protected override void Awake()
    {
        base.Awake();
        
        // 自动获取组件引用
        if (passTitleText == null)
            passTitleText = GetComponentInChildren<TextMeshProUGUI>();
    }

        protected override void OnShow()
    {
        base.OnShow();
        
        // 绑定按钮事件
        BindButtonEvents();
        
        // 更新UI显示
        UpdateUI();
    }
    
    protected override void OnHide()
    {
        base.OnHide();
        
        // 解绑按钮事件
        UnbindButtonEvents();
    }

    protected override void OnReset()
    {
        base.OnReset();
        
        // 重置按钮状态
        if (startBuildingButton != null)
            startBuildingButton.interactable = true;
    }

    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtonEvents()
    {
        if (startBuildingButton != null)
        {
            startBuildingButton.onClick.RemoveAllListeners();
            startBuildingButton.onClick.AddListener(OnStartBuildingClicked);
        }
    }

    /// <summary>
    /// 解绑按钮事件
    /// </summary>
    private void UnbindButtonEvents()
    {
        if (startBuildingButton != null)
        {
            startBuildingButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 开始建设按钮点击事件
    /// </summary>
    private void OnStartBuildingClicked()
    {
        // 切换到建设阶段，准备开始下一个Round
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SwitchGamePhase(GamePhase.BuildingPhase);
        }
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (GameManager.Instance == null) return;

        // 更新通过标题
        UpdatePassTitle();

        // 更新Round信息
        UpdateRoundInfo();
    }

    /// <summary>
    /// 更新通过标题
    /// </summary>
    private void UpdatePassTitle()
    {
        if (passTitleText == null) return;

        var roundManager = GameManager.Instance.GetSystem<RoundManager>();
        if (roundManager != null)
        {
            int currentRound = roundManager.CurrentRoundNumber;
            passTitleText.text = $"回合 {currentRound} 通过！";
        }
        else
        {
            // 如果没有RoundManager，显示默认文本
            passTitleText.text = "通关成功！";
        }
    }

    /// <summary>
    /// 更新Round信息
    /// </summary>
    private void UpdateRoundInfo()
    {
        if (roundInfoText == null) return;

        if (GameManager.Instance == null) return;

        var roundManager = GameManager.Instance.GetSystem<RoundManager>();
        var shopSystem = GameManager.Instance.GetSystem<ShopSystem>();

        string info = "";
        
        if (roundManager != null)
        {
            info += $"完成回合: {roundManager.CurrentRoundNumber}\n";
        }
        
        if (shopSystem != null)
        {
            info += $"当前金钱: {shopSystem.Money}\n";
        }

        // 添加游戏时间统计
        float gameTime = Time.time;
        int minutes = Mathf.FloorToInt(gameTime / 60);
        int seconds = Mathf.FloorToInt(gameTime % 60);
        info += $"游戏时间: {minutes:00}:{seconds:00}";

        roundInfoText.text = info;
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
    /// 公共方法：设置开始建设按钮状态
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetStartBuildingButtonInteractable(bool interactable)
    {
        if (startBuildingButton != null)
        {
            startBuildingButton.interactable = interactable;
        }
    }
} 