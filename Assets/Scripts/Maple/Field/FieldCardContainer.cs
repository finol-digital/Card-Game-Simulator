namespace Maple.Field
{
    public class FieldCardContainer
        :IReadOnlyFieldCardContainer
    {
        public int CardDescriptionKey { get; }

        public FieldGridElement GridRecord { get; set; }

        int IReadOnlyFieldCardContainer.CardDescriptionKey =>
            CardDescriptionKey;

        FieldGridElement IReadOnlyFieldCardContainer.GridRecord =>
            GridRecord;


        public FieldCardContainer(int cardDefinitionId)
        {
            CardDescriptionKey = cardDefinitionId;
        }
    }
}
