namespace Maple.Field
{
    public interface IReadOnlyFieldCardBox
    {
        int CardDefinitionKey { get; }
        FieldGridData GridRecord { get; }
    }
}
