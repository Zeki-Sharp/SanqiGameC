using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// TipSystem - 鼠标悬停塔时显示信息面板
/// </summary>
public class TipSystem : MonoBehaviour
{
    [Header("摄像机")]
    [SerializeField] private Camera mainCamera;

    [Header("提示面板")]
    [SerializeField] private GameObject TipMenu;

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

    private Vector3 _lastMousePos;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        HideTip();
    }

    private void Update()
    {
        if (Vector3.Distance(Input.mousePosition, _lastMousePos) < 0.1f) return;
        _lastMousePos = Input.mousePosition;

        // 如果鼠标在UI上，不显示Tip
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            HideTip();
            return;
        }

        UpdateTip();
    }

    /// <summary>
    /// 更新Tip显示
    /// </summary>
    private void UpdateTip()
    {
        if (mainCamera == null) return;

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
                HideTip();
            }
        }
        else
        {
            HideTip();
        }
    }

    /// <summary>
    /// 显示塔信息
    /// </summary>
    public void ShowTip(Tower tower, Vector3 screenPosition)
    {
        if (TipMenu == null || tower == null) return;

        TipMenu.SetActive(true);

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
        if (TipMenu != null) TipMenu.SetActive(false);
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
