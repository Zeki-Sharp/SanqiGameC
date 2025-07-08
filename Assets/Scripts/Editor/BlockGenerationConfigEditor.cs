/*using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BlockGenerationConfig))]
public class BlockGenerationConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BlockGenerationConfig config = (BlockGenerationConfig)target;

        EditorGUILayout.LabelField("4x4 Block Grid", EditorStyles.boldLabel);

        // 获取当前数组状态
        bool[,] grid = config.BlockGrid;

        for (int y = 0; y < 4; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < 4; x++)
            {
                // 设置按钮样式
                string label = grid[y, x] ? "■" : "□";
                GUILayoutOption width = GUILayout.Width(30);
                if (GUILayout.Button(label, width))
                {
                    // 切换值
                    Undo.RecordObject(config, "Toggle Grid Cell");
                    grid[y, x] = !grid[y, x];
                    config.BlockGrid = grid; // 更新回配置对象
                    EditorUtility.SetDirty(config);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // 保存修改回原始数组（已直接更新）
    }
}*/