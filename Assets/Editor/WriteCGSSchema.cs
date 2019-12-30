using System.IO;
using UnityEngine;
using UnityEditor;
using NJsonSchema;
using NJsonSchema.Generation;

using CardGameDef;

class WriteCGSSchema
{
    public const string SchemaId = "https://cardgamesim.finoldigital.com/schema/CardGameDef.json";
    public const string SchemaTitle = "CGS Custom Game";
    public const string SchemaDescription = "A custom card game definition to be used within Card Game Simulator";

    public static string SchemaFilePath => Application.dataPath.Remove(Application.dataPath.Length - 6, 6)
        + "docs/schema/CardGameDef.json";

    [MenuItem("CGS/Write CardGameDef.json")]
    static void CardGameDef()
    {
        Debug.Log("Writing CardGameDef.json...");
        var settings = new JsonSchemaGeneratorSettings();
        settings.DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;
        settings.SerializerSettings = CardGame.SerializerSettings;
        var schema = JsonSchema.FromType<CardGame>(settings);
        schema.Id = SchemaId;
        schema.Title = SchemaTitle;
        schema.Description = SchemaDescription;
        // HACK: cardImageUrl uses a custom implementation of uri-template to allow for more versatility
        schema.Properties["cardImageUrl"].Format = "uri-template";
        File.WriteAllText(SchemaFilePath, schema.ToJson());
        Debug.Log($"Schema written to {SchemaFilePath}!");
    }
}
