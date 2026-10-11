# Noto Sans CJK UI fonts

Unmodified regional Regular OTF files from the official Noto CJK project:

- [Simplified Chinese source](https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf)
- [Korean source](https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/Korean/NotoSansCJKkr-Regular.otf)
- [Upstream license](https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE)

Downloaded October 5, 2026. The SIL Open Font License 1.1 is included in `OFL.txt`;
original copyright and font naming metadata are retained in the OTF files.

The renamed `CGSLanguagePicker-Regular.otf` is an OFL-licensed derivative of the
Chinese source, containing every native language name from `translations/locales.json`,
the translated language-row label, printable ASCII, and a non-breaking space. Glyph outlines and horizontal metrics are
unchanged. Regenerate it with `tools/localization/subset_picker_font.py` when adding a
language; see that tools directory for its pinned optional dependency.

The picker subset is shared by non-Chinese locales. Chinese reuses its already-loaded
full face for the picker, avoiding an additional font allocation. The full Chinese and Korean fonts remain local to
their language groups, so English startup does not preload either full CJK face.
Japanese UI uses the existing Noto Sans JP source. Sources are included locally for offline
players; generated TMP assets and font asset tables live in `Assets/Localization/Fonts`.
The regular CJK face uses the UI's existing synthetic bold styling where requested.
