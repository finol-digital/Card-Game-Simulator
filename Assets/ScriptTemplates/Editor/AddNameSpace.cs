using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
public class AddNameSpace : UnityEditor.AssetModificationProcessor
{
    public static void OnWillCreateAsset(string metaFilePath)
    {
        string filePath = metaFilePath.Replace(".meta", "");
        int index = filePath.LastIndexOf(".", StringComparison.Ordinal);
        if (index < 0)
            return;

        string fileExtension = filePath.Substring(index);
        if (fileExtension != ".cs")
            return;

        index = Application.dataPath.LastIndexOf("Assets", StringComparison.Ordinal);
        if (index < 0)
            return;

        filePath = Application.dataPath.Substring(0, index) + filePath;
        index = filePath.IndexOf("Scripts/", StringComparison.Ordinal);
        if (index < 0)
            return;

        string lastPartOfPath = filePath.Substring(index + 8);
        string nameSpace = lastPartOfPath.Substring(0, lastPartOfPath.LastIndexOf('/'));
        nameSpace = nameSpace.Replace('/', '.');

        string fileContents = File.ReadAllText(filePath);
        fileContents = fileContents.Replace("#NAMESPACE#", nameSpace);

        File.WriteAllText(filePath, fileContents);
        AssetDatabase.Refresh();
    }
}
