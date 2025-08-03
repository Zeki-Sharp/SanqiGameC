using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class BlockConfigManagerEditor : OdinMenuEditorWindow
{
    public BlockSetting Setting;
    [MenuItem("Windows/编辑器/方块配置管理器")]
    private static void OpenWindow()
    {
        GetWindow<BlockConfigManagerEditor>().Show();
        GetWindow<BlockConfigManagerEditor>().titleContent = new GUIContent("方块配置管理器");
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        tree.Selection.SupportsMultiSelect = true;
        Setting = AssetDatabase.LoadAssetAtPath<BlockSetting>("Assets/Setting/BlockSetting");
        if (Setting == null)
        {
            Setting = ScriptableObject.CreateInstance<BlockSetting>();
            AssetDatabase.CreateAsset(Setting, "Assets/Setting/BlockSetting.asset");
        }
        tree.Add("设置", Setting);
        // 添加创建按钮
        tree.Add("创建新配置", new BlockComboView(Setting));

        // 加载所有 BlockGenerationConfig 资源
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(BlockGenerationConfig)}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BlockGenerationConfig config = AssetDatabase.LoadAssetAtPath<BlockGenerationConfig>(path);
            if (config != null)
            {
                tree.Add("方块配置/"+config.ShapeName,config);
            }
        }

        return tree;
    }
    [CreateAssetMenu(fileName = "BlockSetting", menuName = "Tower Defense/Block/BlockSetting")]
    public class BlockSetting: ScriptableObject
    {
        public string BlockSettingPath = "Assets/Resources/Data/Block";
    }
    private class BlockComboView
    {
        [InlineEditor(InlineEditorModes.GUIOnly, InlineEditorObjectFieldModes.Hidden)]
        public BlockGenerationConfig config;
        private BlockSetting _blockSetting;
        [Button("创建",ButtonStyle.CompactBox)]
        public void Create()
        {

            // 保存到项目目录（默认放在 Assets 目录下）
            string assetPath = $"{_blockSetting.BlockSettingPath}/{GetBlockID()}.asset";
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"成功创建新配置: {assetPath}");
        }
        public BlockComboView(BlockSetting blockSetting)
        {
            config = ScriptableObject.CreateInstance<BlockGenerationConfig>();
            _blockSetting = blockSetting;
        }
        public int GetBlockID()
        {
            string[] guids = AssetDatabase.FindAssets("t:BlockGenerationConfig", new string[]
            {
                _blockSetting.BlockSettingPath
            });
            return guids.Length;
        }
    }
}