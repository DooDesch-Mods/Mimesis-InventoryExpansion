# MIMESIS - InventoryExpansion

> 🛟 **Need help or found a bug?** Get support at [support.doodesch.de/inventoryexpansion](https://support.doodesch.de/inventoryexpansion).


> Adds extra backpack slots beneath the inventory HUD. Press `C` to raise the hotbar and reveal them, using the game's own slot frames and key hint.
> Carry more loot without permanently cluttering the standard 4-slot hotbar. Standalone, no MimicAPI required.

![Version](https://img.shields.io/badge/version-1.5.0-blue)
![Game](https://img.shields.io/badge/game-MIMESIS-purple)
![MelonLoader](https://img.shields.io/badge/MelonLoader-0.7.3+-green)
![Status](https://img.shields.io/badge/status-working-brightgreen)

![InventoryExpansion in MIMESIS](https://raw.githubusercontent.com/DooDesch-Mods/Mimesis-InventoryExpansion/main/docs/img/header.jpg)

## Features

- Adds any whole number from 4 to 24 usable inventory slots and preserves the native cosmetic and equipment slots. Carried bags add their normal capacity bonus.
- Toggle between the standard hotbar and the backpack with a configurable key (default `C`); the mod reads the key live each frame.
- Opening raises only the inventory HUD, including equipment and weight. The backpack sits below it with native slot artwork, a faint backing and small side marks.
- Rows have at most 8 slots and stay centered and balanced: 9 slots use 5+4, 12 use 6+6, 14 use 7+7, and 18 use 6+6+6.
- A small native key badge at the bottom shows the configured toggle key.
- While open, slot cycling affects backpack slots. While closed, it follows the visible native hotbar, cosmetic and equipment slots.
- Cursor handoff on toggle: opening the backpack moves your selection to the first backpack slot, and closing it returns the selection to the standard inventory, so the cursor never stays stuck on a hidden slot. Each direction is configurable.
- Optional backpack-first pickup: while the backpack is open, items you pick up fill the backpack slots before the standard inventory (host / single-player only).
- Configurable movement-speed reduction (50% by default) while the backpack is fully open, restored automatically when it closes.
- Blocks large items from entering the open backpack and prevents opening it while holding one; configurable through `BlockLargeItemsInBackpack`.
- Automatically hides the panel during loading screens and map changes, and when you leave the game or return to the title screen.

## Screenshots

Captured in MIMESIS with the published mod. Open the backpack with `C`; the native inventory HUD moves up above it.

<table>
  <tr>
    <td width="50%"><img src="https://raw.githubusercontent.com/DooDesch-Mods/Mimesis-InventoryExpansion/main/docs/img/backpack-open.jpg" alt="Four backpack slots filled with different items, beneath the raised native inventory HUD"><br><b>Open with loot</b> - native frames, item icons and counters.</td>
    <td width="50%"><img src="https://raw.githubusercontent.com/DooDesch-Mods/Mimesis-InventoryExpansion/main/docs/img/backpack-closed.jpg" alt="Closed backpack with the inventory HUD restored and a small C key hint"><br><b>Closed</b> - the hotbar returns to its original height.</td>
  </tr>
  <tr>
    <td width="50%"><img src="https://raw.githubusercontent.com/DooDesch-Mods/Mimesis-InventoryExpansion/main/docs/img/backpack-9.jpg" alt="Nine backpack slots arranged in centered rows of five and four"><br><b>Nine slots</b> - balanced rows of 5+4.</td>
    <td width="50%"><img src="https://raw.githubusercontent.com/DooDesch-Mods/Mimesis-InventoryExpansion/main/docs/img/backpack-16.jpg" alt="Sixteen backpack slots arranged in two centered rows of eight"><br><b>Sixteen slots</b> - two rows of 8.</td>
  </tr>
</table>

## Requirements

| Component | Version |
|-----------|---------|
| MIMESIS | 0.4.0 |
| MelonLoader | 0.7.3+ |

## Installation

### Recommended: Thunderstore mod manager

Install through a Thunderstore mod manager such as [r2modman](https://thunderstore.io/package/ebkr/r2modman/) or [Gale](https://thunderstore.io/package/Kesomannen/GaleModManager/). It resolves the MelonLoader dependency automatically.

### Manual

1. Install [MelonLoader 0.7.3](https://melonwiki.xyz/#/) into MIMESIS.
2. Download the latest release from the [releases page](https://github.com/DooDesch-Mods/Mimesis-InventoryExpansion/releases).
3. Place `InventoryExpansion.dll` into `MIMESIS/Mods/`.
4. Launch the game once to generate the configuration file at `UserData/MelonPreferences.cfg`.

## Configuration

Stored in `UserData/MelonPreferences.cfg` under `[InventoryExpansion]`.

Gameplay settings follow the lobby host without overwriting saved client preferences. Without the mod on the host, clients use vanilla behavior. Live settings apply after the host changes them through a MelonPreferences UI or reloads the preferences; editing the file alone does not reload it. Use the same current mod version on host and clients for the new options and live updates. Older versions retain their initial legacy snapshot. Invalid new values use the registered default with a warning.

`Enabled` and `AdditionalSlots` are structural and apply to the next lobby. Movement, pickup and large-item rules are live. Input, selection and HUD animation stay local.

| Option | Default | Range and effect |
|---|---|---|
| `Enabled` | `true` | Host master switch, next lobby. |
| `AdditionalSlots` | `4` | Host capacity, 4-24 whole slots, next lobby. Values outside the range are clamped on load. Native balanced rows use at most 8 columns. |
| `ReduceMovementSpeed` | `true` | Apply the movement multiplier while the backpack is fully open. |
| `MovementSpeedMultiplier` | `0.5` | Walking and running multiplier, 0-1. Zero stops movement, 1 keeps normal speed. |
| `FillBackpackFirst` | `true` | Prefer the open backpack for the host's own pickups. |
| `BlockLargeItemsInBackpack` | `true` | Restrict large items and opening while holding one. |
| `BackpackKey` | `C` | Local toggle key; use a Unity key name supported by the Input System. Invalid or empty values fall back to C. |
| `SelectBackpackSlotOnOpen` | `true` | Local selection handoff into the backpack. |
| `RestoreStandardSlotOnClose` | `true` | Local return to the previous standard slot. |
| `AnimationSeconds` | `0.25` | Local HUD animation, 0-2 seconds. Zero opens immediately. |

## Usage

- Press the configured toggle key (default `C`) to raise the inventory HUD and reveal the backpack underneath. Press it again to close the backpack and restore the original HUD height.
- When the backpack is open, slot scrolling and selection cycle through its slots. When closed, native visible hotbar, cosmetic and equipment slots remain available.
- Opening the backpack jumps your selection to the first backpack slot; closing it returns the selection to your previous standard slot, so the cursor is never stuck on a hidden slot (toggle each via `SelectBackpackSlotOnOpen` / `RestoreStandardSlotOnClose`).
- With `FillBackpackFirst` on, items you pick up while the backpack is open go into backpack slots first. This changes server-side placement, so it only applies when you host or play single-player.
- With `ReduceMovementSpeed` on, you move at the configured multiplier (50% by default) while the backpack is fully open, and full speed is restored when it closes.
- The panel auto-hides during loading screens and map changes, and when you leave the game or return to the title screen.
- Set `AdditionalSlots` (any whole number from 4 to 24) and `BackpackKey` in `UserData/MelonPreferences.cfg`; the mod reads the key live each frame.

## Compatibility

Built for Mimesis 0.4.0 / MelonLoader 0.7.3. In multiplayer, install it on the host and clients using the extra slots. Clients automatically use the host's slot count and gameplay settings; they provide the backpack UI. Backpack-first pickup applies to the host's own inventory.

Use InventoryExpansion 1.4.10 or later on both host and clients for counts other than 4, 9 or 16.

## Building (developers)

```
dotnet build -c Release
```

References are resolved from `Workspace/lib/game` (game DLLs) and `Workspace/lib/melonloader`. The shared deploy configuration defaults to `_Test/MIMESIS/Mods`.

## Credits / License

Author: DooDesch. Provided as-is under the MIT License. Contributions welcome via pull requests on the [repository](https://github.com/DooDesch-Mods/Mimesis-InventoryExpansion).

## Images

Screenshots and the package icon are captured in MIMESIS.
