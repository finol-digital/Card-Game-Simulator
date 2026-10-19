/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Cgs.Play.Multiplayer
{
    internal static class LanDiscoveryAddresses
    {
        internal static IEnumerable<IPAddress> GetBroadcastAddresses()
        {
            // Retain the original discovery path on platforms without interface information.
            var addresses = new HashSet<IPAddress> { IPAddress.Broadcast };
            try
            {
                foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
                    AddBroadcastAddresses(network, addresses);
            }
            catch (NetworkInformationException)
            {
                // Interface information can become unavailable while networks change.
            }
            catch (NotSupportedException)
            {
                // Some player platforms do not implement interface enumeration.
            }
            catch (NotImplementedException)
            {
                // Older platform runtimes may expose an unimplemented API instead.
            }

            return addresses;
        }

        private static void AddBroadcastAddresses(NetworkInterface network, ISet<IPAddress> addresses)
        {
            try
            {
                if (network.OperationalStatus != OperationalStatus.Up ||
                    network.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    network.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    return;

                foreach (var unicast in network.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    var broadcast = GetBroadcastAddress(unicast.Address, unicast.IPv4Mask);
                    if (broadcast != null)
                        addresses.Add(broadcast);
                }
            }
            catch (NetworkInformationException)
            {
                // A disconnected adapter must not prevent discovery on the remaining ones.
            }
            catch (NotSupportedException)
            {
                // Keep the limited broadcast when an adapter cannot report its IPv4 mask.
            }
            catch (NotImplementedException)
            {
                // Keep searching other adapters if this runtime cannot report the mask.
            }
        }

        internal static IPAddress GetBroadcastAddress(IPAddress address, IPAddress mask)
        {
            if (address == null || mask == null ||
                address.AddressFamily != AddressFamily.InterNetwork ||
                mask.AddressFamily != AddressFamily.InterNetwork ||
                IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any))
                return null;

            var addressBytes = address.GetAddressBytes();
            var maskBytes = mask.GetAddressBytes();
            uint maskValue = 0;
            foreach (var value in maskBytes)
                maskValue = (maskValue << 8) | value;

            var hostBits = ~maskValue;
            // Reject missing/noncontiguous masks and /31 or /32 point-to-point addresses.
            if (maskValue == 0 || hostBits < 3 || (hostBits & (hostBits + 1)) != 0 ||
                addressBytes[0] >= 224)
                return null;

            for (var i = 0; i < addressBytes.Length; i++)
                addressBytes[i] = (byte)(addressBytes[i] | ~maskBytes[i]);

            return new IPAddress(addressBytes);
        }
    }
}
