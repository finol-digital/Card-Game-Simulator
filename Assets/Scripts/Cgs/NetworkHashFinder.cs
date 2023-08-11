#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using System.Reflection;

public class NetworkHashFinder : EditorWindow
{
    private string hashInput;
    private NetworkObject output;

    [MenuItem("Tools/Network Hash Finder")]
    public static void InitializeWindow()
    {
        NetworkHashFinder window = GetWindow<NetworkHashFinder>();
        window.titleContent = new GUIContent("Network Hash Finder");
    }

    void OnGUI()
    {
        bool wasEnabled = GUI.enabled;

        EditorGUILayout.BeginHorizontal();
        {
            hashInput = EditorGUILayout.TextField("Hash ID", hashInput);
            GUI.enabled = VerifyIsInteger(hashInput);
            if (GUILayout.Button("Find"))
            {
                output = FindByHash(ParseInput(hashInput));
                if (output == null) Debug.Log("No GameObject found with hash ID '" + hashInput + "'");
            }
            GUI.enabled = wasEnabled;
        }
        EditorGUILayout.EndHorizontal();

        NetworkObject newOutput = EditorGUILayout.ObjectField("Object", output, typeof(NetworkObject), false) as NetworkObject;

        if (newOutput != output)
        {
            GUI.FocusControl(null);
            hashInput = newOutput.PrefabIdHash.ToString();
            output = newOutput;
        }

        if (GUILayout.Button("Clear"))
        {
            GUI.FocusControl(null);
            hashInput = "";
            output = null;
        }

        GUI.enabled = wasEnabled;
    }

    private uint ParseInput(string input)
    {
        return uint.Parse(input);
    }

    /// <summary>
    /// Use the hashID to find the GameObject that corresponds with it
    /// </summary>
    private NetworkObject FindByHash(uint hashID)
    {
        FieldInfo info = typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic);
        if (info == null)
        {
            Debug.LogError("GlobalObjectIdHash field is null. Unity might have updated Netcode not to use this parameter anymore, in which case this tool is useless");
            return null;
        }
        foreach (NetworkObject obj in Resources.FindObjectsOfTypeAll<NetworkObject>())
        {
            uint GlobalObjectIdHash = (uint)info.GetValue(obj);
            if (GlobalObjectIdHash == hashID)
            {
                return obj;
            }
        }

        return null;
    }

    private bool VerifyIsInteger(string input)
    {
        try
        {
            uint.Parse(input);
            return true;
        }
        catch (System.Exception)
        {
            return false;
        }
    }
}
#endif
