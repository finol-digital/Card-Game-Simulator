using System.Collections.Generic;
using Maple.Field;

namespace Maple
{
    public interface IReadOnlyMapleContext
    {
        IReadOnlyList<CardDefinition> CardDefinitions { get; }
        IReadOnlyMapleFieldContext FieldContext { get; }
    }
}
