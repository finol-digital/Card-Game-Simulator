using System.Collections.Generic;
using Maple.Field;

namespace Maple
{
    public class MapleContext
        : IReadOnlyMapleContext
    {
        /// <summary>
        /// Holds immutable reference information for cards.
        /// Guaranteed to never change for the lifetime of this context.
        /// </summary>
        public IReadOnlyList<CardDefinition> CardDefinitions { get; } =
            new List<CardDefinition>();


        public MapleFieldContext FieldSubContext { get; } =
            new MapleFieldContext();


        public IReadOnlyMapleFieldContext FieldContext
        {
            get { return (IReadOnlyMapleFieldContext)FieldSubContext; }
        }


        public MapleContext(IEnumerable<CardDefinition> cardDefinitions)
        {
            // Populate Card Definitions table

            var cardDefinitionsCopyBuffer = new List<CardDefinition>();

            foreach (var cardDefinition in cardDefinitions)
                cardDefinitionsCopyBuffer.Add(cardDefinition);

            CardDefinitions = cardDefinitionsCopyBuffer;
        }
    }
}
