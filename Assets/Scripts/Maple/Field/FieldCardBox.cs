namespace Maple.Field
{
    public class FieldCardBox
        :IReadOnlyFieldCardBox
    {
        public int CardDefinitionKey { get; }

        public FieldGridData GridRecord { get; set; }

        int IReadOnlyFieldCardBox.CardDefinitionKey =>
            CardDefinitionKey;

        FieldGridData IReadOnlyFieldCardBox.GridRecord =>
            GridRecord;


        public FieldCardBox(int cardDefinitionId)
        {
            CardDefinitionKey = cardDefinitionId;
        }
    }
}
