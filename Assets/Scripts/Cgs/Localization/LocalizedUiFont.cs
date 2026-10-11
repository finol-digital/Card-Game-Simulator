/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace Cgs.Localization
{
    [Serializable] public sealed class LocalizedLegacyFont : LocalizedAsset<Font> { }
    [Serializable] public sealed class LocalizedTmpFont : LocalizedAsset<TMP_FontAsset> { }

    public sealed class LocalizedUiFont : MonoBehaviour
    {
        [SerializeField] string key = "body.regular";
        private LocalizedLegacyFont _legacyReference;
        private LocalizedTmpFont _tmpReference;

        private void OnEnable()
        {
            if (GetComponent<Text>() != null)
            {
                _legacyReference = new LocalizedLegacyFont { TableReference = "CgsFonts", TableEntryReference = key };
                _legacyReference.AssetChanged += ApplyLegacy;
            }
            if (GetComponent<TMP_Text>() != null)
            {
                _tmpReference = new LocalizedTmpFont { TableReference = "CgsFonts", TableEntryReference = "tmp." + key };
                _tmpReference.AssetChanged += ApplyTmp;
            }
        }

        private void OnDisable()
        {
            if (_legacyReference != null)
                _legacyReference.AssetChanged -= ApplyLegacy;
            if (_tmpReference != null)
                _tmpReference.AssetChanged -= ApplyTmp;
            _legacyReference = null;
            _tmpReference = null;
        }

        private void ApplyLegacy(Font font)
        {
            if (!this || !isActiveAndEnabled || font == null)
                return;
            GetComponent<Text>().font = font;
            Rebuild();
        }

        private void ApplyTmp(TMP_FontAsset font)
        {
            if (!this || !isActiveAndEnabled || font == null)
                return;
            GetComponent<TMP_Text>().font = font;
            Rebuild();
        }

        private void Rebuild()
        {
            if (transform is RectTransform rect)
                LayoutRebuilder.MarkLayoutForRebuild(rect);
        }
    }
}
