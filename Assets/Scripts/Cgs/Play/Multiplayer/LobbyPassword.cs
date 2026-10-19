/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Lobbies;

namespace Cgs.Play.Multiplayer
{
    public static class LobbyPassword
    {
        public const string InvalidPasswordMessage =
            "Password must contain 1–64 characters, or be empty for no password. Please correct it and try again.";
        private const string PasswordDomain = "CGS:lobby-password:v1:";
        private const int MaxLength = 64;
        private const int LegacyRetryDelayMilliseconds = 1100;

        public static bool IsValid(string password) => password == null || password.Length <= MaxLength;

        public static void Validate(string password)
        {
            if (!IsValid(password))
                throw new ArgumentException(InvalidPasswordMessage, nameof(password));
        }

        // This v1 mapping is part of the multiplayer protocol. Keep case, whitespace,
        // UTF-8 encoding and the domain stable across every platform and future release.
        public static string ToServicePassword(string password)
        {
            Validate(password);
            if (string.IsNullOrEmpty(password))
                return null;
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(PasswordDomain + password);
            return BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        public static async Task<T> JoinAsync<T>(string password, Func<string, Task<T>> join)
        {
            if (join == null)
                throw new ArgumentNullException(nameof(join));
            var servicePassword = ToServicePassword(password);
            try
            {
                return await join(servicePassword);
            }
            catch (LobbyServiceException error) when (error.Reason == LobbyExceptionReason.IncorrectPassword &&
                                                     password is { Length: >= 8 and <= MaxLength })
            {
                // Only older hosts use raw passwords. Never retry without a password,
                // on unrelated failures, or with a short password Unity cannot accept.
                // Unity limits player joins to one request per second.
                await Task.Delay(LegacyRetryDelayMilliseconds);
                return await join(password);
            }
        }
    }
}
