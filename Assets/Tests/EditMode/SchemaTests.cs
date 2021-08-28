using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cgs.Editor;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NUnit.Framework;
using UnityEngine;
using UnityExtensionMethods;

namespace Tests.EditMode
{
    public class SchemaTests
    {
        [Test]
        [Ignore("TODO")]
        public void UpdatedSchema()
        {
            string previous = File.ReadAllText(WriteCgsSchema.SchemaFilePath);
            WriteCgsSchema.WriteCardGameDef();
            string current = File.ReadAllText(WriteCgsSchema.SchemaFilePath);
            Assert.AreEqual(previous, current);
        }

        [Test]
        [Ignore("TODO")]
        public void ValidatedSchema()
        {
            JSchema schema = JSchema.Parse(File.ReadAllText(WriteCgsSchema.SchemaFilePath));
            string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            foreach (string directory in Directory.EnumerateDirectories(streamingAssetsPath))
            {
                string fileName = Path.GetFileName(directory);
                string name = fileName.Substring(0, fileName.IndexOf('@'));
                JToken json = JToken.Parse(File.ReadAllText(Path.Combine(directory, $"{name}.json")));
                Assert.IsTrue(json.IsValid(schema));
            }
        }

        [Test]
        [Ignore("TODO")]
        public void ValidatedGames()
        {
            string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            foreach (string directory in Directory.EnumerateDirectories(streamingAssetsPath))
            {
                string fileName = Path.GetFileName(directory);
                string name = fileName.Substring(0, fileName.IndexOf('@'));
                if ("Standard Playing Cards".Equals(name))
                    name = "Standard";

                var streamingDirectoryInfo = new DirectoryInfo(directory);
                var docsDirectoryInfo = new DirectoryInfo(
                    Application.dataPath.Remove(Application.dataPath.Length - 6, 6)
                    + $"docs/games/{name}");

                IEnumerable<FileInfo> streamingFiles =
                    streamingDirectoryInfo.GetFiles("*.*", SearchOption.AllDirectories)
                        .Where(file =>
                            !file.Name.EndsWith(UnityFileMethods.MetaExtension) &&
                            !file.Name.Contains("Standard Playing Cards.json"));
                IEnumerable<FileInfo> docsFiles = docsDirectoryInfo.GetFiles("*.*", SearchOption.AllDirectories);

                // A custom file comparer defined below
                var fileCompare = new FileCompare();
                IEnumerable<FileInfo> missingFiles =
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
