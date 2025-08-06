using System.Drawing.Design;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class EnemiesEditor : OdinMenuEditorWindow
{
    public EnemySettingConfig enemySettingConfig;
    [MenuItem("Window/编辑器/敌人配置及路径编辑器")]
    private static void OpenWindow()
    {
        GetWindow<EnemiesEditor>().Show();
        GetWindow<EnemiesEditor>().titleContent = new GUIContent("敌人配置及路径编辑器");
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        tree.Selection.SupportsMultiSelect = true;
        enemySettingConfig = AssetDatabase.LoadAssetAtPath<EnemySettingConfig>("Assets/Setting/EnemySettingConfig.asset");
        if (enemySettingConfig == null)
        {
            enemySettingConfig = ScriptableObject.CreateInstance<EnemySettingConfig>();
            AssetDatabase.CreateAsset(enemySettingConfig, "Assets/Setting/EnemySettingConfig.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        tree.Add("设置", enemySettingConfig);
        tree.Add("新建", new EnemyDataComboView(enemySettingConfig));
        string enemyPath = enemySettingConfig.savePath;
        string[] guids = AssetDatabase.FindAssets("t:EnemyData", new string[] { enemyPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemyData enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            tree.Add($"敌人数据/{enemyData.EnemyName}{enemyData.GetInstanceID()}", enemyData);
        }
        return tree;
    }
    
}

public class EnemySettingConfig : ScriptableObject
{
    public string savePath = "Assets/Resources/Data/Enemy";
}

public class EnemyDataComboView
{
    [InlineEditor(InlineEditorModes.GUIOnly, InlineEditorObjectFieldModes.Hidden)]
    public EnemyData enemyData;
    
    private EnemySettingConfig enemySettingConfig;

    public EnemyDataComboView(EnemySettingConfig _config)
    {
        enemySettingConfig = _config;
        enemyData = ScriptableObject.CreateInstance<EnemyData>();
    }
    [Button()]
    public void Add()
    {
        AssetDatabase.CreateAsset(enemyData, enemySettingConfig.savePath + $"Enemy{GetEnemyID()}.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    public int GetEnemyID()
    {
        return AssetDatabase.LoadAllAssetsAtPath(enemySettingConfig.savePath).Length;
    }
}
