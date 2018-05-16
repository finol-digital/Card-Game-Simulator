using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Maple.Field
{
    public class MapleFieldContext
        : IReadOnlyMapleFieldContext
    {
        IReadOnlyDictionary<Guid, IFieldCardReader> IReadOnlyMapleFieldContext.FieldCardStore =>
            ReadonlyFieldCardCache;


        //
        // Field Card Transaction Stream
        //

        public IProducerConsumerCollection<FieldCardTransaction> FieldCardTransactionStream { get; } =
            new ConcurrentQueue<FieldCardTransaction>();


        public FieldCardTransaction.TransactionHandle PushSpawnFieldCardTransaction(
            int cardDefinitionKey)
        {
            var spawnFieldCardTransaction = new FieldCardTransaction(
                (context, _) => new WeakReference<IFieldCardReader>(
                        context.CreateFieldCard(cardDefinitionKey)));

            PushFieldCardTransaction(spawnFieldCardTransaction);

            return spawnFieldCardTransaction.Handle;
        }


        void PushFieldCardTransaction(FieldCardTransaction transaction)
        {
            if (!FieldCardTransactionStream.TryAdd(transaction))
                throw new Exception();
        }


        //
        // Field Card Store
        //

        ConcurrentDictionary<Guid, FieldCardBox> FieldCardStore { get; } =
            new ConcurrentDictionary<Guid, FieldCardBox>();

        ConcurrentDictionary<Guid, IFieldCardReader> ReadonlyFieldCardCache { get; } =
            new ConcurrentDictionary<Guid, IFieldCardReader>();


        FieldCardBox CreateFieldCard(int cardDefinitionKey)
        {
            var newFieldCard = new FieldCardBox(cardDefinitionKey);
            var newFieldCardKey = Guid.NewGuid();  // FIXME: Must check for used IDs

            if (!FieldCardStore.TryAdd(
                    newFieldCardKey,
                    newFieldCard))
                throw new Exception();

            if (!ReadonlyFieldCardCache.TryAdd(
                    newFieldCardKey,
                    (IFieldCardReader)newFieldCard))
                throw new Exception();

            return newFieldCard;
        }
    }
}
