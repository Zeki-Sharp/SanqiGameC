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
        Debug.Log("重新开始游戏");
        
        // 恢复时间缩放
        Time.timeScale = 1f;
        
        // 隐藏暂停面板
        Hide();
        
        // 通过GameManager重置游戏
        if (GameManager.Instance != null)
        {
            // 销毁所有敌人
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
                Destroy(enemy);
            }

            // 销毁所有塔防
            GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
            foreach (GameObject tower in towers)
            {
                // 不销毁中心塔
                if (!tower.CompareTag("CenterTower"))
                {
                    Destroy(tower);
                }
            }

            // 重置回合管理器
            var roundManager = GameManager.Instance.GetSystem<RoundManager>();
            if (roundManager != null)
            {
                roundManager.Reset();
            }
            
            // 重置游戏状态
            var gameStateManager = GameManager.Instance.GetSystem<GameStateManager>();
            if (gameStateManager != null)
            {
                gameStateManager.SwitchToBuildingPhase();
            }

            // 重置敌人生成器
            var enemySpawner = GameObject.FindObjectOfType<EnemySpawner>();
            if (enemySpawner != null)
            {
                enemySpawner.StopAllCoroutines(); // 停止所有生成协程
                enemySpawner.enabled = false;      // 禁用生成器
                enemySpawner.enabled = true;       // 重新启用生成器
            }

            // 重置商店/资源系统
            var shopSystem = GameManager.Instance.GetSystem<ShopSystem>();
            if (shopSystem != null)
            {
                // 重置金钱为初始值
                shopSystem.Money = shopSystem.InitialMoney;
            }

            // 重置物品系统
            var itemManage = GameManager.Instance.GetSystem<ItemManage>();
            if (itemManage != null)
            {
                itemManage.ShowItem(false); // 重新生成物品
            }

            // 重置地图
            var gameMap = GameManager.Instance.GetSystem<GameMap>();
            if (gameMap != null)
            {
                gameMap.ResetMap(); // 如果有这个方法的话
            }
        }
        
        // 切换到建设UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SwitchToPhase(GamePhase.BuildingPhase);
        }
    }

    private void OnResumeClicked()
    {
        Debug.Log("恢复游戏");
        
        // 恢复时间缩放
        Time.timeScale = 1f;
        
        // 隐藏暂停面板
        UIManager.Instance.HidePausePanel();
        
        // 恢复战斗UI显示
        var combatUI = UIManager.Instance.GetPanel<CombatUIPanel>();
        if (combatUI != null)
        {
            combatUI.Show();
        }
        
        // 通知GameManager恢复游戏
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("返回主菜单");
        
        // 恢复时间缩放
        Time.timeScale = 1f;
        
        // 清理当前游戏状态
        if (GameManager.Instance != null)
        {
            // 重置所有系统
            var roundManager = GameManager.Instance.GetSystem<RoundManager>();
            if (roundManager != null)
            {
                roundManager.Reset();
            }
            
            // 重置游戏状态
            var gameStateManager = GameManager.Instance.GetSystem<GameStateManager>();
            if (gameStateManager != null)
            {
                gameStateManager.SwitchToBuildingPhase();
            }
            
            // 可以添加其他系统的重置
        }
        
        // 隐藏所有UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideAllPanels();
        }
        
        // 加载主菜单场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
} 