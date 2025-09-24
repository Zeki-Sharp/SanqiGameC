using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class FileFolderDropdownAttribute : PropertyAttribute
{
    public string FolderRelativeToDataPath;
    public string Pattern;
    public bool WithoutExtension;
    public bool ChineseName;

    public FileFolderDropdownAttribute(string folder, string pattern = "*.asset", bool withoutExtension = true)
    {
        FolderRelativeToDataPath = folder;
        Pattern = pattern;
        WithoutExtension = withoutExtension;
    }
}