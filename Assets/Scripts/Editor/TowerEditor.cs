using PlasticGui.WorkspaceWindow.CodeReview;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class TowerEditor : OdinMenuEditorWindow
{

  [MenuItem("Tools/塔编辑器")] 
  public static void OpenWindow()
  {
    GetWindow<TowerEditor>().Show();
  }
  protected override OdinMenuTree BuildMenuTree()
  {
    var tree = new OdinMenuTree();
    tree.Selection.SupportsMultiSelect = true;
    tree.Add("Setting",  TowerSetting.Instance);

    return tree;
  }
}
[CreateAssetMenu(fileName = "TowerSetting", menuName = "Settings/TowerSetting")]
public class TowerSetting: ScriptableObject
{
  public static object Instance { get; set; }
  
}
