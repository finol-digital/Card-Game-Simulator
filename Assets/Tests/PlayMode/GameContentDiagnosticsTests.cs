/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Linq;
using Cgs.GameContent;
using Cgs.Localization;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using NUnit.Framework;
using UnityEngine.Localization.Settings;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class GameContentDiagnosticsTests
    {
        [UnityTest]
        public IEnumerator ActualSelectorReportsUnreachableSiblingsAndDefaultFallbackWithoutRejectingCatalogs()
        {
            CgsLocalization.Initialize();
            yield return LocalizationSettings.InitializationOperation;
            yield return null;
            Assert.That(CgsLocalization.Languages.Select(language => language.Code), Does.Contain("zh-Hans"));
            var game = new UnityCardGame(null, "Base");
            var manifest = LocalizationJson.ReadManifest(@"{""defaultLocale"":""ja-JP"",""locales"":{
                ""es-ES"":{""inline"":{""game"":{""name"":""España""}}},
                ""zh-CN"":{""inline"":{""game"":{""name"":""中文""}}},
                ""ja-JP"":{""inline"":{""game"":{""name"":""日本語""}}},
                ""es"":{""inline"":{""game"":{""name"":""Español""},""properties"":{
                    ""one"":{""display"":""Same""},""two"":{""display"":""Same""}}}}
            }}").Value;
            game.Localization = manifest;
            var targets = new LocalizationTargets(game, game.Cards.Values);
            var reports = GameContentDiagnostics.Inspect(manifest, targets).ToDictionary(report => report.Locale);
            foreach (var code in new[] { "es-ES", "zh-CN" })
            {
                Assert.That(reports[code].Reachable, Is.False);
                Assert.That(reports[code].Diagnostics.Any(item => item.Code == "unreachable-locale" && item.IsWarning), Is.True);
            }
            Assert.That(reports["ja-JP"].Reachable, Is.True, "The default catalog is reachable even if absent from the UI selector.");
            Assert.That(reports["es"].Reachable, Is.True);
            Assert.That(reports["es"].Diagnostics.Count(item => item.Code == "duplicate-label"), Is.EqualTo(2));
            Assert.That(reports["es"].Coverage.Unknown.Count, Is.EqualTo(2));
            Assert.That(reports["es"].Coverage.Missing.Count, Is.GreaterThan(0));
            Assert.That(new LocalizationContext(targets, "es").GameName, Is.EqualTo("Español"));
            Assert.That(new LocalizationContext(targets, "zh-Hans").GameName, Is.EqualTo("日本語"));
            Assert.That(manifest.Locales.Count, Is.EqualTo(4));
            Assert.That(game.HasDownloaded, Is.False);
        }
    }
}
