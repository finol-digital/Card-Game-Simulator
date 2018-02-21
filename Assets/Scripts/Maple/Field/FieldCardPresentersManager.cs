using System;
using System.Collections.Generic;
using UnityEngine;

namespace Maple.Field
{
    public class FieldCardPresentersManager
        : MonoBehaviour
    {
        public IReadOnlyMapleContext RootContext;


        public IReadOnlyMapleFieldContext FieldContext =>
            RootContext.FieldContext;

        /// <summary>
        /// Child presenters. The key is the field card store key of
        /// the card being presented.
        /// </summary>
        IDictionary<Guid, FieldCardPresenter> Children { get; } =
            new Dictionary<Guid, FieldCardPresenter>();

        /// <summary>
        /// Tracking information buffer for storing discovered new field cards.
        /// </summary>
        Queue<Tuple<Guid, WeakReference<IReadOnlyFieldCardContainer>>> newFieldCardInfos  { get; } =
            new Queue<Tuple<Guid, WeakReference<IReadOnlyFieldCardContainer>>>();

        /// <summary>
        /// Tracking information Buffer for storing discovered deleted field cards.
        /// </summary>
        Queue<Guid> deletedFieldCardKeys { get; } =
            new Queue<Guid>();


        void Update()
        {
            // Update tracking information

            // - Discover new field card info

            foreach (var fieldCardEntry in FieldContext.FieldCardStore)
                if (!Children.ContainsKey(fieldCardEntry.Key))
                    newFieldCardInfos.Enqueue(
                        new Tuple<Guid, WeakReference<IReadOnlyFieldCardContainer>>(
                            fieldCardEntry.Key,
                            new WeakReference<IReadOnlyFieldCardContainer>(
                                fieldCardEntry.Value)));


            // - Discover deleted field info

            foreach (var fieldCardKey in Children.Keys)
                if (!FieldContext.FieldCardStore.ContainsKey(fieldCardKey))
                    deletedFieldCardKeys.Enqueue(fieldCardKey);


            // Spawn child Field Card Presenters

            foreach (var info in newFieldCardInfos)
                SpawnCardPresenter(
                    fieldCardKey: info.Item1,
                    fieldCardLink: info.Item2);


            // Destroy child Field Card Presenters

            foreach (var key in deletedFieldCardKeys)
                DestroyCardPresenter(
                    fieldCardKey: key);
        }


        void LateUpdate()
        {
            // Clear tracking information

            newFieldCardInfos.Clear();
            deletedFieldCardKeys.Clear();
        }


        FieldCardPresenter SpawnCardPresenter(
            Guid fieldCardKey,
            WeakReference<IReadOnlyFieldCardContainer> fieldCardLink)
        {
            Debug.Log(
                "Spawning child Card Presenter");

            // Create

            var newCardPresenterEntity = new GameObject("Card Presenter");
            newCardPresenterEntity.transform.parent = this.transform;

            var newCardPresenter =
                newCardPresenterEntity.AddComponent<FieldCardPresenter>();
            newCardPresenter.RootContext = RootContext;
            newCardPresenter.FieldCardLink = fieldCardLink;


            // Manage

            Children.Add(fieldCardKey, newCardPresenter);


            return newCardPresenter;
        }


        void DestroyCardPresenter(Guid fieldCardKey)
        {
            Debug.Log(
                "Destroying child Card Presenter");

            var child = Children[fieldCardKey];

            // Un-manage

            Children.Remove(fieldCardKey);


            // Destroy

            Destroy(child);
        }
    }
}
