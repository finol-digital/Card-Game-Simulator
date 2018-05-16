namespace Maple.Field
{
    public interface IFieldCardReader
    {
        int ReadCardDefinitionKey();
        FieldGridData ReadGridRecord();
    }
}
