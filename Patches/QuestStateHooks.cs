using System.Collections.Generic;
using System.Reflection;

using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using QuestPlaymakerActions;
using Silksong.UnityHelper.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuestMod
{
    public static class QuestStateHooks
    {
        private static readonly HashSet<string> PatchedFSMs = new();

        // Per-NPC substring whitelist retired. Per-action ShouldRedirect handles
        // it now via the action's Quest field.

        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            QuestModPlugin.Log.LogInfo("QuestStateHooks: Registered sceneLoaded hook");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(scene.name) || scene.name == "Menu_Title")
                return;

            QuestModPlugin.SyncFromSaveData();
            QuestModPlugin.LogDebugInfo($"Scene loaded: {scene.name}");

            PatchedFSMs.Clear();
            PatchedActions.Clear();
            SetNpcSpawnFlags();
            ApplyGranularBypasses();
            ActivateQuestBoards();
            RefreshQuestBoards();
            // Sync so Plasmium is interactable on the first frame, not at t+0.5s.
            ApplyAllWishesTargetBypass();

            string queuedScene = scene.name;
            QuestModPlugin.Instance.InvokeAfterSeconds(() =>
            {
                if (SceneManager.GetActiveScene().name != queuedScene)
                {
                    QuestModPlugin.LogDebugInfo($"Delayed re-patch skipped: scene changed from {queuedScene}");
                    return;
                }
                QuestModPlugin.LogDebugInfo("Delayed re-patch running...");
                PatchedFSMs.Clear();
                PatchedActions.Clear();
                PatchAllQuestFSMs();
                SetNpcSpawnFlags();
                ApplyGranularBypasses();
                ActivateQuestBoards();
                RefreshQuestBoards();
                DumpNpcDiagnostics();

                if (QuestModPlugin.AllQuestsAccepted)
                {
                    QuestAcceptance.InjectAndAcceptAllQuests();
                }
                else
                {
                    QuestAcceptance.AutoAcceptFlaggedQuests();
                }

                if (QuestModPlugin.EnableCompletionOverrides.Value)
                    QuestCompletionOverrides.ApplySavedOverrides();

                ApplyAllWishesTargetBypass();
            }, 0.5f);
        }

        // Plasmium quests (Extractor Blue, Extractor Blue Worms) have a target
        // gated on the Needle Phial tool that a fresh save doesn't have. Zero
        // those targets transiently when the wish is Available.
        private static void ApplyAllWishesTargetBypass()
        {
            if (QuestPolicyStore.IsAvailable("Extractor Blue"))
                QuestCompletionOverrides.SetTargetCountTransient("Extractor Blue", 1, 0);
            if (QuestPolicyStore.IsAvailable("Extractor Blue Worms"))
                QuestCompletionOverrides.SetTargetCountTransient("Extractor Blue Worms", 1, 0);
        }

        // Read the quest name from a CheckQuestState action's Quest FsmObject.
        // Null on any failure. Used as the key for chain/exclusion/sequential.
        private static string ReadActionQuestName(object action)
        {
            try
            {
                var f = action.GetType().GetField("Quest", BindingFlags.Public | BindingFlags.Instance);
                if (f == null) return null;
                var fsmObj = f.GetValue(action);
                if (fsmObj == null) return null;
                var valProp = fsmObj.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                if (valProp == null) return null;
                var unityObj = valProp.GetValue(fsmObj);
                if (unityObj == null) return null;
                var nameProp = unityObj.GetType().GetProperty("name", BindingFlags.Public | BindingFlags.Instance);
                if (nameProp == null) return null;
                return nameProp.GetValue(unityObj)?.ToString();
            }
            catch (System.Exception) { return null; }
        }

        // Walk states from `start` looking for BeginQuest/BeginQuestV2/BoardQuest.
        // Distinguishes quest-giver flow from flavor/env reactions. Adjusted only
        // redirects the former. Depth 6 covers normal accept hops.
        private static bool ReachesBeginQuest(PlayMakerFSM fsm, FsmState start, int maxDepth = 6)
        {
            if (fsm == null || start == null) return false;
            var visited = new HashSet<string>();
            return ReachesBeginQuestInner(fsm, start, maxDepth, visited);
        }

        private static bool ReachesBeginQuestInner(PlayMakerFSM fsm, FsmState state, int depth, HashSet<string> visited)
        {
            if (state == null || depth < 0) return false;
            if (visited.Contains(state.Name)) return false;
            visited.Add(state.Name);

            if (state.Actions != null)
            {
                foreach (var a in state.Actions)
                {
                    if (a == null) continue;
                    var n = a.GetType().Name;
                    if (n == "BeginQuest" || n == "BeginQuestV2" || n == "BoardQuest")
                        return true;
                }
            }

            if (state.Transitions == null) return false;
            foreach (var t in state.Transitions)
            {
                if (t == null) continue;
                var nextName = t.ToState;
                if (string.IsNullOrEmpty(nextName)) continue;
                FsmState next = null;
                foreach (var s in fsm.FsmStates) { if (s != null && s.Name == nextName) { next = s; break; } }
                if (next == null) continue;
                if (ReachesBeginQuestInner(fsm, next, depth - 1, visited)) return true;
            }
            return false;
        }

        // Per-action NotTracked redirect decision.
        // Disabled: no. Pure: yes. Adjusted: only if chain prereqs met,
        // no exclusion conflict, and BeginQuest is reachable from this state.
        public static bool ShouldRedirectActionForCurrentMode(PlayMakerFSM fsm, FsmState state, object action)
            => ShouldRedirect(fsm, state, action);

        private static bool ShouldRedirect(PlayMakerFSM fsm, FsmState state, object action)
        {
            if (QuestModPlugin.WishesMode == AllWishesMode.Disabled) return false;
            if (QuestModPlugin.IsPureWishes) return true;

            // Adjusted path: gate on chain + exclusion + BeginQuest reachability.
            var questName = ReadActionQuestName(action);
            if (string.IsNullOrEmpty(questName)) return false;
            if (!QuestAcceptance.IsChainPrereqMet(questName)) return false;
            if (QuestAcceptance.GetExclusionConflict(questName) != null) return false;
            if (!ReachesBeginQuest(fsm, state)) return false;
            return true;
        }

        private static bool AnyQuestAvailable()
        {
            if (QuestModPlugin.AllQuestsAvailable) return true;
            var map = QuestPolicyStore.Map;
            if (map == null) return false;
            foreach (var kvp in map)
            {
                if (kvp.Value != null && kvp.Value.Available)
                    return true;
            }
            return false;
        }

        // Re-evaluate every action's redirect decision after a mid-frame
        // WishesMode toggle, without waiting for a scene change.
        public static void RePatchAllForCurrentMode()
        {
            PatchedFSMs.Clear();
            PatchedActions.Clear();
            PatchAllQuestFSMs();
        }

        // Per-action idempotency by reference. Cleared per scene.
        private static readonly HashSet<object> PatchedActions =
            new HashSet<object>(new ReferenceEqualityComparer());

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        private static void PatchAllQuestFSMs()
        {
            if (!AnyQuestAvailable())
                return;

            var fsms = Object.FindObjectsByType<PlayMakerFSM>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var fsm in fsms)
            {
                if (fsm == null) continue;

                // Per-action ShouldRedirect decides each CheckQuestState.
                // Skip additive-load conditionals to avoid NREs on partial loads.
                if (fsm.gameObject.GetComponent<SceneAdditiveLoadConditional>() != null)
                    continue;



                try
                {
                    if (fsm.FsmStates == null) continue;

                    var fsmKey = $"{fsm.gameObject.name}/{fsm.FsmName}";
                    if (PatchedFSMs.Contains(fsmKey))
                        continue;

                    PatchQuestStateFSM(fsm, fsmKey);
                }
                catch (System.Exception ex)
                {
                    QuestModPlugin.Log.LogDebug($"Error patching FSM {fsm.gameObject.name}: {ex.Message}");
                }
            }
        }

        private static void PatchQuestStateFSM(PlayMakerFSM fsm, string fsmKey)
        {
            bool patched = false;

            foreach (var state in fsm.FsmStates)
            {
                if (state.Actions == null) continue;

                foreach (var action in state.Actions)
                {
                    if (action is CheckQuestStateV2 checkV2)
                    {
                        if (PatchCheckAction(checkV2, fsm, state, fsmKey, "V2")) patched = true;
                    }
                    else if (action.GetType().Name == "CheckQuestState")
                    {
                        if (PatchCheckActionV1(action, fsm, state, fsmKey)) patched = true;
                    }
                    else if (action is PlayerDataBoolTest pdBoolTest)
                    {
                        if (PatchPlayerDataBoolTest(pdBoolTest, state.Name, fsmKey)) patched = true;
                    }
                    else if (action is BoolTest boolTest)
                    {
                        if (PatchBoolTest(boolTest, state.Name, fsmKey)) patched = true;
                    }
                }
            }

            if (patched)
                PatchedFSMs.Add(fsmKey);
        }

        // V2 redirect: NotTracked → IncompleteEvent, fallback CompletedEvent.
        // Redirecting to Completed would jump quest-givers straight to thank-you.
        private static bool PatchCheckAction(CheckQuestStateV2 checkAction, PlayMakerFSM fsm, FsmState state, string fsmKey, string version)
        {
            try
            {
                if (!ShouldRedirect(fsm, state, checkAction)) return false;

                var incompleteEvent = checkAction.IncompleteEvent;
                var completedEvent = checkAction.CompletedEvent;
                var redirectTarget = incompleteEvent ?? completedEvent;

                if (redirectTarget == null)
                {
                    QuestModPlugin.Log.LogDebug($"  {fsmKey}/{state.Name} ({version}): no Incomplete or Completed event, skipping");
                    return false;
                }

                checkAction.NotTrackedEvent = redirectTarget;
                var label = incompleteEvent != null ? "IncompleteEvent" : "CompletedEvent (fallback)";
                var quest = ReadActionQuestName(checkAction) ?? "(?)";
                QuestModPlugin.LogDebugInfo($"  {fsmKey}/{state.Name} ({version}, quest={quest}): Redirected NotTracked → {label}");
                return true;
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogDebug($"  {fsmKey}/{state.Name} ({version}): Patch failed - {ex.Message}");
                return false;
            }
        }

        // V1 redirect: NotTracked → TrackedEvent, fallback CompletedEvent.
        // V1 has no IncompleteEvent; Tracked covers accepted+active.
        private static bool PatchCheckActionV1(FsmStateAction action, PlayMakerFSM fsm, FsmState state, string fsmKey)
        {
            try
            {
                if (!ShouldRedirect(fsm, state, action)) return false;

                var type = action.GetType();
                var notTrackedField = type.GetField("NotTrackedEvent", BindingFlags.Public | BindingFlags.Instance);
                var trackedField = type.GetField("TrackedEvent", BindingFlags.Public | BindingFlags.Instance);
                var completedField = type.GetField("CompletedEvent", BindingFlags.Public | BindingFlags.Instance);

                if (notTrackedField == null) return false;

                var trackedEvent = trackedField?.GetValue(action);
                var completedEvent = completedField?.GetValue(action);
                var redirectTarget = trackedEvent ?? completedEvent;

                if (redirectTarget == null)
                {
                    QuestModPlugin.Log.LogDebug($"  {fsmKey}/{state.Name} (V1): no Tracked or Completed event, skipping");
                    return false;
                }

                notTrackedField.SetValue(action, redirectTarget);
                var label = trackedEvent != null ? "TrackedEvent" : "CompletedEvent (fallback)";
                var quest = ReadActionQuestName(action) ?? "(?)";
                QuestModPlugin.LogDebugInfo($"  {fsmKey}/{state.Name} (V1, quest={quest}): Redirected NotTracked → {label}");
                return true;
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogDebug($"  {fsmKey}/{state.Name} (V1): Patch failed - {ex.Message}");
                return false;
            }
        }

        private static bool PatchPlayerDataBoolTest(PlayerDataBoolTest boolTest, string stateName, string fsmKey)
        {
            try
            {
                var varName = boolTest.boolName != null ? boolTest.boolName.Value : null;
                if (string.IsNullOrEmpty(varName) || !NpcSpawnFlags.Contains(varName))
                    return false;

                var trueEvent = boolTest.isTrue;
                if (trueEvent == null || boolTest.isFalse == null)
                    return false;

                if (!PatchedActions.Add(boolTest))
                    return false;

                boolTest.isFalse = trueEvent;
                QuestModPlugin.LogDebugInfo($"  {fsmKey}/{stateName} (PDTest '{varName}'): Redirected isFalse → isTrue");
                return true;
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogDebug($"  {fsmKey}/{stateName} (PDTest): Patch failed - {ex.Message}");
                return false;
            }
        }

        private static bool PatchBoolTest(BoolTest boolTest, string stateName, string fsmKey)
        {
            try
            {
                var varName = boolTest.boolVariable != null ? boolTest.boolVariable.Name : null;
                if (string.IsNullOrEmpty(varName) || !NpcSpawnFlags.Contains(varName))
                    return false;

                var trueEvent = boolTest.isTrue;
                if (trueEvent == null || boolTest.isFalse == null)
                    return false;

                if (!PatchedActions.Add(boolTest))
                    return false;

                boolTest.isFalse = trueEvent;
                QuestModPlugin.LogDebugInfo($"  {fsmKey}/{stateName} (BoolTest '{varName}'): Redirected isFalse → isTrue");
                return true;
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogDebug($"  {fsmKey}/{stateName} (BoolTest): Patch failed - {ex.Message}");
                return false;
            }
        }

        // Keep ability flags out. A stray hasDoubleJump here = free Faydown.
        private static readonly HashSet<string> NpcSpawnFlags = new HashSet<string>
        {
            nameof(PlayerData.metMapper),
            nameof(PlayerData.MapperAppearInBellhart),
            nameof(PlayerData.hasMarker_a),
            nameof(PlayerData.shermaQuestActive),
            nameof(PlayerData.shermaInBellhart),
            nameof(PlayerData.ShakraFinalQuestAppear),
            // MushroomQuestFound1 stays out -- availableConditions handles it.
            nameof(PlayerData.MetCityMerchantScavenge),
            nameof(PlayerData.MetCityMerchantEnclave),
        };

        private static void SetNpcSpawnFlags()
        {
            if (!AnyQuestAvailable())
                return;

            // PD bool writes are one-way. Gate behind save-safety.
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                QuestModPlugin.LogDebugInfo("SetNpcSpawnFlags: skipped (legacy save, no override)");
                return;
            }

            if (PlayerData.instance == null)
                return;

            int set = 0;
            var pdType = PlayerData.instance.GetType();
            foreach (var flag in NpcSpawnFlags)
            {
                var field = pdType.GetField(flag, BindingFlags.Public | BindingFlags.Instance);
                if (field == null || field.FieldType != typeof(bool))
                    continue;

                if ((bool)field.GetValue(PlayerData.instance))
                    continue;

                field.SetValue(PlayerData.instance, true);
                set++;
                QuestModPlugin.LogDebugInfo($"SetNpcSpawnFlags: {flag} = true");
            }

            if (set > 0)
                QuestModPlugin.Log.LogInfo($"Set {set} NPC spawn flags for AllQuests mode");
        }

        private static void ActivateQuestBoards()
        {
            if (!AnyQuestAvailable())
                return;

            var allObjects = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.GetType().Name == "QuestBoardInteractable" && !obj.gameObject.activeSelf)
                {
                    obj.gameObject.SetActive(true);
                    QuestModPlugin.LogDebugInfo($"Activated quest board: {obj.gameObject.name}");
                }
            }
        }

        private static void RefreshQuestBoards()
        {
            var allObjects = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.GetType().Name == "QuestBoardInteractable")
                {
                    try
                    {
                        var refreshMethod = obj.GetType().GetMethod("RefreshQuestBoard");
                        if (refreshMethod != null)
                        {
                            refreshMethod.Invoke(obj, null);
                            QuestModPlugin.LogDebugInfo($"Refreshed quest board: {obj.gameObject.name}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        QuestModPlugin.Log.LogDebug($"Failed to refresh quest board: {ex.Message}");
                    }
                }
            }
        }

        // Granular prereq bypasses. Each toggle writes a small set of PD bools
        // so one chain can be opened without flipping AllQuestsAvailable.
        private static void ApplyGranularBypasses()
        {
            if (PlayerData.instance == null)
                return;

            // One-way PD writes. Same save-safety gate.
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                QuestModPlugin.LogDebugInfo("ApplyGranularBypasses: skipped (legacy save, no override)");
                return;
            }

            var prereqs = QuestModPlugin.Prereqs;
            if (prereqs == null)
                return;

            int set = 0;

            if (prereqs.BypassFleatopia)
            {
                set += SetPdBoolIfFalse(nameof(PlayerData.visitedFleatopia));
                set += SetPdBoolIfFalse(nameof(PlayerData.SeenFleatopiaEmpty));
                set += SetPdBoolIfFalse(nameof(PlayerData.SethJoinedFleatopia));
                set += SetPdBoolIfFalse(nameof(PlayerData.TroupeLeaderSpokenFleatopiaSearch));
                set += SetPdBoolIfFalse(nameof(PlayerData.MetCaravanTroupeLeader));
            }

            if (prereqs.BypassMandatoryWishes)
            {
                set += SetPdBoolIfFalse(nameof(PlayerData.promisedFirstWish));
            }

            if (prereqs.BypassFaydownCloak)
            {
                set += SetPdBoolIfFalse(nameof(PlayerData.hasDoubleJump));
            }

            if (prereqs.BypassNeedolin)
            {
                set += SetPdBoolIfFalse(nameof(PlayerData.hasNeedolin));
            }

            if (prereqs.BypassBonebottomQuestBoard)
            {
                set += SetPdBoolIfFalse(nameof(PlayerData.bonebottomQuestBoardFixed));
            }

            // BypassAllWishwalls is non-destructive on purpose. Setting
            // defeatedBellBeast / visitedBellhartSaved / metCaretaker would
            // trigger story/Act milestones. ActivateIfPlayerdataTrueStartPatch
            // handles visibility without touching PD.

            if (set > 0)
                QuestModPlugin.Log.LogInfo($"ApplyGranularBypasses: set {set} PlayerData flag(s)");
        }

        private static int SetPdBoolIfFalse(string fieldName)
        {
            try
            {
                var pdType = PlayerData.instance.GetType();
                var field = pdType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (field == null || field.FieldType != typeof(bool))
                {
                    QuestModPlugin.LogDebugInfo($"ApplyGranularBypasses: PlayerData.{fieldName} not found (skipping)");
                    return 0;
                }

                if ((bool)field.GetValue(PlayerData.instance))
                    return 0;

                field.SetValue(PlayerData.instance, true);
                QuestModPlugin.LogDebugInfo($"ApplyGranularBypasses: {fieldName} = true");
                return 1;
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogDebug($"ApplyGranularBypasses: failed to set {fieldName}: {ex.Message}");
                return 0;
            }
        }

        private static void DumpNpcDiagnostics()
        {
            // Gated by DebugLogging or it floods the log.
            if (!QuestModPlugin.DebugLogging.Value) return;
            if (!AnyQuestAvailable()) return;

            QuestModPlugin.Log.LogInfo("=== NPC Diagnostics ===");
            var fsms = Object.FindObjectsByType<PlayMakerFSM>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var fsm in fsms)
            {
                if (fsm == null) continue;
                // Walk every FSM; inner loop filters by action name.


                try
                {
                    if (fsm.FsmStates == null) continue;

                    var go = fsm.gameObject;
                    var actions = new List<string>();
                    foreach (var state in fsm.FsmStates)
                    {
                        if (state.Actions == null) continue;
                        foreach (var action in state.Actions)
                        {
                            var actionName = action.GetType().Name;
                            if (actionName.Contains("Quest") || actionName.Contains("Bool") ||
                                actionName.Contains("PlayerData") || actionName.Contains("Activate") ||
                                actionName.Contains("SetActive") || actionName.Contains("GetPlayerData"))
                            {
                                actions.Add($"{state.Name}/{actionName}");
                            }
                        }
                    }

                    if (actions.Count > 0)
                    {
                        QuestModPlugin.Log.LogInfo($"  [{(go.activeInHierarchy ? "ON" : "OFF")}] {go.name}/{fsm.FsmName}: {string.Join(", ", actions)}");
                    }
                }
                catch (System.Exception ex)
                {
                    QuestModPlugin.Log.LogDebug($"Diagnostics error on {fsm.gameObject.name}: {ex.Message}");
                }
            }
            QuestModPlugin.Log.LogInfo("=== End NPC Diagnostics ===");
        }
    }

    // Mark new saves as QuestMod-aware so the safety gate lets writes through.
    // Priority.Last so DataManager has already created SaveData.
    [HarmonyLib.HarmonyPatch(typeof(GameManager), nameof(GameManager.StartNewGame))]
    [HarmonyLib.HarmonyPriority(HarmonyLib.Priority.Last)]
    public static class QuestModInitializeOnNewGame
    {
        public static void Postfix()
        {
            var data = QuestModPlugin.Instance?.SaveData;
            if (data == null) return;
            if (data.QuestModInitialized) return;
            data.QuestModInitialized = true;
            QuestModPlugin.Log?.LogInfo("StartNewGame: marked save as QuestMod-aware");
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(FullQuestBase), "IsAvailable", HarmonyLib.MethodType.Getter)]
    public static class QuestAvailabilityPatch
    {
        public static void Postfix(FullQuestBase __instance, ref bool __result)
        {
            // Legacy AllQuestsAvailable forces all; else per-quest policy.
            if (!QuestPolicyStore.IsAvailable(__instance.name))
                return;

            if (!QuestModPlugin.IsQuestDiscovered(__instance.name))
                return;

            // Pure mode ignores chain gating; Adjusted respects it.
            if (QuestModPlugin.IsAdjustedWishes && !QuestAcceptance.IsChainPrereqMet(__instance.name))
                return;

            // Adjusted also respects mutually-exclusive twins
            // (Huntress Quest <-> Huntress Quest Runt). Pure ignores -- raw.
            // Sequential quests (Mr Mushroom) are NOT gated here yet; their
            // stages are tracked via PlayerData bools and need a per-quest
            // helper to decide which stage is current. Tracked as residual.
            if (QuestModPlugin.IsAdjustedWishes
                && QuestAcceptance.GetExclusionConflict(__instance.name) != null)
            {
                QuestModPlugin.LogDebugInfo($"IsAvailable narrowing (Adjusted): {__instance.name} blocked by mutually-exclusive twin");
                return;
            }

            // Adjusted also respects custom availableConditions for
            // arena-reuse wishes (Beastfly Hunt requires Fourth Chorus
            // defeated, etc.). Pure ignores -- raw.
            if (QuestModPlugin.IsAdjustedWishes)
            {
                var avail = QuestRequirements.EvaluateAvailableConditions(__instance.name);
                if (!avail.Pass)
                {
                    QuestModPlugin.LogDebugInfo($"IsAvailable narrowing (Adjusted): {__instance.name} blocked -- {avail.Reason}");
                    return;
                }
            }

            if (!__result)
            {
                QuestModPlugin.LogDebugInfo($"IsAvailable override ({QuestModPlugin.WishesMode}): {__instance.name} was False, returning True");
                __result = true;
            }
        }
    }
}
