using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using InventoryExpansion.Config;

namespace InventoryExpansion.Patches
{
    [HarmonyPatch]
    internal static class InventoryControllerPatches
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(InventoryController), nameof(InventoryController.Reset));
            yield return AccessTools.Method(typeof(InventoryController), nameof(InventoryController.RecalculateSlotCount));
        }

        private static int GetBasicSlotCount(InventoryController inventory) =>
            4 + (inventory.HasDedicatedSlots && InventoryExpansionPreferences.Enabled
                ? InventoryExpansionPreferences.AdditionalSlots : 0);

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var dedicated = AccessTools.Method(typeof(InventoryController), "GetDedicatedSlotCount");
            for (int i = 0; i + 3 < codes.Count; i++)
            {
                if (!codes[i].LoadsConstant(4) || codes[i + 1].opcode != OpCodes.Ldarg_0 ||
                    !codes[i + 2].Calls(dedicated) || codes[i + 3].opcode != OpCodes.Add) continue;
                // Add capacity before the native dedicated-slot and bag calculations.
                codes[i].opcode = OpCodes.Ldarg_0;
                codes[i].operand = null;
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(InventoryControllerPatches), nameof(GetBasicSlotCount))));
                return codes;
            }
            throw new InvalidOperationException("[InventoryExpansion] Native base inventory capacity calculation was not found.");
        }
    }
}
