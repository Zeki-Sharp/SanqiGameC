using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

[CustomEditor(typeof(Tilemap))]
public class TilemapAnnotationEditor : Editor
{
    private bool isAnnotating = false;
    private bool showGridInfo = true;
    private bool showCellCoordinates = true;
    private bool autoRefresh = true;
    private float refreshInterval = 1.0f;
    private float lastRefreshTime = 0f;
    private Color annotationColor = Color.green;
    private Vector2 scrollPosition = Vector2.zero;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Tilemap tilemap = (Tilemap)target;

        GUILayout.Space(10);
        GUILayout.Label("标注设置", EditorStyles.boldLabel);
        
        isAnnotating = EditorGUILayout.Toggle("启用实时标注", isAnnotating);
        showGridInfo = EditorGUILayout.Toggle("显示网格信息", showGridInfo);
        showCellCoordinates = EditorGUILayout.Toggle("显示格子坐标", showCellCoordinates);
        annotationColor = EditorGUILayout.ColorField("标注颜色", annotationColor);
        autoRefresh = EditorGUILayout.Toggle("自动刷新", autoRefresh);
        
        if (!autoRefresh)
        {
            refreshInterval = EditorGUILayout.Slider("刷新间隔", refreshInterval, 0.1f, 5.0f);
            
            if (GUILayout.Button("手动刷新"))
            {
                // 强制重绘Scene视图
                SceneView.RepaintAll();
                lastRefreshTime = Time.realtimeSinceStartup;
            }
        }
        
        // 显示Tilemap的基本信息
        if (tilemap != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("Tilemap信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("位置", tilemap.transform.position.ToString());
            EditorGUILayout.LabelField("格子大小", tilemap.cellSize.ToString());
            
            // 获取Tile数量
            int tileCount = 0;
            BoundsInt bounds = tilemap.cellBounds;
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                if (tilemap.HasTile(pos))
                    tileCount++;
            }
            EditorGUILayout.LabelField($"Tile数量: {tileCount}");
            
            // 显示Tilemap的边界信息
            if (bounds.size.x > 0 && bounds.size.y > 0)
            {
                EditorGUILayout.LabelField($"边界范围: {bounds.xMin}-{bounds.xMax}, {bounds.yMin}-{bounds.yMax}");
                EditorGUILayout.LabelField($"边界尺寸: {bounds.size.x}x{bounds.size.y}");
            }
        }
    }

    public void OnSceneGUI()
    {
        Tilemap tilemap = (Tilemap)target;
        
        if (tilemap != null && isAnnotating)
        {
            // 检查是否需要刷新
            if (autoRefresh || Time.realtimeSinceStartup - lastRefreshTime >= refreshInterval)
            {
                lastRefreshTime = Time.realtimeSinceStartup;
                
                // 获取Tilemap的范围
                BoundsInt bounds = tilemap.cellBounds;
                
                // 设置标注颜色
                Handles.color = annotationColor;
                
                // 遍历所有可能的格子位置
                foreach (Vector3Int position in bounds.allPositionsWithin)
                {
                    // 检查该位置是否有Tile
                    if (tilemap.HasTile(position))
                    {
                        // 在Scene视图中绘制标注
                        Vector3 worldPosition = tilemap.GetCellCenterWorld(position);
                        
                        // 显示格子坐标
                        if (showCellCoordinates)
                        {
                            string coordinateText = $"({position.x}, {position.y})";
                            Handles.Label(worldPosition, coordinateText);
                        }
                        
                        // 显示网格信息
                        if (showGridInfo)
                        {
                            // 绘制一个方框来标注位置
                            Handles.DrawWireCube(worldPosition, tilemap.cellSize * 0.8f);
                            
                            // 绘制辅助线
                            Vector3[] lines = new Vector3[8];
                            float halfX = tilemap.cellSize.x * 0.4f;
                            float halfY = tilemap.cellSize.y * 0.4f;
                            
                            lines[0] = new Vector3(worldPosition.x - halfX, worldPosition.y - halfY, worldPosition.z);
                            lines[1] = new Vector3(worldPosition.x + halfX, worldPosition.y - halfY, worldPosition.z);
                            lines[2] = new Vector3(worldPosition.x + halfX, worldPosition.y - halfY, worldPosition.z);
                            lines[3] = new Vector3(worldPosition.x + halfX, worldPosition.y + halfY, worldPosition.z);
                            lines[4] = new Vector3(worldPosition.x + halfX, worldPosition.y + halfY, worldPosition.z);
                            lines[5] = new Vector3(worldPosition.x - halfX, worldPosition.y + halfY, worldPosition.z);
                            lines[6] = new Vector3(worldPosition.x - halfX, worldPosition.y + halfY, worldPosition.z);
                            lines[7] = new Vector3(worldPosition.x - halfX, worldPosition.y - halfY, worldPosition.z);
                            
                            Handles.DrawLines(lines);
                        }
                        
                        // 添加点击检测
                        if (Handles.Button(worldPosition, Quaternion.identity, tilemap.cellSize.x * 0.4f, tilemap.cellSize.x * 0.1f, Handles.CubeHandleCap))
                        {
                            // 当用户点击标注时，可以选择这个Tile的位置
                            Selection.activeGameObject = tilemap.gameObject;
                            EditorGUIUtility.PingObject(tilemap);
                            Debug.Log($"点击了格子位置: {position}, 世界坐标: {worldPosition}");
                        }
                    }
                }
                
                // 绘制Tilemap边界
                Vector3 min = tilemap.CellToWorld(new Vector3Int(bounds.xMin, bounds.yMin, 0));
                Vector3 max = tilemap.CellToWorld(new Vector3Int(bounds.xMax, bounds.yMax, 0));
                Vector3 center = (min + max) / 2f;
                Vector3 size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), 0.1f);
                
                Handles.color = Color.yellow;
                Handles.DrawWireCube(center, size);
                
                // 显示Tilemap信息
                Handles.color = Color.white;
                Handles.Label(center, $"Tilemap边界\n尺寸: {size.x:F2}x{size.y:F2}\n格子大小: {tilemap.cellSize.x:F2}x{tilemap.cellSize.y:F2}");
                
                // 添加帮助信息
                Handles.BeginGUI();
                GUI.color = Color.white;
                GUILayout.BeginArea(new Rect(10, 10, 250, 120));
                GUILayout.Label("Tilemap标注工具说明:");
                GUILayout.Label("- 绿色方框表示有Tile的格子");
                GUILayout.Label("- 黄色方框表示Tilemap边界");
                GUILayout.Label("- 点击标注点可选中Tilemap对象");
                GUILayout.Label($"- 自动刷新: {(autoRefresh ? "已启用" : $"每{refreshInterval:F1}秒刷新")}");
                GUILayout.Label($"- 当前时间: {Time.realtimeSinceStartup:F2}秒");
                GUILayout.Label($"- 最后一次刷新: {lastRefreshTime:F2}秒");
                GUILayout.EndArea();
                Handles.EndGUI();
            }
        }
    }
}