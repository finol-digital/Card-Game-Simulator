namespace Maple.Field
{
    public interface IReadOnlyFieldCardContainer
    {
        int CardDescriptionKey { get; }
        FieldGridElement GridRecord { get; }
    }
}
