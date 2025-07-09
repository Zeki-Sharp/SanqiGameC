using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class BlockConfigManagerEditor : OdinMenuEditorWindow
{
    [MenuItem("Tools/方块配置管理器")]
    private static void OpenWindow()
    {
        GetWindow<BlockConfigManagerEditor>().Show();
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();

        // 添加创建按钮
        tree.Add("创建新配置", new CreateNode());

        // 加载所有 BlockGenerationConfig 资源
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(BlockGenerationConfig)}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BlockGenerationConfig config = AssetDatabase.LoadAssetAtPath<BlockGenerationConfig>(path);
            if (config != null)
            {
                tree.Add(path, config);
            }
        }

        return tree;
    }

    private class CreateNode
    {
        [BoxGroup("新建配置")]
        [LabelText("配置名称")]
        public string ConfigName = "NewBlockConfig";

        [BoxGroup("新建配置")]
        [Button("创建")]
        public void Create()
        {
            if (string.IsNullOrWhiteSpace(ConfigName))
            {
                Debug.LogWarning("配置名称不能为空");
                return;
            }

            // 创建 ScriptableObject 实例
            BlockGenerationConfig config = ScriptableObject.CreateInstance<BlockGenerationConfig>();

            // 保存到项目目录（默认放在 Assets 目录下）
            string assetPath = $"Assets/Resources/Data/Blocks/{ConfigName}.asset";
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"成功创建新配置: {assetPath}");
        }
    }
}