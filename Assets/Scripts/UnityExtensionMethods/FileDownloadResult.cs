/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace UnityExtensionMethods
{
    public sealed class FileDownloadResult
    {
        public bool Succeeded { get; }
        public Uri FinalDocumentUri { get; }
        public bool IsRemote { get; }

        public FileDownloadResult(bool succeeded, Uri finalDocumentUri, bool isRemote)
        {
            Succeeded = succeeded;
            FinalDocumentUri = finalDocumentUri;
            IsRemote = isRemote;
        }

        public const string FinalUrlHeader = "X-CGS-Final-URL";

        // Only the trusted proxy can assert the origin of its response body.
        // An older proxy, missing CORS exposure, or an invalid URL leaves it unknown.
        public static FileDownloadResult FromResponse(string finalTransportUrl, bool usedProxy,
            string proxyFinalUrl = null)
        {
            Uri origin = null;
            if (TryHttpUri(finalTransportUrl, out var transport))
            {
                var trustedProxy = transport.Scheme == Uri.UriSchemeHttps &&
                    transport.Host.Equals("cgs.games", StringComparison.OrdinalIgnoreCase) &&
                    transport.IsDefaultPort &&
                    transport.AbsolutePath.StartsWith("/api/proxy/", StringComparison.Ordinal);
                if (trustedProxy)
                {
                    if (TryHttpUri(proxyFinalUrl, out var upstream))
                        origin = upstream;
                }
                else if (!usedProxy)
                    origin = transport;
            }
            return new FileDownloadResult(true, origin, true);
        }

        private static bool TryHttpUri(string value, out Uri uri)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
                string.IsNullOrEmpty(uri.UserInfo) && !string.IsNullOrEmpty(uri.Host) &&
                !string.IsNullOrWhiteSpace(value) && value == value.Trim() &&
                value.IndexOfAny(new[] { '\r', '\n', '\t' }) < 0;
        }
    }
}
