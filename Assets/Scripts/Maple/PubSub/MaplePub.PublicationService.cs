using System.Threading;
using UnityEditor;
using static UnityEngine.Debug;

namespace Maple.PubSub
{
    public static partial class MaplePub
    {
        static class PublicationService
        {
            static Thread PublicationThread;


            internal static void StartService()
            {
                Log("Starting Maple PubSub publication service");

                PublicationThread = new Thread(new ThreadStart(ServiceLoop));
                PublicationThread.Name = "Maple PubSub Publication Service";
                PublicationThread.IsBackground = true;
                PublicationThread.Start();


#if UNITY_EDITOR

                 EditorApplication.playModeStateChanged += (state) =>
                    {
                        if (state == PlayModeStateChange.EnteredEditMode)
                        {
                                Log("Stopping Maple PubSub.");
                                PublicationThread.Abort();
                        }
                    };
#endif
            }


            static void ServiceLoop()
            {
                while (true)
                {
                    try
                    {
                        SendAllMessages();
                    }
                    finally
                    {
                        Thread.Sleep(40);  // ~25 Hz limit
                    }
                }
            }
        }
    }
}
