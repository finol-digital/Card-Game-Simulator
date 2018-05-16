using System;

namespace Maple.Field
{
    public class FieldCardTransaction
    {
        public delegate WeakReference<IFieldCardReader> FieldCardProcedure(
            MapleFieldContext Context,
            IFieldCardReader FieldCard);


        public MapleFieldContext Context;

        public IFieldCardReader FieldCard;


        public TransactionHandle Handle { get; }

        public bool IsComplete { get; private set; }

        FieldCardProcedure Procedure { get; }


        public FieldCardTransaction(FieldCardProcedure procedure)
        {
            Procedure = procedure;
            Handle = new TransactionHandle(this);
        }


        public WeakReference<IFieldCardReader> Execute()
        {
            var result = Procedure(Context, FieldCard);
            IsComplete = true;

            return result;
        }


        public class TransactionHandle
        {
            public WeakReference<IFieldCardReader> Result { get; private set; }

            public bool IsComplete => Transaction.IsComplete;

            FieldCardTransaction Transaction { get; }


            public TransactionHandle(FieldCardTransaction transaction)
            {
                Transaction = transaction;
            }


            public void SetComplete(
                WeakReference<IFieldCardReader> result) =>
                Result = result;
        }
    }
}
