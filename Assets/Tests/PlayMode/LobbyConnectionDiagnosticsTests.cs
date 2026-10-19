/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Cgs.Play.Multiplayer;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Lobbies;

namespace Tests.PlayMode
{
    public class LobbyConnectionDiagnosticsTests
    {
        private const string Reference = "test-attempt";

        private static JObject Parse(string entry) => JObject.Parse(entry.Substring(entry.IndexOf('{')));

        [Test]
        public void InvalidCodeInNestedAggregateHasActionableMessageAndReference()
        {
            var error = new AggregateException(new AggregateException(new LobbyServiceException(
                LobbyExceptionReason.InvalidJoinCode, "lobby code 'OGGI' contains an invalid character 'O'")));
            var message = LobbyConnectionDiagnostics.JoinFailureMessage(error, Reference);

            Assert.That(message.Key, Is.EqualTo("network.join.code.rejected"));
            StringAssert.Contains("Internet game list", message.English);
            StringAssert.Contains(Reference, message.English);
            StringAssert.DoesNotContain("One or more errors occurred", message.English);
            StringAssert.DoesNotContain("invalid character", message.English);
        }

        [TestCase(LobbyExceptionReason.IncorrectPassword)]
        [TestCase(LobbyExceptionReason.LobbyNotFound)]
        [TestCase(LobbyExceptionReason.ServiceUnavailable)]
        public void OtherFailuresDoNotBlameTheCode(LobbyExceptionReason reason)
        {
            var message = LobbyConnectionDiagnostics.JoinFailureMessage(
                new AggregateException(new LobbyServiceException(reason, "service detail")), Reference);
            Assert.That(message.Key, Is.EqualTo("network.join.failed"));
            StringAssert.DoesNotContain("rejected", message.English);
            StringAssert.Contains(Reference, message.English);
        }

        [Test]
        public void DiagnosticsDistinguishLettersDigitsAndInvisibleCharacters()
        {
            const string code = "O0I1 \u200B\r\n";
            var entry = LobbyConnectionDiagnostics.FormatEvent("join-code-request", Reference, lobbyCode: code);
            var record = Parse(entry);
            Assert.That((string)record["lobbyCode"]["value"], Is.EqualTo(code));
            Assert.That((int)record["lobbyCode"]["length"], Is.EqualTo(8));
            Assert.That(record["lobbyCode"]["utf16"].ToObject<string[]>(), Is.EqualTo(new[]
            {
                "0:U+004F", "1:U+0030", "2:U+0049", "3:U+0031",
                "4:U+0020", "5:U+200B", "6:U+000D", "7:U+000A"
            }));
            StringAssert.DoesNotContain("\n", entry);
            StringAssert.DoesNotContain("\r", entry);
            Assert.That((string)record["reference"], Is.EqualTo(Reference));
            Assert.That((string)record["platform"], Is.Not.Empty);
        }

        [Test]
        public void SharedLobbyAndRelayCodesHaveSeparateLabels()
        {
            var record = Parse(LobbyConnectionDiagnostics.FormatEvent("room-share", null, "lobby-id",
                "OGGI", "ABC123", "OGGI", "UnityTransport"));
            Assert.That((string)record["lobbyCode"]["value"], Is.EqualTo("OGGI"));
            Assert.That((string)record["relayCode"]["value"], Is.EqualTo("ABC123"));
            Assert.That((string)record["sharedCode"]["value"], Is.EqualTo("OGGI"));
            Assert.That((string)record["lobbyId"], Is.EqualTo("lobby-id"));
        }

        [Test]
        public void ServiceFailureRetainsUnderlyingReasonCodeAndMessage()
        {
            var error = new AggregateException(new LobbyServiceException(
                LobbyExceptionReason.InvalidJoinCode, "Rejected O (U+004F) at index 0"));
            var record = Parse(LobbyConnectionDiagnostics.FormatEvent("join-code-failed", Reference,
                lobbyCode: "OGGI", error: error));
            var serviceError = record["errors"][1];
            Assert.That((string)serviceError["reason"], Is.EqualTo("InvalidJoinCode"));
            Assert.That((int)serviceError["errorCode"], Is.EqualTo(16010));
            Assert.That((string)serviceError["message"], Is.EqualTo("Rejected O (U+004F) at index 0"));
        }

        [Test]
        public void LargeInputIsBoundedWithoutLosingOriginalLength()
        {
            var record = Parse(LobbyConnectionDiagnostics.FormatEvent("join-code-request", Reference,
                lobbyCode: new string('A', 10000)));
            Assert.That((int)record["lobbyCode"]["length"], Is.EqualTo(10000));
            Assert.That(((string)record["lobbyCode"]["value"]).Length, Is.EqualTo(64));
            Assert.That((bool)record["lobbyCode"]["truncated"], Is.True);
        }
    }
}
