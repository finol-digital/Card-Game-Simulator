namespace Maple.Field
{
    public class FieldCardBox
        :IFieldCardReader
    {
        public string CardDefinitionId { get; }

        public FieldGridData GridRecord { get; set; }


        public FieldCardBox(string cardDefinitionId)
        {
            CardDefinitionId = cardDefinitionId;
        }


        string IFieldCardReader.ReadCardDefinitionId() =>
            CardDefinitionId;


        FieldGridData IFieldCardReader.ReadGridRecord() =>
            GridRecord;
    }
}
