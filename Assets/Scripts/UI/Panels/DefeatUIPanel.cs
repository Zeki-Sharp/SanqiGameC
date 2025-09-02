using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 失败阶段UI面板 
/// </summary>
public class DefeatUIPanel : UIPanel
{
    [Header("失败阶段UI组件")]
    [SerializeField] private TextMeshProUGUI defeatTitleText;
    [SerializeField] private TextMeshProUGUI StatisticsText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    protected override void Awake()
    {
        base.Awake();
        
        // 自动获取组件引用
        if (defeatTitleText == null)
            defeatTitleText = GetComponentInChildren<TextMeshProUGUI>();
    }

    protected override void OnShow()
    {
        base.OnShow();
        
        // 绑定按钮事件
        BindButtonEvents();
        
        // 设置失败标题
        UpdateDefeatTitle();
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
        if (restartButton != null)
            restartButton.onClick.RemoveAllListeners();
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveAllListeners();
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
        Debug.Log("DefeatUIPanel: 开始重新开始游戏");
        
        // 先隐藏失败面板
        Hide();
        
        if (GameManager.Instance != null)
        {
            // 使用GameManager的统一重置方法
            GameManager.Instance.ResetGameToInitialState();
        }
        else
        {
            Debug.LogError("GameManager.Instance为空，无法重新开始游戏");
        }
    }

    /// <summary>
    /// 更新失败标题
    /// </summary>
    private void UpdateDefeatTitle()
    {
        if (defeatTitleText == null) return;
        defeatTitleText.text = "游戏失败";
    }
}