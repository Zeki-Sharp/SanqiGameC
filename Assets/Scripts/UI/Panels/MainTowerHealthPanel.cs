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
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI maxHealthText;
    
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
        
        // 自动获取组件引用
        if (centerTowerImage == null)
            centerTowerImage = GetComponentInChildren<Image>();
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();
        if (healthBarFill == null && healthSlider != null)
            healthBarFill = healthSlider.fillRect?.GetComponent<Image>();
        if (healthText == null)
            healthText = GetComponentInChildren<TextMeshProUGUI>();
        if (maxHealthText == null)
            maxHealthText = GetComponentInChildren<TextMeshProUGUI>();
    }
    
    protected override void OnShow()
    {
        base.OnShow();
        
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
        
        Debug.Log("主塔血量UI初始化完成");
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
        if (healthSlider != null && centerTowerDamageTaker != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = centerTowerDamageTaker.maxHealth;
            healthSlider.value = centerTowerDamageTaker.currentHealth;
            
            // 设置血条颜色
            UpdateHealthBarColor();
            
            // 更新血量文本
            UpdateHealthText();
        }
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
        
        // 更新血条值
        if (healthSlider != null)
        {
            healthSlider.value = centerTowerDamageTaker.currentHealth;
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
            healthText.text = $"血量: {centerTowerDamageTaker.currentHealth:F0}";
        }
        
        // 更新最大血量文本
        if (maxHealthText != null)
        {
            maxHealthText.text = $"最大血量: {centerTowerDamageTaker.maxHealth:F0}";
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
            UpdateHealthDisplay();
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
