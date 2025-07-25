using UnityEngine;
using UnityEngine.UI;

public class PauseUIPanel : UIPanel
{
    [Header("暂停菜单组件")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;

    protected override void Awake()
    {
        base.Awake();
        Debug.Log("PauseUI Awake, Hide");
        Hide(); // 确保Awake时隐藏
    }

    private void Start()
    {
        Debug.Log("PauseUI Start, Hide");
        Hide(); 
    }

    protected override void OnShow()
    {
        base.OnShow();
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeClicked);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    public virtual void Initialize()//默认隐藏面板
    {
        Hide();
        Debug.Log($"{GetType().Name} 初始化完成");
    }

    private void OnRestartClicked()
    {
        Debug.Log("重新开始功能未实现");
        Hide();
    }

    private void OnResumeClicked()
    {
        Time.timeScale = 1f;
        UIManager.Instance.HidePausePanel();
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("返回主菜单功能未实现");
        Hide();
    }
} 