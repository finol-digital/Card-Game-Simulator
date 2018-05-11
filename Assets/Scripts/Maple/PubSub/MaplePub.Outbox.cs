using System;
using System.Collections.Concurrent;

namespace Maple.PubSub
{
    public static partial class MaplePub
    {
        class Outbox
        {
            ConcurrentBag<WeakReference<MapleSub>> Subs { get; } =
                new ConcurrentBag<WeakReference<MapleSub>>();

            ConcurrentQueue<object> Messages { get; } =
                new ConcurrentQueue<object>();


            public void EnlistSub(MapleSub sub) =>
                Subs.Add(new WeakReference<MapleSub>(sub));


            public void AddMessage(object message) =>
                Messages.Enqueue(message);


            /// <summary>
            /// Take added messages from storage and send them to enlisted subs.
            /// </summary>
            public void SendMessages()
            {
                var messagesConsumeLimit = Messages.Count;

                int limitCounter = 0;
                object msgTemp;
                while (
                    limitCounter++ <= messagesConsumeLimit
                    && Messages.TryDequeue(out msgTemp)
                    && msgTemp != null)
                {
                    foreach (var sub in Subs)
                    {
                        MapleSub realSub;
                        if (sub.TryGetTarget(out realSub))
                            realSub?.RecieveMessage(msgTemp);
                    }
                }
            }
        }
    }
}
