using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 塔血条管理脚本
/// 功能：
/// 1. 自动更新血条显示
/// 2. 控制血条的显示和隐藏
/// 3. 只在建造完成后且非预览区的塔上显示血条
/// </summary>
public class TowerHealthBar : MonoBehaviour
{
    [Header("血条组件")]
    [SerializeField] private Image healthBarFill;
    
    [Header("血条设置")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);
    
    private DamageTaker damageTaker;
    private Tower tower;
    private Canvas healthBarCanvas;
    private bool isInitialized = false;
    private bool isBuilt = false;
    
    private void Awake()
    {
        // 获取DamageTaker组件
        damageTaker = GetComponent<DamageTaker>();
        
        // 获取Tower组件
        tower = GetComponent<Tower>();
        
        // 获取血条Canvas
        healthBarCanvas = GetComponentInChildren<Canvas>();
        
        if (healthBarCanvas != null)
        {
            // 设置血条位置偏移
            healthBarCanvas.transform.localPosition = offset;
            // 初始时隐藏血条
            healthBarCanvas.enabled = false;
        }
        else
        {
            Debug.LogWarning($"[TowerHealthBar] 在 {gameObject.name} 上未找到血条Canvas");
        }
        
        if (healthBarFill == null)
        {
            Debug.LogWarning($"[TowerHealthBar] 在 {gameObject.name} 上未设置血条填充组件");
        }
        
        if (damageTaker == null)
        {
            Debug.LogWarning($"[TowerHealthBar] 在 {gameObject.name} 上未找到DamageTaker组件");
        }
        
        if (tower == null)
        {
            Debug.LogWarning($"[TowerHealthBar] 在 {gameObject.name} 上未找到Tower组件");
        }
    }
    
    private void Start()
    {
        // 延迟初始化，确保DamageTaker的血量已经设置完成
        Invoke(nameof(InitializeHealthBar), 0.1f);
        
        // 检查塔是否已经建造完成
        CheckTowerBuildStatus();
    }
    
    private void Update()
    {
        // 如果塔还没建造完成，不更新血条
        if (!isBuilt) return;
        
        // 如果是预览区的塔，不更新血条
        if (tower != null && tower.IsShowAreaTower) return;
        
        if (!isInitialized || healthBarFill == null || damageTaker == null)
            return;
            
        // 获取塔的血量信息
        float currentHealth = damageTaker.currentHealth;
        float maxHealth = damageTaker.maxHealth;
        
        // 确保血量有效
        if (maxHealth > 0)
        {
            // 计算血量百分比
            float healthPercent = currentHealth / maxHealth;
            
            // 更新血条填充量
            healthBarFill.fillAmount = healthPercent;
            
            // 输出调试信息（可选）
            if (showDebugInfo)
            {
                Debug.Log($"[TowerHealthBar] {gameObject.name} 血量: {currentHealth:F0}/{maxHealth:F0}, 百分比: {healthPercent:F2}");
            }
        }
    }
    
    /// <summary>
    /// 检查塔的建造状态
    /// </summary>
    private void CheckTowerBuildStatus()
    {
        // 检查塔是否有有效的血量（建造完成的标志）
        if (damageTaker != null && damageTaker.maxHealth > 0)
        {
            // 延迟检查，确保血量已经设置
            Invoke(nameof(DelayedBuildCheck), 0.2f);
        }
        else
        {
            // 如果还没有血量信息，继续等待
            Invoke(nameof(CheckTowerBuildStatus), 0.1f);
        }
    }
    
    /// <summary>
    /// 延迟检查建造状态
    /// </summary>
    private void DelayedBuildCheck()
    {
        if (damageTaker != null && damageTaker.maxHealth > 0 && damageTaker.currentHealth > 0)
        {
            isBuilt = true;
            
            // 只有在非预览区的塔上才显示血条
            if (tower != null && !tower.IsShowAreaTower)
            {
                ShowHealthBar();
                if (showDebugInfo)
                {
                    Debug.Log($"[TowerHealthBar] {gameObject.name} 塔建造完成，显示血条");
                }
            }
            else if (showDebugInfo)
            {
                Debug.Log($"[TowerHealthBar] {gameObject.name} 塔建造完成，但为预览区塔，不显示血条");
            }
        }
        else
        {
            // 如果还没建造完成，继续等待
            Invoke(nameof(CheckTowerBuildStatus), 0.1f);
        }
    }
    
    private void InitializeHealthBar()
    {
        if (damageTaker != null && damageTaker.maxHealth > 0)
        {
            isInitialized = true;
            if (showDebugInfo)
            {
                Debug.Log($"[TowerHealthBar] {gameObject.name} 血条初始化完成，血量: {damageTaker.currentHealth:F0}/{damageTaker.maxHealth:F0}");
            }
        }
        else
        {
            // 如果还没准备好，继续等待
            Invoke(nameof(InitializeHealthBar), 0.1f);
        }
    }
    
    [Header("调试选项")]
    [SerializeField] private bool showDebugInfo = false;
    
    /// <summary>
    /// 显示血条
    /// </summary>
    public void ShowHealthBar()
    {
        if (healthBarCanvas != null && isBuilt && tower != null && !tower.IsShowAreaTower)
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
        if (healthBarCanvas != null && isBuilt && tower != null && !tower.IsShowAreaTower)
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
    
    /// <summary>
    /// 手动设置建造完成状态（可选，用于外部控制）
    /// </summary>
    public void SetBuiltStatus(bool built)
    {
        isBuilt = built;
        if (built && tower != null && !tower.IsShowAreaTower)
        {
            ShowHealthBar();
        }
        else
        {
            HideHealthBar();
        }
    }
}
