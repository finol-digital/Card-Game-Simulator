using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace LightReflectiveMirror
{

    // This class handles the proxying from punched socket to transport.
    public class SocketProxy
    {
        public DateTime lastInteractionTime;
        public Action<IPEndPoint, byte[]> dataReceived;
        UdpClient _udpClient;
        IPEndPoint _recvEndpoint = new IPEndPoint(IPAddress.Any, 0);
        IPEndPoint _remoteEndpoint;
        bool _clientInitialRecv = false;

        public SocketProxy(int port, IPEndPoint remoteEndpoint)
        {
            _udpClient = new UdpClient();
            _udpClient.Connect(new IPEndPoint(IPAddress.Loopback, port));
            _udpClient.BeginReceive(new AsyncCallback(RecvData), _udpClient);
            lastInteractionTime = DateTime.Now;
            // Clone it so when main socket recvies new data, it wont switcheroo on us.
            _remoteEndpoint = new IPEndPoint(remoteEndpoint.Address, remoteEndpoint.Port);
        }

        public SocketProxy(int port)
        {
            _udpClient = new UdpClient(port);
            _udpClient.BeginReceive(new AsyncCallback(RecvData), _udpClient);
            lastInteractionTime = DateTime.Now;
        }

        public void RelayData(byte[] data, int length)
        {
            _udpClient.Send(data, length);
            lastInteractionTime = DateTime.Now;
        }

        public void ClientRelayData(byte[] data, int length)
        {
            if (_clientInitialRecv)
            {
                _udpClient.Send(data, length, _recvEndpoint);
                lastInteractionTime = DateTime.Now;
            }
        }

        public void Dispose()
        {
            _udpClient.Dispose();
        }

        void RecvData(IAsyncResult result)
        {
            byte[] data = _udpClient.EndReceive(result, ref _recvEndpoint);
            _udpClient.BeginReceive(new AsyncCallback(RecvData), _udpClient);
            _clientInitialRecv = true;
            lastInteractionTime = DateTime.Now;
            dataReceived?.Invoke(_remoteEndpoint, data);
            
        }
    }
}