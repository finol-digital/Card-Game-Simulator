/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.IO;
using CardGameDef;
using NJsonSchema;
using NJsonSchema.Generation;
using UnityEditor;
using UnityEngine;

namespace Cgs.Editor
{
    public static class WriteCgsSchema
    {
        public static string SchemaFilePath => Application.dataPath.Remove(Application.dataPath.Length - 6, 6)
                                               + "docs/schema/CardGameDef.json";

        private const string SchemaId = "https://cardgamesim.finoldigital.com/schema/CardGameDef.json";
        private const string SchemaTitle = "CGS Custom Game";
        private const string SchemaDescription = "A custom card game definition to be used within Card Game Simulator";

        [MenuItem("CGS/Write CardGameDef.json")]
        public static void WriteCardGameDef()
        {
            Debug.Log("Writing CardGameDef.json...");
            var settings = new JsonSchemaGeneratorSettings
            {
                DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
                SerializerSettings = CardGame.SerializerSettings
            };
            JsonSchema schema = JsonSchema.FromType<CardGame>(settings);
            schema.Id = SchemaId;
            schema.Title = SchemaTitle;
            schema.Description = SchemaDescription;
            // HACK: cardImageUrl uses a custom implementation of uri-template to allow for more versatility
            schema.Properties["cardImageUrl"].Format = "uri-template";
            File.WriteAllText(SchemaFilePath, schema.ToJson());
            Debug.Log($"Schema written to {SchemaFilePath}!");
        }
    }
}
