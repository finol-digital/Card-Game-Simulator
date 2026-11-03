/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using FinolDigital.Cgs.Json.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.GameContent
{
    public sealed class GameContentImage : MonoBehaviour
    {
        private Image _image;
        private UnityCardGame _game;
        private Sprite _canonical;
        private Sprite _localized;
        private string[] _path;
        private bool _oriented;
        private int _refresh;
        private Action<bool> _applied;

        public void ObserveApplied(Action<bool> applied)
        {
            _applied = applied;
            _applied?.Invoke(_localized != null);
        }

        public static GameContentImage Set(Image image, UnityCardGame game, Sprite canonical, bool oriented, params string[] path)
        {
            if (image == null)
                return null;
            var binding = image.GetComponent<GameContentImage>();
            if (binding == null)
                binding = image.gameObject.AddComponent<GameContentImage>();
            if (binding == null)
                return null;
            var sameTarget = binding._game == game && binding._path != null && binding._path.SequenceEqual(path);
            if (!sameTarget)
                binding.Release();
            binding._image = image;
            binding._game = game;
            binding._canonical = canonical;
            binding._oriented = oriented;
            binding._path = path;
            binding.Refresh(null);
            return binding;
        }

        private void OnEnable()
        {
            GameContentPresentation.Changed += Refresh;
            GameContentPresentation.AssetsChanged += OnAsset;
            Refresh(null);
        }

        private void OnDisable()
        {
            GameContentPresentation.Changed -= Refresh;
            GameContentPresentation.AssetsChanged -= OnAsset;
            Release();
        }

        private void OnDestroy() => Release();

        private void Release()
        {
            _refresh++;
            if (_image != null)
                _image.sprite = _canonical;
            GameLocalizationAssets.Release(_localized);
            _localized = null;
        }

        private void Refresh(UnityCardGame changed)
        {
            if (_image == null || _game == null || _path == null || (changed != null && changed != _game))
                return;
            var refresh = ++_refresh;
            var loading = isActiveAndEnabled && GameContentPresentation.CanRefreshImage(_game) &&
                GameContentPresentation.Context(_game).AssetCandidates(_path).Count > 0;
            ApplyAvailable(loading);
            if (loading)
                GameContentPresentation.RefreshImage(_game, _path, () =>
                {
                    if (this && isActiveAndEnabled && refresh == _refresh)
                        ApplyAvailable(false);
                });
        }

        private void OnAsset(UnityCardGame game, string target)
        {
            if (game == _game && _path != null && target == GameLocalizationAssets.Target(_path))
                ApplyAvailable(true);
        }

        private void ApplyAvailable(bool loading)
        {
            var replacement = GameContentPresentation.AvailableImage(_game, _oriented, _path);
            // Retain usable artwork while fetching the next locale. A settled failure,
            // feature removal, or target reuse returns to canonical fallback.
            if (replacement == null && loading && _localized != null)
                return;
            GameLocalizationAssets.Release(_localized);
            _localized = replacement;
            _image.sprite = replacement != null ? replacement : _canonical;
            _applied?.Invoke(_localized != null);
        }
    }
}
