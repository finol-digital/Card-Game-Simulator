## Context

Die.prefab and Counter.prefab have 100 x 100 root RectTransforms. PlayGame uses a 1920 x 1080 reference canvas, and tabletop zoom affects apparent piece size. CgsNetPlayable.Size writes the root RectTransform and BoxCollider2D and synchronizes the size; network spawn also reads the root dimensions. ParentTo resets root scale to one. Applying a mobile multiplier to this root would couple a presentation preference to shared state or lose it during reparenting.

DiceZone determines membership through collider overlaps. CounterDropArea receives uGUI drop events and Counter.OnDrop combines values and deletes the receiving counter. Enlarging raycast rectangles without distinguishing acquisition from drop would therefore change gameplay. The selected-piece viewer and roll actions already exist and remain the action surface for this change.

## Goals / Non-Goals

**Goals:**
- Improve local dice/counter readability and finger acquisition on mobile.
- Preserve default desktop rendering and mouse interactions.
- Keep preference changes independent of shared sizes, ownership, saves, colliders, merging, and zone membership.
- Support existing pieces, new pieces, loaded sessions, and late multiplayer joins.

**Non-Goals:**
- Minimum on-screen visual size across all zoom levels, automatic table zoom, or global canvas scaling.
- Redesigning the action viewer, adding new gestures, or changing roll/merge semantics.
- Resizing cards, boards, zones, or saved/game-authored geometry.
- Separate mobile network prefabs or new packages.

## Decisions

### 1. Store an explicit local preference with stable Auto resolution

Add a PlayerPrefs-backed choice in PlaySettings and a localized dropdown in PlaySettingsMenu: Auto, Standard (1.0x), Large (1.5x), Extra large (2.0x). Missing or unrecognized stored values resolve to Auto. Auto selects Large for native Android/iOS and positively identified mobile browsers, and Standard for desktop, including desktops with touch hardware. Unknown browser classification defaults to Standard; the explicit choices remain available everywhere.

Use a small testable platform-policy boundary. Inspect existing browser detection before adding a focused platform bridge if necessary. Do not classify a device as mobile solely because Touchscreen.current exists, or switch visuals every time a player alternates mouse and touch. Touch acquisition assistance is instead selected per input event, including on touch-enabled desktops. Re-evaluate layout on orientation/viewport changes without resetting the preference.

The initial 1.5x and 2.0x values are concrete implementation defaults to validate on devices. Adjustments require updating these artifacts and their acceptance expectations before implementation is considered complete. A fixed multiplier is chosen over adaptive visual sizing to keep this first version bounded and predictable.

### 2. Separate logical roots, visual children, and acquisition geometry

Keep the original network identity, root size/scale, canonical drop geometry, and colliders. Introduce a local visual child for the artwork, numbers/logo, and selection feedback. Its centered multiplier scales the complete piece consistently, including highlights and current color/value, without altering the root. Update SelectableGraphic and prefab references as needed while preserving the current 1.0x rendering and lifecycle behavior.

Render-only children must not independently intercept gameplay drops. Route acquisition hits to the original Die/Counter component so selection and pointerDrag identity remain compatible with existing handlers. Keep canonical root graphics/raycast surfaces as needed for drop routing, avoiding duplicate visible rendering. Preserve masking, sorting, visibility, and CanvasGroup interaction rules.

Apply the preference through a local presentation refresh on initialization and preference changes; support spawn, reload, and reparent without accumulating multipliers. Recompute screen hit geometry when layout, rotation, zoom, or viewport changes. Do not introduce per-frame scene-wide searches or allocate candidate lists on every frame.

Changing CgsNetPlayable.Size, scaling the root, or making different network prefabs per platform are rejected because they can alter colliders, shared dimensions, or lifecycle behavior.

### 3. Assist acquisition only and resolve ambiguity predictably

Integrate a focused resolver with the existing uGUI input path, maintaining one effective event dispatch per pointer. Keep separate acquisition and canonical drop queries rather than simply padding the shared Graphic raycast region.

For touch pointer-down, consider the locally rendered footprint plus a bounded margin. Start with 12 canvas reference units per edge, converted to screen distance using the owning screen canvas scale, independent of tabletop zoom. This is a tuning baseline, not a claim that Unity units equal native accessibility points or dp. For mouse input, use the displayed footprint with no extra margin; at Standard, this is the existing footprint.

Resolve modal/UI occlusion first. A direct hit on visible interactive content, including a card or another piece, wins over another piece's margin. Where only assisted margins overlap, choose the nearest eligible projected piece center, breaking ties with existing render order. Ignore hidden, clipped, inactive, or non-interactable objects. Empty table space can yield an assisted piece; otherwise it keeps its existing pan/selection behavior.

Capture the resolved piece for the pointer lifetime and preserve existing drag thresholds, offsets, roll selection semantics, authorization, and multi-touch handling. Acquisition must not jump the piece under the finger, emit a second action, or leak state after cancellation. The integration task must prove event routing with the installed input module before broader prefab wiring.

### 4. Resolve drops against canonical geometry

During drop resolution, exclude the local visual enlargement and assistance margins for every pointer type. Use the original root-space target geometry and existing ordering/eligibility rules, respecting rotation and parent transforms. Ensure a rejected visual-only hit does not consume a valid canonical target underneath; filtering must happen before choosing the drop recipient, not solely as an early return inside CounterDropArea.OnDrop.

The expanded acquisition layer must not receive or bubble drop events that merge counters. CounterDropArea can also guard canonical bounds as defense in depth, but it cannot be the only protection. This applies to dropping cards onto counter-adjacent areas as well as counter-to-counter drops. DiceZone continues querying its original colliders with no presentation-dependent changes.

### 5. Refresh and localize through existing settings patterns

Apply changes to visible pieces immediately and use the same preference on later creation/load/join. Subscribe/unsubscribe local listeners through existing lifecycle conventions. Add labels and choices through the existing localization catalogs and bindings, respecting the localized-interface specification without modifying its requirements. The choice must not trigger a network update, roll, counter edit, or save modification.

## Risks / Trade-offs

- Enlarged visuals cover nearby content -> Preserve direct-hit priority and offer Standard; test dense layouts at 1.5x and 2.0x. Automatic overlap avoidance is out of scope.
- The visible counter edge extends beyond its merge target -> Keep the canonical merge rule, document the distinction in developer validation notes, and test deliberate near-edge drops.
- uGUI can use the same raycast result for press, drag, and drop -> Prove separate acquisition/drop routing with integration tests; a padded Graphic alone is insufficient.
- Incorrect browser or touch classification -> Test native/mobile web/desktop web separately and keep explicit overrides; unknown environments use Standard.
- Visual refactoring changes highlighting or preview identity -> Verify selection, hover, color, value, roll animation, counter logo, and network reparenting at 1.0x and enlarged scales.
- A fixed canvas-relative margin may feel different across displays -> Calibrate on physical phones/tablets and representative zoom levels; record results before finalizing constants.

## Migration Plan

No save or protocol migration is required. Existing installs without the preference use Auto. Add the local presentation and input behavior together so enlargement does not accidentally broaden drops during rollout. Reverting this change restores original prefabs/input behavior; the unused local preference can remain harmlessly stored. This proposal does not authorize publishing or deployment.

## Open Questions

- Mobile WebGL classification uses a local browser bridge for Android/iOS user agents and the iPadOS MacIntel/multiple-touch-points convention. Unknown identities fall back to Standard; touch capability alone does not classify a desktop as mobile. Six browser-signal cases pass, while real mobile-browser interaction remains a device acceptance check.
- Do physical-device checks support the initial multiplier and margin values? Record measurements and any justified tuning in developer-docs or ignored Logs/Temp output before completion.
