/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Net;
using System.Net.Sockets;
using Cgs;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class DesktopDeepLinksTests
    {
        [Test]
        public void FindLinkSkipsTheExecutableAndOtherArguments()
        {
            var link = DesktopDeepLinks.FindLink(new[]
            {
                @"C:\Games\CGS\cgs.exe", "-screen-fullscreen", "0",
                " cardgamesim://main?url=https%3A%2F%2Fexample.com%2Fcgs.json "
            });

            Assert.AreEqual("cardgamesim://main?url=https%3A%2F%2Fexample.com%2Fcgs.json", link);
        }

        [TestCase("cardgamesim://link/play", true)]
        [TestCase("CARDGAMESIM://main?url=https://example.com/cgs.json", true)]
        [TestCase("https://cardgamesim.example.com/", false)]
        [TestCase("cardgamesim-other://main", false)]
        [TestCase("-cardgamesim://main", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsLinkAcceptsOnlyCardGameSimulatorLinks(string value, bool expected)
        {
            Assert.AreEqual(expected, DesktopDeepLinks.IsLink(value));
        }

        [Test]
        public void IsLinkRejectsOversizedLinks()
        {
            Assert.IsFalse(DesktopDeepLinks.IsLink("cardgamesim://main?url=" + new string('a', 9000)));
        }

        [Test]
        public void WindowsCommandPassesTheLinkAsOneArgument()
        {
            Assert.AreEqual("\"C:\\Program Files\\CGS\\cgs.exe\" \"%1\"",
                DesktopDeepLinks.WindowsOpenCommand(@"C:\Program Files\CGS\cgs.exe"));
        }

        [Test]
        public void LinuxDesktopEntryRegistersTheSchemeAndQuotesThePath()
        {
            var entry = DesktopDeepLinks.LinuxDesktopEntry("/home/player/Steam Games/CGS/cgs.x86_64");

            StringAssert.Contains("Exec=\"/home/player/Steam Games/CGS/cgs.x86_64\" %u\n", entry);
            StringAssert.Contains("MimeType=x-scheme-handler/cardgamesim;\n", entry);
            StringAssert.Contains("NoDisplay=true\n", entry);
        }

        [Test]
        public void LinuxExecArgumentEscapesReservedCharacters()
        {
            // Quoting escapes $ and \ with a backslash; the string escape then doubles every backslash.
            Assert.AreEqual("\"/games/a\\\\$b\\\\\\\\c 100%%\"",
                DesktopDeepLinks.DesktopExecArgument("/games/a$b\\c 100%"));
        }

        [Test]
        public void SetDefaultApplicationCreatesTheGroup()
        {
            Assert.AreEqual("[Default Applications]\nx-scheme-handler/cardgamesim=cgs.desktop\n",
                DesktopDeepLinks.SetDefaultApplication(string.Empty, "x-scheme-handler/cardgamesim", "cgs.desktop"));
        }

        [Test]
        public void SetDefaultApplicationPreservesOtherAssociations()
        {
            const string mimeApps = "[Added Associations]\ntext/plain=editor.desktop\n\n" +
                                    "[Default Applications]\ntext/html=browser.desktop\n" +
                                    "x-scheme-handler/cardgamesim=old.desktop\n\n[Removed Associations]\n";

            Assert.AreEqual("[Added Associations]\ntext/plain=editor.desktop\n\n" +
                            "[Default Applications]\ntext/html=browser.desktop\n" +
                            "x-scheme-handler/cardgamesim=cgs.desktop\n\n[Removed Associations]\n",
                DesktopDeepLinks.SetDefaultApplication(mimeApps, "x-scheme-handler/cardgamesim", "cgs.desktop"));
        }

        [Test]
        public void SetDefaultApplicationAddsToAnExistingGroupAndIsStable()
        {
            const string mimeApps = "[Default Applications]\ntext/html=browser.desktop\n";
            var updated =
                DesktopDeepLinks.SetDefaultApplication(mimeApps, "x-scheme-handler/cardgamesim", "cgs.desktop");

            Assert.AreEqual("[Default Applications]\nx-scheme-handler/cardgamesim=cgs.desktop\n" +
                            "text/html=browser.desktop\n", updated);
            Assert.AreEqual(updated,
                DesktopDeepLinks.SetDefaultApplication(updated, "x-scheme-handler/cardgamesim", "cgs.desktop"));
        }

        [UnityTest]
        public IEnumerator RelayDeliversALinkFromALaterLaunch()
        {
            var port = FreePort();
            using var relay = LinkRelayServer.Start(port);
            Assert.NotNull(relay);

            const string link = "cardgamesim://main?url=https%3A%2F%2Fexample.com%2Fcgs.json";
            Assert.IsTrue(LinkRelayServer.TryForward(link, port));

            var received = string.Empty;
            for (var frame = 0; frame < 100 && !relay.TryTakeLink(out received); frame++)
                yield return null;
            Assert.AreEqual(link, received);
        }

        [Test]
        public void RelayRejectsOtherLinks()
        {
            var port = FreePort();
            using var relay = LinkRelayServer.Start(port);

            Assert.IsFalse(LinkRelayServer.TryForward("https://example.com/", port));
            Assert.IsFalse(relay.TryTakeLink(out _));
        }

        [Test]
        public void ForwardFailsWhenNoCgsIsRunning()
        {
            Assert.IsFalse(LinkRelayServer.TryForward("cardgamesim://link/play", FreePort()));
        }

        [Test]
        public void SecondRelayOnTheSamePortReportsUnavailable()
        {
            var port = FreePort();
            using var relay = LinkRelayServer.Start(port);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("links from later launches"));
            Assert.IsNull(LinkRelayServer.Start(port));
        }

        private static int FreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
