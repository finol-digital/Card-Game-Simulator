using UnityEngine;
using Maple.Field;

namespace Maple
{
    public class MapleManager
        : MonoBehaviour, IMaple, IMapleField
    {
        public MapleContext Context;


        public IMapleField Field
        {
            /*
                HACK: Imposter!!

                    Use self as an IMapleField instance to handle API calls when
                    the child Field Manager is not available, in order to
                    remain reliable.

                    Notably, this resolves a valid condition that occurs when
                    a MapleManager component has been added, and is
                    immediately messaged (before this instance's Start() runs).
            */
            get { return _field != null ? _field : (IMapleField)this; }
        }

        private IMapleField _field;


        void Start()
        {
            var FieldMngr = gameObject.AddComponent<MapleFieldManager>();
            FieldMngr.RootContext = (IReadOnlyMapleContext)Context;
            FieldMngr.Context = Context.FieldSubContext;

            _field = (IMapleField)FieldMngr;
        }


        FieldCardTransaction.TransactionHandle IMapleField.SpawnFieldCard(
            string cardDefinitionId) =>
            Context.FieldSubContext.PushSpawnFieldCardTransaction(
                cardDefinitionId);
    }
}
