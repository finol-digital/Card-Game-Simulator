using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using static UnityEngine.Debug;

namespace Maple.PubSub
{
    public static partial class MaplePub
    {
        public const string PathSeparator = ".";


        public static CancellationTokenSource PublishTimeoutTokenSrc { get; } =
            new CancellationTokenSource(60000);  // 1 minute


        static MaplePub()
        {
            PublicationService.StartService();
        }


        /// <summary>
        /// Thread-safe lazy loading structure for storing publication data.
        /// </summary>
        static ConcurrentDictionary<string, Lazy<ConcurrentDictionary<string, Lazy<Outbox>>>> Outlets { get; } =
            new ConcurrentDictionary<string, Lazy<ConcurrentDictionary<string, Lazy<Outbox>>>>();


        /// <summary>
        /// Asynchronously publishes a message to a topic and all of the
        /// topic's ancestors.
        /// The messages will be sent to subscribers asynchronously.
        /// </summary>
        public static void Publish(
            object message,
            string outletName,
            params string[] topicPathParts)
        {
            Task.Factory.StartNew(
                () =>
                    {
                        try
                        {
                            AddMessageToTopicAndAncestors(
                                message,
                                outletName,
                                topicPathParts);
                        }
                        catch (Exception e)
                        {
                            LogException(e);

                            Log(
                                "Failed to publish"
                                + $" message: {message}"
                                + $" outletName: {outletName}");
                        }
                    },
                PublishTimeoutTokenSrc.Token);
        }


        /// <summary>
        /// Creates a new subscriber and adds it to a topic.
        /// The subscriber will asynchronously recieve messages.
        /// </summary>
        public static MapleSub Subscribe(
            string outletName,
            params string[] topicPathParts)
        {
            // Lazy load publication data

            var topicOutbox = GetOrAddTopic(outletName, topicPathParts);


            // Add new subscription to topic

            var newSub = new MapleSub();

            topicOutbox.Value.EnlistSub(newSub);


            return newSub;
        }


        static Lazy<Outbox> GetOrAddTopic(
            string outletName,
            params string[] topicPathParts)
        {
            // Get or add outlet

            var outlet = Outlets.GetOrAdd(
                    outletName,
                    (_key) =>
                        new Lazy<ConcurrentDictionary<string, Lazy<Outbox>>>());


            // Get or add topic

            return outlet.Value.GetOrAdd(
                BuildPath(topicPathParts),
                (_key) => new Lazy<Outbox>());
        }


        static void AddMessageToTopicAndAncestors(
            object message,
            string outletName,
            params string[] topicPathParts)
        {
            // If there are no subscribers, do nothing.

            /*
                if
                    outlet key exists
                    and outlet topics table is loaded
             */
            if (
                Outlets.ContainsKey(outletName)
                && Outlets[outletName].IsValueCreated)
            {
                foreach (
                    var topicPath in BuildPathAndAncestorPaths(topicPathParts))
                {
                    /*
                        if
                            topic key exists
                            and topic outbox is loaded
                    */
                    if (
                        Outlets[outletName].Value.ContainsKey(topicPath)
                        && Outlets[outletName].Value[topicPath].IsValueCreated)
                    {
                        Outlets[outletName]
                            .Value[topicPath]
                            .Value.AddMessage(message);
                    }
                }
            }
        }


        static void SendAllMessages()
        {
            // Find loaded outboxes, then send messages in outboxes

            foreach (var outlet in Outlets.Values)
            {
                if (outlet.IsValueCreated)
                {
                    foreach (var outbox in outlet.Value.Values)
                    {
                        if (outbox.IsValueCreated)
                        {
                            outbox.Value.SendMessages();
                        }
                    }
                }
            }
        }


        static string BuildPath(params string[] pathParts) =>
           string.Join(PathSeparator, pathParts);


        static string[] BuildPathAndAncestorPaths(params string[] pathParts)
        {
            var paths = new string[pathParts.Length];

            for (
                int pathsItr = 0, pathPartsItr = 1;
                pathPartsItr <= pathParts.Length;
                pathsItr++, pathPartsItr++)
            {
                paths[pathsItr] =
                    string.Join(PathSeparator, pathParts, 0, pathPartsItr);
            }

            return paths;
        }
    }
}
