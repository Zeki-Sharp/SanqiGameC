using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// TipSystem - show tower info panel on hover (stable, no flicker)
/// </summary>
public class TipSystem : MonoBehaviour
{
    [BoxGroup("Camera"), SerializeField] private Camera mainCamera;
    [BoxGroup("Camera"), SerializeField] private Camera previewCamera;

    [BoxGroup("")] [SerializeField] private RawImage previewImage;
    [SerializeField] private BlockPlacementManager blockPlacementManager;

    [Header("Tip Panel")] [SerializeField] private GameObject TipMenu;
    [SerializeField] private string previewShowName = "Preview_Show";

    [Header("Tower UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image towerImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI maxHealthText;

    [Header("Offsets")]
    [SerializeField] private Vector2 previewOffset = new Vector2(100f, -100f);
    [SerializeField] private bool enableSmartOffset = true;
    [SerializeField] private float smartOffsetDistance = 50f;
    [SerializeField] private float edgeThreshold = 150f;
    [SerializeField] private bool enableDynamicOffset = true;
    [SerializeField] private float minDynamicOffset = 50f;
    [SerializeField] private float maxDynamicOffset = 150f;

    [Header("Debug")] [SerializeField] private bool showDebugInfo = false;

    [Header("Tower Detect")]
    [SerializeField] private LayerMask towerMask;
    [SerializeField] private float rayMaxDistance = 1000f;
    [SerializeField] private float worldHitRadius = 0.08f;

    [Header("Switch Confirm")]
    [SerializeField, Tooltip("Consecutive frames required to confirm switching to a new tower")]
    private int confirmFrames = 2;

    [ShowInInspector] private Vector3 _lastMousePos;

    [ShowInInspector] private float lastLeaveTime = -999f;
    [ShowInInspector] private  float hideDelay = 0.15f;

    [SerializeField] private CanvasGroup tipMenu;

    private Sequence inSeq, outSeq;

    // ==== Stable hover tracking ====
    private Tower _currentTower;        // tower currently displayed (stable)
    private Tower _pendingTower;        // tower detected this frame but not confirmed yet
    private int _pendingCount = 0;      // consecutive frames hitting the pending tower

    // visibility state
    private bool isVisible = false;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (previewCamera == null) previewCamera = GameObject.Find("PreviewCamera")?.GetComponent<Camera>();
        if (previewImage == null) previewImage = GameObject.Find(previewShowName)?.GetComponent<RawImage>();
        if (blockPlacementManager == null)
        {
            blockPlacementManager = GameManager.Instance.GetSystem<BlockPlacementManager>();
        }

        if (previewCamera == null)
        {
            Debug.LogError("PreviewCamera not found in the scene!");
        }

        if (TipMenu != null && tipMenu == null)
        {
            tipMenu = TipMenu.GetComponent<CanvasGroup>();
            if (tipMenu == null) tipMenu = TipMenu.AddComponent<CanvasGroup>();
        }

        lastLeaveTime = Time.time;
        if (tipMenu != null) tipMenu.gameObject.SetActive(false);
    }

    private void Update()
    {
        var mouse = Input.mousePosition;

        // 面板可见时，每帧都根据鼠标更新位置（不受防抖影响）
        if (isVisible && _currentTower != null && TipMenu != null && TipMenu.activeInHierarchy)
            SetTipPosition(mouse);

        // 阶段限制
        if (GameStateManager.Instance != null &&
            (GameStateManager.Instance.IsInPassPhase ||
             GameStateManager.Instance.IsInVictoryPhase ||
             blockPlacementManager.IsPlacing))
        {
            HideTip();
            _lastMousePos = mouse; // 记得更新，防止下帧误判
            return;
        }

        // 只对“重检测”（射线、UI 命中判定等）做防抖节流
        if ((mouse - _lastMousePos).sqrMagnitude < 1f)  // 1 像素阈值更温和
        {
            // 已在上面更新了位置，这里可以直接返回
            return;
        }

        _lastMousePos = mouse;
        UpdateTip();   // 只有明显移动才做检测
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

    public GameObject GetCurrentUI()
    {
        if (EventSystem.current == null) return null;

        var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        if (previewImage != null)
        {
            var target = previewImage.gameObject;
            foreach (var r in results)
            {
                if (r.gameObject == target || r.gameObject.transform.IsChildOf(target.transform))
                    return target;
            }
        }

        return results.Count > 0 ? results[0].gameObject : null;
    }

    private void UpdateTip()
    {
        // phase guard
        if (GameStateManager.Instance != null &&
            (GameStateManager.Instance.IsInPassPhase ||
             GameStateManager.Instance.IsInVictoryPhase ||
             blockPlacementManager.IsPlacing))
        {
            BeginHideCountdownAndMaybeHide();
            return;
        }

        // 1) detect tower under mouse (preview first, world fallback)
        Tower detected = null;

        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (pointerOverUI)
        {
            var ui = GetCurrentUI();
            if (ui != null && ui.name == previewShowName && previewCamera != null && previewImage != null)
            {
                detected = GetTowerFromPreview();
                if (showDebugInfo) Debug.Log($"[TipSystem] Preview hit: {(detected ? detected.name : "null")}");
            }
        }

        if (detected == null)
        {
            Vector2 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = Physics2D.OverlapCircle(worldPos, worldHitRadius, towerMask);
            if (col == null)
            {
                // try parent if collider on child
                RaycastHit2D rh = Physics2D.Raycast(worldPos, Vector2.zero, 0.01f, towerMask);
                if (rh.collider != null) col = rh.collider;
            }
            if (col) detected = col.GetComponentInParent<Tower>();
            if (showDebugInfo) Debug.Log($"[TipSystem] World hit: {(detected ? detected.name : "null")}");
        }

        // 2) sticky over tip panel: keep current tower alive
        bool overTip = (TipMenu != null && TipMenu.activeInHierarchy && IsPointerOverTransform(TipMenu.transform));
        if (overTip && _currentTower != null)
        {
            KeepAlive();
            SetTipPosition(Input.mousePosition);
            lastLeaveTime = -999f;
            return;
        }

        // 3) no hit this frame
        if (detected == null)
        {
            _pendingTower = null;
            _pendingCount = 0;
            BeginHideCountdownAndMaybeHide();
            return;
        }

        // 4) hit same as current tower -> keep alive, do not replay intro
        if (detected == _currentTower)
        {
            _pendingTower = null;
            _pendingCount = 0;
            KeepAlive();
            // optional: refresh data (hp may change)
            UpdateUIData(_currentTower);
            SetTipPosition(Input.mousePosition);
            lastLeaveTime = -999f;
            return;
        }

        // 5) hit another tower -> enter confirm window
        if (_pendingTower != detected)
        {
            _pendingTower = detected;
            _pendingCount = 1;
        }
        else
        {
            _pendingCount++;
        }

        if (_pendingCount >= confirmFrames)
        {
            // confirm switch
            _pendingCount = 0;
            _currentTower = _pendingTower;
            lastLeaveTime = -999f;

            bool needIntro = !isVisible;

            UpdateUIData(_currentTower);
            if (!TipMenu.activeSelf) TipMenu.SetActive(true);
            if (tipMenu != null) tipMenu.alpha = 1f;
            KeepAlive();

            if (needIntro) PlayIntro(_currentTower);
            SetTipPosition(Input.mousePosition);
        }
    }

    private void BeginHideCountdownAndMaybeHide()
    {
        if (lastLeaveTime < 0) lastLeaveTime = Time.time;
        if (Time.time - lastLeaveTime > hideDelay)
        {
            _currentTower = null;
            ReallyHide();
        }
    }

    // Keep panel visible and cancel any ongoing outro
    private void KeepAlive()
    {
        if (outSeq != null && outSeq.IsActive()) outSeq.Kill();
        if (TipMenu != null && !TipMenu.activeSelf) TipMenu.SetActive(true);
        if (tipMenu != null) tipMenu.alpha = 1f;
        isVisible = true;
    }

    private Tower GetTowerFromPreview()
    {
        if (previewImage == null || previewCamera == null) return null;

        var canvas = previewImage.canvas;
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera ?? Camera.main;

        RectTransform rt = previewImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, Input.mousePosition, uiCam, out Vector2 localPoint))
            return null;

        Rect rect = rt.rect;
        Rect contentRect = rect;
        var tex = previewImage.texture;
        if (tex != null)
        {
            float texAspect = (float)tex.width / Mathf.Max(1, tex.height);
            float rectAspect = rect.width / rect.height;

            if (rectAspect > texAspect)
            {
                float cw = rect.height * texAspect;
                float x = rect.x + (rect.width - cw) * 0.5f;
                contentRect = new Rect(x, rect.y, cw, rect.height);
            }
            else if (rectAspect < texAspect)
            {
                float ch = rect.width / texAspect;
                float y = rect.y + (rect.height - ch) * 0.5f;
                contentRect = new Rect(rect.x, y, rect.width, ch);
            }
        }

        if (!contentRect.Contains(localPoint)) return null;

        float u = (localPoint.x - contentRect.x) / contentRect.width;
        float v = (localPoint.y - contentRect.y) / contentRect.height;
        var uvRect = previewImage.uvRect;
        u = uvRect.x + u * uvRect.width;
        v = uvRect.y + v * uvRect.height;

        Ray ray = previewCamera.ViewportPointToRay(new Vector3(u, v, 0f));
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, rayMaxDistance, towerMask);
        return hit.collider != null ? hit.collider.GetComponentInParent<Tower>() : null;
    }

    public void GetPositionFromRawImage()
    {
        if (previewImage == null || previewCamera == null) return;

        var canvas = previewImage.canvas;
        Camera uiCam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : canvas != null
                ? (canvas.worldCamera != null ? canvas.worldCamera : Camera.main)
                : null;

        RectTransform rt = previewImage.rectTransform;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, Input.mousePosition, uiCam,
                out Vector2 localPoint))
            return;

        Rect rect = rt.rect;
        Rect contentRect = rect;
        var tex = previewImage.texture;
        if (tex != null && rect.width > 0.0001f && rect.height > 0.0001f)
        {
            float texAspect = (float)tex.width / Mathf.Max(1, tex.height);
            float rectAspect = rect.width / rect.height;

            if (rectAspect > texAspect)
            {
                float cw = rect.height * texAspect;
                float x = rect.x + (rect.width - cw) * 0.5f;
                contentRect = new Rect(x, rect.y, cw, rect.height);
            }
            else if (rectAspect < texAspect)
            {
                float ch = rect.width / texAspect;
                float y = rect.y + (rect.height - ch) * 0.5f;
                contentRect = new Rect(rect.x, y, rect.width, ch);
            }
        }

        if (!contentRect.Contains(localPoint))
        {
            HideTip();
            return;
        }

        float u = (localPoint.x - contentRect.x) / contentRect.width;
        float v = (localPoint.y - contentRect.y) / contentRect.height;

        var ur = previewImage.uvRect;
        u = ur.x + u * ur.width;
        v = ur.y + v * ur.height;

        Ray ray = previewCamera.ViewportPointToRay(new Vector3(u, v, 0f));

#if UNITY_EDITOR
        Debug.DrawLine(ray.origin + Vector3.left * 0.1f, ray.origin + Vector3.right * 0.1f, Color.cyan, 0.2f);
        Debug.DrawLine(ray.origin + Vector3.down * 0.1f, ray.origin + Vector3.up * 0.1f, Color.cyan, 0.2f);
#endif

        Tower tower = GetTowerFromPreview();
        if (tower != null)
            ShowTip(tower, Input.mousePosition);
        else
            HideTip();
    }

    #region Intro Animation

    public void PlayIntro(Tower tower)
    {
        if (inSeq != null && inSeq.IsActive()) return;
        if (outSeq != null && outSeq.IsActive()) outSeq.Kill();

        DOTween.Kill(this);
        outSeq?.Kill();

        if (tipMenu != null) tipMenu.gameObject.SetActive(true);

        inSeq = DOTween.Sequence();

        if (tipMenu != null)
            inSeq.Append(tipMenu.DOFade(1, 0.25f).From(0));

        if (nameText != null)
        {
            nameText.transform.localPosition = new Vector3(0, 180, 0);
            nameText.alpha = 0;
            inSeq.Join(nameText.transform.DOLocalMoveY(170, 0.35f).SetEase(Ease.OutBack))
                .Join(nameText.DOFade(1, 0.35f));
        }

        if (towerImage != null)
        {
            towerImage.transform.localScale = Vector3.zero;
            inSeq.Join(towerImage.transform.DOScale(1, 0.35f).SetEase(Ease.OutBack));
        }

        if (levelText != null)
        {
            string targetLevel = levelText.text;
            levelText.text = "";
            inSeq.Join(levelText.DOText(targetLevel, 0.3f));
        }

        if (descriptionText != null)
        {
            string full = descriptionText.text;
            descriptionText.text = "";
            inSeq.Join(descriptionText.DOText(full, 0.6f));
        }

        if (healthBar != null)
        {
            float targetHealth = healthBar.value;
            healthBar.value = 0;
            inSeq.Join(DOTween.To(() => healthBar.value, x => healthBar.value = x, targetHealth, 0.45f));
        }

        if (healthText != null)
        {
            string targetHp = healthText.text;
            healthText.text = "";
            inSeq.Join(healthText.DOText(targetHp, 0.6f));
        }

        if (maxHealthText != null)
        {
            string maxHp = maxHealthText.text;
            maxHealthText.text = "";
            inSeq.Join(maxHealthText.DOText(maxHp, 0.6f));
        }

        inSeq.OnComplete(() => { isVisible = true; });
    }

    #endregion

    #region Outro Animation

    public void PlayOutro(System.Action onComplete = null)
    {
        if (outSeq != null && outSeq.IsActive()) return;
        if (inSeq != null && inSeq.IsActive()) return;

        DOTween.Kill(this);
        inSeq?.Kill();

        outSeq = DOTween.Sequence();

        if (tipMenu != null)
            outSeq.Append(tipMenu.DOFade(0, 0.25f))
                .Join(transform.DOScale(0.98f, 0.25f).From(1).SetEase(Ease.InSine));

        if (descriptionText != null) descriptionText.text = "";
        if (maxHealthText != null) maxHealthText.text = "";

        outSeq.OnStart(() => { isVisible = false; });
        outSeq.OnComplete(() =>
        {
            if (tipMenu != null) tipMenu.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    #endregion

    public void ShowTip(Tower tower, Vector3 screenPosition)
    {
        if (TipMenu == null || tower == null) return;

        // if already visible on same tower, do not replay intro
        if (isVisible && tower == _currentTower)
        {
            UpdateUIData(tower);
            KeepAlive();
            SetTipPosition(screenPosition);
            return;
        }

        _currentTower = tower;
        UpdateUIData(tower);

        if (!TipMenu.activeSelf) TipMenu.SetActive(true);
        if (tipMenu != null && tipMenu.alpha < 1f) tipMenu.alpha = 1f;

        if (!isVisible)
        {
            PlayIntro(tower);
        }
        else
        {
            KeepAlive(); // already visible, just switch content
        }

        SetTipPosition(screenPosition);
    }

    private void UpdateUIData(Tower tower)
    {
        if (nameText != null) nameText.text = tower.TowerData.TowerName;
        if (levelText != null) levelText.text = $"等级: {tower.Level}";
        if (descriptionText != null) descriptionText.text = tower.TowerData.TowerDescription;

        if (towerImage != null)
        {
            Sprite sprite = tower.TowerData.GetTowerSprite(tower.Level);
            towerImage.sprite = sprite;
            towerImage.enabled = sprite != null;
        }

        if (healthBar != null)
        {
            float maxHealth = tower.TowerData.GetHealth(tower.Level);
            healthBar.maxValue = maxHealth;
            healthBar.value = Mathf.Clamp(tower.CurrentHealth, 0, maxHealth);

            var fillImg = healthBar.fillRect ? healthBar.fillRect.GetComponent<Image>() : null;
            if (fillImg != null && maxHealth > 0.0001f)
            {
                float percent = tower.CurrentHealth / maxHealth;
                fillImg.color = percent > 0.6f ? Color.green : percent > 0.3f ? Color.yellow : Color.red;
            }
        }

        if (healthText != null) healthText.text = $"血量: {tower.CurrentHealth:F0}";
        if (maxHealthText != null) maxHealthText.text = $"最大血量: {tower.TowerData.GetHealth(tower.Level):F0}";
    }

    public void HideTip()
    {
        if (TipMenu != null)
            PlayOutro(() => { if (showDebugInfo) Debug.Log("Tip closed"); });
    }

    private void ReallyHide()
    {
        lastLeaveTime = -999f;
        _pendingTower = null;
        _pendingCount = 0;
        isVisible = false;
        HideTip();
    }

    /// <summary>
    /// 设置提示框在屏幕上的位置
    /// </summary>
    /// <param name="screenPosition">提示框需要显示的屏幕坐标位置</param>
   private void SetTipPosition(Vector3 screenPosition)
{
    var tipRect = TipMenu.GetComponent<RectTransform>();
    if (tipRect == null) return;

    // 关键：使用 Tip 的父 RectTransform 做坐标转换与边界约束
    var parentRect = tipRect.parent as RectTransform;
    if (parentRect == null) return;

    // 用所在 Canvas 的相机（Screen Space - Overlay 时为 null）
    var canvas = parentRect.GetComponentInParent<Canvas>();
    Camera uiCam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        ? null
        : (canvas != null ? (canvas.worldCamera != null ? canvas.worldCamera : Camera.main) : null);

    // 把屏幕坐标转到“父 RectTransform”局部坐标
    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, uiCam, out var localPoint))
        return;

    // 偏移逻辑（保持你的写法）
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

    // 目标位置（父坐标系）
    Vector2 targetPos = localPoint + offset;

    // 用父 rect 做边界裁剪（不是根 Canvas）
    Vector2 tipSize = tipRect.rect.size;
    Vector2 parentSize = parentRect.rect.size;
    float halfW = tipSize.x * 0.5f;
    float halfH = tipSize.y * 0.5f;

    Vector2 clamped = new Vector2(
        Mathf.Clamp(targetPos.x, -parentSize.x * 0.5f + halfW, parentSize.x * 0.5f - halfW),
        Mathf.Clamp(targetPos.y, -parentSize.y * 0.5f + halfH, parentSize.y * 0.5f - halfH)
    );

    tipRect.anchoredPosition = clamped;

    if (showDebugInfo)
    {
        Debug.Log($"[TipSystem] screen={screenPosition} local(parent)={localPoint} -> anchored={clamped}");
    }
}

}
