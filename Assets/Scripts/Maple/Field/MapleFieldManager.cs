using UnityEngine;

namespace Maple.Field
{
    public class MapleFieldManager
        : MonoBehaviour, IMapleField
    {
        /// <summary>
        /// The injected parent Maple context.
        /// </summary>
        public IReadOnlyMapleContext RootContext;

        /// <summary>
        /// The injected context of this subsystem.
        /// </summary>
        public MapleFieldContext Context;


        void Start()
        {
            var presentersMngr =
                gameObject.AddComponent<FieldCardPresentersManager>();

            presentersMngr.RootContext = RootContext;
        }


        void Update() => ExecuteAPICalls();


        /// <summary>
        /// Consumes stream of Field API calls,
        /// and attempts to execute each call.
        /// </summary>
        void ExecuteAPICalls()
        {
            FieldCardTransaction call;

            while (Context.FieldCardTransactionStream.TryTake(out call))  // FIXME: throttle consumption rate / move to background thread
            {
                // Inject current context into call

                call.Context = Context;


                // Execute call

                try
                {
                    call.Execute();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogWarning(
                        "Exception thrown during call execution."
                        + " Proceeding to execute remaining calls");
                }
            }
        }


        public FieldCardTransaction.TransactionHandle SpawnFieldCard(
            int cardDefinitionId) =>
            Context.PushSpawnFieldCardTransaction(cardDefinitionId);
    }
}
