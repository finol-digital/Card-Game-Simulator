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

        public WeakReference<IReadOnlyFieldCardContainer> FieldCardLink;


        void Start()
        {
            // Set up components

            var volume = gameObject.AddComponent<BoxCollider>();
            var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();


            // Bind data (one-time data binding)

            IReadOnlyFieldCardContainer model;

            if (!FieldCardLink.TryGetTarget(out model))
            {
                Debug.LogWarning(
                    "Field card resource is null."
                    + " This presenter will destroy itself.");

                Destroy(this);

                return;
            }

            var cardDescription =
                RootContext.CardDescriptions[model.CardDescriptionKey];

            var viewModel = new {
                CardName = cardDescription.Name_EN_US,
                CardWidth = cardDescription.Width,
                CardHeight = cardDescription.Height,
                CardImage = Texture2D.whiteTexture
            };

            // - Represent card description

            gameObject.name = viewModel.CardName;

            volume.size = new Vector3(
                x: viewModel.CardWidth,
                y: viewModel.CardHeight,
                z: VolumeThickness
            );

            spriteRenderer.sprite = Sprite.Create(
                viewModel.CardImage,
                new Rect(
                    0f, 0f,
                    viewModel.CardWidth, viewModel.CardHeight),
                Vector2.one * 0.5f,
                1f
            );
        }


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
                CardGridElement = model.GridRecord
            };

            // Represent card field grid element

            // - Represent Position

            transform.localPosition = Vector3.Lerp(
                a: transform.localPosition,
                b: new Vector3(
                    viewModel.CardGridElement.X,
                    viewModel.CardGridElement.Y,
                    transform.localPosition.z),
                t: Time.deltaTime * 4f);


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
