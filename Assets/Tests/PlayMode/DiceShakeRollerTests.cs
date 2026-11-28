/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_EDITOR
using System.Reflection;
using Cgs.CardGameView.Multiplayer;
using Cgs.Play;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.PlayMode
{
    public class DiceShakeRollerTests
    {
        private const float FrameTime = 1f / 60f;

        private static readonly Vector3 Gravity = new(0, -1, 0);
        private static readonly Vector3 Jolt = new(3, -1, 0);

        private GameObject _table;

        [SetUp]
        public void SetUp()
        {
            _table = new GameObject("Dice shake test table", typeof(RectTransform));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_table);
        }

        [Test]
        public void ShakeDetector_IgnoresDeviceAtRest()
        {
            var detector = new ShakeDetector();

            for (var frame = 0; frame < 120; frame++)
                Assert.IsFalse(detector.Sample(Gravity, FrameTime, frame * FrameTime));
        }

        [Test]
        public void ShakeDetector_IgnoresFirstSample()
        {
            var detector = new ShakeDetector();

            Assert.IsFalse(detector.Sample(Jolt, FrameTime, 0));
        }

        [Test]
        public void ShakeDetector_DetectsStrongJolt()
        {
            var detector = new ShakeDetector();
            detector.Sample(Gravity, FrameTime, 0);

            Assert.IsTrue(detector.Sample(Jolt, FrameTime, FrameTime));
        }

        [Test]
        public void ShakeDetector_IgnoresGentleMovement()
        {
            var detector = new ShakeDetector();
            detector.Sample(Gravity, FrameTime, 0);

            Assert.IsFalse(detector.Sample(new Vector3(0.5f, -1, 0), FrameTime, FrameTime));
        }

        [Test]
        public void ShakeDetector_WaitsForCooldownBeforeNextShake()
        {
            var detector = new ShakeDetector();
            detector.Sample(Gravity, FrameTime, 0);
            Assert.IsTrue(detector.Sample(Jolt, FrameTime, FrameTime));

            Assert.IsFalse(detector.Sample(-Jolt, FrameTime, 2 * FrameTime));
            Assert.IsTrue(detector.Sample(-Jolt, FrameTime, ShakeDetector.DefaultCooldown + 2 * FrameTime));
        }

        [Test]
        public void ShakeDetector_ResetForgetsPreviousSamples()
        {
            var detector = new ShakeDetector();
            detector.Sample(Gravity, FrameTime, 0);
            detector.Reset();

            Assert.IsFalse(detector.Sample(Jolt, FrameTime, FrameTime));
        }

        [Test]
        public void RollAllDice_RollsEveryDieOnTheTable()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardGameView/Multiplayer/Die.prefab");
            var dice = new Die[3];
            for (var i = 0; i < dice.Length; i++)
            {
                dice[i] = Object.Instantiate(prefab, _table.transform).GetComponent<Die>();
                dice[i].enabled = false;
            }

            Assert.AreEqual(dice.Length, DiceShakeRoller.RollAllDice(_table.transform));

            foreach (var die in dice)
                Assert.Greater(RollRemainingTime(die), 0f);
        }

        [Test]
        public void RollAllDice_DoesNothingWithoutTable()
        {
            Assert.AreEqual(0, DiceShakeRoller.RollAllDice(null));
        }

        private static float RollRemainingTime(Die die) =>
            (float)typeof(Die).GetField("_rollRemainingTime", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(die);
    }
}
#endif
