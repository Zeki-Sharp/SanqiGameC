using UnityEngine;
using UnityEngine.UI;

public class BlockPlacementUI : MonoBehaviour
{
    [Header("管理器引用")]
    [SerializeField] private BlockPlacementManager placementManager;
    
    [Header("状态显示")]
    [SerializeField] private Text statusText;
    
    private void Start()
    {
        if (placementManager == null)
            placementManager = FindFirstObjectByType<BlockPlacementManager>();
            
        UpdateStatusText("准备就绪，按数字键测试：1=LINE2H, 2=L3, 3=SQUARE2, C=清空, T=测试");
    }
    
    private void Update()
    {
        HandleKeyboardInput();
    }
    
    private void HandleKeyboardInput()
    {
        if (placementManager == null) return;
        
        // 数字键选择形状
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            OnShapeButtonClicked("LINE2H");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            OnShapeButtonClicked("L3");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            OnShapeButtonClicked("SQUARE2");
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            OnClearMapButtonClicked();
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            OnTestButtonClicked();
        }
    }
    
    private void OnShapeButtonClicked(string shapeName)
    {

        if (placementManager == null) return;
        
        //placementManager.StartPlacement(shapeName,towerDatas);
        UpdateStatusText($"开始放置 {shapeName} 方块，点击地图位置放置，右键取消");
    }
    
    private void OnClearMapButtonClicked()
    {
        if (placementManager == null) return;
        
        placementManager.ClearMap();
        UpdateStatusText("地图已清空");
    }
    
    private void OnTestButtonClicked()
    {
        if (placementManager == null) return;
        
        placementManager.TestGenerateBlock("LINE2H", new Vector2Int(2, 2));
        placementManager.TestGenerateBlock("L3", new Vector2Int(5, 2));
        placementManager.TestGenerateBlock("SQUARE2", new Vector2Int(8, 2));
        
        UpdateStatusText("测试方块生成完成");
    }
    
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log(message);
    }
} 