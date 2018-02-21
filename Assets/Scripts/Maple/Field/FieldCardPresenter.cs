using System;
using UnityEngine;
using Maple;

namespace Maple.Field
{
    public partial class FieldCardPresenter
        : MonoBehaviour
    {
        // TODO: Manage max elevation based on active camera Z offset

        public IReadOnlyMapleContext RootContext;

        public WeakReference<IReadOnlyFieldCardContainer> FieldCardLink;

        public float MaxElevationRepresentation = 500.0f;


        void Update()
        {
            IReadOnlyFieldCardContainer model;

            if (!FieldCardLink.TryGetTarget(out model))
            {
                Debug.LogWarning(
                    "Field card resource is null. "
                    + "This presenter will remain alive, but will not update.");
                return;  // TODO: fail gracefully
            }

            var viewModel = new {
                CardName =
                    RootContext.CardDescriptions[model.CardDescriptionKey]
                        .Name_EN_US,
                CardGridElement =
                    model.GridRecord
            };

            // Represent card description

            gameObject.name = viewModel.CardName;


            // Represent card field grid element

            // - Represent Position

            transform.localPosition = Vector3.Lerp(
                a: transform.localPosition,
                b: new Vector3(
                    viewModel.CardGridElement.X,
                    viewModel.CardGridElement.Y,
                    transform.localPosition.z),
                t: Time.deltaTime);


            // - Represent Elevation

            transform.localPosition = new Vector3(
                x: transform.localPosition.x,
                y: transform.localPosition.y,
                z: ((float)viewModel.CardGridElement.Elevation
                        / FieldGridElement.MaxElevation)
                    / MaxElevationRepresentation);


            // - Represent Rotation

            transform.localEulerAngles = Vector3.Lerp(
                a: transform.localEulerAngles,
                b: new Vector3(
                    transform.localEulerAngles.x,
                    viewModel.CardGridElement.RotationDegrees,
                    transform.localEulerAngles.z),
                t: Time.deltaTime);
        }
    }
}
