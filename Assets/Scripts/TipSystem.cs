using System.Collections.Generic;
using NUnit.Framework;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TipSystem : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private GameObject TipMenu;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI infoText;

    [SerializeField] private string previewShowName = "Preview_Show";
    [SerializeField] private RawImage previewImage;

    [SerializeField] private Vector3 _lastMousePosition;
    [SerializeField] private Vector2 previewOffset = new Vector2(12f, -12f);

    [SerializeField] private LayerMask towerMask; // set to "Tower" layer in Inspector

    [SerializeField] private BlockPlacementManager blockPlacementManager;
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
        _lastMousePosition = Input.mousePosition;
        
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
                HideTip();
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
        RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, 0.1f, towerMask);
        Debug.Log(hit2D.collider);
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

        // Screen -> world (2D point)
        Vector2 p = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        // Overlap point on 2D physics
        RaycastHit2D hit = Physics2D.Raycast(p,Vector2.one,0.1f, towerMask);

#if UNITY_EDITOR
        Debug.DrawLine(p + Vector2.left * 0.1f, p + Vector2.right * 0.1f, Color.red, 0.2f);
        Debug.DrawLine(p + Vector2.down * 0.1f, p + Vector2.up * 0.1f, Color.red, 0.2f);
#endif

        if (hit.collider != null)
        {
            ShowTip(hit.collider.gameObject.name, "Tower", Input.mousePosition);
        }
        else
        {
            HideTip();
        }
    }

    public GameObject GetCurrentUI()
    {
        if (EventSystem.current == null) return null;

        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);
        return results.Count > 0 ? results[0].gameObject : null;
    }

    public void HideTip()
    {
        if (TipMenu != null) TipMenu.SetActive(false);
    }

    public void ShowTip(string tile, string context, Vector3 screenPosition)
    {
        if (TipMenu == null) return;

        TipMenu.SetActive(true);
        if (nameText != null) nameText.text = tile;
        if (infoText != null) infoText.text = context;

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

        Vector2 targetPos = localPoint + previewOffset;

        // Clamp inside canvas
        Vector2 tipSize = tipRect.rect.size;   // size in local units
        Vector2 canvasSize = canvasRect.rect.size;

        float halfW = tipSize.x * 0.5f;
        float halfH = tipSize.y * 0.5f;

        targetPos.x = Mathf.Clamp(targetPos.x, -canvasSize.x * 0.5f + halfW, canvasSize.x * 0.5f - halfW);
        targetPos.y = Mathf.Clamp(targetPos.y, -canvasSize.y * 0.5f + halfH, canvasSize.y * 0.5f - halfH);

        tipRect.anchoredPosition = targetPos;
    }
}
