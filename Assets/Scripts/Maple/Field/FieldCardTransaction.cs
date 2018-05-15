using System;

namespace Maple.Field
{
    public class FieldCardTransaction
    {
        public delegate WeakReference<IReadOnlyFieldCardBox> FieldCardProcedure(
            MapleFieldContext Context,
            IReadOnlyFieldCardBox FieldCard);


        public MapleFieldContext Context;

        public IReadOnlyFieldCardBox FieldCard;


        public TransactionHandle Handle { get; }

        public bool IsComplete { get; private set; }

        FieldCardProcedure Procedure { get; }


        public FieldCardTransaction(FieldCardProcedure procedure)
        {
            Procedure = procedure;
            Handle = new TransactionHandle(this);
        }


        public WeakReference<IReadOnlyFieldCardBox> Execute()
        {
            var result = Procedure(Context, FieldCard);
            IsComplete = true;

            return result;
        }


        public class TransactionHandle
        {
            public WeakReference<IReadOnlyFieldCardBox> Result { get; private set; }

            public bool IsComplete => Transaction.IsComplete;

            FieldCardTransaction Transaction { get; }


            public TransactionHandle(FieldCardTransaction transaction)
            {
                Transaction = transaction;
            }


            public void SetComplete(
                WeakReference<IReadOnlyFieldCardBox> result) =>
                Result = result;
        }
    }
}
