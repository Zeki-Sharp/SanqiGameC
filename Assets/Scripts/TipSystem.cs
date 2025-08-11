using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TipSystem : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private GameObject TipMenu;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI infoText;

    [SerializeField]   private string PreviewShowName = "Preview_Show";
   
    [SerializeField] private Vector3 _lastMousePosition;
    [SerializeField] private Vector2 previewOffset;
    
    // 用于避免重复显示PreviewCamera错误
    private bool hasLoggedPreviewCameraError = false;
    private void Awake()
    {
        mainCamera = Camera.main;
        previewCamera = GameObject.Find("PreviewCamera")?.GetComponent<Camera>();
        if (previewCamera == null)
        {
            Debug.LogError("PreviewCamera not found in the scene!");
        }
    }
    
    private void Update()
    {
        if (!GameStateManager.Instance.IsInPassPhase || !GameStateManager.Instance.IsInVictoryPhase)
        { 
      
            if (Vector3.Distance(Input.mousePosition,_lastMousePosition) > 5f)
            { 
                _lastMousePosition = Input.mousePosition;
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    if ( GetCurrentUI().name == PreviewShowName)
                    {
                        // 只有在PreviewCamera存在时才调用
                        if (previewCamera != null)
                        {
                            GetOtherCameraPosition();
                        }
                    }
                }
                else
                {
                    // Debug.Log(EventSystem.current.gameObject.name); 
                }
                // var ray = mainCamera.ScreenPointToRay(Input.mousePosition); 
           
                // if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                // {
                //  
                //     
                //     if (hit.collider.gameObject.TryGetComponent(out EnemyController enemyController))
                //     {
                //         // Handle enemy controller logic here
                //     }
                //     else if (hit.collider.gameObject.TryGetComponent(out Tower tower))
                //     {
                //         // Handle tower logic here
                //     }
                //     else if (hit.collider.gameObject.name == PreviewShowName)
                //     {
                //         GetOtherCameraPosition();
                //     }
                // }
            }
        }
    }
    public void GetOtherCameraPosition()
    {
        if (previewCamera == null)
        {
            // 只在第一次调用时显示错误，避免持续产生错误信息
            if (!hasLoggedPreviewCameraError)
            {
                Debug.LogWarning("TipSystem: PreviewCamera未设置，跳过预览功能");
                hasLoggedPreviewCameraError = true;
            }
            return;
        }

        // 将屏幕坐标转换为世界坐标（z 不影响 2D 检测）
        Vector3 worldPos = previewCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 origin = new Vector2(worldPos.x, worldPos.y);
        origin += previewOffset;
        // 发出一条方向为 zero 的2D射线（点检测）
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right, 1000f);

        Debug.Log(origin);
        // 调试显示
        Debug.DrawLine(origin, origin + Vector2.right * 100f, Color.red, 1f);

        if (hit.collider != null)
        {
            Debug.Log($"2D Hit: {hit.collider.gameObject.name} at {hit.point}");
        }
        else
        {
            Debug.Log("No 2D hit detected");
        }
    }

    public GameObject GetCurrentUI()
    {
        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer,results);
        return results.Count > 0 ? results[0].gameObject : null;
    }
    public void HideTip()
    {
        
    }
    public void ShowTip(string tile, string context, Vector3 position)
    {
        
    }
}