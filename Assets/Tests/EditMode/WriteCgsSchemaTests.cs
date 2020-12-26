using System.IO;
using Cgs.Editor;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class WriteCgsSchemaTests
    {
        [Test]
        public void UpdatedSchema()
        {
            string previous = File.ReadAllText(WriteCgsSchema.SchemaFilePath);
            WriteCgsSchema.WriteCardGameDef();
            string current = File.ReadAllText(WriteCgsSchema.SchemaFilePath);
            Assert.AreEqual(previous, current);
        }

        [Test]
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
    }
}
