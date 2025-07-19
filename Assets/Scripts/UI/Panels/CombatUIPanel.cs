using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 战斗阶段UI面板
/// </summary>
public class CombatUIPanel : UIPanel
{
    [Header("战斗阶段UI组件")]
    [SerializeField] private TextMeshProUGUI roundInfoText;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private GameObject combatUI;
    [SerializeField] private GameObject pauseButton;

    private float roundStartTime;
    private float roundTimeLimit = 300f; // 5分钟时间限制

    protected override void Awake()
    {
        base.Awake();
        
        // 自动获取组件引用
        if (roundInfoText == null)
            roundInfoText = GetComponentInChildren<TextMeshProUGUI>();
    }

    protected override void OnShow()
    {
        base.OnShow();
        
        // 记录回合开始时间
        roundStartTime = Time.time;
        
        // 更新UI显示
        UpdateUI();
        
        // 显示战斗相关UI
        ShowCombatUI();
    }

    protected override void OnHide()
    {
        base.OnHide();
        
        // 隐藏战斗相关UI
        HideCombatUI();
    }

    protected override void OnReset()
    {
        base.OnReset();
        
        // 重置进度条
        if (progressBar != null)
            progressBar.value = 0f;
    }

    private void Update()
    {
        if (isVisible)
        {
            UpdateUI();
        }
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (GameManager.Instance == null) return;

        // 更新回合信息
        var roundManager = GameManager.Instance.GetSystem<RoundManager>();
        if (roundManager != null && roundInfoText != null)
        {
            roundInfoText.text = $"回合 {roundManager.CurrentRoundNumber}";
        }

        // 更新敌人数量
        UpdateEnemyCount();

        // 更新时间显示
        UpdateTimeDisplay();

        // 更新进度条
        UpdateProgressBar();
    }

    /// <summary>
    /// 更新敌人数量显示
    /// </summary>
    private void UpdateEnemyCount()
    {
        if (enemyCountText == null) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        enemyCountText.text = $"敌人: {enemies.Length}";
    }

    /// <summary>
    /// 更新时间显示
    /// </summary>
    private void UpdateTimeDisplay()
    {
        if (timeText == null) return;

        float elapsedTime = Time.time - roundStartTime;
        float remainingTime = Mathf.Max(0, roundTimeLimit - elapsedTime);
        
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        
        timeText.text = $"时间: {minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// 更新进度条
    /// </summary>
    private void UpdateProgressBar()
    {
        if (progressBar == null) return;

        float elapsedTime = Time.time - roundStartTime;
        float progress = Mathf.Clamp01(elapsedTime / roundTimeLimit);
        progressBar.value = progress;
    }

    /// <summary>
    /// 显示战斗相关UI
    /// </summary>
    private void ShowCombatUI()
    {
        if (combatUI != null)
            combatUI.SetActive(true);
        if (pauseButton != null)
            pauseButton.SetActive(true);
    }

    /// <summary>
    /// 隐藏战斗相关UI
    /// </summary>
    private void HideCombatUI()
    {
        if (combatUI != null)
            combatUI.SetActive(false);
        if (pauseButton != null)
            pauseButton.SetActive(false);
    }

    /// <summary>
    /// 设置回合时间限制
    /// </summary>
    /// <param name="timeLimit">时间限制（秒）</param>
    public void SetRoundTimeLimit(float timeLimit)
    {
        roundTimeLimit = timeLimit;
    }

    /// <summary>
    /// 获取当前回合已用时间
    /// </summary>
    /// <returns>已用时间（秒）</returns>
    public float GetElapsedTime()
    {
        return Time.time - roundStartTime;
    }

    /// <summary>
    /// 获取剩余时间
    /// </summary>
    /// <returns>剩余时间（秒）</returns>
    public float GetRemainingTime()
    {
        return Mathf.Max(0, roundTimeLimit - GetElapsedTime());
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
    /// 公共方法：设置暂停按钮状态
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetPauseButtonInteractable(bool interactable)
    {
        if (pauseButton != null)
        {
            pauseButton.GetComponent<Button>().interactable = interactable;
        }
    }
} 