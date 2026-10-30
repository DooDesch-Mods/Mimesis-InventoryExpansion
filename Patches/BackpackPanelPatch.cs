using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using InventoryExpansion.Config;
using MelonLoader;
using Mimic;
using Mimic.Actors;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace InventoryExpansion.Patches
{
    [HarmonyPatch]
    internal static class BackpackPanelPatch
    {
        private static readonly FieldInfo MainInventoryField = AccessTools.Field(typeof(GameMainBase), "inventoryui");
        private static UIPrefab_Inventory _inventoryUi;
        private static RectTransform _nativeHud;
        private static Vector2 _hudRest;
        private static RectTransform _backpackPanel;
        private static RectTransform _firstNativeSlot, _lastNativeSlot;
        private static GameObject _bodyVisuals;
        private static UIPrefab_KeyIcon _keyBadge;
        private static readonly List<GameObject> Containers = new();
        private static bool _slotsMoved, _backpackFullyVisible, _targetOpen, _layoutPending;
        private static float _offset, _openOffset, _nextBindAttempt, _nextBadgeAttempt;
        private static bool _badgeMissing;
        private static int _savedStandardSlot;
        private static object _animationCoroutine;
        private static string _lastLayoutError;

        internal static bool IsBackpackFullyVisible => _backpackFullyVisible;
        internal static bool IsBackpackOpen => _targetOpen;

        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_Inventory), "Awake")]
        private static void AwakePostfix(UIPrefab_Inventory __instance)
        {
            BackpackSceneChangeHandler.Initialize();
            Relayout(__instance);
        }

        private static UIPrefab_Inventory LocalUi() => Hub.Main == null ? null : MainInventoryField.GetValue(Hub.Main) as UIPrefab_Inventory;

        internal static void Relayout(UIPrefab_Inventory ui)
        {
            if (_layoutPending || !InventoryExpansionPreferences.Enabled) return;
            _layoutPending = true;
            MelonCoroutines.Start(LayoutAfterFrame(ui));
        }

        private static IEnumerator LayoutAfterFrame(UIPrefab_Inventory ui)
        {
            yield return null;
            _layoutPending = false;
            if (ui == null || LocalUi() != ui || !InventoryExpansionPreferences.Enabled) yield break;
            try
            {
                if (_inventoryUi != ui || _backpackPanel == null) Bind(ui);
                ArrangeSlots();
                if (_lastLayoutError != null) MelonLogger.Msg("[InventoryExpansion] Native backpack layout recovered.");
                _lastLayoutError = null;
            }
            catch (Exception error)
            {
                string reason = error.GetType().Name + ": " + error.Message;
                if (_lastLayoutError != reason) MelonLogger.Error("[InventoryExpansion] Native backpack layout failed: " + error);
                _lastLayoutError = reason;
            }
        }

        private static void Bind(UIPrefab_Inventory ui)
        {
            HideBackpackCompletely();
            if (_backpackPanel != null) UnityEngine.Object.Destroy(_backpackPanel.gameObject);
            Containers.Clear();
            _firstNativeSlot = _lastNativeSlot = null;
            _keyBadge = null;
            _inventoryUi = ui;
            _nativeHud = ui.UE_rootNode as RectTransform;
            if (_nativeHud == null) throw new InvalidOperationException("Native inventory root is missing.");
            _hudRest = _nativeHud.anchoredPosition;
            var go = new GameObject("InventoryExpansion_BackpackPanel", typeof(RectTransform));
            _backpackPanel = (RectTransform)go.transform;
            _backpackPanel.SetParent(_nativeHud.parent, false);
            var parent = (RectTransform)_nativeHud.parent;
            // Anchor at the parent's pivot so panel positions use the same local coordinates as the HUD bounds.
            _backpackPanel.anchorMin = _backpackPanel.anchorMax = parent.pivot;
            _backpackPanel.pivot = new Vector2(0.5f, 0);
            _bodyVisuals = new GameObject("InventoryExpansion_Group", typeof(RectTransform));
            _bodyVisuals.transform.SetParent(_backpackPanel, false);
            var body = (RectTransform)_bodyVisuals.transform;
            body.anchorMin = Vector2.zero; body.anchorMax = Vector2.one;
            body.offsetMin = body.offsetMax = Vector2.zero;
            _slotsMoved = false;
        }

        private static void ArrangeSlots()
        {
            var slots = (List<UIPrefab_Inventory.Slot>)InventoryUiPatches.SlotsField.GetValue(_inventoryUi);
            int first = InvenSlotLayout.FirstExtensionSlotKey - 1;
            int count = Math.Min(slots.Count, InventoryUiPatches.LogicalSlotCount) - first;
            if (count <= 0) return;
            int[] rows = BackpackLayout.Rows(count);
            var source = (RectTransform)slots[0].root.transform;
            _firstNativeSlot = source;
            _lastNativeSlot = (RectTransform)slots[3].root.transform;
            // The native layout has already sized the slot root; icon padding does not describe its frame.
            float cellWidth = source.rect.width;
            float cellHeight = source.rect.height;
            if (cellWidth <= 0 || cellHeight <= 0) throw new InvalidOperationException("Native inventory slot dimensions are not ready.");
            float gap = (source.parent.GetComponent<HorizontalLayoutGroup>()?.spacing ?? 15f);
            float rowGap = 8f;
            float width = rows.Max() * cellWidth + (rows.Max() - 1) * gap;
            float gridHeight = rows.Length * cellHeight + (rows.Length - 1) * rowGap;
            const float badgeHeight = 36f, padding = 12f;
            _backpackPanel.sizeDelta = new Vector2(width + padding * 2, gridHeight + badgeHeight + padding * 2);
            var parent = (RectTransform)_backpackPanel.parent;
            var corners = new Vector3[4];
            source.GetWorldCorners(corners);
            float bottom = corners.Min(point => parent.InverseTransformPoint(point).y) - _offset;
            float left = parent.InverseTransformPoint(source.position).x;
            float right = parent.InverseTransformPoint(slots[3].root.transform.position).x;
            float panelBottom = Mathf.Max(parent.rect.yMin + padding, bottom - badgeHeight - padding);
            _backpackPanel.anchoredPosition = new Vector2((left + right) * 0.5f, panelBottom);
            _openOffset = panelBottom + _backpackPanel.rect.height + padding - bottom;
            int index = first;
            for (int row = 0; row < rows.Length; row++)
            {
                float rowWidth = rows[row] * cellWidth + (rows[row] - 1) * gap;
                for (int column = 0; column < rows[row]; column++, index++)
                {
                    var slot = slots[index];
                    RectTransform container;
                    if (index - first < Containers.Count) container = (RectTransform)Containers[index - first].transform;
                    else
                    {
                        var item = new GameObject("InventoryExpansion_SlotContainer_" + index, typeof(RectTransform));
                        item.transform.SetParent(_backpackPanel, false);
                        Containers.Add(item);
                        container = (RectTransform)item.transform;
                    }
                    container.anchorMin = container.anchorMax = new Vector2(0.5f, 1);
                    container.pivot = new Vector2(0.5f, 0.5f);
                    container.sizeDelta = new Vector2(cellWidth, cellHeight);
                    container.anchoredPosition = new Vector2(-rowWidth / 2 + cellWidth / 2 + column * (cellWidth + gap),
                        -padding - cellHeight / 2 - row * (cellHeight + rowGap));
                    var root = (RectTransform)slot.root.transform;
                    root.SetParent(container, false);
                    root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
                    root.sizeDelta = new Vector2(cellWidth, cellHeight);
                    root.anchoredPosition = Vector2.zero;
                    root.localScale = Vector3.one;
                    slot.group = null;
                }
            }
            for (int child = _bodyVisuals.transform.childCount - 1; child >= 0; child--)
                UnityEngine.Object.Destroy(_bodyVisuals.transform.GetChild(child).gameObject);
            var tint = _inventoryUi.UE_BG_normal1.color;
            RectImage("Backing", new Vector2(width + 20, gridHeight + 12),
                new Vector2(0, badgeHeight + padding + gridHeight / 2), new Color(tint.r * 0.2f, tint.g * 0.15f, tint.b * 0.1f, 0.07f));
            foreach (int side in new[] { -1, 1 })
                RectImage("End", new Vector2(2, 26), new Vector2(side * (width / 2 + 8), badgeHeight + padding + gridHeight / 2), tint);
            _bodyVisuals.transform.SetAsFirstSibling();
            EnsureBadge();
            _slotsMoved = true;
            ApplyOffset(_targetOpen ? _openOffset : 0);
            ShowContents(_targetOpen);
        }

        internal static void FollowNativeHud()
        {
            if (_backpackPanel == null || !_backpackPanel.gameObject.activeInHierarchy ||
                _firstNativeSlot == null || _lastNativeSlot == null) return;
            // Equipment changes can move the native hotbar without changing backpack capacity.
            var parent = _backpackPanel.parent;
            float first = parent.InverseTransformPoint(_firstNativeSlot.position).x;
            float last = parent.InverseTransformPoint(_lastNativeSlot.position).x;
            _backpackPanel.anchoredPosition = new Vector2((first + last) / 2, _backpackPanel.anchoredPosition.y);
        }

        private static Image RectImage(string name, Vector2 size, Vector2 position, Color color)
        {
            var go = new GameObject("InventoryExpansion_" + name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_bodyVisuals.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
            rect.sizeDelta = size; rect.anchoredPosition = position;
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }

        private static void EnsureBadge()
        {
            if (_keyBadge == null)
            {
                if (Time.unscaledTime < _nextBadgeAttempt) return;
                _nextBadgeAttempt = Time.unscaledTime + 1f;
                // Gameplay tips can be inactive in a dungeon, but their native key-cap art is still available.
                var template = UnityEngine.Object.FindObjectsByType<UIPrefab_KeyIcon>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(icon => !icon.name.StartsWith("InventoryExpansion_"));
                if (template == null)
                {
                    if (!_badgeMissing) MelonLogger.Warning("[InventoryExpansion] Waiting for the native key-cap prefab.");
                    _badgeMissing = true;
                    return;
                }
                if (_badgeMissing) MelonLogger.Msg("[InventoryExpansion] Native key-cap prefab is available.");
                _badgeMissing = false;
                _keyBadge = UnityEngine.Object.Instantiate(template, _backpackPanel, false);
                _keyBadge.name = "InventoryExpansion_KeyHint";
                var rect = (RectTransform)_keyBadge.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
                rect.pivot = new Vector2(0.5f, 0);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(38, 36);
                rect.localScale = Vector3.one;
            }
            var key = InventoryExpansionPreferences.BackpackKey.ToString();
            _keyBadge.SetKey("k_" + key.ToLowerInvariant());
            _keyBadge.UE_Key.gameObject.SetActive(true);
            _keyBadge.UE_UI_KeyText.text = key;
            _keyBadge.gameObject.SetActive(true);
        }

        private static void ApplyOffset(float offset)
        {
            _offset = offset;
            if (_nativeHud != null) _nativeHud.anchoredPosition = _hudRest + new Vector2(0, offset);
        }

        private static void ShowContents(bool visible)
        {
            if (_bodyVisuals != null) _bodyVisuals.SetActive(visible);
            int count = Math.Max(0, InventoryUiPatches.LogicalSlotCount - InvenSlotLayout.FirstExtensionSlotKey + 1);
            for (int i = 0; i < Containers.Count; i++) Containers[i].SetActive(visible && i < count);
        }

        internal static void EnsurePeekIfHidden()
        {
            if (!InventoryExpansionPreferences.Enabled) return;
            var current = LocalUi();
            if (current == null) return;
            if (_inventoryUi != current || !_slotsMoved || _backpackPanel == null)
            {
                if (Time.unscaledTime >= _nextBindAttempt) { _nextBindAttempt = Time.unscaledTime + 1f; Relayout(current); }
                return;
            }
            _backpackPanel.gameObject.SetActive(true);
            EnsureBadge();
        }

        internal static void ToggleBackpack()
        {
            if (_backpackPanel == null || IsLoadingScreenActive() || IsGamePaused()) return;
            _targetOpen = !_targetOpen;
            if (_animationCoroutine != null) MelonCoroutines.Stop(_animationCoroutine);
            _backpackPanel.gameObject.SetActive(true);
            ShowContents(_targetOpen);
            if (InventoryExpansionPreferences.AnimationSeconds == 0)
            {
                ApplyOffset(_targetOpen ? _openOffset : 0);
                _backpackFullyVisible = _targetOpen;
                _animationCoroutine = null;
                return;
            }
            _animationCoroutine = MelonCoroutines.Start(Animate(_targetOpen));
        }

        private static IEnumerator Animate(bool open)
        {
            float start = _offset, target = open ? _openOffset : 0, elapsed = 0;
            _backpackFullyVisible = false;
            float duration = InventoryExpansionPreferences.AnimationSeconds;
            while (elapsed < duration && _nativeHud != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                ApplyOffset(Mathf.Lerp(start, target, 1 - Mathf.Pow(1 - progress, 3)));
                yield return null;
            }
            ApplyOffset(target);
            _backpackFullyVisible = open;
            _animationCoroutine = null;
        }

        internal static void HideBackpackCompletely()
        {
            if (_targetOpen || _backpackFullyVisible) HandleCursorHandoff(Hub.Main?.GetMyAvatar(), false);
            if (_animationCoroutine != null) MelonCoroutines.Stop(_animationCoroutine);
            _animationCoroutine = null;
            _targetOpen = _backpackFullyVisible = false;
            ApplyOffset(0);
            if (_backpackPanel != null) _backpackPanel.gameObject.SetActive(false);
        }

        internal static void ResetForSettingsChange() { HideBackpackCompletely(); _savedStandardSlot = 0; }

        internal static void HandleCursorHandoff(ProtoActor avatar, bool opening)
        {
            if (avatar == null || (opening ? !InventoryExpansionPreferences.SelectBackpackSlotOnOpen
                : !InventoryExpansionPreferences.RestoreStandardSlotOnClose)) return;
            var inventory = InventorySelectionHelper.GetActorInventoryField()?.GetValue(avatar);
            if (inventory == null) return;
            int current = avatar.GetSelectedInventorySlotIndex();
            int first = InvenSlotLayout.FirstExtensionSlotKey - 1;
            if (opening && current < first) _savedStandardSlot = current;
            int target = opening ? first : (avatar.GetVisibleInventorySlotIndices().Contains(_savedStandardSlot) ? _savedStandardSlot : 0);
            if (opening && InventoryUiPatches.LogicalSlotCount <= first) return;
            InventorySelectionHelper.GetSelectSlotMethod(inventory.GetType()).Invoke(inventory, new object[] { target });
        }

		internal static bool IsGamePaused()
		{
			try
			{
				if (Time.timeScale <= 0.01f)
				{
					return true;
				}
				
				if (Cursor.lockState == CursorLockMode.None && Cursor.visible)
				{
					if (IsInGame())
					{
						return true;
					}
				}
				
				return false;
			}
			catch
			{
				return false;
			}
		}

		internal static bool IsInGame()
		{
			try
			{
				ProtoActor protoActor = Hub.Main?.GetMyAvatar();
				if (protoActor == null)
				{
					return false;
				}

				return protoActor.AmIAvatar();
			}
			catch (Exception ex)
			{
				MelonLogger.Warning($"[InventoryExpansion][BackpackPanel] IsInGame check failed: {ex}");
				return false;
			}
		}

		private static bool IsInLoadingScreen()
		{
			try
			{
				var hub = Hub.s;
				if (hub == null)
				{
					return true;
				}

				var protoActor = Hub.Main?.GetMyAvatar();
				if (protoActor == null)
				{
					return true;
				}

				return !protoActor.AmIAvatar();
			}
			catch
			{
				return false;
			}
		}

		private static FieldInfo _uimanField;

		// True while the game's full-screen scene loading UI is shown. Used to keep the
		// backpack hidden (and non-interactive) during map changes / loading screens.
		internal static bool IsLoadingScreenActive()
		{
			try
			{
				var hub = Hub.s;
				if (hub == null)
				{
					return false;
				}
				// Hub.uiman is internal; reach it via its auto-property backing field.
				if (_uimanField == null)
				{
					_uimanField = typeof(Hub).GetField("<uiman>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
				}
				var uiman = _uimanField?.GetValue(hub) as UIManager;
				var loading = uiman?.ui_sceneloading;
				return loading != null && loading.isActiveAndEnabled;
			}
			catch
			{
				return false;
			}
		}

		// Restore the panel to its resting "peek" state if it is currently hidden (e.g.
		// after a map change or loading screen deactivated it). No-op while an open/close
		// animation is running or when the panel is already visible.
    }

	[HarmonyPatch(typeof(ProtoActor), "Update")]
	internal static class BackpackInputUpdatePatch
	{
		private static bool wasKeyPressedLastFrame = false;

		[HarmonyPostfix]
		private static void Update_Postfix(ProtoActor __instance)
		{
			try
			{
				if (!InventoryExpansionPreferences.Enabled)
				{
					return;
				}

				if (!__instance.AmIAvatar())
				{
					return;
				}

				// Hide the backpack and restore the native inventory position while a map loads.
				if (BackpackPanelPatch.IsLoadingScreenActive())
				{
					BackpackPanelPatch.HideBackpackCompletely();
					wasKeyPressedLastFrame = false;
					return;
				}

				if (BackpackPanelPatch.IsGamePaused())
				{
					BackpackPanelPatch.HideBackpackCompletely();
					wasKeyPressedLastFrame = false;
					return;
				}

				// In interactive gameplay (not loading, not paused): make sure the panel is
				// at least at its resting peek state, e.g. after a map change had hidden it.
				BackpackPanelPatch.EnsurePeekIfHidden();

				Keyboard keyboard = Keyboard.current;
				if (keyboard == null)
				{
					wasKeyPressedLastFrame = false;
					return;
				}

				KeyCode targetKey = InventoryExpansionPreferences.BackpackKey;
				Key key = Key.None;

				try
				{
					key = (Key)Enum.Parse(typeof(Key), targetKey.ToString());
				}
				catch
				{
					MelonLogger.Warning($"[InventoryExpansion][BackpackPanel] Could not convert KeyCode {targetKey} to Input System Key");
					return;
				}

				bool isKeyPressed = keyboard[key].isPressed;
				bool wasKeyPressedThisFrame = isKeyPressed && !wasKeyPressedLastFrame;
				wasKeyPressedLastFrame = isKeyPressed;

				if (wasKeyPressedThisFrame)
				{
					// Capture the toggle direction before flipping (fully visible -> closing).
					bool opening = !BackpackPanelPatch.IsBackpackOpen;
					if (opening && InventoryExpansionPreferences.BlockLargeItemsInBackpack && LargeItemBackpackPatch.IsLargeHeldItem(__instance))
					{
						// A large item occupies the hand, so the backpack cannot be opened while holding one.
						LargeItemBackpackPatch.ShowBackpackBlockedMessage("You can't open the backpack while holding a large item.");
					}
					else
					{
						BackpackPanelPatch.ToggleBackpack();
						BackpackPanelPatch.HandleCursorHandoff(__instance, opening);
					}
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Error($"[InventoryExpansion][BackpackPanel] Update postfix failed: {ex}");
			}
		}
	}

	[HarmonyPatch(typeof(ProtoActor))]
	internal static class BackpackMovementSpeedPatch
	{
		private static FieldInfo _netSyncActorDataField;
		private static bool _fieldInitialized = false;
		private static long _originalMoveSpeedWalk = 0L;
		private static long _originalMoveSpeedRun = 0L;
		private static bool _speedReduced = false;
		private static ProtoActor _speedReducedActor;

		internal static void ResetLocalState()
		{
			try
			{
				RestoreMoveSpeed(_speedReducedActor);
			}
			catch (Exception ex)
			{
				MelonLogger.Error($"[InventoryExpansion][Movement] Failed to restore local movement speed on settings change: {ex}");
			}
		}

		internal static void RestoreForActor(ProtoActor actor)
		{
			if (ReferenceEquals(actor, _speedReducedActor)) ResetLocalState();
		}

		[HarmonyPostfix]
		[HarmonyPatch("Update")]
		private static void Update_Postfix(ProtoActor __instance)
		{
			try
			{
				if (!__instance.AmIAvatar())
				{
					return;
				}

				if (!InventoryExpansionPreferences.Enabled)
				{
					RestoreMoveSpeed(__instance);
					return;
				}

				if (_speedReduced && !ReferenceEquals(_speedReducedActor, __instance))
				{
					ResetLocalState();
					if (_speedReduced) return;
				}

				if (!_fieldInitialized)
				{
					var protoActorType = typeof(ProtoActor);
					_netSyncActorDataField = protoActorType.GetField("netSyncActorData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					_fieldInitialized = true;
				}

				if (_netSyncActorDataField == null)
				{
					return;
				}

				var netSyncActorData = _netSyncActorDataField.GetValue(__instance);
				if (netSyncActorData == null)
				{
					return;
				}

				var moveSpeedWalkField = netSyncActorData.GetType().GetField("MoveSpeedWalk", BindingFlags.Instance | BindingFlags.Public);
				var moveSpeedRunField = netSyncActorData.GetType().GetField("MoveSpeedRun", BindingFlags.Instance | BindingFlags.Public);

				if (moveSpeedWalkField == null || moveSpeedRunField == null)
				{
					return;
				}

				if (BackpackPanelPatch.IsBackpackFullyVisible && InventoryExpansionPreferences.ReduceMovementSpeed)
				{
					if (!_speedReduced)
					{
						_originalMoveSpeedWalk = (long)(moveSpeedWalkField.GetValue(netSyncActorData) ?? 350L);
						_originalMoveSpeedRun = (long)(moveSpeedRunField.GetValue(netSyncActorData) ?? 700L);
						_speedReduced = true;
						_speedReducedActor = __instance;
					}

					moveSpeedWalkField.SetValue(netSyncActorData, (long)(_originalMoveSpeedWalk * InventoryExpansionPreferences.MovementSpeedMultiplier));
					moveSpeedRunField.SetValue(netSyncActorData, (long)(_originalMoveSpeedRun * InventoryExpansionPreferences.MovementSpeedMultiplier));
				}
				else
				{
					RestoreMoveSpeed(netSyncActorData, moveSpeedWalkField, moveSpeedRunField);
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Error($"[InventoryExpansion][Movement] Movement speed patch failed: {ex}");
			}
		}

		private static void RestoreMoveSpeed(ProtoActor instance)
		{
			if (!_speedReduced || _netSyncActorDataField == null || !ReferenceEquals(instance, _speedReducedActor))
			{
				return;
			}

			var netSyncActorData = _netSyncActorDataField.GetValue(instance);
			if (netSyncActorData == null)
			{
				return;
			}

			var moveSpeedWalkField = netSyncActorData.GetType().GetField("MoveSpeedWalk", BindingFlags.Instance | BindingFlags.Public);
			var moveSpeedRunField = netSyncActorData.GetType().GetField("MoveSpeedRun", BindingFlags.Instance | BindingFlags.Public);

			if (moveSpeedWalkField != null && moveSpeedRunField != null)
			{
				RestoreMoveSpeed(netSyncActorData, moveSpeedWalkField, moveSpeedRunField);
			}
		}

		private static void RestoreMoveSpeed(object netSyncActorData, FieldInfo moveSpeedWalkField, FieldInfo moveSpeedRunField)
		{
			if (_speedReduced)
			{
				moveSpeedWalkField.SetValue(netSyncActorData, _originalMoveSpeedWalk);
				moveSpeedRunField.SetValue(netSyncActorData, _originalMoveSpeedRun);
				_speedReduced = false;
				_speedReducedActor = null;
				_originalMoveSpeedWalk = 0L;
				_originalMoveSpeedRun = 0L;
			}
		}
	}

	[HarmonyPatch(typeof(ProtoActor), "OnDestroy")]
	internal static class BackpackProtoActorDestroyPatch
	{
		[HarmonyPostfix]
		private static void OnDestroy_Postfix(ProtoActor __instance)
		{
			try
			{
				BackpackMovementSpeedPatch.RestoreForActor(__instance);

				if (__instance.AmIAvatar())
				{
					BackpackSceneChangeHandler.HideBackpack();
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Error($"[InventoryExpansion][BackpackPanel] ProtoActor OnDestroy patch failed: {ex}");
			}
		}
	}

	internal static class BackpackSceneChangeHandler
	{
		private static bool _initialized = false;

		internal static void Initialize()
		{
			if (_initialized) return;
			
			SceneManager.activeSceneChanged += OnActiveSceneChanged;
            Canvas.willRenderCanvases += BackpackPanelPatch.FollowNativeHud;
			_initialized = true;
		}

		private static void OnActiveSceneChanged(Scene previousScene, Scene newScene)
		{
			try
			{
				if (!IsInGame())
				{
					HideBackpack();
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Error($"[InventoryExpansion][BackpackPanel] Scene change handler failed: {ex}");
			}
		}

		private static bool IsInGame()
		{
			try
			{
				ProtoActor protoActor = Hub.Main?.GetMyAvatar();
				if (protoActor == null)
				{
					return false;
				}

				return protoActor.AmIAvatar();
			}
			catch
			{
				return false;
			}
		}

		internal static void HideBackpack()
		{
			BackpackPanelPatch.HideBackpackCompletely();
			BackpackMovementSpeedPatch.ResetLocalState();
		}
	}
}
