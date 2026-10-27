using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using InventoryExpansion.Config;
using Mimic;
using UnityEngine;

namespace InventoryExpansion.Patches
{
    [HarmonyPatch(typeof(UIPrefab_Inventory))]
    internal static class InventoryUiPatches
    {
        internal static int LogicalSlotCount { get; private set; }
        private static UIPrefab_Inventory _currentUi;
        internal static readonly FieldInfo SlotsField = AccessTools.Field(typeof(UIPrefab_Inventory), "inventorySlots");

        [HarmonyPostfix, HarmonyPatch("Awake")]
        private static void AwakePostfix(UIPrefab_Inventory __instance) => EnsureSlots(__instance, 0);

        [HarmonyPrefix, HarmonyPatch(nameof(UIPrefab_Inventory.UpdateSlot))]
        private static void UpdatePrefix(UIPrefab_Inventory __instance, List<InventoryItem> inventoryItems) =>
            EnsureSlots(__instance, inventoryItems.Count);

        private static void EnsureSlots(UIPrefab_Inventory ui, int count)
        {
            if (!InventoryExpansionPreferences.Enabled) return;
            var slots = (List<UIPrefab_Inventory.Slot>)SlotsField.GetValue(ui);
            int first = InvenSlotLayout.FirstExtensionSlotKey - 1;
            int wanted = Math.Max(count, first + InventoryExpansionPreferences.AdditionalSlots);
            bool changed = _currentUi != ui || LogicalSlotCount != wanted || slots.Count < wanted;
            _currentUi = ui;
            LogicalSlotCount = wanted;
            var template = slots[first] ?? throw new InvalidOperationException("[InventoryExpansion] Native extension slot template is missing.");
            while (slots.Count < wanted)
            {
                var root = UnityEngine.Object.Instantiate(template.root, template.root.transform.parent, false);
                root.name = "InventoryExpansion_Slot_" + slots.Count;
                root.SetActive(false);
                slots.Add(new UIPrefab_Inventory.Slot {
                    root = root, group = template.group,
                    itemGroup = CloneNode(template.itemGroup.transform, template.root.transform, root.transform).gameObject,
                    frame = CloneNode(template.frame, template.root.transform, root.transform),
                    waitEvent = CloneNode(template.waitEvent, template.root.transform, root.transform),
                    image = CloneNode(template.image.transform, template.root.transform, root.transform).GetComponent<UnityEngine.UI.Image>(),
                    stackCount = CloneNode(template.stackCount.transform, template.root.transform, root.transform).GetComponent<TMPro.TMP_Text>()
                });
            }
            if (changed) BackpackPanelPatch.Relayout(ui);
        }

        private static Transform CloneNode(Transform node, Transform source, Transform target)
        {
            if (node == source) return target;
            var indices = new Stack<int>();
            for (var current = node; current != source; current = current.parent)
            {
                if (current == null) throw new InvalidOperationException("[InventoryExpansion] Native slot reference leaves its root.");
                indices.Push(current.GetSiblingIndex());
            }
            while (indices.Count > 0) target = target.GetChild(indices.Pop());
            return target;
        }
    }
}
