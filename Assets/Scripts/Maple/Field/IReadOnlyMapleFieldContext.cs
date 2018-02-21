using System;
using System.Collections.Generic;

namespace Maple.Field
{
    public interface IReadOnlyMapleFieldContext
    {
        IReadOnlyDictionary<Guid, IReadOnlyFieldCardContainer> FieldCardStore { get; }
    }
}
