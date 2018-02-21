using System.Collections.Generic;
using Maple.Field;

namespace Maple
{
    public interface IReadOnlyMapleContext
    {
        IReadOnlyList<CardDescription> CardDescriptions { get; }
        IReadOnlyMapleFieldContext FieldContext { get; }
    }
}
