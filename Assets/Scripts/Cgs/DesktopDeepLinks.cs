/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Cgs
{
    /// <summary>
    /// Opens cardgamesim:// links on Windows and Linux, where Unity does not raise Application.deepLinkActivated.
    /// The player registers itself as the link handler and reads the link from its launch arguments. A launch
    /// that finds CGS already running forwards its link to that window over the local machine, then quits.
    /// </summary>
    public sealed class DesktopDeepLinks : MonoBehaviour
    {
        public const string Scheme = "cardgamesim";
        internal const int RelayPort = 47787;
        internal const string LinuxDesktopFileName = "cardgamesim-url-handler.desktop";
        private const string LinkHandlerName = "Card Game Simulator";
        private const string RegistrationWarningMessage = "Could not register cardgamesim:// links: ";

        /// <summary>The link this player was launched with on Windows or Linux; null otherwise.</summary>
        internal static string LaunchLink { get; private set; }

        private LinkRelayServer _relay;

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            LaunchLink = FindLink(Environment.GetCommandLineArgs());
            if (LaunchLink != null && LinkRelayServer.TryForward(LaunchLink, RelayPort))
            {
                Debug.Log("DesktopDeepLinks: forwarded the launch link to the open CGS window");
                Application.Quit();
                return;
            }

            try
            {
                RegisterLinkHandler();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(RegistrationWarningMessage + exception.Message);
            }

            var host = new GameObject(nameof(DesktopDeepLinks));
            DontDestroyOnLoad(host);
            host.AddComponent<DesktopDeepLinks>()._relay = LinkRelayServer.Start(RelayPort);
        }
#endif

        private void Update()
        {
            if (_relay is not { HasLinks: true })
                return;
            // Keep links queued until the scene's CardGameManager exists.
            if (CardGameManager.IsQuitting || GameObject.FindGameObjectWithTag(Tags.CardGameManager) == null)
                return;
            while (_relay.TryTakeLink(out var link))
                CardGameManager.Instance.OnDeepLinkActivated(link);
        }

        private void OnDestroy()
        {
            _relay?.Dispose();
            _relay = null;
        }

        internal static string FindLink(IEnumerable<string> arguments) =>
            arguments?.Select(argument => argument?.Trim()).FirstOrDefault(IsLink);

        internal static bool IsLink(string value) =>
            !string.IsNullOrEmpty(value) && value.Length <= LinkRelayServer.MaxLinkLength &&
            Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase);

        internal static string WindowsOpenCommand(string executablePath) => "\"" + executablePath + "\" \"%1\"";

        internal static string LinuxDesktopEntry(string executablePath) =>
            "[Desktop Entry]\n" +
            "Type=Application\n" +
            "Name=" + LinkHandlerName + "\n" +
            "Exec=" + DesktopExecArgument(executablePath) + " %u\n" +
            "NoDisplay=true\n" +
            "MimeType=x-scheme-handler/" + Scheme + ";\n";

        // Desktop Entry Specification: quote the argument, escape reserved characters, then apply string escapes.
        internal static string DesktopExecArgument(string path)
        {
            var quoted = new StringBuilder("\"");
            foreach (var character in path)
            {
                if (character is '"' or '`' or '$' or '\\')
                    quoted.Append('\\');
                quoted.Append(character);
            }

            quoted.Append('"');
            return quoted.ToString().Replace("\\", "\\\\").Replace("%", "%%");
        }

        /// <summary>Sets one entry in the [Default Applications] group of an XDG mimeapps.list file.</summary>
        internal static string SetDefaultApplication(string mimeApps, string mimeType, string desktopFile)
        {
            const string defaultGroup = "[Default Applications]";
            var entry = mimeType + "=" + desktopFile;
            var lines = (mimeApps ?? string.Empty).Replace("\r\n", "\n").Split('\n').ToList();
            if (lines.Count > 0 && lines[^1].Length == 0)
                lines.RemoveAt(lines.Count - 1);

            var groupIndex = lines.FindIndex(line => line.Trim() == defaultGroup);
            if (groupIndex < 0)
            {
                if (lines.Count > 0 && lines[^1].Trim().Length > 0)
                    lines.Add(string.Empty);
                lines.Add(defaultGroup);
                lines.Add(entry);
                return string.Join("\n", lines) + "\n";
            }

            var groupEnd = lines.FindIndex(groupIndex + 1, line => line.TrimStart().StartsWith("["));
            if (groupEnd < 0)
                groupEnd = lines.Count;
            var entryIndex = lines.FindIndex(groupIndex + 1, groupEnd - groupIndex - 1,
                line => line.Split('=')[0].Trim() == mimeType);
            if (entryIndex >= 0)
                lines[entryIndex] = entry;
            else
                lines.Insert(groupIndex + 1, entry);
            return string.Join("\n", lines) + "\n";
        }

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
        private static string ExecutablePath()
        {
            var executable = Environment.GetCommandLineArgs().FirstOrDefault();
            if (string.IsNullOrEmpty(executable))
                throw new InvalidOperationException("The player's executable path is unknown.");
            return Path.GetFullPath(executable);
        }
#endif

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // HKEY_CURRENT_USER is sign-extended on 64-bit Windows.
        private static readonly IntPtr CurrentUserKey = new(unchecked((int)0x80000001));
        private const int KeyWrite = 0x20006;
        private const int RegistryString = 1;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegCreateKeyExW(IntPtr key, string subKey, int reserved, string keyClass,
            int options, int desired, IntPtr securityAttributes, out IntPtr result, out int disposition);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegSetValueExW(IntPtr key, string valueName, int reserved, int type, string data,
            int dataBytes);

        [DllImport("advapi32.dll")]
        private static extern int RegCloseKey(IntPtr key);

        private static void RegisterLinkHandler()
        {
            const string classKey = @"Software\Classes\" + Scheme;
            SetRegistryValue(classKey, null, "URL:" + LinkHandlerName);
            SetRegistryValue(classKey, "URL Protocol", string.Empty);
            SetRegistryValue(classKey + @"\shell\open\command", null, WindowsOpenCommand(ExecutablePath()));
        }

        private static void SetRegistryValue(string subKey, string name, string value)
        {
            var status = RegCreateKeyExW(CurrentUserKey, subKey, 0, null, 0, KeyWrite, IntPtr.Zero, out var key,
                out _);
            if (status != 0)
                throw new IOException($"Opening {subKey} failed with error {status}.");
            try
            {
                status = RegSetValueExW(key, name, 0, RegistryString, value, (value.Length + 1) * sizeof(char));
                if (status != 0)
                    throw new IOException($"Writing {subKey} failed with error {status}.");
            }
            finally
            {
                RegCloseKey(key);
            }
        }
#elif UNITY_STANDALONE_LINUX && !UNITY_EDITOR
        private static void RegisterLinkHandler()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrEmpty(dataHome))
                dataHome = Path.Combine(home ?? string.Empty, ".local", "share");
            if (string.IsNullOrEmpty(configHome))
                configHome = Path.Combine(home ?? string.Empty, ".config");

            var applications = Path.Combine(dataHome, "applications");
            Directory.CreateDirectory(applications);
            File.WriteAllText(Path.Combine(applications, LinuxDesktopFileName), LinuxDesktopEntry(ExecutablePath()));

            var mimeAppsPath = Path.Combine(configHome, "mimeapps.list");
            var mimeApps = File.Exists(mimeAppsPath) ? File.ReadAllText(mimeAppsPath) : string.Empty;
            var updated = SetDefaultApplication(mimeApps, "x-scheme-handler/" + Scheme, LinuxDesktopFileName);
            if (updated == mimeApps)
                return;
            Directory.CreateDirectory(configHome);
            File.WriteAllText(mimeAppsPath, updated);
        }
#endif
    }

    /// <summary>
    /// Receives links from later CGS launches over the local machine only. Each request is one line,
    /// "CGS-LINK &lt;link&gt;", answered with "CGS-OK" once the link is accepted.
    /// </summary>
    internal sealed class LinkRelayServer : IDisposable
    {
        internal const int MaxLinkLength = 8192;
        private const string RequestPrefix = "CGS-LINK ";
        private const string Acknowledgement = "CGS-OK";
        private const int TimeoutMilliseconds = 2000;

        private readonly TcpListener _listener;
        private readonly ConcurrentQueue<string> _links = new();

        private LinkRelayServer(TcpListener listener)
        {
            _listener = listener;
            new Thread(Listen) { IsBackground = true, Name = "CGS link relay" }.Start();
        }

        /// <summary>Starts listening, or returns null if another program already uses the port.</summary>
        internal static LinkRelayServer Start(int port)
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            try
            {
                listener.Start();
            }
            catch (SocketException exception)
            {
                Debug.LogWarning("LinkRelayServer: links from later launches are unavailable: " + exception.Message);
                return null;
            }

            return new LinkRelayServer(listener);
        }

        internal bool HasLinks => !_links.IsEmpty;

        internal bool TryTakeLink(out string link) => _links.TryDequeue(out link);

        /// <summary>Sends a link to a running CGS; returns false if none accepted it.</summary>
        internal static bool TryForward(string link, int port)
        {
            try
            {
                using var client = new TcpClient();
                client.SendTimeout = TimeoutMilliseconds;
                client.ReceiveTimeout = TimeoutMilliseconds;
                client.Connect(IPAddress.Loopback, port);
                var stream = client.GetStream();
                var request = Encoding.UTF8.GetBytes(RequestPrefix + link + "\n");
                stream.Write(request, 0, request.Length);
                return ReadLine(stream, Acknowledgement.Length + 1) == Acknowledgement;
            }
            catch (Exception exception) when (exception is SocketException or IOException
                                                  or ObjectDisposedException)
            {
                return false;
            }
        }

        public void Dispose() => _listener.Stop();

        private void Listen()
        {
            while (true)
            {
                TcpClient client;
                try
                {
                    client = _listener.AcceptTcpClient();
                }
                catch (Exception exception) when (exception is SocketException or ObjectDisposedException
                                                      or InvalidOperationException)
                {
                    return; // Stopped.
                }

                try
                {
                    using (client)
                        Receive(client);
                }
                catch (Exception exception) when (exception is SocketException or IOException
                                                      or ObjectDisposedException)
                {
                    Debug.LogWarning("LinkRelayServer: could not receive a link: " + exception.Message);
                }
            }
        }

        private void Receive(TcpClient client)
        {
            client.ReceiveTimeout = TimeoutMilliseconds;
            client.SendTimeout = TimeoutMilliseconds;
            var stream = client.GetStream();
            var request = ReadLine(stream, RequestPrefix.Length + MaxLinkLength);
            if (request == null || !request.StartsWith(RequestPrefix, StringComparison.Ordinal))
                return;

            var link = request[RequestPrefix.Length..];
            if (!DesktopDeepLinks.IsLink(link))
                return;

            _links.Enqueue(link);
            var acknowledgement = Encoding.UTF8.GetBytes(Acknowledgement + "\n");
            stream.Write(acknowledgement, 0, acknowledgement.Length);
        }

        // Reads one newline-terminated UTF-8 line, or returns null if it is too long or the stream ends first.
        private static string ReadLine(Stream stream, int maxBytes)
        {
            var bytes = new List<byte>();
            while (bytes.Count <= maxBytes)
            {
                var value = stream.ReadByte();
                if (value < 0)
                    return null;
                if (value == '\n')
                    return Encoding.UTF8.GetString(bytes.ToArray()).TrimEnd('\r');
                bytes.Add((byte)value);
            }

            return null;
        }
    }
}
