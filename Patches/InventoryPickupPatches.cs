using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using InventoryExpansion.Config;
using Mimic;

namespace InventoryExpansion.Patches
{
    [HarmonyPatch(typeof(InventoryController), "FindSlotForAdd")]
    internal static class InventoryPickupPatches
    {
        private static readonly FieldInfo SelfField = AccessTools.Field(typeof(InventoryController), "_self");
        private static readonly FieldInfo SlotsField = AccessTools.Field(typeof(InventoryController), "_inventorySlots");

        private static void Postfix(InventoryController __instance, ref int __result)
        {
            if (!InventoryExpansionPreferences.Enabled || !InventoryExpansionPreferences.FillBackpackFirst ||
                !BackpackPanelPatch.IsBackpackFullyVisible || __result <= 0 || __result > 4) return;
            // Keep native dedicated-slot priority and item validation. Redirect only a basic-slot
            // choice for the confirmed local player's own server inventory.
            var owner = SelfField.GetValue(__instance) as VActor;
            var avatar = Hub.Main?.GetMyAvatar();
            if (owner == null || avatar == null || owner.ObjectID != avatar.ActorID) return;
            var slots = (Dictionary<int, ItemElement>)SlotsField.GetValue(__instance);
            int target = 0;
            foreach (var slot in slots)
                if (slot.Key >= InvenSlotLayout.FirstExtensionSlotKey && slot.Value == null &&
                    !__instance.IsSlotLocked(slot.Key) && (target == 0 || slot.Key < target)) target = slot.Key;
            if (target != 0) __result = target;
        }
    }
}
