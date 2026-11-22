/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs;
using NUnit.Framework;

namespace Tests.PlayMode
{
    public class DeepLinkQueryTests
    {
        [TestCase("?url=http%3A%2F%2F127.0.0.1%3A9%2Fcgs.json", "http://127.0.0.1:9/cgs.json")]
        [TestCase("url=card+game", "card game")]
        [TestCase("?name=Standard&URL=https%3A%2F%2Fexample.com", "https://example.com")]
        [TestCase("?url=first&url=second", "first")]
        [TestCase("?url", "")]
        [TestCase("?url=", "")]
        public void GetQueryParameterDecodesTheValue(string query, string expected)
        {
            Assert.AreEqual(expected, CardGameManager.GetQueryParameter(query, "url"));
        }

        [TestCase("")]
        [TestCase("?")]
        [TestCase("?name=Standard")]
        public void GetQueryParameterReturnsNullWhenMissing(string query)
        {
            Assert.IsNull(CardGameManager.GetQueryParameter(query, "url"));
        }
    }
}
