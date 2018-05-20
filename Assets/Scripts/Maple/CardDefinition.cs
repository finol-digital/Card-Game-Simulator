using UnityEngine;

namespace Maple
{
    public struct CardDefinition
    {
        public string Id { get; }

        /// <summary>
        /// Card width in meters. Defaults to 0.0635 meters (2.5")
        /// </summary>
        public float Width { get; }

        /// <summary>
        /// Card height in meters. Defaults to 0.0889 meters (3.5")
        /// </summary>
        public float Height { get; }

        public Sprite FrontFace { get; }

        public CardDefinition(string id, Sprite frontFace)
            : this(id, 0.0635f, 0.0889f, frontFace) { }

        public CardDefinition(
            string id,
            float width,
            float height,
            Sprite frontFace)
        {
            Id = id;
            Width = width;
            Height = height;
            FrontFace = frontFace;
        }
    }
}
