using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuPanel : UIPanel
{
    [Header("主菜单按钮")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnShow()
    {
        base.OnShow();
        
        // 绑定按钮事件
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    private void OnStartGameClicked()
    {
        Debug.Log("开始游戏");
        // 加载游戏场景
        SceneManager.LoadScene("GameScene"); // 确保你有一个名为"GameScene"的场景
    }

    private void OnSettingsClicked()
    {
        Debug.Log("打开设置");
        // TODO: 显示设置面板
    }

    private void OnQuitClicked()
    {
        Debug.Log("退出游戏");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
