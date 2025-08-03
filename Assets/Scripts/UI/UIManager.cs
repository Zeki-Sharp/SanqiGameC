using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UI管理器 - 统一管理所有UI面板
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UIManager>();
                if (instance == null)
                {
                    Debug.LogError("UIManager未找到！请在场景中添加UIManager组件。");
                }
            }
            return instance;
        }
    }

    [Header("UI面板引用")]
    [SerializeField] private BuildingUIPanel buildingUIPanel;
    [SerializeField] private CombatUIPanel combatUIPanel;
    [SerializeField] private PassUIPanel passUIPanel;
    [SerializeField] private VictoryUIPanel victoryUIPanel;
    [SerializeField] private PauseUIPanel pauseUIPanel;

    // 当前显示的面板
    private UIPanel currentPanel;
    
    // 面板字典，用于快速查找
    private Dictionary<PanelType, UIPanel> panels = new Dictionary<PanelType, UIPanel>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("UIManager已初始化");
        }
        else if (instance != this)
        {
            Debug.LogWarning("发现重复的UIManager，销毁当前实例");
            Destroy(gameObject);
            return;
        }

        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSystem(this);
        }
    }

    void Start()
    {
        InitializePanels();
    }

    /// <summary>
    /// 初始化所有UI面板
    /// </summary>
    private void InitializePanels()
    {
        // 自动查找面板组件
        if (buildingUIPanel == null)
            buildingUIPanel = FindFirstObjectByType<BuildingUIPanel>();
        if (combatUIPanel == null)
            combatUIPanel = FindFirstObjectByType<CombatUIPanel>();
        if (passUIPanel == null)
            passUIPanel = FindFirstObjectByType<PassUIPanel>();
        if (victoryUIPanel == null)
            victoryUIPanel = FindFirstObjectByType<VictoryUIPanel>();
        if (pauseUIPanel == null)
            pauseUIPanel = FindFirstObjectByType<PauseUIPanel>();

        // 先隐藏所有面板
        if (buildingUIPanel != null) buildingUIPanel.Hide();
        if (combatUIPanel != null) combatUIPanel.Hide();
        if (passUIPanel != null) passUIPanel.Hide();
        if (victoryUIPanel != null) victoryUIPanel.Hide();
        if (pauseUIPanel != null) pauseUIPanel.Hide();

        // 注册面板到字典
        if (buildingUIPanel != null)
        {
            panels[PanelType.Building] = buildingUIPanel;
            buildingUIPanel.Initialize();
        }
        if (combatUIPanel != null)
        {
            panels[PanelType.Combat] = combatUIPanel;
            combatUIPanel.Initialize();
        }
        if (passUIPanel != null)
        {
            panels[PanelType.Pass] = passUIPanel;
            passUIPanel.Initialize();
        }
        if (victoryUIPanel != null)
        {
            panels[PanelType.Victory] = victoryUIPanel;
            victoryUIPanel.Initialize();
        }
        if (pauseUIPanel != null)
        {
            panels[PanelType.Pause] = pauseUIPanel; 
            pauseUIPanel.Initialize();
        }

        // 默认显示建设阶段UI
        if (panels.ContainsKey(PanelType.Building))
        {
            ShowPanel(PanelType.Building);
        }
    }

    /// <summary>
    /// 显示指定面板
    /// </summary>
    /// <param name="panelType">面板类型</param>
    public void ShowPanel(PanelType panelType)
    {
        if (panels.TryGetValue(panelType, out UIPanel panel))
        {
            // 隐藏所有其他面板
            foreach (var kvp in panels)
            {
                if (kvp.Value != panel && kvp.Value.IsVisible)
                {
                    kvp.Value.Hide();
                }
            }

            // 显示新面板
            panel.Show();
            currentPanel = panel;
        }
        else
        {
            Debug.LogWarning($"未找到面板：{panelType}");
        }
    }

    /// <summary>
    /// 隐藏指定面板
    /// </summary>
    /// <param name="panelType">面板类型</param>
    public void HidePanel(PanelType panelType)
    {
        if (panels.TryGetValue(panelType, out UIPanel panel))
        {
            panel.Hide();
            if (currentPanel == panel)
            {
                currentPanel = null;
            }
        }
    }

    /// <summary>
    /// 切换到指定游戏阶段的UI
    /// </summary>
    /// <param name="gamePhase">游戏阶段</param>
    public void SwitchToPhase(GamePhase gamePhase)
    {
        // 先隐藏所有面板
        HideAllPanels();
        
        switch (gamePhase)
        {
            case GamePhase.BuildingPhase:
                ShowPanel(PanelType.Building);
                break;
            case GamePhase.CombatPhase:
                ShowPanel(PanelType.Combat);
                break;
            case GamePhase.PassPhase:
                ShowPanel(PanelType.Pass);
                break;
            case GamePhase.VictoryPhase:
                ShowPanel(PanelType.Victory);
                break;
            default:
                Debug.LogWarning($"未知的游戏阶段：{gamePhase}");
                break;
        }
    }

    /// <summary>
    /// 获取指定面板
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <returns>面板实例</returns>
    public T GetPanel<T>() where T : UIPanel
    {
        foreach (var panel in panels.Values)
        {
            if (panel is T targetPanel)
            {
                return targetPanel;
            }
        }
        return null;
    }

    /// <summary>
    /// 隐藏所有面板
    /// </summary>
    public void HideAllPanels()
    {
        foreach (var panel in panels.Values)
        {
            panel.Hide();
        }
        currentPanel = null;
    }

    /// <summary>
    /// 重置所有面板
    /// </summary>
    public void ResetAllPanels()
    {
        foreach (var panel in panels.Values)
        {
            panel.Reset();
        }
    }

    public void ShowPausePanel()
    {
        if (pauseUIPanel != null)
            pauseUIPanel.Show();
    }

    public void HidePausePanel()
    {
        if (pauseUIPanel != null)
            pauseUIPanel.Hide();
    }
}

/// <summary>
/// 面板类型枚举
/// </summary>
public enum PanelType
{
    Building,  // 建设阶段UI
    Combat,    // 战斗阶段UI
    Pass,      // 通过阶段UI（新增）
    Victory,   // 胜利阶段UI
    Pause      // 暂停界面UI
} 