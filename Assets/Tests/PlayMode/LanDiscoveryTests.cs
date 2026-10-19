/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using Cgs.Play.Multiplayer;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class LanDiscoveryTests
    {
        [TestCase("192.168.1.42", "255.255.255.0", "192.168.1.255")]
        [TestCase("10.20.34.5", "255.255.240.0", "10.20.47.255")]
        [TestCase("172.20.2.5", "255.255.0.0", "172.20.255.255")]
        [TestCase("192.168.1.5", "255.255.255.252", "192.168.1.7")]
        [TestCase("169.254.4.5", "255.255.0.0", "169.254.255.255")]
        public void BroadcastUsesTheActualSubnetMask(string address, string mask, string expected)
        {
            Assert.That(LanDiscoveryAddresses.GetBroadcastAddress(IPAddress.Parse(address), IPAddress.Parse(mask)),
                Is.EqualTo(IPAddress.Parse(expected)));
        }

        [TestCase("127.0.0.1", "255.0.0.0")]
        [TestCase("0.0.0.0", "255.255.255.0")]
        [TestCase("224.0.0.1", "255.255.255.0")]
        [TestCase("192.168.1.2", "0.0.0.0")]
        [TestCase("192.168.1.2", "255.0.255.0")]
        [TestCase("192.168.1.2", "255.255.255.254")]
        [TestCase("192.168.1.2", "255.255.255.255")]
        [TestCase("::1", "255.255.255.0")]
        [TestCase("192.168.1.2", "::")]
        public void UnusableSubnetsAreNotBroadcastTargets(string address, string mask)
        {
            Assert.That(LanDiscoveryAddresses.GetBroadcastAddress(IPAddress.Parse(address), IPAddress.Parse(mask)),
                Is.Null);
        }

        [Test]
        public void MissingAddressInformationIsIgnored()
        {
            Assert.That(LanDiscoveryAddresses.GetBroadcastAddress(null, IPAddress.Any), Is.Null);
            Assert.That(LanDiscoveryAddresses.GetBroadcastAddress(IPAddress.Loopback, null), Is.Null);
        }

        [Test]
        public void LimitedBroadcastRemainsAvailable()
        {
            Assert.That(LanDiscoveryAddresses.GetBroadcastAddresses(), Contains.Item(IPAddress.Broadcast));
        }

        [UnityTest]
        public IEnumerator DiscoveryReachesEveryTargetAfterReceiveResetsAndRestarts()
        {
            var serverObject = new GameObject("LAN discovery server test");
            var clientObject = new GameObject("LAN discovery client test");
            try
            {
                var server = serverObject.AddComponent<LanDiscoveryTestPeer>();
                var client = clientObject.AddComponent<LanDiscoveryTestPeer>();
                ushort port;
                using (var reservation = new UdpClient(0))
                    port = (ushort)((IPEndPoint)reservation.Client.LocalEndPoint).Port;
                Configure(server, port);
                Configure(client, port);

                for (var round = 1; round <= 3; round++)
                {
                    server.ReceiveErrors.Enqueue(new SocketException((int)SocketError.ConnectionReset));
                    client.ReceiveErrors.Enqueue(new SocketException((int)SocketError.ConnectionReset));
                    server.StartServer();
                    client.StartClient();
                    client.ClientBroadcast(new DiscoveryBroadcastData());
                    var deadline = Time.realtimeSinceStartup + 5;
                    while (client.Responses < round * 2 && Time.realtimeSinceStartup < deadline)
                        yield return null;

                    Assert.That(server.Requests, Is.EqualTo(round * 2));
                    Assert.That(client.Responses, Is.EqualTo(round * 2));
                    Assert.That(client.LastResponse.ServerName, Is.EqualTo("LAN test room"));
                    Assert.That(client.LastResponse.Port, Is.EqualTo(12345));
                    client.StopDiscovery();
                    server.StopDiscovery();
                    Assert.That(client.IsRunning, Is.False);
                    Assert.That(server.IsRunning, Is.False);
                    // Deliberately restart before the old receive continuations have completed.
                }
            }
            finally
            {
                Object.DestroyImmediate(clientObject);
                Object.DestroyImmediate(serverObject);
            }
            yield return null;
        }

        [Test]
        public void FatalReceiveErrorStopsDiscoveryAndAllowsRestart()
        {
            var gameObject = new GameObject("LAN discovery receive failure test");
            try
            {
                var peer = gameObject.AddComponent<LanDiscoveryTestPeer>();
                peer.ReceiveErrors.Enqueue(new SocketException((int)SocketError.AccessDenied));
                LogAssert.Expect(LogType.Warning, new Regex("Unable to receive LAN discovery packets:"));
                peer.StartClient();
                Assert.That(peer.IsRunning, Is.False);
                Assert.That(peer.IsClient, Is.False);
                peer.StartClient();
                Assert.That(peer.IsRunning, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void UnexpectedReceiveErrorDoesNotLeaveDiscoveryMarkedAsRunning()
        {
            var gameObject = new GameObject("LAN discovery unexpected receive failure test");
            try
            {
                var peer = gameObject.AddComponent<LanDiscoveryTestPeer>();
                peer.ReceiveErrors.Enqueue(new System.InvalidOperationException("Injected receive failure"));
                LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: Injected receive failure"));
                peer.StartClient();
                Assert.That(peer.IsRunning, Is.False);
                Assert.That(peer.IsClient, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void DestroyingDiscoveryReleasesItsSocket()
        {
            var gameObject = new GameObject("LAN discovery cleanup test");
            try
            {
                var peer = gameObject.AddComponent<LanDiscoveryTestPeer>();
                peer.StartClient();
                var socket = (UdpClient)typeof(NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>)
                    .GetField("_mClient", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(peer);
                Assert.That(socket, Is.Not.Null);
                Object.DestroyImmediate(peer);
                Assert.That(socket.Client, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator StartingNativeDiscoverySearchesWithoutWaitingForRefresh()
        {
            var previous = NetworkManager.Singleton;
            var serverObject = new GameObject("Immediate discovery server test");
            var clientObject = new GameObject("Immediate discovery client test");
            try
            {
                var server = serverObject.AddComponent<LanDiscoveryTestPeer>();
                var client = clientObject.AddComponent<LanDiscoveryTestPeer>();
                ushort port;
                using (var reservation = new UdpClient(0))
                    port = (ushort)((IPEndPoint)reservation.Client.LocalEndPoint).Port;
                Configure(server, port);
                Configure(client, port);
                server.StartServer();
                var manager = clientObject.AddComponent<CgsNetManager>();
                manager.enabled = false;
                var found = false;
                manager.StartNativeLanDiscovery((_, response) => found = response.Port == 12345);

                var deadline = Time.realtimeSinceStartup + 5;
                while (!found && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(found, Is.True, "Starting discovery must send a request without a menu refresh.");
            }
            finally
            {
                Object.DestroyImmediate(clientObject);
                Object.DestroyImmediate(serverObject);
                if (previous != null)
                    previous.SetSingleton();
            }
        }

        private static void Configure(LanDiscoveryTestPeer peer, ushort port)
        {
            var type = typeof(NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>);
            type.GetField("mPort", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(peer, port);
            type.GetField("mUniqueApplicationId", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(peer, 42L);
        }
    }
}
