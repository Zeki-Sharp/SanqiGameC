using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 主塔血量UI面板 - 在建造阶段和战斗阶段都一直存在
/// 包含中心塔图像和血条显示
/// </summary>
public class MainTowerHealthPanel : UIPanel
{
    [Header("主塔图像")]
    [SerializeField] private Image centerTowerImage;
    [SerializeField] private Sprite centerTowerSprite;
    
    [Header("血条组件")]
    [SerializeField] private Image healthBarFill; // 血条填充图像
    [SerializeField] private TextMeshProUGUI healthText; // 当前血量文本
    [SerializeField] private TextMeshProUGUI maxHealthText; // 最大血量文本
    
    [Header("血条颜色设置")]
    [SerializeField] private Color highHealthColor = Color.green;
    [SerializeField] private Color mediumHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;
    
    [Header("UI设置")]
    [SerializeField] private float updateInterval = 0.1f; // 血条更新间隔
    
    // 私有变量
    private GameObject centerTower;
    private DamageTaker centerTowerDamageTaker;
    private float lastUpdateTime;
    private bool isInitialized = false;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 确保面板一开始就是隐藏的
        gameObject.SetActive(false);
        
        // 自动获取组件引用（如果Inspector中没有分配）
        if (centerTowerImage == null)
            centerTowerImage = GetComponentInChildren<Image>();
        
        if (healthText == null)
        {
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>();
            if (allTexts.Length > 0)
                healthText = allTexts[0];
        }
        
        if (maxHealthText == null)
        {
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>();
            if (allTexts.Length > 1)
                maxHealthText = allTexts[1];
        }
    }
    
    public override void Show()
    {
        Debug.Log("MainTowerHealthPanel.Show() 被调用");
        
        // 先激活GameObject
        gameObject.SetActive(true);
        
        // 然后调用基类的Show方法
        base.Show();
    }
    
    protected override void OnShow()
    {
        base.OnShow();
        
        // 显示时进行初始化，确保中心塔已经生成
        StartCoroutine(DelayedInitializeMainTowerHealthUI());
    }
    
    /// <summary>
    /// 延迟初始化主塔血量UI
    /// </summary>
    private System.Collections.IEnumerator DelayedInitializeMainTowerHealthUI()
    {
        // 等待一帧，确保所有组件的Awake都执行完成
        yield return null;
        
        // 初始化主塔血量UI
        InitializeMainTowerHealthUI();
        
        // 开始定期更新
        InvokeRepeating(nameof(UpdateHealthDisplay), 0f, updateInterval);
    }
    
    protected override void OnHide()
    {
        base.OnHide();
        
        // 停止定期更新
        CancelInvoke(nameof(UpdateHealthDisplay));
    }
    
    /// <summary>
    /// 初始化主塔血量UI
    /// </summary>
    private void InitializeMainTowerHealthUI()
    {
        // 查找中心塔
        FindCenterTower();
        
        // 设置中心塔图像
        SetupCenterTowerImage();
        
        // 初始化血条
        InitializeHealthBar();
        
        isInitialized = true;
    }
    
    /// <summary>
    /// 查找中心塔
    /// </summary>
    private void FindCenterTower()
    {
        centerTower = GameObject.FindGameObjectWithTag("CenterTower");
        if (centerTower != null)
        {
            centerTowerDamageTaker = centerTower.GetComponent<DamageTaker>();
            if (centerTowerDamageTaker == null)
            {
                Debug.LogWarning("中心塔没有DamageTaker组件");
            }
        }
        else
        {
            Debug.LogWarning("未找到标签为'CenterTower'的中心塔");
        }
    }
    
    /// <summary>
    /// 设置中心塔图像
    /// </summary>
    private void SetupCenterTowerImage()
    {
        if (centerTowerImage != null)
        {
            if (centerTowerSprite != null)
            {
                centerTowerImage.sprite = centerTowerSprite;
                centerTowerImage.preserveAspect = true;
            }
            else if (centerTower != null)
            {
                // 如果没有设置sprite，尝试从中心塔获取
                var spriteRenderer = centerTower.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    centerTowerImage.sprite = spriteRenderer.sprite;
                    centerTowerImage.preserveAspect = true;
                }
            }
        }
    }
    
    /// <summary>
    /// 初始化血条
    /// </summary>
    private void InitializeHealthBar()
    {
        if (healthBarFill != null && centerTowerDamageTaker != null)
        {
            // 检查血量是否已经正确设置
            if (centerTowerDamageTaker.maxHealth <= 0 || centerTowerDamageTaker.currentHealth <= 0)
            {
                // 如果血量还没有设置，延迟重试
                StartCoroutine(DelayedInitializeHealthBar());
                return;
            }
            
            // 设置血条填充量
            float healthPercent = centerTowerDamageTaker.currentHealth / centerTowerDamageTaker.maxHealth;
            healthBarFill.fillAmount = healthPercent;
            
            // 设置血条颜色
            UpdateHealthBarColor();
            
            // 更新血量文本
            UpdateHealthText();
            
            Debug.Log($"主塔血条初始化完成: {centerTowerDamageTaker.currentHealth}/{centerTowerDamageTaker.maxHealth}");
        }
        else
        {
            Debug.LogWarning("血条组件或DamageTaker组件未找到，无法初始化血条");
        }
    }
    
    /// <summary>
    /// 延迟初始化血条
    /// </summary>
    private System.Collections.IEnumerator DelayedInitializeHealthBar()
    {
        float retryTime = 0f;
        float maxRetryTime = 2f; // 最大重试时间2秒
        
        while (retryTime < maxRetryTime)
        {
            yield return new WaitForSeconds(0.1f);
            retryTime += 0.1f;
            
            // 重新查找中心塔和DamageTaker
            FindCenterTower();
            
            if (centerTowerDamageTaker != null && 
                centerTowerDamageTaker.maxHealth > 0 && 
                centerTowerDamageTaker.currentHealth > 0)
            {
                // 血量已经设置，重新初始化血条
                InitializeHealthBar();
                yield break;
            }
        }
        
        Debug.LogError("主塔血量初始化超时，请检查Tower组件是否正确设置血量");
    }
    
    /// <summary>
    /// 更新血量显示
    /// </summary>
    private void UpdateHealthDisplay()
    {
        if (!isInitialized || centerTowerDamageTaker == null)
            return;
        
        // 检查中心塔是否还存在
        if (centerTower == null || !centerTower.activeInHierarchy)
        {
            FindCenterTower();
            if (centerTowerDamageTaker == null)
                return;
        }
        
        // 检查血量数据是否有效
        if (centerTowerDamageTaker.maxHealth <= 0)
            return;
        
        // 更新血条填充量
        if (healthBarFill != null)
        {
            float healthPercent = centerTowerDamageTaker.currentHealth / centerTowerDamageTaker.maxHealth;
            healthBarFill.fillAmount = healthPercent;
        }
        
        // 更新血条颜色
        UpdateHealthBarColor();
        
        // 更新血量文本
        UpdateHealthText();
    }
    
    /// <summary>
    /// 更新血条颜色
    /// </summary>
    private void UpdateHealthBarColor()
    {
        if (healthBarFill == null || centerTowerDamageTaker == null)
            return;
        
        float healthPercent = centerTowerDamageTaker.currentHealth / centerTowerDamageTaker.maxHealth;
        
        if (healthPercent > 0.6f)
            healthBarFill.color = highHealthColor;
        else if (healthPercent > 0.3f)
            healthBarFill.color = mediumHealthColor;
        else
            healthBarFill.color = lowHealthColor;
    }
    
    /// <summary>
    /// 更新血量文本
    /// </summary>
    private void UpdateHealthText()
    {
        if (centerTowerDamageTaker == null)
            return;
        
        // 更新当前血量文本
        if (healthText != null)
        {
            healthText.text = $"{centerTowerDamageTaker.currentHealth:F0}";
            Debug.Log($"更新当前血量文本: {healthText.text}");
        }
        else
        {
            Debug.LogWarning("healthText 组件为 null");
        }
        
        // 更新最大血量文本
        if (maxHealthText != null)
        {
            maxHealthText.text = $"{centerTowerDamageTaker.maxHealth:F0}";
            Debug.Log($"更新最大血量文本: {maxHealthText.text}");
        }
        else
        {
            Debug.LogWarning("maxHealthText 组件为 null");
        }
    }
    
    /// <summary>
    /// 公共方法：手动刷新UI（供外部调用）
    /// </summary>
    public void RefreshUI()
    {
        if (isVisible)
        {
            FindCenterTower();
            if (centerTowerDamageTaker != null)
            {
                UpdateHealthDisplay();
            }
        }
    }
    
    /// <summary>
    /// 公共方法：设置血条更新间隔
    /// </summary>
    /// <param name="interval">更新间隔（秒）</param>
    public void SetUpdateInterval(float interval)
    {
        updateInterval = Mathf.Max(0.01f, interval);
        
        if (isVisible)
        {
            CancelInvoke(nameof(UpdateHealthDisplay));
            InvokeRepeating(nameof(UpdateHealthDisplay), 0f, updateInterval);
        }
    }
    
    /// <summary>
    /// 公共方法：获取中心塔当前血量
    /// </summary>
    /// <returns>当前血量，如果未找到则返回-1</returns>
    public float GetCurrentHealth()
    {
        if (centerTowerDamageTaker != null)
            return centerTowerDamageTaker.currentHealth;
        return -1f;
    }
    
    /// <summary>
    /// 公共方法：获取中心塔最大血量
    /// </summary>
    /// <returns>最大血量，如果未找到则返回-1</returns>
    public float GetMaxHealth()
    {
        if (centerTowerDamageTaker != null)
            return centerTowerDamageTaker.maxHealth;
        return -1f;
    }
    
    /// <summary>
    /// 公共方法：获取中心塔血量百分比
    /// </summary>
    /// <returns>血量百分比（0-1），如果未找到则返回-1</returns>
    public float GetHealthPercentage()
    {
        if (centerTowerDamageTaker != null && centerTowerDamageTaker.maxHealth > 0)
            return centerTowerDamageTaker.currentHealth / centerTowerDamageTaker.maxHealth;
        return -1f;
    }
}
