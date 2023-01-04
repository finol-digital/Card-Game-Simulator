/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Cgs.Play.Multiplayer
{
    public class Transports : MonoBehaviour
    {
        public UnityTransport unityTransport;
        public UnityTransport relayUnityTransport;
    }
}
