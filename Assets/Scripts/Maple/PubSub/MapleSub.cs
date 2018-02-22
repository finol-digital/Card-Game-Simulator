using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Maple.PubSub
{
    public class MapleSub
    {
        ConcurrentQueue<object> Inbox { get; } =
            new ConcurrentQueue<object>();


        public void RecieveMessage(object message) =>
            Inbox.Enqueue(message);


        /// <summary>
        /// Removes then returns messages.
        /// </summary>
        public IEnumerable<object> TakeMessages()
        {
            var throttle = Inbox.Count;
            var messages = new List<object>();

            for (int i = 1; i <= throttle; i++)
            {
                object objTemp;
                if (Inbox.TryDequeue(out objTemp))
                    messages.Add(objTemp);
            }

            return messages;
        }
    }
}
