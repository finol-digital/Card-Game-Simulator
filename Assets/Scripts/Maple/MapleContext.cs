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
        public IReadOnlyDictionary<string, CardDefinition> CardDefinitionsTable { get; }


        public MapleFieldContext FieldSubContext { get; } =
            new MapleFieldContext();


        public IReadOnlyMapleFieldContext FieldContext
        {
            get { return (IReadOnlyMapleFieldContext)FieldSubContext; }
        }


        /// <summary>
        /// Instantiate MapleContext with the Card Definitions Table as
        /// a deep clone of the 'cardDefsBase' argument.
        /// </summary>
        public MapleContext(IDictionary<string, CardDefinition> cardDefinitionsBase)
        {
            var cardDefinitionsCopyBuffer =
                new Dictionary<string, CardDefinition>(cardDefinitionsBase);

            CardDefinitionsTable = cardDefinitionsCopyBuffer;
        }
    }
}
