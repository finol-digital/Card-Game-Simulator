namespace Maple
{
    public struct CardDescription
    {
        public string Name_EN_US { get; }

        /// <summary>
        /// Card width in meters. Defaults to 0.0635 meters (2.5")
        /// </summary>
        public float Width { get; }

        /// <summary>
        /// Card height in meters. Defaults to 0.0889 meters (3.5")
        /// </summary>
        public float Height { get; }


        public CardDescription(string name_en_us)
            : this(name_en_us, 0.0635f, 0.0889f) { }

        public CardDescription(
            string name_en_us,
            float width,
            float height)
        {
            Name_EN_US = name_en_us;
            Width = width;
            Height = height;
        }
    }
}
