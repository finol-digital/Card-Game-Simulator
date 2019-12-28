using System.IO;
using UnityEngine;
using UnityEditor;
using NJsonSchema;

using CardGameDef;

class WriteJsonSchema
{
    [MenuItem("CGS/Write Schema for CardGameDef")]
    static void CardGameDef()
    {
        Debug.Log("Doing Something...");
        var jsonSchema = JsonSchema.FromType<CardGame>();
        File.WriteAllText(Application.dataPath + "schema.json", jsonSchema.ToJson());
    }
}
