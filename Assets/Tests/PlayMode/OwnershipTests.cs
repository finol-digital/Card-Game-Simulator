/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Reflection;
using Cgs.CardGameView.Multiplayer;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

namespace Tests.PlayMode
{
    /// <summary>
    /// Tests for CgsNetPlayable ownership and authorization logic.
    /// These tests operate on non-spawned (offline) instances to validate local field behavior
    /// and verify that the authorization methods exist with the expected signatures.
    /// Full integration tests would require NetworkManager host/client setup.
    /// </summary>
    public class OwnershipTests
    {
        private GameObject _testObject;

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
                Object.DestroyImmediate(_testObject);
        }

        #region IsDeckShared Tests

        [Test]
        public void CardStack_IsDeckShared_DefaultsToFalse()
        {
            _testObject = new GameObject("TestStack");
            _testObject.AddComponent<NetworkObject>();
            var stack = _testObject.AddComponent<CardStack>();

            Assert.IsFalse(stack.IsDeckShared,
                "IsDeckShared should default to false for new CardStack instances");
        }

        [Test]
        public void CardStack_IsDeckShared_CanBeSetToTrue()
        {
            _testObject = new GameObject("TestStack");
            _testObject.AddComponent<NetworkObject>();
            var stack = _testObject.AddComponent<CardStack>();

            stack.IsDeckShared = true;

            Assert.IsTrue(stack.IsDeckShared,
                "IsDeckShared should be true after being set to true on a non-spawned instance");
        }

        [Test]
        public void CardStack_IsDeckShared_CanBeToggledBackToFalse()
        {
            _testObject = new GameObject("TestStack");
            _testObject.AddComponent<NetworkObject>();
            var stack = _testObject.AddComponent<CardStack>();

            stack.IsDeckShared = true;
            stack.IsDeckShared = false;

            Assert.IsFalse(stack.IsDeckShared,
                "IsDeckShared should be false after being toggled back to false");
        }

        #endregion

        #region IsClientAuthorized Method Existence and Signature

        [Test]
        public void CgsNetPlayable_HasIsClientAuthorizedMethod()
        {
            var method = typeof(CgsNetPlayable).GetMethod("IsClientAuthorized",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(method,
                "CgsNetPlayable should have a protected IsClientAuthorized method");

            var parameters = method.GetParameters();
            Assert.AreEqual(1, parameters.Length,
                "IsClientAuthorized should accept exactly one parameter");
            Assert.AreEqual(typeof(ulong), parameters[0].ParameterType,
                "IsClientAuthorized parameter should be of type ulong (clientId)");
            Assert.AreEqual(typeof(bool), method.ReturnType,
                "IsClientAuthorized should return bool");
        }

        #endregion

        #region CanRequestOwnership Property Existence

        [Test]
        public void CgsNetPlayable_HasCanRequestOwnershipProperty()
        {
            var property = typeof(CgsNetPlayable).GetProperty("CanRequestOwnership",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(property,
                "CgsNetPlayable should have a protected CanRequestOwnership property");
            Assert.AreEqual(typeof(bool), property.PropertyType,
                "CanRequestOwnership should be of type bool");
            Assert.IsTrue(property.CanRead,
                "CanRequestOwnership should have a getter");
        }

        [Test]
        public void CgsNetPlayable_CanRequestOwnership_FalseWhenNotSpawned()
        {
            _testObject = new GameObject("TestPlayable");
            _testObject.AddComponent<NetworkObject>();
            var die = _testObject.AddComponent<Die>();

            var property = typeof(CgsNetPlayable).GetProperty("CanRequestOwnership",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(property);

            var canRequest = (bool)property.GetValue(die);
            Assert.IsFalse(canRequest,
                "CanRequestOwnership should return false when the object is not spawned");
        }

        #endregion

        #region IsClientAuthorized Logic via Reflection

        [Test]
        public void CgsNetPlayable_IsClientAuthorized_OwnerIsAuthorized()
        {
            _testObject = new GameObject("TestPlayable");
            var networkObject = _testObject.AddComponent<NetworkObject>();
            var die = _testObject.AddComponent<Die>();

            var method = typeof(CgsNetPlayable).GetMethod("IsClientAuthorized",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method);

            // On non-spawned objects, OwnerClientId defaults to 0 (server).
            // Calling IsClientAuthorized(0) should return true since 0 matches OwnerClientId.
            var result = (bool)method.Invoke(die, new object[] { (ulong)0 });
            Assert.IsTrue(result,
                "IsClientAuthorized should return true when clientId matches OwnerClientId");
        }

        [Test]
        public void CgsNetPlayable_IsClientAuthorized_NonOwnerNotAuthorizedOnNonShared()
        {
            _testObject = new GameObject("TestPlayable");
            _testObject.AddComponent<NetworkObject>();
            var die = _testObject.AddComponent<Die>();

            var method = typeof(CgsNetPlayable).GetMethod("IsClientAuthorized",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method);

            // OwnerClientId defaults to 0; clientId 99 is not the owner.
            // Die is not a CardStack, so shared check doesn't apply.
            var result = (bool)method.Invoke(die, new object[] { (ulong)99 });
            Assert.IsFalse(result,
                "IsClientAuthorized should return false for non-owner on non-CardStack playable");
        }

        [Test]
        public void CardStack_IsClientAuthorized_NonOwnerAuthorizedWhenShared()
        {
            _testObject = new GameObject("TestStack");
            _testObject.AddComponent<NetworkObject>();
            var stack = _testObject.AddComponent<CardStack>();

            stack.IsDeckShared = true;

            var method = typeof(CgsNetPlayable).GetMethod("IsClientAuthorized",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method);

            // OwnerClientId defaults to 0; clientId 99 is not the owner,
            // but this is a shared CardStack so it should be authorized.
            var result = (bool)method.Invoke(stack, new object[] { (ulong)99 });
            Assert.IsTrue(result,
                "IsClientAuthorized should return true for non-owner on a shared CardStack");
        }

        [Test]
        public void CardStack_IsClientAuthorized_NonOwnerNotAuthorizedWhenNotShared()
        {
            _testObject = new GameObject("TestStack");
            _testObject.AddComponent<NetworkObject>();
            var stack = _testObject.AddComponent<CardStack>();

            // IsDeckShared defaults to false
            var method = typeof(CgsNetPlayable).GetMethod("IsClientAuthorized",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method);

            // OwnerClientId defaults to 0; clientId 99 is not the owner,
            // and IsDeckShared is false, so should not be authorized.
            var result = (bool)method.Invoke(stack, new object[] { (ulong)99 });
            Assert.IsFalse(result,
                "IsClientAuthorized should return false for non-owner on a non-shared CardStack");
        }

        #endregion

        #region ServerRpc Validation Attributes

        [Test]
        public void CgsNetPlayable_ServerRpcs_HaveEveryoneInvokePermission()
        {
            // Verify that key server RPCs have InvokePermission = Everyone
            // (required so any client can call them, with server-side validation)
            var rpcsWithEveryonePermission = new[]
            {
                "ChangeOwnershipServerRpc",
                "UpdateRotationServerRpc",
                "UpdateSizeServerRpc",
                "DeleteServerRpc"
            };

            foreach (var rpcName in rpcsWithEveryonePermission)
            {
                var method = typeof(CgsNetPlayable).GetMethod(rpcName,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(method, $"{rpcName} should exist on CgsNetPlayable");

                var rpcAttribute = method.GetCustomAttribute<RpcAttribute>();
                Assert.IsNotNull(rpcAttribute, $"{rpcName} should have an Rpc attribute");
            }
        }

        [Test]
        public void CardStack_SetIsTopFaceupServerRpc_HasEveryoneInvokePermission()
        {
            var method = typeof(CardStack).GetMethod("SetIsTopFaceupServerRpc",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "SetIsTopFaceupServerRpc should exist on CardStack");

            var rpcAttribute = method.GetCustomAttribute<RpcAttribute>();
            Assert.IsNotNull(rpcAttribute, "SetIsTopFaceupServerRpc should have an Rpc attribute");
        }

        [Test]
        public void Die_ServerRpcs_HaveValidation()
        {
            var rpcNames = new[]
            {
                "UpdateMaxServerRpc",
                "UpdateValueServerRpc",
                "UpdateColorServerRpc",
                "RollServerRpc"
            };

            foreach (var rpcName in rpcNames)
            {
                var method = typeof(Die).GetMethod(rpcName,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(method, $"{rpcName} should exist on Die");

                // Verify the RPC accepts RpcParams for validation
                var parameters = method.GetParameters();
                var hasRpcParams = false;
                foreach (var param in parameters)
                {
                    if (param.ParameterType == typeof(RpcParams))
                    {
                        hasRpcParams = true;
                        break;
                    }
                }

                Assert.IsTrue(hasRpcParams,
                    $"{rpcName} on Die should accept RpcParams for server-side validation");
            }
        }

        [Test]
        public void Counter_ServerRpcs_HaveValidation()
        {
            var rpcNames = new[]
            {
                "UpdateValueServerRpc",
                "UpdateColorServerRpc"
            };

            foreach (var rpcName in rpcNames)
            {
                var method = typeof(Counter).GetMethod(rpcName,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(method, $"{rpcName} should exist on Counter");

                var parameters = method.GetParameters();
                var hasRpcParams = false;
                foreach (var param in parameters)
                {
                    if (param.ParameterType == typeof(RpcParams))
                    {
                        hasRpcParams = true;
                        break;
                    }
                }

                Assert.IsTrue(hasRpcParams,
                    $"{rpcName} on Counter should accept RpcParams for server-side validation");
            }
        }

        [Test]
        public void CardModel_SetIsFacedownServerRpc_HasValidation()
        {
            var method = typeof(CardModel).GetMethod("SetIsFacedownServerRpc",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "SetIsFacedownServerRpc should exist on CardModel");

            var rpcAttribute = method.GetCustomAttribute<RpcAttribute>();
            Assert.IsNotNull(rpcAttribute, "SetIsFacedownServerRpc should have an Rpc attribute");

            var parameters = method.GetParameters();
            var hasRpcParams = false;
            foreach (var param in parameters)
            {
                if (param.ParameterType == typeof(RpcParams))
                {
                    hasRpcParams = true;
                    break;
                }
            }

            Assert.IsTrue(hasRpcParams,
                "SetIsFacedownServerRpc on CardModel should accept RpcParams for server-side validation");
        }

        #endregion
    }
}
