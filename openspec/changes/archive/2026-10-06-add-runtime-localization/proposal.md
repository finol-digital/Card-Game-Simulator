## Why

CGS currently presents its interface in English, limiting access for players who use other languages. [Issue #232](https://github.com/finol-digital/Card-Game-Simulator/issues/232) calls for multilingual support; shipping machine-generated translations with an approachable contribution workflow lets players use CGS sooner and contributors improve wording over time.

## What Changes

- Add a Language dropdown as the first setting, immediately before Framerate, with native language names and keyboard/gamepad navigation.
- Apply language changes to all CGS-owned interface text immediately without restarting, including dynamically generated messages, tooltips, dropdown options, and newly opened screens.
- Remember the selected language locally; use the supported device language on first launch and English as the final fallback.
- Initially support the ten languages selected for this change: English, Spanish, Simplified Chinese, Brazilian Portuguese, Korean, French, German, Japanese, Italian, and Russian (`en`, `es`, `zh-Hans`, `pt-BR`, `ko`, `fr`, `de`, `ja`, `it`, `ru`).
- Seed non-English catalogs with machine-generated translations, record review status, and provide editable translation files, validation, and contributor instructions that do not require Unity for wording fixes.
- Bundle the fonts and translation data needed for offline use, including Chinese, Japanese, Korean, and Cyrillic coverage, and verify both legacy uGUI Text and TextMeshPro rendering.

Localization covers application interface text across the title screen, main menu, settings, cards explorer, deck editor, play screen, and their dialogs. User-entered text, card/game definitions and artwork, chat, proper names, native OS dialog controls, and developer-only logs are outside the translation scope. CGS-supplied dialog titles and prompts remain in scope.

## Capabilities

### New Capabilities

- `language-selection`: First-position language setting, startup selection, persistence, supported locale list, and input navigation.
- `localized-interface`: Live and lifecycle-safe localization of static and dynamic application text, English fallback, formatting, fonts, and layouts.
- `translation-contributions`: Editable catalogs, machine-translation provenance, protected human corrections, deterministic Unity import, validation, and contributor documentation.

### Modified Capabilities

None. Existing OpenSpec capabilities do not define language or localization behavior.

## Impact

- Extend `Assets/Scripts/Cgs/Menu/Settings.cs` and `Assets/Scenes/Settings.unity`, reusing `Assets/Prefabs/Components/Settings Dropdown.prefab`.
- Use the installed Unity Localization 1.5.12 and Addressables packages and existing `Assets/Localization/` settings/locales; configure the supported runtime subset of the 339 existing locale assets.
- Add translation catalogs and import/validation tooling; add runtime localization support and migrate CGS-owned text in scenes, prefabs, tooltips, modals, and script-generated UI.
- Update font assets and Addressables content, retaining existing Japanese game-name rendering; adjust affected assembly references as needed.
- Add focused NUnit coverage and desktop, mobile-layout, WebGL/offline, and multiplayer presentation checks. Place the contribution guide in `developer-docs/` and link it from the repository README.
- No public website, game-data schema, network protocol, or `.github` changes are planned.
