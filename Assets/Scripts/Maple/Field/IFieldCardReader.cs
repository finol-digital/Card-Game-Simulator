namespace Maple.Field
{
    public interface IFieldCardReader
    {
        string ReadCardDefinitionId();
        FieldGridData ReadGridRecord();
    }
}
