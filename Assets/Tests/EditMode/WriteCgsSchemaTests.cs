using System.IO;
using Cgs.Editor;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class WriteCgsSchemaTests
    {
        [Test]
        public void WriteCgsSchemaTestsSimplePasses()
        {
            string prev = File.ReadAllText(WriteCgsSchema.SchemaFilePath);
            WriteCgsSchema.WriteCardGameDef();
            string current = File.ReadAllText(WriteCgsSchema.SchemaFilePath);
            Assert.AreEqual(prev, current);
        }
    }
}
