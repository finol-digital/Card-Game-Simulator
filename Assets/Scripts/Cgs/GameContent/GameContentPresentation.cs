/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cgs.Localization;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using UnityEngine;

namespace Cgs.GameContent
{
    // Presentation owns no gameplay objects and starts no canonical loads/downloads.
    public static class GameContentPresentation
    {
        private sealed class Entry
        {
            private readonly UnityCardGame _game;
            private LocalizationTargets _targets;
            private LocalizationContext _context;
            private string _locale;
            private long _dataGeneration = -1;
            private long _definitionGeneration = -1;
            private GameLocalizationCatalogs _catalogs;

            public Entry(UnityCardGame game)
            {
                _game = game;
                game.DefinitionChanged += OnDefinitionChanged;
                game.ContentChanged += Invalidate;
            }

            private void OnDefinitionChanged(UnityCardGame game)
            {
                _targets = null;
                _context = null;
                if (game == _selectedGame)
                    Select(game);
                NotifyChanged(game);
            }

            public GameLocalizationCatalogs Catalogs
            {
                get
                {
                    if (_catalogs == null || !_catalogs.MatchesInstallation)
                        _catalogs = new GameLocalizationCatalogs(_game);
                    return _catalogs;
                }
            }

            public void Refresh(string locale, int selection, long definition)
            {
                if (_game.CoroutineRunner == null || _game.AutoUpdate < 0 || _game.Localization == null ||
                    !_game.Localization.IsUsable)
                    return;
                _game.CoroutineRunner.StartCoroutine(Catalogs.Refresh(locale, () =>
                {
                    // Cache publication has its own ticket/incarnation guard. A cache
                    // success alone never authorizes updating a stale view selection.
                    if (_selectedGame == _game && _selection == selection &&
                        _game.DefinitionGeneration == definition && CgsLocalization.CurrentLocale == locale)
                        Invalidate(_game);
                }));
            }

            private void Invalidate(UnityCardGame game)
            {
                _targets = null;
                _context = null;
                NotifyChanged(game);
            }

            public LocalizationContext Get(string locale)
            {
                if (_targets == null || _dataGeneration != _game.ContentGeneration ||
                    _definitionGeneration != _game.DefinitionGeneration)
                {
                    _targets = BuildTargets();
                    _dataGeneration = _game.ContentGeneration;
                    _definitionGeneration = _game.DefinitionGeneration;
                    _context = null;
                }
                if (_context == null || !string.Equals(_locale, locale, StringComparison.OrdinalIgnoreCase))
                {
                    _locale = locale;
                    var available = _game.Localization?.IsUsable == true ? Catalogs.Available(locale) : null;
                    _context = new LocalizationContext(_targets, locale, available, _game.DefinitionOrigin);
                }
                return _context;
            }

            public IReadOnlyList<GameContentDiagnostics.CatalogReport> Diagnostics()
            {
                Get(CgsLocalization.CurrentLocale);
                var documents = new Dictionary<string, LocalizationCatalogDocument>(StringComparer.OrdinalIgnoreCase);
                if (_game.Localization?.IsUsable == true)
                    foreach (var locale in _game.Localization.Locales.Keys)
                        foreach (var document in Catalogs.Available(locale))
                            documents[document.Key] = document.Value;
                return GameContentDiagnostics.Inspect(_game.Localization, _targets, documents);
            }

            private LocalizationTargets BuildTargets()
            {
                if (_game.Localization != null && _game.Localization.IsUsable)
                {
                    try
                    {
                        return new LocalizationTargets(_game, _game.Cards.Values, _game.Sets.Values,
                            authoredDeckNames: AuthoredDeckFiles.Names(_game), cardBackIds: _game.CardBackFaceImageSprites.Keys);
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning("Game-content inventory unavailable: " + error.Message);
                    }
                }
                // Opt-out games do not need a full card inventory or valid optional
                // collections just to keep displaying their existing strings.
                var canonical = new CardGame
                {
                    Name = _game.Name, Copyright = _game.Copyright,
                    GamePlayDeckName = _game.GamePlayDeckName,
                    SetCodeDefault = _game.SetCodeDefault, SetNameDefault = _game.SetNameDefault
                };
                return new LocalizationTargets(canonical, Array.Empty<Card>());
            }
        }

        private static ConditionalWeakTable<UnityCardGame, Entry> _entries = new();
        private static UnityCardGame _selectedGame;
        private static int _selection;
        public static event Action<UnityCardGame> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            CgsLocalization.Changed -= OnLocaleChanged;
            _entries = new ConditionalWeakTable<UnityCardGame, Entry>();
            Changed = null;
            AssetsChanged = null;
            _selectedGame = null;
            _selection++;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            CgsLocalization.Changed -= OnLocaleChanged;
            CgsLocalization.Changed += OnLocaleChanged;
        }

        private static void OnLocaleChanged()
        {
            if (_selectedGame != null)
                Select(_selectedGame);
            NotifyChanged(null);
        }

        public static void Select(UnityCardGame game)
        {
            _selectedGame = game;
            var selection = ++_selection;
            if (game == null || game == UnityCardGame.UnityInvalid)
                return;
            _entries.GetValue(game, value => new Entry(value))
                .Refresh(CgsLocalization.CurrentLocale, selection, game.DefinitionGeneration);
        }

        private static void NotifyChanged(UnityCardGame game)
        {
            if (Changed == null)
                return;
            foreach (Action<UnityCardGame> handler in Changed.GetInvocationList())
            {
                try
                {
                    handler(game);
                }
                catch (Exception error)
                {
                    Debug.LogWarning("Game-content binding refresh failed: " + error.Message);
                }
            }
        }

        public static event Action<UnityCardGame, string> AssetsChanged;

        public static Sprite AvailableImage(UnityCardGame game, bool oriented, params string[] path)
        {
            var context = Context(game);
            var candidates = context.AssetCandidates(path);
            return candidates.Count == 0 ? null : _entries.GetValue(game, value => new Entry(value)).Catalogs.Images
                .Available(candidates, GameLocalizationAssets.Target(path), oriented);
        }

        public static bool CanRefreshImage(UnityCardGame game) => game == _selectedGame &&
            game.CoroutineRunner != null && game.Localization?.IsUsable == true && game.AutoUpdate >= 0;

        public static void RefreshImage(UnityCardGame game, string[] path, Action completed = null)
        {
            if (!CanRefreshImage(game))
            {
                completed?.Invoke();
                return;
            }
            var locale = CgsLocalization.CurrentLocale;
            var definition = game.DefinitionGeneration;
            var selection = _selection;
            var target = GameLocalizationAssets.Target(path);
            var owner = _entries.GetValue(game, value => new Entry(value));
            bool IsCurrent() => game == _selectedGame && selection == _selection &&
                definition == game.DefinitionGeneration && locale == CgsLocalization.CurrentLocale;
            IEnumerator Refresh()
            {
                yield return owner.Catalogs.Images.Refresh(owner.Get(locale).AssetCandidates(path), target, () =>
                {
                    if (IsCurrent())
                        AssetsChanged?.Invoke(game, target);
                });
                if (IsCurrent())
                    completed?.Invoke();
            }
            game.CoroutineRunner.StartCoroutine(Refresh());
        }

        public static Uri RulesUri(UnityCardGame game)
        {
            // External rulebook sites are links; never crawl or mirror them into a package.
            foreach (var candidate in Context(game).AssetCandidates("rulesUrl"))
                if (!candidate.Uri.IsFile || File.Exists(candidate.Uri.LocalPath))
                    return candidate.Uri;
            return game.RulesUrl;
        }

        public static IReadOnlyList<GameContentDiagnostics.CatalogReport> Diagnostics(UnityCardGame game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));
            return _entries.GetValue(game, value => new Entry(value)).Diagnostics();
        }

        public static LocalizationContext Context(UnityCardGame game) => Context(game, CgsLocalization.CurrentLocale);

        public static LocalizationContext Context(UnityCardGame game, string locale)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));
            return _entries.GetValue(game, value => new Entry(value)).Get(locale);
        }
    }
}
