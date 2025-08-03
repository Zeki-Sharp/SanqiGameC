using UnityEngine;

/// <summary>
/// UI面板基类 - 定义所有UI面板的基本行为
/// </summary>
public abstract class UIPanel : MonoBehaviour
{
    [Header("面板设置")]
    [SerializeField] protected bool isVisible = false;
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected GameObject panelRoot;

    /// <summary>
    /// 面板是否可见
    /// </summary>
    public bool IsVisible => isVisible;

    protected virtual void Awake()
    {
        // 自动获取组件引用
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (panelRoot == null)
            panelRoot = gameObject;
            
        // 确保面板一开始是隐藏的
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>
    /// 初始化面板
    /// </summary>
    public virtual void Initialize()
    {
        // 默认隐藏面板
        Hide();
    }

    /// <summary>
    /// 显示面板
    /// </summary>
    public virtual void Show()
    {
        if (isVisible) return;
        
        isVisible = true;
        
        // 显示面板根对象
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"{GetType().Name}.Show: panelRoot为null");
        }
        
        // 设置CanvasGroup
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        OnShow();
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public virtual void Hide()
    {
        if (!isVisible) return;
        
        isVisible = false;
        
        // 隐藏面板根对象
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"{GetType().Name}.Hide: panelRoot为null");
        }
        
        // 设置CanvasGroup
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        OnHide();
    }

    /// <summary>
    /// 重置面板状态
    /// </summary>
    public virtual void Reset()
    {
        OnReset();
    }

    /// <summary>
    /// 显示时的回调（子类重写）
    /// </summary>
    protected virtual void OnShow()
    {
        // 子类重写此方法实现具体的显示逻辑
    }

    /// <summary>
    /// 隐藏时的回调（子类重写）
    /// </summary>
    protected virtual void OnHide()
    {
        // 子类重写此方法实现具体的隐藏逻辑
    }

    /// <summary>
    /// 重置时的回调（子类重写）
    /// </summary>
    protected virtual void OnReset()
    {
        // 子类重写此方法实现具体的重置逻辑
    }

    /// <summary>
    /// 切换面板显示状态
    /// </summary>
    public virtual void Toggle()
    {
        if (isVisible)
            Hide();
        else
            Show();
    }

    /// <summary>
    /// 设置面板透明度
    /// </summary>
    /// <param name="alpha">透明度值 (0-1)</param>
    public virtual void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }

    /// <summary>
    /// 设置面板交互性
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public virtual void SetInteractable(bool interactable)
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }
    }
} 