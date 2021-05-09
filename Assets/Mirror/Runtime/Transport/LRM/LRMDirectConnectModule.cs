// This is an optional module for adding direct connect support

using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using LightReflectiveMirror;

[RequireComponent(typeof(LightReflectiveMirrorTransport))]
public class LRMDirectConnectModule : MonoBehaviour
{
    public Transport directConnectTransport;
    public bool showDebugLogs;
    private LightReflectiveMirrorTransport _lightMirrorTransport;

    void Awake()
    {
        _lightMirrorTransport = GetComponent<LightReflectiveMirrorTransport>();

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

        if(directConnectTransport == _lightMirrorTransport.clientToServerTransport)
        {
            Debug.LogError("LRM | Direct connect transport cannot be the same transport used to communicate with LRM servers!");
            _lightMirrorTransport.useNATPunch = false;
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
        if(SupportsNATPunch())
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
        return directConnectTransport.ServerDisconnect(clientID);
    }

    public void ClientDisconnect()
    {
        directConnectTransport.ClientDisconnect();
    }

    public void ServerSend(int clientID, ArraySegment<byte> data, int channel)
    {
        directConnectTransport.ServerSend(clientID, channel, data);
    }

    public void ClientSend(ArraySegment<byte> data, int channel)
    {
        directConnectTransport.ClientSend(channel, data);
    }

    #region Transport Callbacks
    void OnServerConnected(int clientID)
    {
        if (showDebugLogs)
            Debug.Log("Direct Connect Client Connected");
        _lightMirrorTransport.DirectAddClient(clientID);
    }

    void OnServerDataReceived(int clientID, ArraySegment<byte> data, int channel)
    {
        _lightMirrorTransport.DirectReceiveData(data, channel, clientID);
    }

    void OnServerDisconnected(int clientID)
    {
        _lightMirrorTransport.DirectRemoveClient(clientID);
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

        _lightMirrorTransport.DirectClientConnected();
    }

    void OnClientDisconnected()
    {
        _lightMirrorTransport.DirectDisconnected();
    }

    void OnClientDataReceived(ArraySegment<byte> data, int channel)
    {
        _lightMirrorTransport.DirectReceiveData(data, channel);
    }

    void OnClientError(Exception error)
    {
        if (showDebugLogs)
            Debug.Log("Direct Client Error: " + error);
    }
    #endregion
}