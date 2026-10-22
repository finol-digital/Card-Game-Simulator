# Steam Deck validation (issue #581)

## Integration

`Cgs.Steam` is a desktop-only assembly using the pinned Steamworks.NET package.
It initializes Steam without requiring Steam for other distributions, pumps
callbacks, and shuts down its owned API session. In the Editor it does not
initialize Steam automatically. `steam_appid.txt` identifies CGS (1742850) for
local development; do not copy this development file into Steam depots. Builds
launched by Steam obtain their App ID from the client.

On a Steam Deck, the integration selects a 1280 × 800 fullscreen framebuffer,
including when Unity previously saved a 4:3 display mode. The Settings resolution
selector remains a UI scaling preference, independent of the framebuffer.

Prompts default to a connected controller (or the detected Deck). Keyboard keys
and mouse clicks switch to desktop prompts; controller buttons and deliberate
stick movement restore controller prompts. Mouse motion, including trackpad or
gyro input, does not change the prompt device. Keyboard events generated while
Steam's floating keyboard is open also preserve controller prompts.

The floating keyboard watches the currently focused uGUI or TMP input field,
including dynamically created fields. Read-only and disabled fields are ignored.
The field rectangle is converted to Steam's top-left client pixel coordinates.
Single-line and multiline editing use their corresponding Steam modes. Dismissal
is handled through the Steam callback; a dismissed keyboard stays closed until
another field is focused or the player activates the field again.

## Steam release setup

In Steamworks for App ID 1742850, publish a default Steam Input configuration
based on the **Gamepad** template. CGS uses Unity's gamepad bindings; a legacy
configuration that emulates only keyboard/mouse events cannot supply controller
identity to Unity. Keep trackpad mouse movement if desired. This partner-site
configuration is separate from the repository and must be checked before release.

## Validation

Run `unity command run_tests --mode playmode --filter SteamDeckTests --async_tests`
and poll `unity command test_status`. These tests mock the floating keyboard and
exercise real Unity input events and both input field implementations. They do
not verify the Steam overlay or actual Deck hardware.

On a Steam-launched build on physical Deck hardware (native Linux and the Windows
build under Proton if both are distributed):

1. Start with the built-in controls. Confirm controller button labels on the title
   screen, menus, card/deck actions, and play help. Move the trackpad and gyro;
   prompts must remain controller prompts. Connect a keyboard/mouse, press a key
   or click, then use the controller again; prompts should follow the active device.
2. Focus Player Name, Deck Name, Id, Set Code, Rank, and card search using both
   controller navigation and the trackpad. Confirm the keyboard opens without
   Steam+X, the field remains visible, and typing edits the correct field.
3. Dismiss with Enter and with the keyboard close button. Confirm it stays closed,
   can reopen on reactivation, and does not reopen during navigation away. Switch
   directly between fields and close their containing menu. Also check multiline
   text, validation, selection replacement, and non-ASCII text entry.
4. Verify the framebuffer is 1280 × 800, with no letterboxing/pillarboxing, from
   first launch and after previously using a different display mode. Check title,
   settings, deck/card editors, gameplay, and modal dialogs for clipped controls.
5. Launch the same build without Steam and confirm ordinary desktop input still
   works. Confirm mobile/WebGL builds contain no Steam integration assembly.

Reference: [Valve's Steam Deck recommendations](https://partner.steamgames.com/doc/steamhardware/recommendations)
and [ISteamUtils floating keyboard API](https://partner.steamgames.com/doc/api/ISteamUtils#ShowFloatingGamepadTextInput).
