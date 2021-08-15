using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Net;
using System.Reflection;
using System.Linq;
using Mirror;
using Mirror.SimpleWeb;
using System;
using kcp2k;

namespace LightReflectiveMirror
{
#if UNITY_EDITOR
    [CustomEditor(typeof(LightReflectiveMirrorTransport))]
    public class LRMInspector : Editor
    {
        int serverPort = 7777;
        string serverIP;
        float invalidServerIP = 0;
        bool usingLLB = false;
        LRMDirectConnectModule directModule;
        string[] tabs = new string[] { "LRM Settings", "NAT Punch", "Load Balancer", "Other" };
        int currentTab = 0;
        Type[] supportedTransports = new Type[3] { typeof(KcpTransport), typeof(SimpleWebTransport), typeof(TelepathyTransport) };

        public override void OnInspectorGUI()
        {
            var lrm = (LightReflectiveMirrorTransport)target;
            directModule = lrm.GetComponent<LRMDirectConnectModule>();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(Resources.Load<Texture>("LRM"), GUILayout.Height(50), GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(lrm.loadBalancerAddress))
            {
                // First setup screen, ask if they are using LLB or just a single LRM node.
                EditorGUILayout.HelpBox("Thank you for using LRM!\nTo get started, please select which setup you are using.", MessageType.None);

                if (GUILayout.Button("Load Balancer Setup"))
                {
                    usingLLB = true;
                    lrm.loadBalancerAddress = "127.0.0.1";
                    serverPort = 7070;
                }

                if (GUILayout.Button("Single LRM Node Setup"))
                {
                    lrm.loadBalancerAddress = "127.0.0.1";
                    lrm.useLoadBalancer = false;
                    usingLLB = false;
                    serverIP = "172.105.109.117";
                }
            }
            else if (usingLLB)
            {
                // They said they are using LLB, configure it!
                EditorGUILayout.HelpBox("The Load Balancer is another server that is different than the LRM node. Please enter the IP address or domain name of your Load Balancer server, along with the port.", MessageType.None);
                EditorGUILayout.HelpBox("Acceptable Examples: 127.0.0.1, mydomain.com", MessageType.Info);
                if (Time.realtimeSinceStartup - invalidServerIP < 5)
                    EditorGUILayout.HelpBox("Invalid Server Address!", MessageType.Error);

                serverIP = EditorGUILayout.TextField("Server Address", serverIP);
                serverPort = Mathf.Clamp(EditorGUILayout.IntField("Server Port", serverPort), ushort.MinValue, ushort.MaxValue);

                if (GUILayout.Button("Continue"))
                {
                    if (IPAddress.TryParse(serverIP, out IPAddress serverAddr))
                    {
                        lrm.loadBalancerAddress = serverAddr.ToString();
                        lrm.loadBalancerPort = (ushort)serverPort;
                        lrm.serverIP = "127.0.0.1";
                        lrm.useLoadBalancer = true;
                        usingLLB = false;
                        serverIP = "";
                    }
                    else
                    {
                        try
                        {
                            if (Dns.GetHostEntry(serverIP).AddressList.Length > 0)
                            {
                                lrm.loadBalancerAddress = serverIP;
                                lrm.loadBalancerPort = (ushort)serverPort;
                                lrm.serverIP = "127.0.0.1";
                                usingLLB = false;
                                serverIP = "";
                            }
                            else
                                invalidServerIP = Time.realtimeSinceStartup;
                        }
                        catch
                        {
                            invalidServerIP = Time.realtimeSinceStartup;
                        }
                    }
                }
            }
            else if (lrm.clientToServerTransport == null)
            {
                // next up, the actual transport. We are going to loop over all the transport types here and let them select one.
                EditorGUILayout.HelpBox("We need to use the same transport used on the server. Please select the same transport used on your LRM Node(s)", MessageType.None);

                foreach (var transport in supportedTransports)
                {
                    if (GUILayout.Button(transport.Name))
                    {
                        var newTransportGO = new GameObject("LRM - Connector");
                        newTransportGO.transform.SetParent(lrm.transform);
                        var newTransport = newTransportGO.AddComponent(transport);
                        lrm.clientToServerTransport = (Transport)newTransport;
                    }
                }
            }
            else if (string.IsNullOrEmpty(lrm.serverIP))
            {
                // Empty server IP, this is pretty important! Lets show the UI to require it.
                EditorGUILayout.HelpBox("For a single LRM node setup, we need the IP address or domain name of your LRM server.", MessageType.None);
                EditorGUILayout.HelpBox("Acceptable Examples: 172.105.109.117, mydomain.com", MessageType.Info);

                if (Time.realtimeSinceStartup - invalidServerIP < 5)
                    EditorGUILayout.HelpBox("Invalid Server Address!", MessageType.Error);

                serverIP = EditorGUILayout.TextField("Server Address", serverIP);
                serverPort = Mathf.Clamp(EditorGUILayout.IntField("Server Port", serverPort), ushort.MinValue, ushort.MaxValue);

                if (GUILayout.Button("Continue"))
                {
                    if (IPAddress.TryParse(serverIP, out IPAddress serverAddr))
                    {
                        lrm.serverIP = serverAddr.ToString();
                        lrm.SetTransportPort((ushort)serverPort);
                    }
                    else
                    {
                        try
                        {
                            if (Dns.GetHostEntry(serverIP).AddressList.Length > 0)
                            {
                                lrm.serverIP = serverIP;
                                lrm.SetTransportPort((ushort)serverPort);
                            }
                            else
                                invalidServerIP = Time.realtimeSinceStartup;
                        }
                        catch
                        {
                            invalidServerIP = Time.realtimeSinceStartup;
                        }
                    }
                }
            }
            else if (lrm.NATPunchtroughPort < 0)
            {
                // NAT Punchthrough configuration.
                EditorGUILayout.HelpBox("Do you wish to use NAT Punchthrough? This can reduce load by up to 80% on your LRM nodes, but exposes players IP's to other players.", MessageType.None);

                if (GUILayout.Button("Use NAT Punchthrough"))
                {
                    lrm.NATPunchtroughPort = 1;
                    lrm.useNATPunch = true;
                    lrm.gameObject.AddComponent<LRMDirectConnectModule>();
                }

                if (GUILayout.Button("Do NOT use NAT Punchthrough"))
                    lrm.NATPunchtroughPort = 1;

            }
            else if (directModule != null && directModule.directConnectTransport == null)
            {
                // NAT Punchthrough setup.
                EditorGUILayout.HelpBox("To use direct connecting, we need a transport to communicate with the other clients. Please select a transport to use.", MessageType.None);

                foreach (var transport in supportedTransports)
                {
                    if (lrm.useNATPunch && transport != typeof(KcpTransport))
                        continue;

                    if (GUILayout.Button(transport.Name))
                    {
                        var newTransportGO = new GameObject("LRM - Direct Connect");
                        newTransportGO.transform.SetParent(lrm.transform);
                        var newTransport = newTransportGO.AddComponent(transport);
                        directModule.directConnectTransport = (Transport)newTransport;
                    }
                }
            }
            else
            {
                // They have completed the "setup guide" Show them the main UI

                // Remove unused transports...
                foreach (var transport in lrm.GetComponentsInChildren<Transport>())
                {
                    if (!(transport is LightReflectiveMirrorTransport))
                    {
                        if (transport != lrm.clientToServerTransport && (directModule == null ? true : directModule.directConnectTransport != transport))
                        {
                            if (transport.gameObject == lrm.gameObject)
                                DestroyImmediate(transport);
                            else
                                DestroyImmediate(transport.gameObject);
                        }
                    }
                }

                currentTab = GUILayout.Toolbar(currentTab, tabs);
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical("Window");
                switch (currentTab)
                {
                    case 0:
                        using (var change = new EditorGUI.ChangeCheckScope())
                        {


                            // They are in the LRM Settings tab.
                            if (lrm.useLoadBalancer)
                            {
                                EditorGUILayout.HelpBox("While using a Load Balancer, you don't set the LRM node IP or port.", MessageType.Info);
                                GUI.enabled = false;
                            }
                            lrm.serverIP = EditorGUILayout.TextField("LRM Node IP", lrm.serverIP);
                            lrm.serverPort = (ushort)Mathf.Clamp(EditorGUILayout.IntField("LRM Node Port", lrm.serverPort), ushort.MinValue, ushort.MaxValue);
                            lrm.endpointServerPort = (ushort)Mathf.Clamp(EditorGUILayout.IntField("Endpoint Port", lrm.endpointServerPort), ushort.MinValue, ushort.MaxValue);

                            if (lrm.useLoadBalancer)
                            {
                                GUI.enabled = true;
                            }

                            lrm.authenticationKey = EditorGUILayout.TextField("LRM Auth Key", lrm.authenticationKey);
                            lrm.heartBeatInterval = EditorGUILayout.Slider("Heartbeat Time", lrm.heartBeatInterval, 0.1f, 5f);
                            lrm.connectOnAwake = EditorGUILayout.Toggle("Connect on Awake", lrm.connectOnAwake);
                            lrm.clientToServerTransport = (Transport)EditorGUILayout.ObjectField("LRM Transport", lrm.clientToServerTransport, typeof(Transport), true);
                            if (change.changed)
                            {
                                EditorUtility.SetDirty(lrm);
                            }
                        }
                        serializedObject.ApplyModifiedProperties();
                        break;
                    case 1:
                        // NAT punch tab.
                        if (directModule == null)
                        {
                            EditorGUILayout.HelpBox("NAT Punchthrough disabled, missing Direct Connect.", MessageType.Info);
                            if (GUILayout.Button("Add Direct Connect"))
                                lrm.gameObject.AddComponent<LRMDirectConnectModule>();
                        }
                        else
                        {
                            if (!(directModule.directConnectTransport is KcpTransport))
                            {
                                EditorGUILayout.HelpBox("NAT Punch only supports KCP currently.", MessageType.Info);
                                GUI.enabled = false;
                                lrm.useNATPunch = false;
                            }

                            lrm.useNATPunch = EditorGUILayout.Toggle("Use NAT Punch", lrm.useNATPunch);
                            GUI.enabled = true;
                            directModule.directConnectTransport = (Transport)EditorGUILayout.ObjectField("Direct Transport", directModule.directConnectTransport, typeof(Transport), true);
                        }
                        serializedObject.ApplyModifiedProperties();
                        break;
                    case 2:
                        // Load balancer tab
                        lrm.useLoadBalancer = EditorGUILayout.Toggle("Use Load Balancer", lrm.useLoadBalancer);
                        if (!lrm.useLoadBalancer)
                            GUI.enabled = false;
                        lrm.loadBalancerAddress = EditorGUILayout.TextField("Load Balancer Address", lrm.loadBalancerAddress);
                        lrm.loadBalancerPort = (ushort)Mathf.Clamp(EditorGUILayout.IntField("Load Balancer Port", lrm.loadBalancerPort), ushort.MinValue, ushort.MaxValue);
                        lrm.region = (LRMRegions)EditorGUILayout.EnumPopup("Node Region", lrm.region);
                        if (!lrm.useLoadBalancer)
                            GUI.enabled = true;
                        serializedObject.ApplyModifiedProperties();
                        break;
                    case 3:
                        // Other tab...

                        GUI.enabled = false;
                        EditorGUILayout.TextField("Server Status", lrm.serverStatus);
                        EditorGUILayout.TextField("Server ID", string.IsNullOrEmpty(lrm.serverId) ? "Not Hosting." : lrm.serverId);
                        GUI.enabled = true;

                        EditorGUILayout.Space();

                        lrm.serverName = EditorGUILayout.TextField("Server Name", lrm.serverName);
                        lrm.extraServerData = EditorGUILayout.TextField("Extra Server Data", lrm.extraServerData);
                        lrm.maxServerPlayers = EditorGUILayout.IntField("Max Server Players", lrm.maxServerPlayers);
                        lrm.isPublicServer = EditorGUILayout.Toggle("Is Public Server", lrm.isPublicServer);

                        EditorGUILayout.Space();
                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("connectedToRelay"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("disconnectedFromRelay"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("serverListUpdated"));
                        serializedObject.ApplyModifiedProperties();
                        break;
                }
                EditorGUILayout.EndVertical();
            }
        }
    }
#endif
}