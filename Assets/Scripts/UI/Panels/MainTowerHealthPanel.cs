using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 主塔血量UI面板 - 在建造阶段和战斗阶段都一直存在
/// 包含固定的用户头像和血条显示（头像与主塔图像已脱钩）
/// </summary>
public class MainTowerHealthPanel : UIPanel
{
    [Header("用户头像（固定，与主塔图像脱钩）")]
    [SerializeField] private Image centerTowerImage;
    [SerializeField] private Sprite centerTowerSprite;
    
    [Header("血条组件")]
    [SerializeField] private Image healthBarFill; // 血条填充图像
    [SerializeField] private RectTransform healthBarFillRect; // 血条填充的RectTransform
    [SerializeField] private TextMeshProUGUI healthText; // 当前血量文本
    [SerializeField] private TextMeshProUGUI maxHealthText; // 最大血量文本
    
    [Header("效果显示")]
    [SerializeField] private Transform effectListContainer; // 效果列表容器
    [SerializeField] private GameObject effectItemPrefab; // 效果项预制体
    
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
    private float originalHealthBarWidth; // 血条原始宽度
    
    
    protected override void Awake()
    {
        base.Awake();
        
        // 确保面板一开始就是隐藏的
        gameObject.SetActive(false);
        
        // 自动获取组件引用（如果Inspector中没有分配）
        if (centerTowerImage == null)
            centerTowerImage = GetComponentInChildren<Image>();
        
        // 自动获取血条填充的RectTransform
        if (healthBarFillRect == null && healthBarFill != null)
            healthBarFillRect = healthBarFill.GetComponent<RectTransform>();
        
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
        
        // 订阅物品事件
        EventBus.Instance.Subscribe<ItemEvent>(UpdateEffectList);
        EventBus.Instance.SubscribeSimple("Game_NextRound", OnGameNextRound);
    }
    
    public override void Show()
    {
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
        InvokeRepeating(nameof(UpdateHealthDisplay), 0.2f, updateInterval);
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
    /// 设置中心塔图像（头像保持固定，不从主塔数据读取）
    /// </summary>
    private void SetupCenterTowerImage()
    {
        if (centerTowerImage != null)
        {
            // 头像保持固定，只使用预设的sprite，不从主塔数据中读取
            if (centerTowerSprite != null)
            {
                centerTowerImage.sprite = centerTowerSprite;
                centerTowerImage.preserveAspect = true;
            }
            // 移除从中心塔获取sprite的逻辑，保持头像固定
        }
    }
    
    /// <summary>
    /// 初始化血条
    /// </summary>
    private void InitializeHealthBar()
    {
        if (healthBarFillRect != null && centerTowerDamageTaker != null)
        {
            // 检查血量是否已经正确设置
            if (centerTowerDamageTaker.maxHealth <= 0 || centerTowerDamageTaker.currentHealth <= 0)
            {
                // 如果血量还没有设置，延迟重试
                StartCoroutine(DelayedInitializeHealthBar());
                return;
            }
            
            // 记录血条原始宽度
            originalHealthBarWidth = healthBarFillRect.sizeDelta.x;
            
            // 设置血条颜色
            UpdateHealthBarColor();
            
            // 更新血量文本
            UpdateHealthText();
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
        
        // 更新血条长度
        UpdateHealthBarLength();
        
        // 更新血条颜色
        UpdateHealthBarColor();
        
        // 更新血量文本
        UpdateHealthText();
    }
    
    /// <summary>
    /// 更新血条长度（通过调整RectTransform的宽度）
    /// </summary>
    private void UpdateHealthBarLength()
    {
        if (healthBarFillRect == null || centerTowerDamageTaker == null)
            return;
        
        float healthPercent = centerTowerDamageTaker.currentHealth / centerTowerDamageTaker.maxHealth;
        healthPercent = Mathf.Clamp01(healthPercent); // 确保在0-1范围内
        
        // 计算新的宽度
        float newWidth = originalHealthBarWidth * healthPercent;
        
        // 更新RectTransform的sizeDelta，只改变宽度
        Vector2 currentSize = healthBarFillRect.sizeDelta;
        healthBarFillRect.DOSizeDelta(new Vector2(newWidth, currentSize.y),0.5f)  ;
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
            healthText.DOText( $"{centerTowerDamageTaker.currentHealth:F0}",0.5f);
            // Debug.Log($"更新当前血量文本: {healthText.text}");
        }
        else
        {
            Debug.LogWarning("healthText 组件为 null");
        }
        
        // 更新最大血量文本
        if (maxHealthText != null)
        {
            maxHealthText.DOText( $"{centerTowerDamageTaker.maxHealth:F0}",0.5f);
            // Debug.Log($"更新最大血量文本: {maxHealthText.text}");
        }
        else
        {
            Debug.LogWarning("maxHealthText 组件为 null");
        }
    }

    private void UpdateEffectList(ItemEvent itemEvent)
    {
        if (itemEvent == null || itemEvent.ItemConfig == null) return;
        
        // 刷新效果显示
        DisplayActiveEffects();
    }
    
    /// <summary>
    /// 显示当前激活的物品效果
    /// </summary>
    public void DisplayActiveEffects()
    {
        // 获取物品管理系统
        ItemManage itemManage = GameManager.Instance?.GetSystem<ItemManage>();
        if (itemManage == null) return;
        
        // 获取激活的物品效果列表
        List<ActiveItemEffect> activeItemEffects = itemManage.GetActiveItemEffects();
        int currentRound = itemManage.GetDuration();
        
        // 如果没有效果容器或预制体，直接返回
        if (effectListContainer == null || effectItemPrefab == null) return;
        
        // 清除现有的效果显示
        foreach (Transform child in effectListContainer)
        {
            Destroy(child.gameObject);
        }
        
        // 显示所有激活的效果
        foreach (var effect in activeItemEffects)
        {
            GameObject effectItem = Instantiate(effectItemPrefab, effectListContainer);
            EffectItemUI effectItemUI = effectItem.GetComponent<EffectItemUI>();
            
            if (effectItemUI != null)
            {
                effectItemUI.SetEffectData(effect.itemName, effect.itemDescription, effect.itemSprite);
                // 设置关联的物品效果数据
                effectItemUI.SetItemEffect(effect);
                
                // 根据物品类型显示不同的信息
                if (effect.itemConfig is TemmporaryItemConfig tempConfig)
                {
                    // 临时物品显示回合数
                    effectItemUI.SetDurationText(currentRound, tempConfig.durationMax);
                }
                else
                {
                    // 永久物品隐藏回合数显示
                    effectItemUI.HideDurationText();
                }
            }
        }
    }
    
    /// <summary>
    /// 游戏回合更新事件处理
    /// </summary>
    private void OnGameNextRound()
    {
        // 刷新效果显示
        DisplayActiveEffects();
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
                DisplayActiveEffects();
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
    
    private void OnDestroy()
    {
        // 取消订阅事件
        EventBus.Instance.Unsubscribe<ItemEvent>(UpdateEffectList);
        EventBus.Instance.UnsubscribeSimple("Game_NextRound", OnGameNextRound);
    }
}