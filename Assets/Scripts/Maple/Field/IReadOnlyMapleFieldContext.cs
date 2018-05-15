using System;
using System.Collections.Generic;

namespace Maple.Field
{
    public interface IReadOnlyMapleFieldContext
    {
        IReadOnlyDictionary<Guid, IReadOnlyFieldCardBox> FieldCardStore { get; }
    }
}
