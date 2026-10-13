/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Linq;
using System.Reflection;
using Cgs.Localization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class LocalizationInitializationTestSettings : LocalizationSettings
    {
        public AsyncOperationHandle<LocalizationSettings> Operation;
        public override AsyncOperationHandle<LocalizationSettings> GetInitializationOperation() => Operation;
    }

    public class LocalizationInitializationTests
    {
        private sealed class PendingInitialization : AsyncOperationBase<LocalizationSettings>
        {
            protected override void Execute() { }
            public void Finish(LocalizationSettings settings) => Complete(settings, true, null);
        }

        private static void ResetService() => typeof(CgsLocalization)
            .GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);

        [UnityTest]
        public IEnumerator Initialize_CompletedOperationDoesNotDependOnDeferredCallback()
        {
            yield return CheckInitialization(false);
        }

        [UnityTest]
        public IEnumerator Initialize_ReplacedOperationRecoversAndIgnoresStaleCompletion()
        {
            yield return CheckInitialization(true);
        }

        [UnityTest]
        public IEnumerator Initialize_ReplacedOperationRecoversWithoutAnotherCaller()
        {
            yield return CheckInitialization(true, true);
        }

        private static IEnumerator CheckInitialization(bool replacePendingOperation, bool waitForQueue = false)
        {
            CgsLocalization.Initialize();
            yield return LocalizationSettings.InitializationOperation;
            yield return null;
            var original = LocalizationSettings.Instance;
            var languages = CgsLocalization.Languages.ToArray();
            var settings = ScriptableObject.CreateInstance<LocalizationInitializationTestSettings>();
            settings.GetStartupLocaleSelectors().Add(new CgsLocaleSelector(languages));
            using var resourceManager = new ResourceManager();
            var completed = resourceManager.CreateCompletedOperation<LocalizationSettings>(settings, null);
            var pending = new PendingInitialization();
            var pendingHandle = resourceManager.StartOperation(pending, default);
            var changes = 0;
            System.Action changed = () => changes++;
            try
            {
                ResetService();
                LocalizationSettings.Instance = settings;
                CgsLocalization.Changed += changed;
                if (replacePendingOperation)
                {
                    settings.Operation = pendingHandle;
                    CgsLocalization.Initialize();
                    Assert.IsFalse(CgsLocalization.IsReady);
                }

                settings.Operation = completed;
                if (waitForQueue)
                {
                    yield return null;
                    yield return null;
                }
                else
                    CgsLocalization.Initialize();
                Assert.IsTrue(CgsLocalization.IsReady,
                    "A completed current operation must make localization ready even if its callback was deferred or replaced.");
                Assert.AreEqual(1, changes);
                CgsLocalization.Initialize();
                pending.Finish(settings);
                yield return null;
                yield return null;
                Assert.IsTrue(CgsLocalization.IsReady);
                Assert.AreEqual(1, changes, "Repeated calls and stale callbacks must not publish readiness twice.");
            }
            finally
            {
                CgsLocalization.Changed -= changed;
                ResetService();
                LocalizationSettings.Instance = original;
                if (!pendingHandle.IsDone)
                    pending.Finish(settings);
                resourceManager.Release(completed);
                resourceManager.Release(pendingHandle);
                Object.DestroyImmediate(settings);
                CgsLocalization.Initialize();
            }
            yield return LocalizationSettings.InitializationOperation;
            yield return null;
            Assert.IsTrue(CgsLocalization.IsReady);
        }
    }
}
