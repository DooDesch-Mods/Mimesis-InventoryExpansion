using System;
using System.Reflection;
using HarmonyLib;
using InventoryExpansion.Config;
using MelonLoader;
using Mimic.Actors;

namespace InventoryExpansion.Patches
{
	[HarmonyPatch(typeof(Hub))]
	internal static class GameConfigPatches
	{
		private static object _baselinePlayerActor;
		private static int _baseSlots;

		[HarmonyPostfix]
		[HarmonyPatch("Awake")]
		private static void Hub_Awake_Postfix(Hub __instance) => ApplyEffectiveSettings(__instance);

		internal static void ApplyEffectiveSettings(Hub hub = null)
		{
			try
			{
				hub = hub ?? Hub.s;
				if (hub == null) return;
				var configField = typeof(Hub).GetField("gameConfig", BindingFlags.Instance | BindingFlags.NonPublic);
				var config = configField?.GetValue(hub);
				if (config == null)
				{
					MelonLogger.Warning("InventoryExpansion: gameConfig is null, cannot apply inventory capacity.");
					return;
				}
				var playerActorField = config.GetType().GetField("playerActor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				var playerActor = playerActorField?.GetValue(config);
				var slotField = playerActor?.GetType().GetField("maxGenericInventorySlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (slotField == null)
				{
					MelonLogger.Error("InventoryExpansion: PlayerActor.maxGenericInventorySlot not found - cannot apply inventory capacity.");
					return;
				}
				if (!ReferenceEquals(_baselinePlayerActor, playerActor))
				{
					_baseSlots = (int)slotField.GetValue(playerActor);
					_baselinePlayerActor = playerActor;
				}
				int target = _baseSlots + (InventoryExpansionPreferences.Enabled ? InventoryExpansionPreferences.AdditionalSlots : 0);
				int current = (int)slotField.GetValue(playerActor);
				if (current == target) return;
				slotField.SetValue(playerActor, target);
				MelonLogger.Msg($"InventoryExpansion: set inventory capacity from {current} to {target}.");
			}
			catch (Exception ex)
			{
				MelonLogger.Error($"InventoryExpansion: failed to apply inventory capacity: {ex}");
			}
		}
	}

	[HarmonyPatch(typeof(ProtoActor), "Awake")]
	internal static class InventoryCapacityBeforeActorPatch
	{
		[HarmonyPrefix]
		private static void Prefix() => GameConfigPatches.ApplyEffectiveSettings();
	}

	[HarmonyPatch(typeof(UIPrefab_Inventory), "Awake")]
	internal static class InventoryCapacityBeforeUiPatch
	{
		[HarmonyPrefix]
		private static void Prefix() => GameConfigPatches.ApplyEffectiveSettings();
	}
}
