/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_EDITOR
using System.Reflection;
using Cgs.CardGameView.Multiplayer;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.PlayMode
{
    public class DieSynchronizationTests
    {
        private GameObject _table;
        private Die _die;

        [SetUp]
        public void SetUp()
        {
            _table = new GameObject("Dice synchronization test table", typeof(RectTransform));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardGameView/Multiplayer/Die.prefab");
            _die = Object.Instantiate(prefab, _table.transform).GetComponent<Die>();
            _die.enabled = false;
            _die.Value = Die.DefaultValue;
            _die.DieColor = Color.white;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_table);
        }

        [TestCase(6, 2, false)]
        [TestCase(20, 17, false)]
        [TestCase(6, 6, false)]
        [TestCase(6, 1, true)]
        public void InitialSnapshotDisplaysExistingDieWithoutAnotherRoll(int max, int value, bool black)
        {
            var color = black ? Vector3.zero : new Vector3(0.2f, 0.4f, 0.8f);
            var changeCount = 0;
            NetworkField<int>("_valueNetworkVariable").OnValueChanged += (_, _) => changeCount++;

            ReadSnapshot("_maxNetworkVariable", max);
            ReadSnapshot("_valueNetworkVariable", value);
            ReadSnapshot("_colorNetworkVariable", color);
            Assert.AreEqual(0, changeCount, "Initial synchronization does not send change notifications.");

            _die.OnNetworkSpawn();

            Assert.AreEqual(max, _die.Max);
            Assert.AreEqual(value, _die.Value);
            Assert.AreEqual(value.ToString(), Field<Text>("valueText").text);
            Assert.AreEqual(new Color(color.x, color.y, color.z), _die.DieColor);
            Assert.AreEqual(_die.DieColor, Field<Image>("dieImage").color);

            // A subsequent roll must still refresh the display after initial synchronization.
            var next = value == max ? 1 : value + 1;
            NetworkField<int>("_valueNetworkVariable").Value = next;
            Assert.AreEqual(1, changeCount);
            Assert.AreEqual(next.ToString(), Field<Text>("valueText").text);
        }

        [Test]
        public void NewDieSpawnsWithValidDefaultPresentation()
        {
            _die.OnNetworkSpawn();

            Assert.AreEqual(Die.DefaultMax, _die.Max);
            Assert.AreEqual(Die.DefaultValue.ToString(), Field<Text>("valueText").text);
            Assert.AreEqual(Color.white, _die.DieColor);
        }

        private T Field<T>(string name) =>
            (T)typeof(Die).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_die);

        private NetworkVariable<T> NetworkField<T>(string name) => Field<NetworkVariable<T>>(name);

        private void ReadSnapshot<T>(string name, T value)
        {
            using var source = new NetworkVariable<T>(value);
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            source.WriteField(writer);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            NetworkField<T>(name).ReadField(reader);
        }
    }
}
#endif
