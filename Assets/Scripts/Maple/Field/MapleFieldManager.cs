using UnityEngine;

namespace Maple.Field
{
    public class MapleFieldManager
        : MonoBehaviour, IMapleField
    {
        public IReadOnlyMapleContext RootContext;
        public MapleFieldContext FieldContext;


        void Start()
        {
            var FieldCardPresentersMngr =
                gameObject.AddComponent<FieldCardPresentersManager>();
            FieldCardPresentersMngr.RootContext = RootContext;
        }


        void Update() => ExecuteAPICalls();


        /// <summary>
        /// Consumes stream of Field API calls,
        /// and attempts to execute each call.
        /// </summary>
        void ExecuteAPICalls()
        {
            FieldCardTransaction call;

            while (FieldContext.FieldCardTransactionStream.TryTake(out call))  // FIXME: throttle consumption rate / move to background thread
            {
                // Inject current context into call

                call.Context = FieldContext;


                // Execute call

                try
                {
                    call.Execute();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogWarning(
                        "Exception thrown during call execution. "
                        + "Proceeding to execute remaining calls");
                }
            }
        }


        public FieldCardTransaction.TransactionHandle SpawnFieldCard(
            int cardDefinitionId) =>
            FieldContext.PushSpawnFieldCardTransaction(cardDefinitionId);
    }
}
