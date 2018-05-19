using System;
using UnityEngine;
using Maple;

namespace Maple.Field
{
    public partial class FieldCardPresenter
        : MonoBehaviour
    {
        // TODO: Manage max elevation based on active camera Z offset
        const float MaxElevationRepresentation = 500f;

        const float VolumeThickness = 0.001f;

        public IReadOnlyMapleContext RootContext;

        public WeakReference<IFieldCardReader> FieldCardLink;


        void Start()
        {
            // Set up components

            var volume = gameObject.AddComponent<BoxCollider>();
            var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();


            // Bind data (one-time data binding)

            IFieldCardReader model;

            if (!FieldCardLink.TryGetTarget(out model))
            {
                Debug.LogWarning(
                    "Field card resource is null."
                    + " This presenter will destroy itself.");

                Destroy(this);

                return;
            }

            var cardDef =
                RootContext.CardDefinitions[model.ReadCardDefinitionKey()];

            var viewModel = new {
                CardName = cardDef.Name_EN_US,
                CardWidth = cardDef.Width,
                CardHeight = cardDef.Height,
                CardImage = cardDef.FrontFace
            };

            // - Represent card definition

            gameObject.name = viewModel.CardName;

            volume.size = new Vector3(
                x: viewModel.CardWidth,
                y: viewModel.CardHeight,
                z: VolumeThickness
            );

            spriteRenderer.sprite = viewModel.CardImage;
        }


        void Update()
        {
            IFieldCardReader model;

            if (!FieldCardLink.TryGetTarget(out model))
            {
                Debug.LogWarning(
                    "Field card resource is null. "
                    + "This presenter will remain alive, but will not update.");
                return;  // TODO: fail gracefully
            }

            var viewModel = new {
                CardGridData = model.ReadGridRecord()
            };

            // Represent card field grid element

            // - Represent Position

            transform.localPosition = Vector3.Lerp(
                a: transform.localPosition,
                b: new Vector3(
                    viewModel.CardGridData.X,
                    viewModel.CardGridData.Y,
                    transform.localPosition.z),
                t: Time.deltaTime * 4f);


            // - Represent Elevation

            transform.localPosition = new Vector3(
                x: transform.localPosition.x,
                y: transform.localPosition.y,
                z: ((float)viewModel.CardGridData.Elevation
                        / FieldGridData.MaxElevation)
                    / MaxElevationRepresentation);


            // - Represent Rotation

            transform.localEulerAngles = Vector3.Lerp(
                a: transform.localEulerAngles,
                b: new Vector3(
                    transform.localEulerAngles.x,
                    viewModel.CardGridData.RotationDegrees,
                    transform.localEulerAngles.z),
                t: Time.deltaTime);
        }
    }
}
