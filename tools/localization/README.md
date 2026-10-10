# Catalog tools

Run from the repository root with Python 3.10 or newer:

```sh
python tools/localization/validate.py
python -m unittest discover -s tools/localization -p 'test_*.py'
python tools/localization/merge_drafts.py es path/to/es-draft.json
```

These tools use only the Python standard library. Validation never writes files.
Draft merging validates before writing, uses atomic replacement, and changes only
the requested locale. It can prepare an incomplete catalog; validation still
rejects missing entries before release. The Unity importer and full contributor
workflow are documented in `developer-docs/translating.md`; remaining player and
visual validation is tracked in `developer-docs/localization-coverage.md`.

## Contract (schema version 1)

`translations/locales.json` contains `schemaVersion: 1` and a `locales` array.
Each locale has `code`, `nativeName`, and an explicit `aliases` array. Array order
is the display order; English comes first. Aliases and locale codes cannot collide,
including case-insensitive collisions. Regional device language matching must
preserve script: `zh-CN` and `zh-SG` alias `zh-Hans`; `zh-Hant` and `pt-PT` have
no alias. Manifest entries describe the intended shipped set; runtime integration
must also enforce complete tables and font coverage before offering them.

Every catalog has `schemaVersion: 1`, `locale`, and `entries`, an object sorted by
semantic key. Keys use lowercase dotted identifiers, for example
`settings.language`, `common.cancel`, and `downloads.progress`. Keep keys stable
when wording changes. Different meanings get different keys even when their
English wording matches. JSON must be UTF-8 without duplicate properties.

English entries have exactly these fields:

```json
{
  "text": "{count:plural:{count} card|{count} cards}",
  "context": "Card count in the deck editor; count is the total number of cards.",
  "arguments": { "count": "Non-negative integer number of cards." },
  "smart": true
}
```

Use `smart: false` and `arguments: {}` for literal labels. Dynamic messages use
named arguments (`count`, `gameName`, `progress`), never positional or implicit
selectors. Supported source syntax uses `{name}` or `{name:formatter:format}`,
including nested named fields and backslash escapes. The Python checker checks
balanced braces and argument names; Unity must check formatter semantics and
render representative values. Preserve rich-text tag names, attributes and
balanced nesting. Do not localize URLs, identifiers, game definitions or values
supplied by players. Context must describe where a message appears, its meaning,
space constraints and any argument units; it must not merely repeat its key.

Translation entries have `text`, `status`, `sourceHash`, and `provenance`:

```json
{
  "text": "{count:plural:{count} carta|{count} cartas}",
  "status": "machine",
  "sourceHash": "<64 lowercase hexadecimal SHA-256 characters>",
  "provenance": { "method": "Generator/provider and method", "date": "2026-10-04" }
}
```

`sourceHash` is SHA-256 over UTF-8 JSON containing only the English `text`,
`context`, `arguments`, and `smart` fields. Sort every object's keys recursively,
use compact separators (`,` and `:`), preserve Unicode, and omit the final newline.
Whitespace inside strings matters; property order and file formatting do not.
`catalog.source_hash` is the reference implementation.

`status` is `machine`, `reviewed`, or `needs-review`. Only a person's explicit
review can justify `reviewed`. `provenance.method` and the ISO calendar `date` are
required; an optional `note` records corrections or review context. Keep the
generation method/date when correcting text; add a note instead of erasing its
origin. An English edit keeps translated text and its old hash, with status
`needs-review`. Validation warns on acknowledged stale entries and fails on
unexplained stale entries. After reconciling the wording against the new English
contract, update the hash and appropriate status explicitly.

## Generation and glossary

Generation input is the English catalog, locale manifest, this contract and
message context. Generate only missing/empty entries into a separate draft file
with the same locale catalog envelope. Output actual translations, complete
named arguments and machine provenance. Never copy English into missing entries
to conceal a gap. No service, credential or network call is part of these tools.
The initial nine non-English catalogs were generated on October 5, 2026 by Codex
using the English context and this glossary. Every generated entry records this
method/date and has `machine` status; no fluent human review is claimed. Identical
values for numeric formats, IDs, conventional acronyms, or words shared across
languages (such as “Internet”) are intentional translations rather than gap fillers.

Keep these meanings consistent, while using natural local terminology:

| Term | Meaning |
| --- | --- |
| CGS / Card Game Simulator | Product names; retain them. |
| Game | A card-game definition, or the current play session as stated by context. |
| Deck | A saved collection of cards. |
| Stack | Cards piled together on the tabletop. |
| Hand | A player's private cards. |
| Tap | Rotate a card sideways; distinguish from a touchscreen tap. |
| Flip | Turn a card over to its other face. |
| Set | A named collection of card definitions, not an instruction to assign a value. |
| Counter | A tabletop piece representing a number. |
| Host / Join | Create a multiplayer session / connect to an existing session. |

Merge with `merge_drafts.py`, then validate. The merge keeps every non-empty
translation, even an edited machine draft whose review flag has not changed.
It retains metadata and flags stale sources without replacing their hashes.
Unknown keys require an explicit retirement migration; neither tool silently
deletes them. Catalog additions must be reconciled across every shipped language.
