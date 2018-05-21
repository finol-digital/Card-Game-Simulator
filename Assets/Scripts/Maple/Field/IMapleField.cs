namespace Maple.Field
{
    public interface IMapleField
    {
        FieldCardTransaction.TransactionHandle SpawnFieldCard(
            string cardDefinitionId);
    }
}
