## Why

Dice and counters have comfortable desktop dimensions but can be difficult to read and select on mobile devices. Players need larger local visuals and more forgiving touch selection without changing the shared tabletop or the existing desktop experience.

## What Changes

- Add a device-local Dice and counter size preference: Auto, Standard, Large, and Extra large.
- Default Auto to Standard (1.0x) on desktop and Large (initially 1.5x) on mobile; offer Extra large (2.0x) as an explicit choice.
- Scale dice and counter artwork, values, and selection feedback locally while preserving shared sizes, positions, colliders, saves, and network state.
- Expand touch acquisition targets for selecting and starting drags, with direct-hit priority and deterministic resolution of overlapping targets.
- Keep counter merge/drop targets and dice-zone membership independent of visual enlargement and touch assistance.
- Localize the preference and apply it to existing and newly created pieces without restarting play.

## Capabilities

### New Capabilities

- `local-dice-counter-sizing`: Local display preference, platform defaults, visual scaling, and multiplayer/gameplay isolation.
- `dice-counter-touch-targets`: Touch-only acquisition assistance with predictable targeting and unchanged drop and zone behavior.

### Modified Capabilities

None. Existing localization, ownership, and zone contracts remain in force.

## Impact

- PlaySettings, PlaySettingsMenu, the Play Settings Menu prefab, and localization catalogs.
- Die and Counter prefabs and presentation components; focused integration with CgsNetPlayable input and selection handling.
- Touch routing around the existing uGUI input path and CounterDropArea; DiceZone collider behavior must remain unchanged.
- Focused PlayMode coverage and device checks for desktop, mobile, touch-enabled browsers, and mixed desktop/mobile multiplayer.
- No new package dependency, network protocol, save format, public website change, or changes under `.github` are planned.
