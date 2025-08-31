using System.Collections.Generic;
using NUnit.Framework;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 塔信息提示系统
/// 功能：
/// 1. 鼠标悬停在塔上时显示信息面板
/// 2. 智能偏移避免遮挡目标对象
/// 3. 动态偏移根据鼠标位置调整
/// 4. 边缘检测自动调整偏移方向
/// </summary>
public class TipSystem : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private GameObject TipMenu;

    
    [Header("塔信息显示组件")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image towerImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI maxHealthText;
    [SerializeField] private string previewShowName = "Preview_Show";
    [SerializeField] private RawImage previewImage;

    [SerializeField] private Vector3 _lastMousePosition;
    
    [Header("提示面板位置偏移")]
    [SerializeField, Tooltip("提示面板相对于鼠标位置的偏移量\nX: 正值向右偏移，负值向左偏移\nY: 正值向上偏移，负值向下偏移")]
    private Vector2 previewOffset = new Vector2(100f, -100f);
    
    [SerializeField, Tooltip("是否启用智能偏移（避免遮挡目标对象）")]
    private bool enableSmartOffset = true;
    
    [SerializeField, Tooltip("智能偏移的额外距离")]
    private float smartOffsetDistance = 50f;
    
    [SerializeField, Tooltip("屏幕边缘检测阈值（像素）")]
    private float edgeThreshold = 150f;
    
    [SerializeField, Tooltip("是否启用动态偏移（根据鼠标位置动态调整）")]
    private bool enableDynamicOffset = true;
    
    [SerializeField, Tooltip("动态偏移的最小距离")]
    private float minDynamicOffset = 50f;
    
    [SerializeField, Tooltip("动态偏移的最大距离")]
    private float maxDynamicOffset = 150f;
    
    [Header("调试选项")]
    [SerializeField, Tooltip("是否显示调试信息")]
    private bool showDebugInfo = false;
    
    [SerializeField, Tooltip("是否在Scene视图中显示偏移线")]
    private bool showOffsetLines = false;
    
    [SerializeField, Tooltip("是否输出偏移计算信息到Console")]
    private bool logOffsetInfo = false;

    [SerializeField] private LayerMask towerMask; // set to "Tower" layer in Inspector

    [SerializeField] private BlockPlacementManager blockPlacementManager;
    [SerializeField] private float rayMaxDistance = 1000f;
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
            Debug.LogWarning("PreviewCamera not found.");
        }
    }

    private void Update()
    {
        // If in pass or victory phase, do nothing
        if (GameStateManager.Instance != null &&
            (GameStateManager.Instance.IsInPassPhase || GameStateManager.Instance.IsInVictoryPhase|| blockPlacementManager.IsPlacing))
        {
            HideTip();
            return;
        }

        // Update only when mouse moves enough
        if (Vector3.Distance(Input.mousePosition, _lastMousePosition) <= 0.1f) return;
        if (Vector3.Distance(Input.mousePosition, _lastMousePosition) <= 2f) HideTip();
        _lastMousePosition = Input.mousePosition;
        
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            var ui = GetCurrentUI();
            // Debug.Log("123");
            if (ui != null && previewImage != null && (ui == previewImage.gameObject || ui.transform.IsChildOf(previewImage.transform)))
            {
                if (previewCamera != null)
                    GetPositionFromRawImage();
                // else
                    // HideTip();
            }
            else
            {
                // HideTip();
            }
        }
        else
        {
            GetCameraPosition();
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
        // Debug.Log(hit2D.collider);
        if (hit2D.collider != null)
        {
            // Show tooltip at current mouse screen position
            ShowTip(hit2D.collider.gameObject.name, "Tower", Input.mousePosition);
        }
        else
        {
            HideTip();
        }
    }

    public void GetCameraPosition()
    {
        if (mainCamera == null) return;
        Vector2 p = mainCamera.ScreenToWorldPoint(Input.mousePosition);

#if UNITY_EDITOR
        Debug.DrawLine(p + Vector2.left * 0.1f, p + Vector2.right * 0.1f, Color.red, 0.2f);
        Debug.DrawLine(p + Vector2.down * 0.1f, p + Vector2.up * 0.1f, Color.red, 0.2f);
#endif

        // 点检测（命中 2D 碰撞体）
        Collider2D col = Physics2D.OverlapPoint(p, towerMask);
        if (col != null)
        {
            Tower tower = col.GetComponent<Tower>();
            ShowTip($"<align=\"left\">{tower.TowerData.TowerName}</align><align=\"right\">{tower.Level}</align>", $"", Input.mousePosition);
        }
        else
            HideTip();
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

    public void HideTip()
    {
        if (TipMenu != null) 
        {
            TipMenu.SetActive(false);
            // 隐藏塔详细信息
            HideTowerDetails();
        }
    }
    
    /// <summary>
    /// 隐藏塔详细信息
    /// </summary>
    private void HideTowerDetails()
    {
        // 隐藏塔图片
        if (towerImage != null)
        {
            towerImage.enabled = false;
        }
        
        // 清空文本
        if (levelText != null) levelText.text = "";
        if (descriptionText != null) descriptionText.text = "";
        if (healthText != null) healthText.text = "";
        if (maxHealthText != null) maxHealthText.text = "";
        
        // 重置血量条
        if (healthBar != null)
        {
            healthBar.value = 0;
        }
    }

    public void ShowTip(string tile, string context, Vector3 screenPosition)
    {
        if (TipMenu == null) return;

        TipMenu.SetActive(true);
        
        // 设置基础文本信息
        if (nameText != null) nameText.text = tile;
        // 尝试获取塔组件并显示详细信息
        TryShowTowerDetails(screenPosition);

        RectTransform tipRect = TipMenu.GetComponent<RectTransform>();
        RectTransform canvasRect = TipMenu.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (tipRect == null || canvasRect == null) return;

        // Resolve canvas render camera
        var canvas = canvasRect.GetComponent<Canvas>();
        Camera uiCam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : canvas != null ? (canvas.worldCamera != null ? canvas.worldCamera : Camera.main) : null;

        // Screen -> canvas local
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCam, out Vector2 localPoint))
            return;

        // 计算智能偏移
        Vector2 finalOffset = CalculateSmartOffset(screenPosition, tipRect, canvasRect, uiCam);
        Vector2 targetPos = localPoint + finalOffset;
        
        // 输出调试信息
        if (showDebugInfo)
        {
            Debug.Log($"[TipSystem] 鼠标位置: {screenPosition}, 基础偏移: {previewOffset}, 最终偏移: {finalOffset}, 目标位置: {targetPos}");
            Debug.Log($"[TipSystem] 本地点: {localPoint}, 最终目标位置: {targetPos}");
        }
        
        // 可选的偏移信息日志
        if (logOffsetInfo)
        {
            Debug.Log($"[TipSystem] 偏移计算 - 基础偏移: {previewOffset}, 最终偏移: {finalOffset}, 目标位置: {targetPos}");
        }

        // Clamp inside canvas
        Vector2 tipSize = tipRect.rect.size;   // size in local units
        Vector2 canvasSize = canvasRect.rect.size;

        float halfW = tipSize.x * 0.5f;
        float halfH = tipSize.y * 0.5f;

        // 放宽边界限制，允许更大的偏移
        Vector2 clampedPos = targetPos;
        clampedPos.x = Mathf.Clamp(targetPos.x, -canvasSize.x * 0.5f + halfW, canvasSize.x * 0.5f - halfW);
        clampedPos.y = Mathf.Clamp(targetPos.y, -canvasSize.y * 0.5f + halfH, canvasSize.y * 0.5f - halfH);

        tipRect.anchoredPosition = clampedPos;
    }
    
    /// <summary>
    /// 计算智能偏移，避免提示面板遮挡目标对象
    /// </summary>
    private Vector2 CalculateSmartOffset(Vector3 screenPosition, RectTransform tipRect, RectTransform canvasRect, Camera uiCam)
    {
        // 始终应用基础偏移
        Vector2 offset = previewOffset;
        
        if (!enableSmartOffset)
        {
            return offset;
        }
        
        // 获取屏幕尺寸
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        
        // 检查鼠标是否在屏幕边缘，应用额外的智能偏移
        // 如果鼠标靠近右边缘，向左偏移
        if (screenPosition.x > screenSize.x - edgeThreshold)
        {
            offset.x = -Mathf.Abs(offset.x) - smartOffsetDistance;
        }
        // 如果鼠标靠近左边缘，向右偏移
        else if (screenPosition.x < edgeThreshold)
        {
            offset.x = Mathf.Abs(offset.x) + smartOffsetDistance;
        }
        
        // 如果鼠标靠近上边缘，向下偏移
        if (screenPosition.y > screenSize.y - edgeThreshold)
        {
            offset.y = -Mathf.Abs(offset.y) - smartOffsetDistance;
        }
        // 如果鼠标靠近下边缘，向上偏移
        else if (screenPosition.y < edgeThreshold)
        {
            offset.y = Mathf.Abs(offset.y) + smartOffsetDistance;
        }
        
        // 应用动态偏移
        if (enableDynamicOffset)
        {
            offset = ApplyDynamicOffset(offset, screenPosition, screenSize);
        }
        
        return offset;
    }
    
    /// <summary>
    /// 应用动态偏移，根据鼠标在屏幕中的位置调整偏移量
    /// </summary>
    private Vector2 ApplyDynamicOffset(Vector2 baseOffset, Vector3 screenPosition, Vector2 screenSize)
    {
        Vector2 dynamicOffset = baseOffset;
        
        // 计算鼠标在屏幕中的相对位置 (0-1)
        Vector2 relativePos = new Vector2(
            screenPosition.x / screenSize.x,
            screenPosition.y / screenSize.y
        );
        
        // 根据鼠标位置动态调整偏移
        // 鼠标在屏幕中心时使用基础偏移，在边缘时增加偏移
        float centerInfluence = 1f - Mathf.Max(
            Mathf.Abs(relativePos.x - 0.5f) * 2f,
            Mathf.Abs(relativePos.y - 0.5f) * 2f
        );
        
        // 计算动态偏移量
        float dynamicAmount = Mathf.Lerp(maxDynamicOffset, minDynamicOffset, centerInfluence);
        
        // 应用动态偏移（作为额外偏移，不覆盖基础偏移）
        if (Mathf.Abs(baseOffset.x) > 0.1f)
        {
            // 保持基础偏移的方向，但调整距离
            dynamicOffset.x = baseOffset.x > 0 ? 
                Mathf.Max(baseOffset.x, dynamicAmount) : 
                Mathf.Min(baseOffset.x, -dynamicAmount);
        }
        if (Mathf.Abs(baseOffset.y) > 0.1f)
        {
            // 保持基础偏移的方向，但调整距离
            dynamicOffset.y = baseOffset.y > 0 ? 
                Mathf.Max(baseOffset.y, dynamicAmount) : 
                Mathf.Min(baseOffset.y, -dynamicAmount);
        }
        
        return dynamicOffset;
    }

    /// <summary>
    /// 尝试显示塔的详细信息
    /// </summary>
    private void TryShowTowerDetails(Vector3 screenPosition)
    {
        // 将屏幕坐标转换为世界坐标
        if (mainCamera == null) return;
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
        
        // 检测塔
        Collider2D col = Physics2D.OverlapPoint(worldPos, towerMask);
        if (col == null) return;
        
        Tower tower = col.GetComponent<Tower>();
        if (tower == null || tower.TowerData == null) return;
        
        // 显示塔图片
        if (towerImage != null)
        {
            Sprite towerSprite = tower.TowerData.GetTowerSprite(tower.Level);
            if (towerSprite != null)
            {
                towerImage.sprite = towerSprite;
                towerImage.enabled = true;
            }
            else
            {
                towerImage.enabled = false;
            }
        }
        
        // 显示等级
        if (levelText != null)
        {
            levelText.text = $"等级: {tower.Level}";
        }
        
        // 显示描述
        if (descriptionText != null)
        {
            descriptionText.text = tower.TowerData.TowerDescription;
        }
        
        // 显示血量信息
        if (healthBar != null)
        {
            float maxHealth = tower.TowerData.GetHealth(tower.Level);
            float currentHealth = tower.CurrentHealth;
            
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
            
            // 设置血量条颜色
            var fillImage = healthBar.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                float healthPercent = currentHealth / maxHealth;
                if (healthPercent > 0.6f)
                    fillImage.color = Color.green;
                else if (healthPercent > 0.3f)
                    fillImage.color = Color.yellow;
                else
                    fillImage.color = Color.red;
            }
        }
        
        // 显示当前血量文本
        if (healthText != null)
        {
            healthText.text = $"血量: {tower.CurrentHealth:F0}";
        }
        
        // 显示最大血量文本
        if (maxHealthText != null)
        {
            maxHealthText.text = $"最大血量: {tower.TowerData.GetHealth(tower.Level):F0}";
        }
    }
}
