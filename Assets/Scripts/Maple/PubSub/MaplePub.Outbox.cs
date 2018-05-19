using System;
using System.Collections.Concurrent;

namespace Maple.PubSub
{
    public static partial class MaplePub
    {
        class Outbox
        {
            ConcurrentBag<WeakReference<MapleSub>> SubRefs { get; } =
                new ConcurrentBag<WeakReference<MapleSub>>();

            ConcurrentQueue<object> Messages { get; } =
                new ConcurrentQueue<object>();


            public void EnlistSub(MapleSub sub) =>
                SubRefs.Add(new WeakReference<MapleSub>(sub));


            public void AddMessage(object message) =>
                Messages.Enqueue(message);


            /// <summary>
            /// Take added messages from storage and send them to enlisted subs.
            /// </summary>
            public void SendMessages()
            {

                object messageBuffer;
                var throttle = Messages.Count;
                var limitCounter = 0;

                while (
                    limitCounter++ <= throttle
                    && Messages.TryDequeue(out messageBuffer)
                    && messageBuffer != null)
                {
                    foreach (var subRef in SubRefs)
                    {
                        MapleSub sub;
                        if (subRef.TryGetTarget(out sub))
                            sub?.ReceiveMessage(messageBuffer);
                    }
                }
            }
        }
    }
}
