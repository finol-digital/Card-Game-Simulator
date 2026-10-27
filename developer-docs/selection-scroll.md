# Selection visibility in scroll views

Audit date: 2026-10-23. Scope: maintained Unity scenes, prefabs, navigation
controllers, custom ScrollRect subclasses, and the bundled file picker.

## Implemented behavior

`ScrollRectAutoScroll` observes EventSystem selection changes in `LateUpdate`,
after UI navigation. It updates layout and moves only far enough to reveal the
selected RectTransform on enabled scroll axes. It handles controls created after
the view opens, uneven row heights, repeated navigation, and paused time. It
stops inertia for a newly selected control, even when already visible. Visible controls do not cause an
extra scroll; wheel, drag, and page scrolling remain free until selection changes.
Oversized controls align to the top/left because they cannot fit fully.

Only the nearest ScrollRect owns a selection. Dropdown popups can therefore
scroll without moving the settings/form view behind them. This rule is intended
for menus and popups; inline nested filters need the additional handling below.

The old implementation cached a list of controls, converted list indices to
scroll percentages, checked only `WasPressedThisFrame`, and suppressed following
while the pointer hovered over the view. Its existing attachments were dropdown
templates, including copies in the Settings scene and Play Settings prefab;
they did not cover the main settings views.

The main Settings scene now has the component. The shared `ScrollView Vertical`
prefab adds it to Play Settings, Card Editor, Card Game Editor, CGS Games Browser,
and Move Menu. Existing dropdown attachments use the updated behavior. This does
not add keyboard navigation to fields whose Navigation mode is currently None.

## Project-wide findings

| Surface | Existing behavior / recommendation |
| --- | --- |
| General Settings | Main view now follows selection, including Language and platform-specific rows. |
| Play Settings | Main view now inherits following from the reusable vertical view. |
| Dropdown option lists | Existing attachments now observe actual selection changes and dynamic options; no hover suppression or single-option division. |
| Card Editor / Card Game Editor | Form views now inherit following, including programmatic focus on dynamic card-property controls. Some input fields have Navigation=None; full keyboard form navigation is a separate change. |
| CGS Games Browser / Move Menu | Inherit following as a safety net. Their `SelectionPanel.SelectPrevious/SelectNext` paths still explicitly scroll by index. |
| Deck Load / Games Management / Lobby | Already scroll through `SelectionPanel`. Follow-up: replace index percentages with bounds-based reveal, cover wrapped grids, and guard the single-item `(count - 1)` division in both navigation directions. Also cover focus changes outside those custom selection methods. |
| Cards Explorer / Deck Editor / card-search results | Existing `CardSelector` and `DeckEditor.FocusScrollRectOn` scroll rows or card zones. Preserve those layout-specific behaviors. |
| Property filters | High-priority follow-up. `PropertyFilters.prefab` has an outer vertical view and an inner horizontal `PassThroughScrollRect`; filter fields/toggles have Navigation=None. Add explicit navigation, reveal the selected option in the inner view, and reveal that inner viewport in the outer view. Simply adding the menu component to both is insufficient because it deliberately isolates nested selection. |
| Hands / card drawer / stack viewer | Follow-up in `PlaySelector` and card-zone/viewer integration. Selection currently changes without a matching reveal. Target the owning hand/stack view and account for overlapping cards. Keep card dragging and automatic hand fan/layout behavior intact. |
| Tabletop (`RotateZoomableScrollRect`) | Requires a separate pan-to-selection policy that understands rotation, zoom, world geometry, and drag state. Do not attach the generic menu component globally. |
| Card Viewer / docked Card Viewer information | Scrollable content is text/properties; selected viewer root is outside that content. Existing page scrolling is appropriate. |
| Play Menu scoreboard | Score list rows contain display text; local name/points input fields are outside the scrolling score list. No selected row to reveal. |
| Help text / `TMP ScrollText` / label scroll areas | Display-only scrolling; no focusable descendant list to follow. |
| Deck Save / Set Import / Download / dialogs / Hand Dealer | No additional owned form ScrollRect. Dropdown popups are covered through the dropdown prefab; native dialogs manage their own focus. |
| SimpleFileBrowser package | Already checks selected file visibility and adjusts its recycled list in `FileBrowser.cs`. Keep its package-owned scrolling behavior. |

## Verification

- `ScrollRectAutoScrollTests` covers uneven rows in both directions, dynamic
  controls, pointer hover/exit, manual scrolling, residual inertia, visible/outside selections,
  nested popup ownership, horizontal scrolling with a scaled hierarchy, fitting
  and oversized content, and reopening while time is paused.
- `ScrollNavigationWiringTests` checks the actual Settings scene and the affected
  menu/dropdown prefabs, including inherited components.
- Run these with the Unity CLI (`run_tests --mode playmode --filter
  ScrollRectAutoScrollTests` and `run_tests --mode editor --filter
  ScrollNavigationWiringTests`).
- Manually verify Settings on a short viewport: navigate down and back up using
  both individual presses and a held stick/key, with the pointer inside the menu.
  Open a long dropdown and navigate its options, then close it and continue.
  Wheel/page-scroll without changing focus and confirm that it does not snap back.
