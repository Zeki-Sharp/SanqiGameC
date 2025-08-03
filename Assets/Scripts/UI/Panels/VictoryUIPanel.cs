using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 胜利阶段UI面板
/// </summary>
public class VictoryUIPanel : UIPanel
{
    [Header("胜利阶段UI组件")]
    [SerializeField] private TextMeshProUGUI victoryTitleText;
    [SerializeField] private TextMeshProUGUI statisticsText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    protected override void Awake()
    {
        base.Awake();
        
        // 自动获取组件引用
        if (victoryTitleText == null)
            victoryTitleText = GetComponentInChildren<TextMeshProUGUI>();
        
        Debug.Log("VictoryUIPanel Awake - 初始化完成");
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
        if (nextLevelButton != null)
            nextLevelButton.interactable = true;
        if (restartButton != null)
            restartButton.interactable = true;
        if (mainMenuButton != null)
            mainMenuButton.interactable = true;
    }

    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtonEvents()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    /// <summary>
    /// 解绑按钮事件
    /// </summary>
    private void UnbindButtonEvents()
    {
        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveAllListeners();
        if (restartButton != null)
            restartButton.onClick.RemoveAllListeners();
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// 下一关按钮点击事件（已废弃，最终胜利不需要下一关）
    /// </summary>
    private void OnNextLevelClicked()
    {
        Debug.Log("下一关按钮被点击（最终胜利模式）");
        
        // 最终胜利时，下一关按钮应该被隐藏
        // 这里保留以防万一，但实际不应该被调用
        RestartGame();
    }

    /// <summary>
    /// 重新开始按钮点击事件
    /// </summary>
    private void OnRestartClicked()
    {
        Debug.Log("重新开始按钮被点击");
        
        RestartGame();
    }

    /// <summary>
    /// 主菜单按钮点击事件
    /// </summary>
    private void OnMainMenuClicked()
    {
        Debug.Log("主菜单按钮被点击");
        
        // TODO: 实现返回主菜单逻辑
        // 暂时重新开始游戏
        RestartGame();
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    private void RestartGame()
    {
        if (GameManager.Instance != null)
        {
            // 重置所有管理器
            var roundManager = GameManager.Instance.GetSystem<RoundManager>();
            if (roundManager != null)
                roundManager.Reset();

            var victoryChecker = GameManager.Instance.GetSystem<VictoryConditionChecker>();
            if (victoryChecker != null)
                victoryChecker.Reset();

            // 切换回建设阶段
            GameManager.Instance.SwitchGamePhase(GamePhase.BuildingPhase);
        }
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (GameManager.Instance == null) return;

        // 更新胜利标题
        UpdateVictoryTitle();

        // 更新统计数据
        UpdateStatistics();
    }

    /// <summary>
    /// 更新胜利标题
    /// </summary>
    private void UpdateVictoryTitle()
    {
        if (victoryTitleText == null) return;

        // 最终胜利显示固定文本
        victoryTitleText.text = "最终胜利！";
    }

    /// <summary>
    /// 更新统计数据
    /// </summary>
    private void UpdateStatistics()
    {
        if (statisticsText == null) return;

        if (GameManager.Instance == null) return;

        var roundManager = GameManager.Instance.GetSystem<RoundManager>();
        var shopSystem = GameManager.Instance.GetSystem<ShopSystem>();

        string stats = "";
        
        if (roundManager != null)
        {
            stats += $"完成回合: {roundManager.CurrentRoundNumber}\n";
        }
        
        if (shopSystem != null)
        {
            stats += $"当前金钱: {shopSystem.Money}\n";
        }

        // 添加游戏时间统计
        float gameTime = Time.time;
        int minutes = Mathf.FloorToInt(gameTime / 60);
        int seconds = Mathf.FloorToInt(gameTime % 60);
        stats += $"游戏时间: {minutes:00}:{seconds:00}";

        statisticsText.text = stats;
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
    /// 公共方法：设置下一关按钮状态
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetNextLevelButtonInteractable(bool interactable)
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.interactable = interactable;
        }
    }

    /// <summary>
    /// 公共方法：显示最终胜利界面
    /// </summary>
    public void ShowFinalVictory()
    {
        if (victoryTitleText != null)
        {
            victoryTitleText.text = "最终胜利！";
        }
        
        // 隐藏下一关按钮，因为已经通关
        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(false);
        }
        
        // 确保重新开始和主菜单按钮可见
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(true);
        }
    }
} 