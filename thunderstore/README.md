# MIMESIS - InventoryExpansion

> 🛟 **Need help or found a bug?** Get support at [support.doodesch.de/inventoryexpansion](https://support.doodesch.de/inventoryexpansion).


> Adds extra backpack slots beneath the inventory HUD. Press `C` to raise the hotbar and reveal them, using the game's own slot frames and key hint.
> Carry more loot without permanently cluttering the standard 4-slot hotbar. Standalone, no MimicAPI required.

![Version](https://img.shields.io/badge/version-1.4.10-blue)
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
- Optional movement-speed reduction to 50% while the backpack is fully open, restored automatically when it closes.
- Blocks large items from entering the open backpack and prevents opening it while holding one; configurable through `BlockLargeItemsInBackpack`.
- Automatically hides the panel during loading screens and map changes, and when you leave the game or return to the title screen.

## Screenshots

**Open with loot** - press `C` to raise the native inventory HUD and reveal the backpack underneath.

![Open backpack with different items and native counters](https://raw.githubusercontent.com/DooDesch-Mods/Mimesis-InventoryExpansion/main/docs/img/backpack-open.jpg)

**Room for more** - choose any whole number from 4 to 24 extra slots. Rows stay centered and use at most eight slots each.

![Sixteen backpack slots in two rows of eight](https://raw.githubusercontent.com/DooDesch-Mods/Mimesis-InventoryExpansion/main/docs/img/backpack-16.jpg)

## Requirements

| Component | Version |
|-----------|---------|
| MIMESIS | 0.4.0 |
| MelonLoader | 0.7.3+ |

Dependency: [LavaGang-MelonLoader-0.7.3](https://thunderstore.io/c/mimesis/p/LavaGang/MelonLoader/).

## Installation

### Recommended: Thunderstore mod manager

Install through a Thunderstore mod manager such as [r2modman](https://thunderstore.io/package/ebkr/r2modman/) or [Gale](https://thunderstore.io/package/Kesomannen/GaleModManager/). The MelonLoader dependency is handled automatically.

### Manual

1. Install [MelonLoader 0.7.3](https://melonwiki.xyz/#/) into MIMESIS.
2. Download this package and place `InventoryExpansion.dll` into `MIMESIS/Mods/`.
3. Launch the game once to generate the configuration file at `UserData/MelonPreferences.cfg`.

## Configuration

Stored in `UserData/MelonPreferences.cfg` under the `[InventoryExpansion]` category. You can also edit these through a MelonPreferences UI.

The host controls `Enabled`, `AdditionalSlots`, `ReduceMovementSpeed`, `FillBackpackFirst`, and `BlockLargeItemsInBackpack` for the lobby. Joining clients use those values automatically without changing their local config. `BackpackKey`, `SelectBackpackSlotOnOpen`, and `RestoreStandardSlotOnClose` stay local. A host without InventoryExpansion uses native inventory capacity and no mod backpack. Shared-setting changes apply to the next lobby.

| Option | Description | Default | Values/Range |
|--------|-------------|---------|--------------|
| `Enabled` | Enable InventoryExpansion functionality. When disabled, the mod will not modify game behavior. | `true` | `true` / `false` |
| `AdditionalSlots` | Number of extra inventory slots to add on top of the game's default inventory size. Any whole number from 4 to 24. Rows are balanced with at most 8 slots each. Values outside this range are clamped. | `4` | `4` to `24` (clamped on load) |
| `BackpackKey` | Key to toggle backpack visibility. Press to switch between standard inventory and backpack. | `C` | A `UnityEngine.KeyCode` name that is also supported by the Unity Input System. Invalid or empty values fall back to `C`. |
| `ReduceMovementSpeed` | When enabled, player movement speed is reduced to 50% while the backpack is fully open. | `true` | `true` / `false` |
| `SelectBackpackSlotOnOpen` | When opening the backpack, move the selected slot to the first backpack slot. | `true` | `true` / `false` |
| `RestoreStandardSlotOnClose` | When closing the backpack, return the selected slot to the standard inventory (the slot selected before opening). | `true` | `true` / `false` |
| `FillBackpackFirst` | While the backpack is open, picked-up items fill the backpack slots before the standard inventory. Host / single-player only. | `true` | `true` / `false` |
| `BlockLargeItemsInBackpack` | Block large items from entering the open backpack and prevent opening it while holding one. | `true` | `true` / `false` |

## Usage

- Press the configured toggle key (default `C`) to raise the inventory HUD and reveal the backpack underneath. Press it again to close the backpack and restore the original HUD height.
- When the backpack is open, slot scrolling and selection cycle through its slots. When closed, native visible hotbar, cosmetic and equipment slots remain available.
- Opening the backpack jumps your selection to the first backpack slot; closing it returns the selection to your previous standard slot, so the cursor is never stuck on a hidden slot (toggle each via `SelectBackpackSlotOnOpen` / `RestoreStandardSlotOnClose`).
- With `FillBackpackFirst` on, items you pick up while the backpack is open go into backpack slots first. This changes server-side placement, so it only applies when you host or play single-player.
- With `ReduceMovementSpeed` on, you move at 50% speed while the backpack is fully open, and full speed is restored when it closes.
- The panel auto-hides during loading screens and map changes, and when you leave the game or return to the title screen.

In multiplayer, install the mod on the host and clients using the extra slots. Clients automatically use the host's slot count and gameplay settings. Backpack-first pickup applies to the host's own inventory.

Source code and issues: [github.com/DooDesch-Mods/Mimesis-InventoryExpansion](https://github.com/DooDesch-Mods/Mimesis-InventoryExpansion).

## Images

Screenshots and the package icon are captured in MIMESIS.

Built against MIMESIS 0.4.0 / MelonLoader 0.7.3.

Use InventoryExpansion 1.4.10 or later on both host and clients for counts other than 4, 9 or 16.
