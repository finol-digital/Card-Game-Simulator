/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using Cgs.Localization;
using Newtonsoft.Json;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using UnityEngine;

namespace Cgs.Play.Multiplayer
{
    public static class LobbyConnectionDiagnostics
    {
        private const int MaxCodeLength = 64;
        private const string InvalidCodeMessage = "The game service rejected this Room Id. " +
            "Try selecting the host from the Internet game list, or ask the host to create a new room. ";
        private const string JoinFailedMessage = "Unable to join the game. Check that the host is still online " +
            "and the room password is correct, then try again. ";

        public static UiMessage JoinFailureMessage(Exception error, string reference)
        {
            var invalidCode = Exceptions(error).OfType<LobbyServiceException>()
                .Any(item => item.Reason == LobbyExceptionReason.InvalidJoinCode);
            var message = (invalidCode ? InvalidCodeMessage : JoinFailedMessage) + "Reference: " + reference;
            return UiMessage.With(invalidCode ? "network.join.code.rejected" : "network.join.failed",
                message, ("reference", reference));
        }

        // Keep the exact input, including whitespace and UTF-16 units, without letting
        // pasted control characters create log lines. Never pass passwords or tokens here.
        private static object DescribeCode(string value)
        {
            if (value == null)
                return null;
            var sample = value.Substring(0, Math.Min(value.Length, MaxCodeLength));
            return new
            {
                value = sample,
                length = value.Length,
                utf16 = sample.Select((character, index) => $"{index}:U+{(int)character:X4}").ToArray(),
                truncated = value.Length > MaxCodeLength
            };
        }

        private static System.Collections.Generic.IEnumerable<Exception> Exceptions(Exception error)
        {
            if (error == null)
                yield break;
            yield return error;
            if (error is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                    foreach (var item in Exceptions(inner))
                        yield return item;
            }
            else
            {
                foreach (var item in Exceptions(error.InnerException))
                    yield return item;
            }
        }

        public static string FormatEvent(string stage, string reference, string lobbyId = null,
            string lobbyCode = null, string relayCode = null, string sharedCode = null,
            string transport = null, Exception error = null)
        {
            return "[CgsNet Connection] " + JsonConvert.SerializeObject(new
            {
                stage,
                reference,
                utc = DateTime.UtcNow.ToString("O"),
                app = Application.version,
                build = Application.buildGUID,
                platform = Application.platform.ToString(),
                unity = Application.unityVersion,
                project = Application.cloudProjectId,
                lobbySdk = typeof(LobbyService).Assembly.GetName().Version.ToString(),
                transport,
                lobbyId,
                lobbyCode = DescribeCode(lobbyCode),
                relayCode = DescribeCode(relayCode),
                sharedCode = DescribeCode(sharedCode),
                errors = Exceptions(error).Select(item => new
                {
                    type = item.GetType().FullName,
                    reason = (item as LobbyServiceException)?.Reason.ToString(),
                    errorCode = (item as RequestFailedException)?.ErrorCode,
                    message = item.Message,
                    stack = item.StackTrace
                }).ToArray()
            }, new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii });
        }

        public static void Record(string stage, string reference, string lobbyId = null,
            string lobbyCode = null, string relayCode = null, string sharedCode = null,
            string transport = null, Exception error = null)
        {
            var entry = FormatEvent(stage, reference, lobbyId, lobbyCode, relayCode, sharedCode, transport, error);
            if (error != null)
                Debug.LogError(entry);
            else
            {
                Debug.Log(entry);
                CgsNetDiagnostics.Record("connection", details: entry);
            }
        }
    }
}
