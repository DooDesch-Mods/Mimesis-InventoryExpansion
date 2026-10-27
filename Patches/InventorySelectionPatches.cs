using System;
using System.Reflection;
using HarmonyLib;
using InventoryExpansion.Config;
using InventoryExpansion.Patches;
using MelonLoader;
using Mimic.Actors;

namespace InventoryExpansion.Patches
{
	internal static class InventorySelectionHelper
	{
		internal static Type GetInventoryType()
		{
			return typeof(ProtoActor).GetNestedType("Inventory", BindingFlags.Public | BindingFlags.NonPublic);
		}

		internal static FieldInfo GetSelectedSlotIndexField(Type inventoryType)
		{
			return inventoryType?.GetField("selectedSlotIndex", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		internal static FieldInfo GetSlotSizeField(Type inventoryType)
		{
			return inventoryType?.GetField("slotSize", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		// The local avatar's client-side Inventory instance is stored in ProtoActor's
		// private "inventory" field.
		internal static FieldInfo GetActorInventoryField()
		{
			return typeof(ProtoActor).GetField("inventory", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		internal static MethodInfo GetSelectSlotMethod(Type inventoryType)
		{
			return inventoryType?.GetMethod("SelectSlot", BindingFlags.Instance | BindingFlags.NonPublic);
		}
	}

	[HarmonyPatch]
	internal static class InventorySelectionNextPatches
	{
		private static MethodBase TargetMethod()
		{
			var inventoryType = InventorySelectionHelper.GetInventoryType();
			return inventoryType?.GetMethod("SelectNextSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		[HarmonyPrefix]
		private static bool SelectNextSlot_Prefix(object __instance)
		{
			return InventorySelectionCommon.HandleSlotSelection(__instance, true);
		}
	}

	[HarmonyPatch]
	internal static class InventorySelectionPreviousPatches
	{
		private static MethodBase TargetMethod()
		{
			var inventoryType = InventorySelectionHelper.GetInventoryType();
			return inventoryType?.GetMethod("SelectPreviousSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		[HarmonyPrefix]
		private static bool SelectPreviousSlot_Prefix(object __instance)
		{
			return InventorySelectionCommon.HandleSlotSelection(__instance, false);
		}
	}

	internal static class InventorySelectionCommon
	{
		internal static bool HandleSlotSelection(object __instance, bool forward)
		{
			try
			{
				if (!InventoryExpansionPreferences.Enabled)
				{
					return true;
				}

				if (__instance == null)
				{
					return true;
				}

				var inventoryType = __instance.GetType();
				var selectedSlotIndexField = InventorySelectionHelper.GetSelectedSlotIndexField(inventoryType);
				var slotSizeField = InventorySelectionHelper.GetSlotSizeField(inventoryType);

				if (selectedSlotIndexField == null || slotSizeField == null)
				{
					return true;
				}

                int currentSlot = (int)selectedSlotIndexField.GetValue(__instance);
                bool open = BackpackPanelPatch.IsBackpackFullyVisible;
                var visible = (System.Collections.Generic.List<int>)inventoryType.GetMethod("GetVisibleSlotIndices").Invoke(__instance, null);
                var candidates = visible.FindAll(index => (index >= Mimic.InvenSlotLayout.FirstExtensionSlotKey - 1) == open);
                if (candidates.Count == 0) return false;
                int current = candidates.IndexOf(currentSlot);
                int next = current < 0 ? (forward ? 0 : candidates.Count - 1)
                    : (current + (forward ? 1 : -1) + candidates.Count) % candidates.Count;
                InventorySelectionHelper.GetSelectSlotMethod(inventoryType).Invoke(__instance, new object[] { candidates[next] });
                return false;

			}
			catch (Exception ex)
			{
				MelonLogger.Error($"[InventoryExpansion][Selection] Slot selection prefix failed: {ex}");
				return true;
			}
		}
	}
}
