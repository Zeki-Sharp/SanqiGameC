using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class EnemiesEditor : OdinMenuEditorWindow
{
    [MenuItem("Window/编辑器/敌人配置及路径编辑器")]
    private static void OpenWindow()
    {
        GetWindow<BlockConfigManagerEditor>().Show();
        GetWindow<BlockConfigManagerEditor>().titleContent = new GUIContent("敌人配置及路径编辑器");
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        tree.Selection.SupportsMultiSelect = true;

        return tree;
    }
}
