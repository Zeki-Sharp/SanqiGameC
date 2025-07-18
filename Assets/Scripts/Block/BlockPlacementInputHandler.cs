using System;
using UnityEngine;

/// <summary>
/// 负责监听玩家输入（鼠标、键盘、UI），并通过事件分发输入结果。
/// 不处理建造、预览等具体逻辑。
/// </summary>
public class BlockPlacementInputHandler : MonoBehaviour
{
    public event Action<Vector3Int> OnPlaceBlockRequested;
    public event Action OnCancelPlacementRequested;
    public event Action<Vector3> OnPreviewPositionChanged;

    [SerializeField] private Camera mainCamera;
    [SerializeField] private bool isPlacing = false;
    [SerializeField] private GameMap gameMap;
    private Vector3Int lastPreviewGridPos;

    public void StartPlacement()
    {
        isPlacing = true;
    }

    public void StopPlacement()
    {
        isPlacing = false;
    }

    private void Update()
    {
        if (!isPlacing) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (gameMap == null) gameMap = FindFirstObjectByType<GameMap>();

        // 获取鼠标世界坐标
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        Vector3Int gridPos = TileMapUtility.WorldToCellPosition(gameMap.GetTilemap(), mouseWorldPos);

        // 预览位置变化事件
        if (lastPreviewGridPos != gridPos)
        {
            lastPreviewGridPos = gridPos;
            OnPreviewPositionChanged?.Invoke(mouseWorldPos);
        }

        // 鼠标左键点击请求建造
        if (Input.GetMouseButtonDown(0))
        {
            OnPlaceBlockRequested?.Invoke(gridPos);
        }

        // 鼠标右键或ESC取消建造
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancelPlacementRequested?.Invoke();
        }
    }
} 