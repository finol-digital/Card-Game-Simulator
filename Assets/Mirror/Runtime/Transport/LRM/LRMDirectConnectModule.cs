// This is an optional module for adding direct connect support

using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using LightReflectiveMirror;

[RequireComponent(typeof(LightReflectiveMirrorTransport))]
public class LRMDirectConnectModule : MonoBehaviour
{
    [HideInInspector]
    public Transport directConnectTransport;
    public bool showDebugLogs;
    private LightReflectiveMirrorTransport lightMirrorTransport;

    void Awake()
    {
        lightMirrorTransport = GetComponent<LightReflectiveMirrorTransport>();

        if (directConnectTransport == null)
        {
            Debug.Log("Direct Connect Transport is null!");
            return;
        }

        if (directConnectTransport is LightReflectiveMirrorTransport)
        {
            Debug.Log("Direct Connect Transport Cannot be the relay, silly. :P");
            return;
        }

        directConnectTransport.OnServerConnected = (OnServerConnected);
        directConnectTransport.OnServerDataReceived = (OnServerDataReceived);
        directConnectTransport.OnServerDisconnected = (OnServerDisconnected);
        directConnectTransport.OnServerError = (OnServerError);
        directConnectTransport.OnClientConnected = (OnClientConnected);
        directConnectTransport.OnClientDataReceived = (OnClientDataReceived);
        directConnectTransport.OnClientDisconnected = (OnClientDisconnected);
        directConnectTransport.OnClientError = (OnClientError);
    }

    public void StartServer(int port)
    {
        if(port > 0)
            SetTransportPort(port);

        directConnectTransport.ServerStart();
        if (showDebugLogs)
            Debug.Log("Direct Connect Server Created!");
    }

    public void StopServer()
    {
        directConnectTransport.ServerStop();
    }

    public void JoinServer(string ip, int port)
    {
        if (SupportsNATPunch())
            SetTransportPort(port);

        directConnectTransport.ClientConnect(ip);
    }

    public void SetTransportPort(int port)
    {
        if (directConnectTransport is kcp2k.KcpTransport kcpTransport)
            kcpTransport.Port = (ushort)port;
        else
        {
            throw new Exception("DIRECT CONNECT MODULE ONLY SUPPORTS KCP AT THE MOMENT.");
        }
    }

    public int GetTransportPort()
    {
        if (directConnectTransport is kcp2k.KcpTransport kcpTransport)
            return kcpTransport.Port;
        else
        {
            throw new Exception("DIRECT CONNECT MODULE ONLY SUPPORTS KCP AT THE MOMENT.");
        }
    }

    public bool SupportsNATPunch()
    {
        return directConnectTransport is kcp2k.KcpTransport;
    }

    public bool KickClient(int clientID)
    {
        if (showDebugLogs)
            Debug.Log("Kicked direct connect client.");
#if MIRROR_37_0_OR_NEWER
        directConnectTransport.ServerDisconnect(clientID);
        return true;
#else
        return directConnectTransport.ServerDisconnect(clientID);
#endif
    }

    public void ClientDisconnect()
    {
        directConnectTransport.ClientDisconnect();
    }

    public void ServerSend(int clientID, ArraySegment<byte> data, int channel)
    {
#if MIRROR_40_0_OR_NEWER
        directConnectTransport.ServerSend(clientID, data, channel);
#else
        directConnectTransport.ServerSend(clientID, channel, data);
#endif
    }

    public void ClientSend(ArraySegment<byte> data, int channel)
    {
#if MIRROR_40_0_OR_NEWER
        directConnectTransport.ClientSend(data, channel);
#else
        directConnectTransport.ClientSend(channel, data);
#endif
    }

#region Transport Callbacks
    void OnServerConnected(int clientID)
    {
        if (showDebugLogs)
            Debug.Log("Direct Connect Client Connected");
        lightMirrorTransport.DirectAddClient(clientID);
    }

    void OnServerDataReceived(int clientID, ArraySegment<byte> data, int channel)
    {
        lightMirrorTransport.DirectReceiveData(data, channel, clientID);
    }

    void OnServerDisconnected(int clientID)
    {
        lightMirrorTransport.DirectRemoveClient(clientID);
    }

    void OnServerError(int client, Exception error)
    {
        if (showDebugLogs)
            Debug.Log("Direct Server Error: " + error);
    }

    void OnClientConnected()
    {
        if (showDebugLogs)
            Debug.Log("Direct Connect Client Joined");

        lightMirrorTransport.DirectClientConnected();
    }

    void OnClientDisconnected()
    {
        lightMirrorTransport.DirectDisconnected();
    }

    void OnClientDataReceived(ArraySegment<byte> data, int channel)
    {
        lightMirrorTransport.DirectReceiveData(data, channel);
    }

    void OnClientError(Exception error)
    {
        if (showDebugLogs)
            Debug.Log("Direct Client Error: " + error);
    }
#endregion
}