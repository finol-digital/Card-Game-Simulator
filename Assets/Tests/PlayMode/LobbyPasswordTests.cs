/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Cgs;
using Cgs.Menu;
using Cgs.Play.Multiplayer;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class LobbyPasswordTests
    {
        [TestCase(null)]
        [TestCase("")]
        public async Task EmptyPasswordRemainsOptional(string password)
        {
            Assert.That(LobbyPassword.ToServicePassword(password), Is.Null);
            var attempts = new List<string>();
            var result = await LobbyPassword.JoinAsync(password, value =>
            {
                attempts.Add(value);
                return Task.FromResult("joined");
            });
            Assert.That(result, Is.EqualTo("joined"));
            Assert.That(attempts, Is.EqualTo(new string[] { null }));
        }

        [TestCase(1)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(64)]
        public async Task EveryNonemptyPasswordUsesTheSameServiceValueOnHostAndClient(int length)
        {
            var password = new string('x', length);
            var hostedPassword = LobbyPassword.ToServicePassword(password);
            Assert.That(hostedPassword, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(hostedPassword, Is.Not.EqualTo(password));
            var attempts = 0;
            var joined = await LobbyPassword.JoinAsync(password, submitted =>
            {
                attempts++;
                Assert.That(submitted, Is.EqualTo(hostedPassword));
                return Task.FromResult(true);
            });
            Assert.That(joined, Is.True);
            Assert.That(attempts, Is.EqualTo(1));
        }

        [Test]
        public void VersionOneMappingHasStableKnownValue()
        {
            Assert.That(LobbyPassword.ToServicePassword("1234"),
                Is.EqualTo("6647f4c684f5a3118ee4b376a5f05a90d7b9c6936b599b1b75c8663dd6717bc4"));
            Assert.That(LobbyPassword.ToServicePassword("Ab"), Is.Not.EqualTo(LobbyPassword.ToServicePassword("ab")));
            Assert.That(LobbyPassword.ToServicePassword("ab "), Is.Not.EqualTo(LobbyPassword.ToServicePassword("ab")));
            Assert.That(LobbyPassword.ToServicePassword(" "), Is.Not.Null);
            Assert.That(LobbyPassword.ToServicePassword("é猫"), Does.Match("^[0-9a-f]{64}$"));
        }

        [UnityTest]
        public IEnumerator OlderRoomIsJoinedByRetryingTheOriginalPasswordOnce()
        {
            foreach (var length in new[] { 8, 64 })
            {
                var password = new string('p', length);
                var attempts = new List<string>();
                var elapsed = System.Diagnostics.Stopwatch.StartNew();
                var join = LobbyPassword.JoinAsync(password, submitted =>
                {
                    if (attempts.Count > 0)
                        Assert.That(elapsed.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(1000),
                            "Legacy retry must respect Unity's join rate limit.");
                    attempts.Add(submitted);
                    return submitted == password ? Task.FromResult("legacy-room") :
                        Task.FromException<string>(new LobbyServiceException(LobbyExceptionReason.IncorrectPassword, "wrong"));
                });
                while (!join.IsCompleted)
                    yield return null;
                Assert.That(join.IsFaulted, Is.False, join.Exception?.ToString());
                Assert.That(join.Result, Is.EqualTo("legacy-room"));
                Assert.That(attempts, Is.EqualTo(new[] { LobbyPassword.ToServicePassword(password), password }));
            }
        }

        [TestCase(LobbyExceptionReason.InvalidJoinCode)]
        [TestCase(LobbyExceptionReason.LobbyNotFound)]
        [TestCase(LobbyExceptionReason.ServiceUnavailable)]
        [TestCase(LobbyExceptionReason.RateLimited)]
        public void UnrelatedServiceErrorsAreNotRetried(LobbyExceptionReason reason)
        {
            var attempts = 0;
            var error = new LobbyServiceException(reason, "service failed");
            var thrown = Assert.ThrowsAsync<LobbyServiceException>(async () => await LobbyPassword.JoinAsync("password", _ =>
            {
                attempts++;
                return Task.FromException<bool>(error);
            }));
            Assert.That(thrown, Is.SameAs(error));
            Assert.That(attempts, Is.EqualTo(1));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("1")]
        [TestCase("1234567")]
        public void MissingOrShortPasswordIsNeverRetriedAsLegacy(string password)
        {
            var attempts = 0;
            Assert.ThrowsAsync<LobbyServiceException>(async () => await LobbyPassword.JoinAsync(password, _ =>
            {
                attempts++;
                return Task.FromException<bool>(new LobbyServiceException(LobbyExceptionReason.IncorrectPassword, "wrong"));
            }));
            Assert.That(attempts, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FailedLegacyRetryStopsWithoutTryingAnUnprotectedJoin()
        {
            var attempts = new List<string>();
            var join = LobbyPassword.JoinAsync("password", submitted =>
            {
                attempts.Add(submitted);
                return Task.FromException<bool>(new LobbyServiceException(LobbyExceptionReason.IncorrectPassword, "wrong"));
            });
            while (!join.IsCompleted)
                yield return null;
            Assert.That(join.IsFaulted, Is.True);
            Assert.That(join.Exception?.InnerException, Is.TypeOf<LobbyServiceException>());
            Assert.That(attempts, Is.EqualTo(new[] { LobbyPassword.ToServicePassword("password"), "password" }));
        }

        [Test]
        public void InvalidPasswordNeverReachesAServiceAndIsNotIncludedInTheError()
        {
            var password = new string('x', 65);
            var attempts = 0;
            var error = Assert.ThrowsAsync<ArgumentException>(async () => await LobbyPassword.JoinAsync(password, _ =>
            {
                attempts++;
                return Task.FromResult(true);
            }));
            Assert.That(attempts, Is.Zero);
            Assert.That(error.Message, Does.Not.Contain(password));
            Assert.Throws<ArgumentException>(() => LobbyPassword.ToServicePassword(password));
        }

        [Test]
        public void InvalidPasswordKeepsMenuAndInputAndShowsOnlyValidationWarning()
        {
            var instanceField = typeof(CardGameManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instanceField.GetValue(null);
            var root = new GameObject("Inactive password menu test");
            root.SetActive(false);
            try
            {
                var gameManager = root.AddComponent<CardGameManager>();
                instanceField.SetValue(null, gameManager);
                var dialogObject = new GameObject("Queued dialog");
                dialogObject.transform.SetParent(root.transform);
                var dialog = dialogObject.AddComponent<Dialog>();
                typeof(CardGameManager).GetField("messenger", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(gameManager, dialog);
                var menuObject = new GameObject("Lobby menu");
                menuObject.transform.SetParent(root.transform);
                var menu = menuObject.AddComponent<LobbyMenu>();
                var password = new string('x', 65);
                menu.SetPassword(password);

                LogAssert.Expect(LogType.Log, LobbyMenu.InvalidPasswordWarningMessage);
                menu.Host();

                Assert.That(menuObject.activeSelf, Is.True);
                Assert.That(typeof(LobbyMenu).GetField("_password", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(menu), Is.EqualTo(password));
                var queue = (System.Collections.ICollection)typeof(Dialog)
                    .GetProperty("MessageQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(dialog);
                Assert.That(queue.Count, Is.EqualTo(1), "Only the password warning should be displayed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                instanceField.SetValue(null, previous);
            }
        }

        [Test]
        public void NetworkEntryPointsRejectInvalidPasswordsBeforeStartingCoroutines()
        {
            var previous = NetworkManager.Singleton;
            var gameObject = new GameObject("Password validation test");
            gameObject.SetActive(false);
            try
            {
                var manager = gameObject.AddComponent<CgsNetManager>();
                var password = new string('x', 65);
                Assert.Throws<ArgumentException>(() => manager.StartBroadcastHost(password));
                Assert.Throws<ArgumentException>(() => manager.StartJoinLobby("lobby-id", password));
                Assert.Throws<ArgumentException>(() => manager.StartJoinLobbyCode("CODE", password));
                Assert.That(manager.IsListening, Is.False);
                Assert.That(manager.IsHost, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                if (previous != null)
                    previous.SetSingleton();
            }
        }
    }
}
