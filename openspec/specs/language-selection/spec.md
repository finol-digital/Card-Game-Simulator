# language-selection Specification

## Purpose
Define supported interface languages, persistent player selection, startup locale resolution, and asynchronous Settings behavior.

## Requirements

### Requirement: Language is the first setting

The Settings screen SHALL display a Language dropdown as the first setting immediately before Framerate. It SHALL be usable with pointer, touch, keyboard, and gamepad input, and SHALL become the initial settings navigation target when no object is selected.

#### Scenario: Open settings
- **WHEN** a player opens Settings
- **THEN** Language appears immediately above Framerate and reflects the active locale
- **AND** all existing settings remain available

#### Scenario: Start controller navigation
- **WHEN** the player starts keyboard or gamepad navigation with no selected object
- **THEN** the Language dropdown receives focus and navigation can continue to Framerate

### Requirement: Offer only shipped languages using native names

The dropdown SHALL offer English, Spanish, Simplified Chinese, Brazilian Portuguese, Korean, French, German, Japanese, Italian, and Russian, mapped to `en`, `es`, `zh-Hans`, `pt-BR`, `ko`, `fr`, `de`, `ja`, `it`, and `ru`. It SHALL display native names in a stable manifest-defined order with English first. Existing locale assets without shipped catalogs and fonts SHALL NOT appear as supported choices.

#### Scenario: Recover from an unfamiliar language
- **WHEN** the player opens the dropdown while any supported language is active
- **THEN** every native language name is readable, including Chinese, Japanese, Korean, and Russian
- **AND** option order remains unchanged and English remains first

#### Scenario: Locale asset without shipped support
- **WHEN** a locale asset exists but is absent from the shipped-locale manifest
- **THEN** it is absent from the dropdown and cannot be selected by startup locale resolution

### Requirement: Apply and remember explicit selection

Selecting a supported language SHALL set Unity Localization's selected locale, refresh the interface without restarting or reloading the scene, and persist its locale code locally. Selection SHALL NOT alter another setting, reset game state, or change another player's language. Redisplay and reopening Settings SHALL NOT trigger redundant selection writes or callbacks.

#### Scenario: Change language and reopen settings
- **WHEN** the player selects French and later leaves and reopens Settings
- **THEN** the interface and selected dropdown entry remain French
- **AND** framerate, resolution, and other preferences retain their values

#### Scenario: Relaunch after choosing a language
- **WHEN** the player chooses Korean and relaunches without a command-line language override
- **THEN** the application starts in Korean regardless of the device language
- **AND** reordering manifest entries does not change the saved selection

### Requirement: Resolve startup language predictably

Startup SHALL select the first supported choice from an explicit command-line language override, saved locale code, device locale, and English. Device matching SHALL use exact codes, script-preserving supported parents, and documented aliases (`zh-CN` and `zh-SG` to `zh-Hans`). It SHALL NOT substitute an unsupported script or regional variant without a declared alias. A command-line override SHALL NOT overwrite the saved preference unless the player makes an explicit menu selection.

#### Scenario: First launch on a supported regional language
- **WHEN** there is no saved selection or override and the device reports `es-MX`
- **THEN** the application selects Spanish (`es`)

#### Scenario: Unsupported or corrupt preference
- **WHEN** the saved code is invalid or no longer shipped
- **THEN** startup continues with the supported device language or English without an exception or blank interface

#### Scenario: Unsupported language variant
- **WHEN** there is no saved selection or override and the device reports `zh-Hant` or `pt-PT`
- **THEN** the application falls back to English instead of silently selecting `zh-Hans` or `pt-BR`
- **AND** the player can explicitly select any shipped language in Settings

#### Scenario: Temporary command-line override
- **WHEN** a player with saved Spanish launches with a supported German override
- **THEN** the session starts in German
- **AND** a later launch without that override still uses Spanish unless the player changed the preference

### Requirement: Initialization keeps settings usable

Localization initialization SHALL be asynchronous and SHALL NOT require blocking completion on WebGL. The Language dropdown SHALL remain non-interactive until its supported locales are ready. If initialization fails, the application SHALL retain readable English UI and other usable settings, and record a diagnostic.

#### Scenario: Delayed or failed initialization
- **WHEN** Settings is opened before localization completes or localization fails
- **THEN** the Language dropdown does not accept an invalid selection
- **AND** the application does not freeze, erase English text, or prevent use of unrelated settings
