namespace Maple.Field
{
    public class FieldCardBox
        :IFieldCardReader
    {
        public int CardDefinitionKey { get; }

        public FieldGridData GridRecord { get; set; }


        public FieldCardBox(int cardDefinitionId)
        {
            CardDefinitionKey = cardDefinitionId;
        }


        int IFieldCardReader.ReadCardDefinitionKey() =>
            CardDefinitionKey;


        FieldGridData IFieldCardReader.ReadGridRecord() =>
            GridRecord;
    }
}
