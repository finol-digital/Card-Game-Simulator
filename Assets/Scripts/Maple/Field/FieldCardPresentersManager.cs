using System;
using System.Collections.Generic;
using UnityEngine;

namespace Maple.Field
{
    public class FieldCardPresentersManager
        : MonoBehaviour
    {
        public IReadOnlyMapleContext RootContext;


        public IReadOnlyMapleFieldContext Context =>
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
        Queue<Tuple<Guid, WeakReference<IFieldCardReader>>> newFieldCardInfos  { get; } =
            new Queue<Tuple<Guid, WeakReference<IFieldCardReader>>>();

        /// <summary>
        /// Tracking information Buffer for storing discovered deleted field cards.
        /// </summary>
        Queue<Guid> deletedFieldCardKeys { get; } =
            new Queue<Guid>();


        void Update()
        {
            // Update tracking information

            // - Discover new field card info

            foreach (var fieldCardEntry in Context.FieldCardStore)
                if (!Children.ContainsKey(fieldCardEntry.Key))
                    newFieldCardInfos.Enqueue(
                        new Tuple<Guid, WeakReference<IFieldCardReader>>(
                            fieldCardEntry.Key,
                            new WeakReference<IFieldCardReader>(
                                fieldCardEntry.Value)));


            // - Discover deleted field info

            foreach (var fieldCardKey in Children.Keys)
                if (!Context.FieldCardStore.ContainsKey(fieldCardKey))
                    deletedFieldCardKeys.Enqueue(fieldCardKey);


            // Spawn child Field Card Presenters

            foreach (var info in newFieldCardInfos)
                SpawnPresenter(
                    fieldCardKey: info.Item1,
                    fieldCardLink: info.Item2);


            // Destroy child Field Card Presenters

            foreach (var key in deletedFieldCardKeys)
                DestroyPresenter(
                    fieldCardKey: key);
        }


        void LateUpdate()
        {
            // Clear tracking information

            newFieldCardInfos.Clear();
            deletedFieldCardKeys.Clear();
        }


        FieldCardPresenter SpawnPresenter(
            Guid fieldCardKey,
            WeakReference<IFieldCardReader> fieldCardLink)
        {
            Debug.Log(
                "Spawning child Card Presenter");

            // Create

            var newPresenterGameObject = new GameObject("Card Presenter");
            newPresenterGameObject.transform.parent = this.transform;

            var newPresenter =
                newPresenterGameObject.AddComponent<FieldCardPresenter>();
            newPresenter.RootContext = RootContext;
            newPresenter.FieldCardLink = fieldCardLink;


            // Manage

            Children.Add(fieldCardKey, newPresenter);


            return newPresenter;
        }


        void DestroyPresenter(Guid fieldCardKey)
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
