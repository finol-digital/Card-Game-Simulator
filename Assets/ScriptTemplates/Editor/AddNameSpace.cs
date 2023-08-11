using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AddNameSpace : AssetModificationProcessor
{
    public static void OnWillCreateAsset(string metaFilePath)
    {
        if (!metaFilePath.Contains(".meta"))
            return;

        var filePath = metaFilePath.Replace(".meta", "");
        var index = filePath.LastIndexOf(".", StringComparison.Ordinal);
        if (index < 0)
            return;

        var fileExtension = filePath[index..];
        if (fileExtension != ".cs")
            return;

        index = Application.dataPath.LastIndexOf("Assets", StringComparison.Ordinal);
        if (index < 0)
            return;

        filePath = Application.dataPath[..index] + filePath;
        index = filePath.IndexOf("Scripts/", StringComparison.Ordinal);
        if (index < 0)
            return;

        var lastPartOfPath = filePath[(index + 8)..];
        var nameSpace = lastPartOfPath[..lastPartOfPath.LastIndexOf('/')];
        nameSpace = nameSpace.Replace('/', '.');

        var fileContents = File.ReadAllText(filePath);
        fileContents = fileContents.Replace("#NAMESPACE#", nameSpace);

        File.WriteAllText(filePath, fileContents);
        AssetDatabase.Refresh();
    }
}
