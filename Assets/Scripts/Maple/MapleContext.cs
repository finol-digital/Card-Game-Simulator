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
        public IReadOnlyList<CardDescription> CardDescriptions { get; } =
            new List<CardDescription>();


        public MapleFieldContext FieldSubContext { get; } =
            new MapleFieldContext();


        public IReadOnlyMapleFieldContext FieldContext
        {
            get { return (IReadOnlyMapleFieldContext)FieldSubContext; }
        }


        public MapleContext(IEnumerable<CardDescription> cardDefinitions)
        {
            // Populate Card Definitions table

            var cardDefinitionsCopyBuffer = new List<CardDescription>();

            foreach (var cardDefinition in cardDefinitions)
                cardDefinitionsCopyBuffer.Add(cardDefinition);

            CardDescriptions = cardDefinitionsCopyBuffer;
        }
    }
}
