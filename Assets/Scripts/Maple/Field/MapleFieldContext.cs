using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Maple.Field
{
    public class MapleFieldContext
        : IReadOnlyMapleFieldContext
    {
        IReadOnlyDictionary<Guid, IReadOnlyFieldCardContainer> IReadOnlyMapleFieldContext.FieldCardStore =>
            ReadonlyFieldCardCache;


        //
        // Field Card Transaction Stream
        //

        public IProducerConsumerCollection<FieldCardTransaction> FieldCardTransactionStream { get; } =
            new ConcurrentQueue<FieldCardTransaction>();


        public FieldCardTransaction.TransactionHandle PushSpawnFieldCardTransaction(
            int cardDescriptionKey)
        {
            var spawnFieldCardTransaction = new FieldCardTransaction(
                (context, _) => new WeakReference<IReadOnlyFieldCardContainer>(
                        context.CreateFieldCard(cardDescriptionKey)));

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

        ConcurrentDictionary<Guid, FieldCardContainer> FieldCardStore { get; } =
            new ConcurrentDictionary<Guid, FieldCardContainer>();

        ConcurrentDictionary<Guid, IReadOnlyFieldCardContainer> ReadonlyFieldCardCache { get; } =
            new ConcurrentDictionary<Guid, IReadOnlyFieldCardContainer>();


        FieldCardContainer CreateFieldCard(int cardDescriptionKey)
        {
            var newFieldCard = new FieldCardContainer(cardDescriptionKey);
            var newFieldCardKey = Guid.NewGuid();  // FIXME: Must check for used IDs

            if (!FieldCardStore.TryAdd(
                    newFieldCardKey,
                    newFieldCard))
                throw new Exception();

            if (!ReadonlyFieldCardCache.TryAdd(
                    newFieldCardKey,
                    (IReadOnlyFieldCardContainer)newFieldCard))
                throw new Exception();

            return newFieldCard;
        }
    }


    public class FieldCardTransaction
    {
        public delegate WeakReference<IReadOnlyFieldCardContainer> FieldCardProcedure(
            MapleFieldContext Context,
            IReadOnlyFieldCardContainer FieldCard);


        public MapleFieldContext Context;

        public IReadOnlyFieldCardContainer FieldCard;


        public TransactionHandle Handle { get; }

        public bool IsComplete { get; private set; }

        FieldCardProcedure Procedure { get; }


        public FieldCardTransaction(FieldCardProcedure procedure)
        {
            Procedure = procedure;
            Handle = new TransactionHandle(this);
        }


        public WeakReference<IReadOnlyFieldCardContainer> Execute()
        {
            var result = Procedure(Context, FieldCard);
            IsComplete = true;

            return result;
        }


        public class TransactionHandle
        {
            public WeakReference<IReadOnlyFieldCardContainer> Result { get; private set; }

            public bool IsComplete => Transaction.IsComplete;

            FieldCardTransaction Transaction { get; }


            public TransactionHandle(FieldCardTransaction transaction)
            {
                Transaction = transaction;
            }


            public void SetComplete(
                WeakReference<IReadOnlyFieldCardContainer> result) =>
                Result = result;
        }
    }
}
