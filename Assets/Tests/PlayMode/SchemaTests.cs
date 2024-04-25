using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityExtensionMethods;

namespace Tests.PlayMode
{
    public class SchemaTests
    {
        [Test]
        public void ValidatedGames()
        {
            var streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            foreach (var directory in Directory.EnumerateDirectories(streamingAssetsPath))
            {
                var fileName = Path.GetFileName(directory);
                var name = fileName[..fileName.IndexOf('@')];
                if ("Standard Playing Cards".Equals(name))
                    name = "Standard";

                var streamingDirectoryInfo = new DirectoryInfo(directory);
                var docsDirectoryInfo = new DirectoryInfo(
                    Application.dataPath.Remove(Application.dataPath.Length - 6, 6)
                    + $"docs/games/{name}");

                var streamingFiles =
                    streamingDirectoryInfo.GetFiles("*.*", SearchOption.AllDirectories)
                        .Where(file =>
                            !file.Name.EndsWith(UnityFileMethods.MetaExtension) &&
                            !file.Name.Contains("Standard Playing Cards.json"));
                IEnumerable<FileInfo> docsFiles = docsDirectoryInfo.GetFiles("*.*", SearchOption.AllDirectories);

                // A custom file comparer defined below
                var fileCompare = new FileCompare();
                var missingFiles =
                    (from file in streamingFiles select file).Except(docsFiles, fileCompare);
                Assert.IsEmpty(missingFiles);
            }
        }

        private class FileCompare : IEqualityComparer<FileInfo>
        {
            public bool Equals(FileInfo f1, FileInfo f2)
            {
                return f1 != null && f2 != null && f1.Name == f2.Name && f1.Length == f2.Length;
            }

            public int GetHashCode(FileInfo fi)
            {
                var s = $"{fi.Name}{fi.Length}";
                return s.GetHashCode();
            }
        }
    }
}
