## 1. Confirm integration boundaries

- [x] 1.1 Read the Unity C# style guide and relevant uGUI/Unity CLI skills; inspect the installed input module, prefab references, masking, and current acquisition/drop event ordering.
- [x] 1.2 Resolve native/mobile-browser/desktop classification behind a testable policy, documenting the available browser signal and Standard fallback without classifying touch desktops as mobile.
- [x] 1.3 Prove a focused input integration that separates acquisition hits from canonical drop hits, preserves original playable pointerDrag identity, and does not duplicate event dispatch; cover a drop target underneath an acquisition-only area before expanding the integration.

## 2. Add the local preference and presentation

- [x] 2.1 Add the persistent Auto/Standard/Large/Extra large preference and platform defaults to PlaySettings, including invalid-value fallback and a local change notification.
- [x] 2.2 Refactor Die and Counter presentation into local visual children while retaining root geometry, network identity, colliders, canonical drop surfaces, and serialized references; verify Standard matches the current appearance.
- [x] 2.3 Apply centered 1.0x/1.5x/2.0x scaling to artwork, values/logos, and selection feedback; refresh current pieces and handle creation, load, spawn, re-enable, and reparent without accumulated scale or stale subscriptions.

## 3. Add touch acquisition while preserving drops

- [x] 3.1 Implement projected visual-footprint acquisition with the initial 12 canvas-unit per-edge touch margin; keep the margin independent of table zoom and absent for mouse events.
- [x] 3.2 Implement direct-hit and UI-blocking priority, nearest-center assisted selection, render-order tie breaking, and eligibility checks for clipping, visibility, and interaction state.
- [x] 3.3 Integrate pointer capture/release with existing selection, drag thresholds/offsets, roll settings, multi-touch, cancellation, and ownership behavior; update geometry correctly after zoom, rotation, and viewport changes.
- [x] 3.4 Resolve all drops using original canonical geometry and ordering, excluding visual/margin-only surfaces before choosing recipients; verify CounterDropArea cannot merge through assistance and valid underlying targets remain reachable.

## 4. Expose the localized setting

- [x] 4.1 Add the size dropdown to the Play Settings Menu and wire it through PlaySettingsMenu with immediate refresh, correct reopening behavior, and existing keyboard/controller navigation patterns.
- [x] 4.2 Add localized label and option entries/bindings for all shipped locales; verify live language changes preserve the chosen preference and do not invoke gameplay actions.

## 5. Validate isolation and interaction behavior

- [x] 5.1 Add focused tests for platform defaults, explicit choices, invalid preferences, persistence, lifecycle refresh, and absence of logical size/transform/collider changes.
- [x] 5.2 Add input integration tests for touch-only margins, direct-hit priority, ties, blocked/clipped objects, zoom/rotation, mouse baseline, pointer cancellation, multi-touch, and drag/roll dispatch through the original playable.
- [x] 5.3 Add regression tests for counter drops inside canonical bounds, drops only inside enlarged visuals/margins, valid targets underneath assistance, and unchanged dice-zone membership/totals/roll scope at boundaries.
- [ ] 5.4 Verify mixed mobile/desktop multiplayer in both host directions, including different explicit preferences, late joins, reparenting, and save/reload; confirm preference changes emit no gameplay size update or ownership request.
- [x] 5.5 Use the Unity CLI to recompile and run the relevant PlayMode suites; investigate failures and record results under ignored Logs/ or Temp/.
- [ ] 5.6 Compare desktop Standard visuals and mouse behavior against the baseline, then validate readability and touch accuracy on physical phones/tablets, mobile web, and a touch desktop across dense tables, zoom levels, and orientations; record any unavailable environment explicitly.
- [ ] 5.7 Finalize multiplier/margin calibration from device evidence, update the design/specs if constants change, and retain a concise reproducible validation guide under developer-docs/ with generated reports under ignored Logs/ or Temp/.
