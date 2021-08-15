using Mirror;
using System;

namespace LightReflectiveMirror
{
    public partial class LightReflectiveMirrorTransport : Transport
    {
        public void DirectAddClient(int clientID)
        {
            if (!_isServer)
                return;

            _connectedDirectClients.Add(clientID, _currentMemberId);
            OnServerConnected?.Invoke(_currentMemberId);
            _currentMemberId++;
        }

        public void DirectRemoveClient(int clientID)
        {
            if (!_isServer)
                return;

            OnServerDisconnected?.Invoke(_connectedDirectClients.GetByFirst(clientID));
            _connectedDirectClients.Remove(clientID);
        }

        public void DirectReceiveData(ArraySegment<byte> data, int channel, int clientID = -1)
        {
            if (_isServer)
                OnServerDataReceived?.Invoke(_connectedDirectClients.GetByFirst(clientID), data, channel);

            if (_isClient)
                OnClientDataReceived?.Invoke(data, channel);
        }

        public void DirectClientConnected()
        {
            _directConnected = true;
            OnClientConnected?.Invoke();
        }

        public void DirectDisconnected()
        {
            if (_directConnected)
            {
                _isClient = false;
                _directConnected = false;
                OnClientDisconnected?.Invoke();
            }
            else
            {
                int pos = 0;
                _directConnected = false;
                _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.JoinServer);
                _clientSendBuffer.WriteString(ref pos, _cachedHostID);
                _clientSendBuffer.WriteBool(ref pos, false); // Direct failed, use relay

                _isClient = true;

#if MIRROR_40_0_OR_NEWER
                clientToServerTransport.ClientSend(new System.ArraySegment<byte>(_clientSendBuffer, 0, pos), 0);
#else
                clientToServerTransport.ClientSend(0, new System.ArraySegment<byte>(_clientSendBuffer, 0, pos));
#endif
            }

            if (_clientProxy != null)
            {
                _clientProxy.Dispose();
                _clientProxy = null;
            }
        }
    }
}