/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Localization;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class LocaleSelectionTests
    {
        private CgsLocaleSelector _selector;

        [SetUp]
        public void SetUp()
        {
            _selector = new CgsLocaleSelector(new[]
            {
                new SupportedLanguage("en", "English", null),
                new SupportedLanguage("es", "Español", null),
                new SupportedLanguage("zh-Hans", "简体中文", new[] { "zh-CN", "zh-SG" }),
                new SupportedLanguage("pt-BR", "Português (Brasil)", null),
                new SupportedLanguage("ko", "한국어", null),
                new SupportedLanguage("de", "Deutsch", null)
            });
        }

        [TestCase("de", "es", "ko-KR", "de")]
        [TestCase("invalid", "es", "ko-KR", "es")]
        [TestCase(null, "removed", "es-MX", "es")]
        [TestCase(null, null, "ko-KR", "ko")]
        [TestCase(null, null, "zh-CN-u-nu-latn", "zh-Hans")]
        [TestCase(null, null, "zh-CN-x-test", "zh-Hans")]
        [TestCase(null, null, "zh-Hant-TW-u-nu-latn", "en")]
        [TestCase(null, null, "es-MX-u-nu-latn", "es")]
        [TestCase(null, null, "es-Cyrl-u-nu-latn", "en")]
        [TestCase(null, null, "pt-BR-x-test", "pt-BR")]
        [TestCase(null, null, "zh-CN", "zh-Hans")]
        [TestCase(null, null, "zh-SG", "zh-Hans")]
        [TestCase(null, null, "zh-Hans-CN", "zh-Hans")]
        [TestCase(null, null, "zh-Hant", "en")]
        [TestCase(null, null, "zh-Hant-TW", "en")]
        [TestCase(null, null, "pt-PT", "en")]
        [TestCase(null, null, "pt", "en")]
        [TestCase(null, null, "pt-BR", "pt-BR")]
        [TestCase(null, null, "en-Cyrl", "en")]
        [TestCase(null, null, "es-Cyrl", "en")]
        [TestCase(null, null, "ko_KR", "ko")]
        [TestCase(null, null, "xx-YY", "en")]
        [TestCase(null, null, "", "en")]
        [TestCase(null, null, null, "en")]
        [TestCase("ES", null, "en", "es")]
        [TestCase("es-MX", "ko", "en", "ko")]
        public void ResolvesSupportedCodesWithoutImplicitScriptOrRegionSubstitution(
            string commandLine, string saved, string device, string expected)
        {
            Assert.That(_selector.ResolveCode(commandLine, saved, device), Is.EqualTo(expected));
        }
    }
}
