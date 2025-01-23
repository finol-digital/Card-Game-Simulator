/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace FinolDigital.Cgs.Json.Unity
{
    public struct CgsGame
    {
        public string Username;
        public string Slug;
        public string Name;
        public string BannerImageUrl;
        public string AutoUpdateUrl;
        public string Copyright;

        public override string ToString()
        {
            return $"{Name}\nCopyright of {Copyright} and Uploaded by {Username}";
        }
    }
}
