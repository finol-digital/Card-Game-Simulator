using System.Collections.Generic;
using Maple.Field;

namespace Maple
{
    public interface IReadOnlyMapleContext
    {
        IReadOnlyDictionary<string, CardDefinition> CardDefinitionsTable { get; }
        IReadOnlyMapleFieldContext FieldContext { get; }
    }
}
