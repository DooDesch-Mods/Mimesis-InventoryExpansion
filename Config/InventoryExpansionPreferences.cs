using System;
using DooDesch;
using MelonLoader;
using UnityEngine;

namespace InventoryExpansion.Config
{
	internal static class InventoryExpansionPreferences
	{
		private const string CategoryId = "InventoryExpansion";

		private static MelonPreferences_Category _category;
		private static MelonPreferences_Entry<bool> _enabled;
		private static MelonPreferences_Entry<int> _additionalSlots;
		private static MelonPreferences_Entry<string> _backpackKey;
		private static MelonPreferences_Entry<bool> _reduceMovementSpeed;
		private static MelonPreferences_Entry<bool> _selectBackpackSlotOnOpen;
		private static MelonPreferences_Entry<bool> _restoreStandardSlotOnClose;
		private static MelonPreferences_Entry<bool> _fillBackpackFirst;
		private static MelonPreferences_Entry<bool> _blockLargeItemsInBackpack;
		private static HostSetting<bool> _effectiveEnabled;
		private static HostSetting<int> _effectiveAdditionalSlots;
		private static HostSetting<bool> _effectiveReduceMovementSpeed;
		private static HostSetting<bool> _effectiveFillBackpackFirst;
		private static HostSetting<bool> _effectiveBlockLargeItemsInBackpack;
		internal static event Action EffectiveValuesChanged;
#if DEBUG
		private static MelonPreferences_Entry<bool> _debugDisableOwnerGate;
#endif

		internal static void Initialize()
		{
			if (_category != null)
			{
				return;
			}

			_category = MelonPreferences.CreateCategory(CategoryId, "InventoryExpansion");
			_enabled = CreateEntry("Enabled", true, "Enabled", "Enable InventoryExpansion functionality. When disabled, the mod will not modify game behavior.");
			_additionalSlots = CreateEntry(
				"AdditionalSlots",
				4,
				"Additional Inventory Slots",
				"Number of extra inventory slots to add on top of the game's default inventory size. Any whole number from 4 to 24 is supported. Rows stay balanced with at most eight slots each. Values outside this range are clamped."
			);
			_backpackKey = CreateEntry(
				"BackpackKey",
				"C",
				"Backpack Toggle Key",
				"Key to toggle backpack visibility. Press to switch between standard inventory and backpack."
			);
			_reduceMovementSpeed = CreateEntry(
				"ReduceMovementSpeed",
				true,
				"Reduce Movement Speed",
				"When enabled, player movement speed is reduced to 50% while the backpack is fully open."
			);
			_selectBackpackSlotOnOpen = CreateEntry(
				"SelectBackpackSlotOnOpen",
				true,
				"Select Backpack Slot On Open",
				"When opening the backpack, move the selected slot to the first backpack slot."
			);
			_restoreStandardSlotOnClose = CreateEntry(
				"RestoreStandardSlotOnClose",
				true,
				"Restore Standard Slot On Close",
				"When closing the backpack, return the selected slot to the standard inventory (the slot that was selected before opening)."
			);
			_fillBackpackFirst = CreateEntry(
				"FillBackpackFirst",
				true,
				"Fill Backpack First",
				"While the backpack is open, picked-up items fill the backpack slots before the standard inventory. Takes effect only when you are the host or in single-player."
			);
			_blockLargeItemsInBackpack = CreateEntry(
				"BlockLargeItemsInBackpack",
				true,
				"Block Large Items In Backpack",
				"When enabled, large items (the ones you cannot switch away from while holding them, e.g. two-handed items) cannot be picked up into the backpack while it is open. Trying to do so shows a message instead. Disable to allow large items in the backpack."
			);
#if DEBUG
			_debugDisableOwnerGate = CreateEntry(
				"DebugDisableOwnerGate",
				false,
				"[DEBUG] Disable Backpack Owner Gate",
				"Debug only: when true, the backpack-first redirect ignores who owns the inventory (the old, buggy behaviour), so the multiplayer item duplication can be reproduced for diagnosis. Keep this false for normal play."
			);
#endif
			_additionalSlots.Value = ClampToValidBackpackSize(_additionalSlots.Value);
			var hostSettings = new HostSettings(CategoryId);
			_effectiveEnabled = hostSettings.Register(_enabled, false);
			_effectiveAdditionalSlots = hostSettings.Register(_additionalSlots, 0, value => value == 0 || (value >= 4 && value <= 24));
			_effectiveReduceMovementSpeed = hostSettings.Register(_reduceMovementSpeed, false);
			_effectiveFillBackpackFirst = hostSettings.Register(_fillBackpackFirst, false);
			_effectiveBlockLargeItemsInBackpack = hostSettings.Register(_blockLargeItemsInBackpack, false);
			hostSettings.EffectiveValuesChanged += () => EffectiveValuesChanged?.Invoke();
		}

		private static MelonPreferences_Entry<T> CreateEntry<T>(string identifier, T defaultValue, string displayName, string description = null)
		{
			if (_category == null)
			{
				throw new InvalidOperationException("Preference category not initialized.");
			}

			return _category.CreateEntry(identifier, defaultValue, displayName, description);
		}

		internal static bool Enabled => _effectiveEnabled?.Value ?? _enabled?.Value ?? true;

		private static int ClampToValidBackpackSize(int value)
		{
			return Math.Clamp(value, 4, 24);
		}

		internal static int AdditionalSlots => _effectiveAdditionalSlots?.Value ?? ClampToValidBackpackSize(_additionalSlots?.Value ?? 4);

		internal static KeyCode BackpackKey
		{
			get
			{
				if (_backpackKey == null)
				{
					return KeyCode.C;
				}

				string keyString = _backpackKey.Value;
				if (string.IsNullOrEmpty(keyString))
				{
					return KeyCode.C;
				}

				if (Enum.TryParse<KeyCode>(keyString, true, out KeyCode keyCode))
				{
					return keyCode;
				}

				return KeyCode.C;
			}
		}

		internal static bool ReduceMovementSpeed => _effectiveReduceMovementSpeed?.Value ?? _reduceMovementSpeed?.Value ?? true;

		internal static bool SelectBackpackSlotOnOpen => _selectBackpackSlotOnOpen?.Value ?? true;

		internal static bool RestoreStandardSlotOnClose => _restoreStandardSlotOnClose?.Value ?? true;

		internal static bool FillBackpackFirst => _effectiveFillBackpackFirst?.Value ?? _fillBackpackFirst?.Value ?? true;

		internal static bool BlockLargeItemsInBackpack => _effectiveBlockLargeItemsInBackpack?.Value ?? _blockLargeItemsInBackpack?.Value ?? true;
#if DEBUG
		internal static bool DebugDisableOwnerGate => _debugDisableOwnerGate?.Value ?? false;
#endif
	}
}
