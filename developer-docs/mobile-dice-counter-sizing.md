# Local dice and counter sizing

Play Settings offers Auto, Standard, Large, and Extra large. The preference is
stored locally as `DiceAndCounterDisplaySize`, with enum values 0 through 3.
Auto uses 1.5x on native mobile platforms and identified mobile browsers and 1x
on desktop or unknown environments. Explicit sizes are 1x, 1.5x, and 2x.

The browser bridge recognizes Android/iOS user agents and the iPadOS
MacIntel-with-multiple-touch-points convention. Windows touch hardware does not
select the mobile default. Unknown or spoofed browser identities can require an
explicit user choice; no user-agent heuristic guarantees device identity.

## Implementation boundaries

- Die and Counter keep their original network root, 100 x 100 prefab dimensions,
  and colliders. `LocalPiecePresentation` scales only the `Local Visual` child.
- The original root Image is transparent and remains a canonical drop surface.
  Render-only children have raycast targeting disabled. Value/color references
  and highlighting point to the visual child.
- `PieceGraphicRaycaster` on PlayGame Canvas adds acquisition results for the
  displayed footprint and, for touch, 12 canvas reference units per edge. The
  extra margin is converted using the root canvas scale, independently of table
  zoom. These units are not native points or dp.
- Acquisition results identify the original playable. Unity's input module owns
  pointer capture and event dispatch. Direct content wins over margin-only hits;
  margin ties use projected center distance and then render order.
- During a drag, raycasting delegates to the original GraphicRaycaster behavior.
  CGS's per-pointer drag registry is necessary because InputSystemUIInputModule
  raycasts before restoring the active mouse button's state. A second finger
  can still acquire a piece while the first finger drags.
- Counter merging and dice-zone overlap retain their original bounds. An enlarged
  counter's outer visual edge is deliberately not a larger merge target.

## Automated checks

Run through a connected Unity Editor with the Pipeline package:

```text
unity command recompile
unity command recompile_status
unity command run_tests --mode playmode --filter PiecePresentationTests --async_tests
unity command test_status
```

The suite checks defaults, preference persistence, actual prefab lifecycle and
geometry, touch versus mouse acquisition, overlap resolution, invisible objects,
rotation/zoom, per-pointer drag tracking, real Input System dispatch, actual
counter merging, and actual dice-zone membership. It restores preferences and
input settings after each test. Complete script imports before starting a run;
an interrupted Test Runner can leave Pipeline reporting a stale running state.

Also run `python tools/localization/validate.py`, validate generated tables and
the Play Settings prefab bindings in Unity, and run the existing ownership and
localization presentation suites. Generated reports and preview images belong
in ignored `Temp/` or `Logs/`.

## Device and multiplayer acceptance checklist

These checks require actual devices and remain separate from Editor test results.

1. On desktop with Auto, compare the initial view and mouse interactions with the
   pre-change version at the same resolution and zoom. Standard must restore the
   original dimensions after switching through all choices.
2. On Android/iOS and mobile web, confirm Auto uses Large. Test Standard, Large,
   and Extra large, application restart, orientation changes, small/large values,
   colored pieces, and dense tables at minimum/default/maximum zoom.
3. Check a touch-enabled desktop: Auto remains Standard, fingers receive the
   margin, and mouse input does not. Switching input devices must not resize pieces.
4. Touch a nearby card, overlap two margins, open a modal over pieces, and interact
   at viewport edges. Confirm direct content and UI blocking take priority.
5. Begin a drag in a margin, use two fingers, cancel a touch, and repeat. Confirm
   no jump, duplicate roll, stuck drag, or changed ownership authorization.
6. Drop a counter onto another counter's original center: values merge once.
   Drop only onto its enlarged edge: no merge. Verify underlying valid drop
   targets still work. Keep dice just outside/inside zone boundaries and switch
   sizes: membership, totals, and rolled dice must not change.
7. Run mobile host/desktop client and desktop host/mobile client with different
   preferences. Include late join, reparent, save/reload, and ownership changes.
   Confirm local size changes send no gameplay size update or ownership request.
8. Change language with the setting visible and reopen it. All four localized
   choices must retain their selection and fit the dropdown.

Record devices, OS/browser versions, resolutions, zoom, preference, host/client
roles, results, and screenshots. The initial 1.5x/2x multipliers and 12-unit margin
remain provisional until physical-device calibration is complete. If tuning is
needed, update the OpenSpec requirements and tests alongside the constants.

## Validation status

Validated in Unity 6000.3.15f1 on 2026-10-16:

- Compilation succeeded without script errors.
- PiecePresentationTests: 21/21 passed, including real Input System touch dispatch,
  two simultaneous drags, cancellation, roll preferences, clipping, merging, and zones.
- OwnershipTests: 12/12 passed; these are offline authorization tests, not a
  substitute for a live host/client session.
- LocalizationPresentationTests: 22/22 passed. LifecycleDispatchTests: 3/3 passed.
- Browser bridge: six simulated browser-signal cases passed.
- All ten catalogs passed with zero errors or review warnings; generated Unity
  tables and the Play Settings prefab bindings validated successfully.
- Rendered previews confirmed the four-option settings row fits and the three
  explicit piece sizes render with their values. These are isolated previews,
  not a full baseline comparison of the desktop game.
- OpenSpec strict validation and `git diff --check` passed.

Reports are `Temp/piece-presentation-tests.json`, `Temp/piece-ownership-tests.json`,
`Temp/piece-localization-presentation-tests.json`, and `Temp/piece-lifecycle-tests.json`.
Previews are `Temp/piece-sizing-preview.png` and `Temp/piece-setting-preview.png`.
The touch test owns its input actions/device, drives player input updates explicitly,
and restores Editor input settings so its results do not depend on Game View focus.

Physical phone/tablet validation,
real mobile-browser interaction, and mixed-device multiplayer have not been
verified. No Android device was attached during implementation. Do not treat
Editor policy tests or a rendered preview as those device checks.
