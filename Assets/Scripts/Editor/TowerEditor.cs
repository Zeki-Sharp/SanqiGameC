using System;
using PlasticGui.WorkspaceWindow.CodeReview;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class TowerEditor : OdinMenuEditorWindow
{
    private TowerSetting _towerSetting;

    private void Awake()
    {
        GetWindow<BlockConfigManagerEditor>().titleContent = new GUIContent("塔编辑器");
    }
    [MenuItem("Windows/编辑器/塔编辑器")]
    public static void OpenWindow()
    {
        GetWindow<TowerEditor>().Show();

    }
    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        tree.Selection.SupportsMultiSelect = true;
        TowerSetting setting = AssetDatabase.LoadAssetAtPath<TowerSetting>("Assets/Setting/TowerSetting");
        if (setting == null)
        {
            setting = ScriptableObject.CreateInstance<TowerSetting>();
            AssetDatabase.CreateAsset(setting, "Assets/Setting/TowerSetting.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        tree.Add("设置", setting);
        _towerSetting = setting;
        tree.Add("新建", AddNewTowerData());
        // tree.AddAllAssetsAtPath("塔数据组", setting.TowerDataPath, typeof(TowerData), true, true);
        string[] guids = AssetDatabase.FindAssets("t:TowerData", new string[] { setting.TowerDataPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TowerData data = AssetDatabase.LoadAssetAtPath<TowerData>(path);
            if (data != null)
            {
                tree.Add($"塔数据组/{data.TowerName}", data);
            }
        }
        return tree;
    }
    private TowerData newData;
    public object AddNewTowerData()
    {
        var ComboView = new TowerComboView(_towerSetting);
        return ComboView;
    }
   
    public int GetTowerID()
    {
        return AssetDatabase.LoadAllAssetsAtPath(_towerSetting.TowerDataPath).Length;
    }
}
public class TowerComboView
{
    [InlineEditor(InlineEditorModes.GUIOnly,InlineEditorObjectFieldModes.Hidden)]
    public TowerData towerData;
    private TowerSetting _towerSetting;

    public TowerComboView(TowerSetting towerSetting)
    {
        towerData = ScriptableObject.CreateInstance<TowerData>();
        _towerSetting = towerSetting;
    }
    [Button(ButtonSizes.Large)]
    public void SaveTowerData()
    {
        AssetDatabase.CreateAsset(towerData, _towerSetting.TowerDataPath + "Tower" + GetTowerID() + ".asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    public int GetTowerID()
    {
        return AssetDatabase.LoadAllAssetsAtPath(_towerSetting.TowerDataPath).Length;
    }
}