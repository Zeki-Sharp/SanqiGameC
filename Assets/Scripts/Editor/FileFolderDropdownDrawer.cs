#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[DrawerPriority(0, 0, 2000)]
public class FileFolderDropdownDrawer : OdinAttributeDrawer<FileFolderDropdownAttribute, string>
{
    private string[] names;
    private string[] displayNames;

    protected override void Initialize()
    {
        string absFolder = Path.Combine(Application.dataPath, Attribute.FolderRelativeToDataPath);
        if (!Directory.Exists(absFolder))
        {
            names = new[] { "<FolderNotFound>" };
            displayNames = names;
            return;
        }

        var files = Directory.GetFiles(absFolder, Attribute.Pattern, SearchOption.TopDirectoryOnly)
            .Where(f => !f.EndsWith(".meta"))
            .ToArray();

        names = files.Select(f => Attribute.WithoutExtension ? Path.GetFileNameWithoutExtension(f) : Path.GetFileName(f))
            .ToArray();

        displayNames = Attribute.ChineseName
            ? names.Select(n => n.Replace("_", " ")).ToArray() // 简单替换
            : names;
    }

    protected override void DrawPropertyLayout(GUIContent label)
    {
        var rect = EditorGUILayout.GetControlRect();
        if (label != null && label.text != "")
            rect = EditorGUI.PrefixLabel(rect, label);

        int currentIndex = System.Array.IndexOf(names, ValueEntry.SmartValue);
        if (currentIndex < 0) currentIndex = 0;

        int newIndex = EditorGUI.Popup(rect, currentIndex, displayNames);
        if (newIndex != currentIndex)
            ValueEntry.SmartValue = names[newIndex];
    }
}
#endif