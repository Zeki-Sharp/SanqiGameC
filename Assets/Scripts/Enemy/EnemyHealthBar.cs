using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敌人血条管理脚本
/// 功能：
/// 1. 自动更新血条显示
/// 2. 控制血条的显示和隐藏
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("血条组件")]
    [SerializeField] private Image healthBarFill;
    
    [Header("血条设置")]
    [SerializeField] private Vector3 offset = new Vector3(0, -1.5f, 0);
    
    private DamageTaker damageTaker;
    private Canvas healthBarCanvas;
    private bool isInitialized = false;
    
    private void Awake()
    {
        // 获取DamageTaker组件（与EnemyController使用相同的方法）
        damageTaker = GetComponent<DamageTaker>();
        
        // 获取血条Canvas
        healthBarCanvas = GetComponentInChildren<Canvas>();
        
        if (healthBarCanvas != null)
        {
            // 设置血条位置偏移
            healthBarCanvas.transform.localPosition = offset;
        }
        else
        {
            Debug.LogWarning($"[EnemyHealthBar] 在 {gameObject.name} 上未找到血条Canvas");
        }
        
        if (healthBarFill == null)
        {
            Debug.LogWarning($"[EnemyHealthBar] 在 {gameObject.name} 上未设置血条填充组件");
        }
        
        if (damageTaker == null)
        {
            Debug.LogWarning($"[EnemyHealthBar] 在 {gameObject.name} 上未找到DamageTaker组件");
        }
    }
    
    private void Start()
    {
        // 延迟初始化，确保DamageTaker的血量已经设置完成
        Invoke(nameof(InitializeHealthBar), 0.1f);
    }
    
    private void InitializeHealthBar()
    {
        if (damageTaker != null && damageTaker.maxHealth > 0)
        {
            isInitialized = true;
            if (showDebugInfo)
            {
                Debug.Log($"[EnemyHealthBar] {gameObject.name} 血条初始化完成，血量: {damageTaker.currentHealth:F0}/{damageTaker.maxHealth:F0}");
            }
        }
        else
        {
            // 如果还没准备好，继续等待
            Invoke(nameof(InitializeHealthBar), 0.1f);
        }
    }
    
    private void Update()
    {
        if (!isInitialized || healthBarFill == null || damageTaker == null)
            return;
            
        // 使用与EnemyController完全相同的方法获取血量
        float currentHealth = damageTaker.currentHealth;
        float maxHealth = damageTaker.maxHealth;
        
        // 确保血量有效
        if (maxHealth > 0)
        {
            // 计算血量百分比
            float healthPercent = currentHealth / maxHealth;
            
            // 更新血条填充量
            healthBarFill.fillAmount = healthPercent;
        }
    }
    
    [Header("调试选项")]
    [SerializeField] private bool showDebugInfo = false;
    
    /// <summary>
    /// 显示血条
    /// </summary>
    public void ShowHealthBar()
    {
        if (healthBarCanvas != null)
        {
            healthBarCanvas.enabled = true;
        }
    }
    
    /// <summary>
    /// 隐藏血条
    /// </summary>
    public void HideHealthBar()
    {
        if (healthBarCanvas != null)
        {
            healthBarCanvas.enabled = false;
        }
    }
    
    /// <summary>
    /// 切换血条显示状态
    /// </summary>
    public void ToggleHealthBar()
    {
        if (healthBarCanvas != null)
        {
            healthBarCanvas.enabled = !healthBarCanvas.enabled;
        }
    }
    
    /// <summary>
    /// 设置血条位置偏移
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
        if (healthBarCanvas != null)
        {
            healthBarCanvas.transform.localPosition = offset;
        }
    }
}
