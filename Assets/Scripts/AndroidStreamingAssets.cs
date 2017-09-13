using UnityEngine;
using System.IO;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

public static class AndroidStreamingAssets
{
    public const string StreamingAssetsDirectory = "assets/";
    public const string StreamingAssetsInternalDataDirectory = "assets/bin/";

    public static void Extract(string targetPath)
    {
        if (!Directory.Exists(targetPath))
            Directory.CreateDirectory(targetPath);

        if (targetPath [targetPath.Length - 1] != '/' || targetPath [targetPath.Length - 1] != '\\')
            targetPath += '/';

        HashSet<string> createdDirectories = new HashSet<string>();

        ZipFile zf = null;
        try {
            using (FileStream fs = File.OpenRead(Application.dataPath)) {
                zf = new ZipFile(fs);
                foreach (ZipEntry zipEntry in zf) {
                    if (!zipEntry.IsFile)
                        continue;

                    string name = zipEntry.Name;
                    if (name.StartsWith(StreamingAssetsDirectory) && !name.StartsWith(StreamingAssetsInternalDataDirectory)) {
                        name = name.Replace(StreamingAssetsDirectory, string.Empty);
                        string relativeDir = System.IO.Path.GetDirectoryName(name);
                        if (!createdDirectories.Contains(relativeDir)) {
                            Directory.CreateDirectory(targetPath + relativeDir);
                            createdDirectories.Add(relativeDir);
                        }

                        byte[] buffer = new byte[4096];
                        using (Stream zipStream = zf.GetInputStream(zipEntry))
                        using (FileStream streamWriter = File.Create(targetPath + name)) {
                            StreamUtils.Copy(zipStream, streamWriter, buffer);
                        }
                    }
                }
            }
        } finally {
            if (zf != null) {
                zf.IsStreamOwner = true;
                zf.Close();
            }
        }
    }
}