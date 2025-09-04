using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// ItemTipSystem - 鼠标悬停物品时显示详细信息面板
/// </summary>
public class ItemTipSystem : MonoBehaviour
{
    [Header("物品检测")]
    [SerializeField] private LayerMask itemMask; // 物品所在Layer
    [SerializeField] private float rayMaxDistance = 1000f;
    [SerializeField] private float worldHitRadius = 0.08f;

    [Header("提示面板")]
    [SerializeField] private GameObject tipMenu;
    [SerializeField] private string itemPreviewName = "ItemPreview";

    [Header("物品UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI typeText;

    [Header("偏移设置")]
    [SerializeField] private Vector2 previewOffset = new Vector2(100f, -100f);
    [SerializeField] private bool enableSmartOffset = true;
    [SerializeField] private float smartOffsetDistance = 50f;
    [SerializeField] private float edgeThreshold = 150f;
    [SerializeField] private bool enableDynamicOffset = true;
    [SerializeField] private float minDynamicOffset = 50f;
    [SerializeField] private float maxDynamicOffset = 150f;

    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;

    private ActiveItemEffect _currentItemEffect;        // 当前显示的物品效果（稳定的）
    private ActiveItemEffect _pendingItemEffect;        // 本帧检测到但尚未确认的物品效果
    private int _pendingCount = 0;    // 连续几帧击中待确认物品效果
    private int confirmFrames = 2;    // 确认切换到新物品效果所需的连续帧数

    private bool isVisible = false;
    private Sequence inSeq, outSeq;

    [SerializeField] private CanvasGroup tipCanvasGroup;

    private void Update()
    {
        UpdateTip();
    }

    private void UpdateTip()
    {
        // 1) 检测鼠标下的物品效果UI（先检测UI，再检测世界）
        ActiveItemEffect detected = null;

        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (pointerOverUI)
        {
            // 检测UI中的物品效果
            var uiItemEffect = GetItemEffectFromUI();
            if (uiItemEffect != null)
            {
                detected = uiItemEffect;
                if (showDebugInfo) Debug.Log($"[ItemTipSystem] UI hit: {detected.itemName}");
            }
        }

        if (detected == null)
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = Physics2D.OverlapCircle(worldPos, worldHitRadius, itemMask);
            if (col == null)
            {
                // 如果碰撞体在子对象上，尝试射线检测
                RaycastHit2D rh = Physics2D.Raycast(worldPos, Vector2.zero, 0.01f, itemMask);
                if (rh.collider != null) col = rh.collider;
            }
            if (col) detected = col.GetComponentInParent<ActiveItemEffect>();
            if (showDebugInfo) Debug.Log($"[ItemTipSystem] World hit: {(detected != null ? detected.itemName : "null")}");
        }

        // 2) 鼠标悬停在提示面板上：保持当前物品显示
        bool overTip = (tipMenu != null && tipMenu.activeInHierarchy && IsPointerOverTransform(tipMenu.transform));
        if (tipMenu == null)
        {
            HideTip();
            return;
        }

        if (overTip && _currentItemEffect != null)
        {
            KeepAlive();
            SetTipPosition(Input.mousePosition);
            return;
        }

        // 3) 本帧未检测到物品效果
        if (detected == null)
        {
            _pendingItemEffect = null;
            _pendingCount = 0;
            HideTip();
            return;
        }

        // 4) 击中与当前显示相同的物品效果 -> 保持显示，不重新播放动画
        if (detected == _currentItemEffect)
        {
            _pendingItemEffect = null;
            _pendingCount = 0;
            KeepAlive();
            // 可选：刷新数据（如果物品效果数据可能变化）
            UpdateUIData(detected);
            SetTipPosition(Input.mousePosition);
            return;
        }

        // 5) 击中另一个物品效果 -> 进入确认窗口
        if (_pendingItemEffect != detected)
        {
            _pendingItemEffect = detected;
            _pendingCount = 1;
        }
        else
        {
            _pendingCount++;
        }

        if (_pendingCount >= confirmFrames)
        {
            // 确认切换
            _pendingCount = 0;
            _currentItemEffect = _pendingItemEffect;

            bool needIntro = !isVisible;

            UpdateUIData(_currentItemEffect);
            if (!tipMenu.activeSelf) tipMenu.SetActive(true);
        if (tipCanvasGroup != null && tipCanvasGroup.alpha < 1f) tipCanvasGroup.alpha = 1f;
            if (tipCanvasGroup != null) tipCanvasGroup.alpha = 1f;
            KeepAlive();

            if (needIntro) PlayIntro(_currentItemEffect);
            SetTipPosition(Input.mousePosition);
        }
    }

    /// <summary>
    /// 从UI中获取物品效果组件
    /// </summary>
    /// <returns>检测到的物品效果，未检测到则返回null</returns>
    private ActiveItemEffect GetItemEffectFromUI()
    {
        if (EventSystem.current == null) return null;

        if (EventSystem.current == null) return null;
        
        var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        foreach (var result in results)
        {
            if (result.gameObject != null)
            {
                // 检查GameObject上是否有EffectItemUI组件
                EffectItemUI effectItemUI = result.gameObject.GetComponent<EffectItemUI>();
                if (effectItemUI != null)
                {
                    // 获取对应的效果数据
                    return effectItemUI.GetItemEffect();
                }
            }
        }

        return null;
    }

    private bool IsPointerOverTransform(Transform root)
    {
        if (EventSystem.current == null || root == null) return false;
        var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);
        foreach (var r in results)
        {
            if (r.gameObject != null && (r.gameObject.transform == root || r.gameObject.transform.IsChildOf(root)))
                return true;
        }
        return false;
    }

    private void KeepAlive()
    {
        // 重置隐藏计时器
    }

    private void UpdateUIData(ActiveItemEffect itemEffect)
    {
        if (itemEffect == null) return;

        if (nameText != null) nameText.text = itemEffect.itemName;
        if (descriptionText != null) descriptionText.text = itemEffect.itemDescription;
        if (itemImage != null)
        {
            itemImage.sprite = itemEffect.itemSprite;
            itemImage.enabled = itemEffect.itemSprite != null;
        }
        
        // 物品效果不包含价格和类型信息
        if (priceText != null) priceText.text = "";
        if (typeText != null) typeText.text = "";
    }

    public void ShowTip(ActiveItemEffect itemEffect, Vector3 screenPosition)
    {
        if (tipMenu == null || itemEffect == null) return;

        // 如果已经在相同物品效果上显示，不重新播放动画
        if (isVisible && itemEffect == _currentItemEffect)
        {
            UpdateUIData(itemEffect);
            KeepAlive();
            SetTipPosition(screenPosition);
            return;
        }

        _currentItemEffect = itemEffect;
        UpdateUIData(itemEffect);

        if (!tipMenu.activeSelf) tipMenu.SetActive(true);
        if (tipCanvasGroup != null && tipCanvasGroup.alpha < 1f) tipCanvasGroup.alpha = 1f;

        if (!isVisible)
        {
            PlayIntro(itemEffect);
        }
        else
        {
            KeepAlive(); // 已经可见，只切换内容
        }

        SetTipPosition(screenPosition);
    }

    public void PlayIntro(ActiveItemEffect itemEffect)
    {
        if (inSeq != null && inSeq.IsActive()) return;
        if (outSeq != null && outSeq.IsActive()) outSeq.Kill();

        DOTween.Kill(this);
        outSeq?.Kill();

        inSeq = DOTween.Sequence();

        if (tipCanvasGroup != null)
            inSeq.Append(tipCanvasGroup.DOFade(1, 0.3f))
                .Join(transform.DOScale(1, 0.3f).From(0.8f).SetEase(Ease.OutBack));

        inSeq.OnComplete(() => { isVisible = true; });
    }

    public void HideTip()
    {
        if (tipMenu != null)
            PlayOutro(() => { if (showDebugInfo) Debug.Log("ItemTip closed"); });
    }

    public void PlayOutro(System.Action onComplete = null)
    {
        if (outSeq != null && outSeq.IsActive()) return;
        if (inSeq != null && inSeq.IsActive()) return;

        DOTween.Kill(this);
        inSeq?.Kill();

        outSeq = DOTween.Sequence();

        if (tipCanvasGroup != null)
            outSeq.Append(tipCanvasGroup.DOFade(0, 0.25f))
                .Join(transform.DOScale(0.98f, 0.25f).From(1).SetEase(Ease.InSine));

        outSeq.OnStart(() => { isVisible = false; });
        outSeq.OnComplete(() =>
        {
            if (tipCanvasGroup != null) tipCanvasGroup.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 设置提示面板位置，支持智能和动态偏移
    /// </summary>
    private void SetTipPosition(Vector3 screenPosition)
    {
        if (tipMenu == null) return;
        
        RectTransform tipRect = tipMenu.GetComponent<RectTransform>();
        RectTransform canvasRect = tipMenu.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (tipRect == null || canvasRect == null) return;

        Camera uiCam = canvasRect.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCam, out Vector2 localPoint))
            return;

        Vector2 offset = previewOffset;

        if (enableSmartOffset)
        {
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            if (screenPosition.x > screenSize.x - edgeThreshold) offset.x = -Mathf.Abs(offset.x) - smartOffsetDistance;
            else if (screenPosition.x < edgeThreshold) offset.x = Mathf.Abs(offset.x) + smartOffsetDistance;

            if (screenPosition.y > screenSize.y - edgeThreshold) offset.y = -Mathf.Abs(offset.y) - smartOffsetDistance;
            else if (screenPosition.y < edgeThreshold) offset.y = Mathf.Abs(offset.y) + smartOffsetDistance;
        }

        if (enableDynamicOffset)
        {
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 relativePos = new Vector2(screenPosition.x / screenSize.x, screenPosition.y / screenSize.y);
            float centerInfluence = 1f - Mathf.Max(Mathf.Abs(relativePos.x - 0.5f) * 2f, Mathf.Abs(relativePos.y - 0.5f) * 2f);
            float dynamicAmount = Mathf.Lerp(maxDynamicOffset, minDynamicOffset, centerInfluence);

            if (Mathf.Abs(offset.x) > 0.1f) offset.x = offset.x > 0 ? Mathf.Max(offset.x, dynamicAmount) : Mathf.Min(offset.x, -dynamicAmount);
            if (Mathf.Abs(offset.y) > 0.1f) offset.y = offset.y > 0 ? Mathf.Max(offset.y, dynamicAmount) : Mathf.Min(offset.y, -dynamicAmount);
        }

        Vector2 targetPos = localPoint + offset;

        // 限制在画布内
        Vector2 tipSize = tipRect.rect.size;
        Vector2 canvasSize = canvasRect.rect.size;
        float halfW = tipSize.x * 0.5f;
        float halfH = tipSize.y * 0.5f;

        tipRect.anchoredPosition = new Vector2(
            Mathf.Clamp(targetPos.x, -canvasSize.x * 0.5f + halfW, canvasSize.x * 0.5f - halfW),
            Mathf.Clamp(targetPos.y, -canvasSize.y * 0.5f + halfH, canvasSize.y * 0.5f - halfH)
        );

        if (showDebugInfo)
        {
            Debug.Log($"[ItemTipSystem] 鼠标: {screenPosition}, 偏移: {offset}, 面板位置: {tipRect.anchoredPosition}");
        }
    }
}