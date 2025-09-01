using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// TipSystem - 鼠标悬停塔时显示信息面板
/// </summary>
public class TipSystem : MonoBehaviour
{
    [BoxGroup("摄像机"),SerializeField] private Camera mainCamera;
    [BoxGroup("摄像机"),SerializeField] private Camera previewCamera;
    
    [BoxGroup("")]
    [SerializeField] private RawImage previewImage;
    [SerializeField] private BlockPlacementManager blockPlacementManager;
    
    [Header("提示面板")]
    [SerializeField] private GameObject TipMenu;
    [SerializeField]   private string previewShowName = "Preview_Show";
    [Header("塔信息UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image towerImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI maxHealthText;

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

    [Header("塔检测")]
    [SerializeField] private LayerMask towerMask; // 塔所在Layer
    [SerializeField] private float rayMaxDistance = 1000f;

    [ShowInInspector]private Vector3 _lastMousePos;
    
    private float lastLeaveTime = -999f;   // 上一次离开塔区域的时间
    private const float hideDelay = 0.15f; // 防抖时长
    

    [SerializeField] private CanvasGroup tipMenu;   // 对应你之前的 TipMenu

    private Sequence inSeq, outSeq;
    private Vector3 _lastMousePosition;

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
        lastLeaveTime = Time.time;   // 新增：避免第一帧就被判定需要隐藏
        HideTip();
    }

    private void Update()
    {
        /* 1. 微小抖动直接 return */
        if (Vector3.Distance(Input.mousePosition, _lastMousePos) < 2f) return;
        _lastMousePos = Input.mousePosition;

        /* 2. 阶段判断 */
        if (GameStateManager.Instance != null &&
            (GameStateManager.Instance.IsInPassPhase || GameStateManager.Instance.IsInVictoryPhase || blockPlacementManager.IsPlacing))
        {
            HideTip();
            return;
        }
// // 防抖隐藏
//         if (Time.time - lastLeaveTime > hideDelay)
//         {
//             HideTip();
//         }

        /* 3. 更新检测 */
        UpdateTip();
    }
    public GameObject GetCurrentUI()
    {
        if (EventSystem.current == null) return null;

        var pointer = new PointerEventData(EventSystem.current){ position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        if (previewImage != null)
        {
            var target = previewImage.gameObject;
            foreach (var r in results)
            {
                if (r.gameObject == target || r.gameObject.transform.IsChildOf(target.transform))
                    return target; // 明确是预览区域
            }
        }
        return results.Count > 0 ? results[0].gameObject : null;
    }
    /// <summary>
    /// 更新Tip显示
    /// </summary>
    private void UpdateTip()
    {
        if (mainCamera == null || previewCamera == null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            var ui = GetCurrentUI();
            Debug.Log("123");
            if (ui != null && ui.name == previewShowName && previewCamera != null && previewImage != null)
            {
                GetPositionFromRawImage();
            }
            else
            {
                // 记录离开时间，但不立即隐藏
                lastLeaveTime = Time.time;
            }
        }
        else
        {
            Vector2 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = Physics2D.OverlapPoint(worldPos, towerMask);

            if (col != null)
            {
                Tower tower = col.GetComponent<Tower>();
                if (tower != null)
                {
                    ShowTip(tower, Input.mousePosition);
                }
                else
                {
                    // 记录离开时间，但不立即隐藏
                    lastLeaveTime = Time.time;
                }
            }
            else
            {
                // 记录离开时间，但不立即隐藏
                lastLeaveTime = Time.time;
            }
        }
     
    }
   public void GetPositionFromRawImage()
    {
        if (previewImage == null || previewCamera == null) return;

        // Resolve UI camera for the RawImage canvas
        var canvas = previewImage.canvas;
        Camera uiCam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : canvas != null ? (canvas.worldCamera != null ? canvas.worldCamera : Camera.main) : null;

        RectTransform rt = previewImage.rectTransform;

        // Screen -> local point in RawImage rect
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, Input.mousePosition, uiCam, out Vector2 localPoint))
            return;

        // Compute the actually visible content rect (letterboxing)
        Rect rect = rt.rect;
        Rect contentRect = rect;
        var tex = previewImage.texture;
        if (tex != null && rect.width > 0.0001f && rect.height > 0.0001f)
        {
            float texAspect = (float)tex.width / Mathf.Max(1, tex.height);
            float rectAspect = rect.width / rect.height;

            if (rectAspect > texAspect)
            {
                // pillarbox: left/right black bars
                float cw = rect.height * texAspect;
                float x = rect.x + (rect.width - cw) * 0.5f;
                contentRect = new Rect(x, rect.y, cw, rect.height);
            }
            else if (rectAspect < texAspect)
            {
                // letterbox: top/bottom black bars
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

        // Local content -> UV
        float u = (localPoint.x - contentRect.x) / contentRect.width;
        float v = (localPoint.y - contentRect.y) / contentRect.height;

        // Apply RawImage uvRect
        var ur = previewImage.uvRect;
        u = ur.x + u * ur.width;
        v = ur.y + v * ur.height;

        // Build a ray from the preview camera
        Ray ray = previewCamera.ViewportPointToRay(new Vector3(u, v, 0f));
        
#if UNITY_EDITOR
        Debug.DrawLine(ray.origin + Vector3.left * 0.1f, ray.origin + Vector3.right * 0.1f, Color.cyan, 0.2f);
        Debug.DrawLine(ray.origin + Vector3.down * 0.1f, ray.origin + Vector3.up * 0.1f, Color.cyan, 0.2f);
#endif

        // Intersect with 2D physics using the 3D ray
        RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, rayMaxDistance, towerMask);
        Debug.Log(hit2D.collider);
        if (hit2D.collider != null)
        {
            Tower tower = hit2D.collider.GetComponent<Tower>();
            if (tower != null)
            {
                ShowTip(tower, Input.mousePosition);
            }
            else
            {
                HideTip();
            }
        }
        else
        {
            HideTip();
        }
    }

  
   #region 进入动画
    public void PlayIntro(Tower tower)
    {
        // 先杀掉旧动画
        DOTween.Kill(this);
        outSeq?.Kill();      // 如果正在做离开动画也杀掉

        // 确保可见
        if (tipMenu != null) tipMenu.gameObject.SetActive(true);

        inSeq = DOTween.Sequence();

        /* 0. 整体淡入 */
        if (tipMenu != null)
            inSeq.Append(tipMenu.DOFade(1, 0.3f).From(0));

        /* 1. 名字文本：从上淡入并下移 */
        nameText.transform.localPosition = new Vector3(0, 180, 0);
        nameText.alpha = 0;
        inSeq.Join(nameText.transform.DOLocalMoveY(170, 0.5f).SetEase(Ease.OutBack))
             .Join(nameText.DOFade(1, 0.5f));

        /* 2. 塔图片：缩放弹出 */
        towerImage.transform.localScale = Vector3.zero;
        inSeq.Join(towerImage.transform.DOScale(1, 0.5f).SetEase(Ease.OutBack));

        /* 3. 等级文本：数字滚动到目标值 */
        string targetLevel = levelText.text;
        levelText.text = "";
        inSeq.Join(levelText.DOText(targetLevel, 0.4f));

        /* 4. 描述文本：打字机效果 */
        string full = descriptionText.text;
        descriptionText.text = "";
        inSeq.Join(descriptionText.DOText(full, 0.8f));

        /* 5. 血条：从 0 滑到当前值 */
        float targetHealth = healthBar.value;
        healthBar.value = 0;
        inSeq.Join(DOTween.To(() => healthBar.value, x => healthBar.value = x,
                              targetHealth, 0.6f));
        
        /* 4. 描述文本：打字机效果 */
        string targetHp = healthText.text;
        healthText.text = "";
        inSeq.Join(healthText.DOText(targetHp, 0.8f));
        
        /* 4. 描述文本：打字机效果 */
        string maxHealth = maxHealthText.text;
        maxHealthText.text = "";
        inSeq.Join(maxHealthText.DOText(maxHealth, 0.8f));
        
        // /* 6. 当前血量文字：数字滚动 */
        // int targetHp = int.Parse(healthText.text);
        // healthText.text = "0";
        // inSeq.Join(healthText.DOCounter(0, targetHp, 0.5f));
        //
        // /* 7. 最大血量文字：淡入 */
        // maxHealthText.alpha = 0;
        // inSeq.Join(maxHealthText.DOFade(1, 0.4f));
    }
    #endregion

    #region 离开动画
    public void PlayOutro(System.Action onComplete = null)
    {
        DOTween.Kill(this);
        inSeq?.Kill();

        outSeq = DOTween.Sequence();

        /* 0. 整体淡出（同时整体缩小一点） */
        if (tipMenu != null)
            outSeq.Append(tipMenu.DOFade(0, 0.3f))
                  .Join(transform.DOScale(0.9f, 0.3f).From(1).SetEase(Ease.InBack));
        

        /* 2. 动画完成后把物体真正隐藏 */
        outSeq.OnComplete(() =>
        {
            if (tipMenu != null) tipMenu.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }
    #endregion
    /// <summary>
    /// 显示塔信息
    /// </summary>
    public void ShowTip(Tower tower, Vector3 screenPosition)
    {
        if (TipMenu == null || tower == null) return; 
        TipMenu.SetActive(true);
        PlayIntro(tower);
        // 基础信息
        if (nameText != null) nameText.text = tower.TowerData.TowerName;
        if (levelText != null) levelText.text = $"等级: {tower.Level}";
        if (descriptionText != null) descriptionText.text = tower.TowerData.TowerDescription;

        // 塔图片
        if (towerImage != null)
        {
            Sprite sprite = tower.TowerData.GetTowerSprite(tower.Level);
            towerImage.sprite = sprite;
            towerImage.enabled = sprite != null;
        }

        // 血量
        if (healthBar != null)
        {
            float maxHealth = tower.TowerData.GetHealth(tower.Level);
            healthBar.maxValue = maxHealth;
            healthBar.value = tower.CurrentHealth;

            var fillImg = healthBar.fillRect?.GetComponent<Image>();
            if (fillImg != null)
            {
                float percent = tower.CurrentHealth / maxHealth;
                fillImg.color = percent > 0.6f ? Color.green : percent > 0.3f ? Color.yellow : Color.red;
            }
        }

        if (healthText != null) healthText.text = $"血量: {tower.CurrentHealth:F0}";
        if (maxHealthText != null) maxHealthText.text = $"最大血量: {tower.TowerData.GetHealth(tower.Level):F0}";

        SetTipPosition(screenPosition);
    }

    /// <summary>
    /// 隐藏Tip面板
    /// </summary>
    public void HideTip()
    {
        if (TipMenu != null) 
            PlayOutro(() => Debug.Log("已完全关闭"));
    }

    /// <summary>
    /// 设置Tip面板位置，支持智能和动态偏移
    /// </summary>
    private void SetTipPosition(Vector3 screenPosition)
    {
        RectTransform tipRect = TipMenu.GetComponent<RectTransform>();
        RectTransform canvasRect = TipMenu.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
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
            Debug.Log($"[TipSystem] 鼠标: {screenPosition}, 偏移: {offset}, 面板位置: {tipRect.anchoredPosition}");
        }
    }
}
