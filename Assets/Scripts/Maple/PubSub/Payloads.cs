using UnityEngine;

namespace Maple.PubSub
{
    public struct FingerSlidePayload
    {
        public Vector2 StartPoint { get; }

        public bool IsStartTerminal { get; }

        public Vector2 EndPoint { get; }

        public bool IsEndTerminal { get; }


        /// <summary>
        /// This payload is expected at Ingress.Input.FingerSlide
        /// </summary>
        public FingerSlidePayload(
            Vector2 startPoint,
            bool isStartTerminal,
            Vector2 endPoint,
            bool isEndTerminal)
        {
            StartPoint = startPoint;
            IsStartTerminal = isStartTerminal;
            EndPoint = endPoint;
            IsEndTerminal = isEndTerminal;
        }
    }
}
