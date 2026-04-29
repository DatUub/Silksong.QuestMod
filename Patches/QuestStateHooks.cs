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

        // CLUSTER O retired the per-NPC `WhitelistedObjects` substring set in
        // favour of component-based detection (`HasAnyNpcMarker`). The list
        // over-matched (e.g. "BG Fixer" environment FSM) and required manual
        // maintenance per NPC.

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
            SetNpcSpawnFlags();
            ApplyGranularBypasses();
            ActivateQuestBoards();
            RefreshQuestBoards();

            QuestModPlugin.Instance.InvokeAfterSeconds(() =>
            {
                QuestModPlugin.LogDebugInfo("Delayed re-patch running...");
                PatchedFSMs.Clear();
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

        /// <summary>
        /// In All Wishes Mode, zero out tool-gate target counts that the player
        /// can't normally satisfy without progression-locked items. The Plasmium
        /// quests ("Extractor Blue", "Extractor Blue Worms") have a target at
        /// index 1 that requires the Needle Phial tool — which a fresh save
        /// doesn't have. The Phial target is not a PlayerData bool, it's a
        /// ToolItemBasic counter on the quest itself, so we satisfy it by
        /// setting its required count to 0. Transient (not saved) so toggling
        /// All Wishes off restores the original values on next load.
        /// </summary>
        private static void ApplyAllWishesTargetBypass()
        {
            // Fires per-quest: if the player has flagged either Plasmium quest as Available
            // (via per-quest policy) OR has any global mode on, satisfy the Phial tool-gate.
            if (QuestPolicyStore.IsAvailable("Extractor Blue"))
                QuestCompletionOverrides.SetTargetCountTransient("Extractor Blue", 1, 0);
            if (QuestPolicyStore.IsAvailable("Extractor Blue Worms"))
                QuestCompletionOverrides.SetTargetCountTransient("Extractor Blue Worms", 1, 0);
        }

        // CLUSTER Q retired the per-FSM NPC-marker filter from cluster O.
        // The decision of whether to apply the redirect arch is now made
        // per-CheckQuestState-action by ShouldRedirect, which reads the
        // action's Quest field and checks WishesMode + chain prereq +
        // mutually-exclusive + BeginQuest reachability. No FSM-level filter
        // needed -- patch every FSM, let the action-level gate decide.

        // CLUSTER Q: read the quest name from a CheckQuestState action's `Quest`
        // FsmObject field. Returns null if the action has no Quest, the Quest
        // is unset, or anything fails. Quest name is the canonical key for
        // chain / exclusion / sequential lookups.
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

        // CLUSTER Q: walk the FSM graph from `state` looking for any reachable
        // state that contains a quest-acceptance action (BeginQuest /
        // BeginQuestV2 / BoardQuest). Used to distinguish quest-giver-flow
        // CheckQuestState actions (an "Accept Quest" decision) from quest-
        // affected-flow CheckQuestState actions (flavor reactions, environment
        // gating). Only quest-giver-flow gets the redirect under Adjusted.
        //
        // Bounded depth so pathological FSMs don't loop forever; 6 is enough
        // to cover the 2-3 hops a typical accept-flow uses while staying cheap.
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

        // CLUSTER Q: per-action decision. Returns true if this action's NotTracked
        // event should be redirected.
        //
        //   Disabled: never (mod doesn't fire)
        //   Pure:     always (raw -- no gating; soft-locks accepted as part of Pure's tradeoff)
        //   Adjusted: only when ALL of:
        //     - chain prereqs met
        //     - no mutually-exclusive twin currently accepted/completed
        //     - BeginQuest is reachable from this action's state (it's quest-giver flow,
        //       not a flavor reaction or environment gate)
        //
        // Action's Quest field is the source of truth for the quest name.
        // Public alias of ShouldRedirect so external callers can stamp each
        // action with the same decision the runtime patcher would make.
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

        // Public re-patch entry point. Lets external callers toggle WishesMode
        // then re-evaluate every action's redirect decision mid-frame without
        // waiting for a real scene transition.
        public static void RePatchAllForCurrentMode()
        {
            PatchedFSMs.Clear();
            PatchAllQuestFSMs();
        }

        private static void PatchAllQuestFSMs()
        {
            if (!AnyQuestAvailable())
                return;

            var fsms = Object.FindObjectsByType<PlayMakerFSM>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var fsm in fsms)
            {
                if (fsm == null) continue;

                // CLUSTER Q: no per-FSM filter -- the per-action ShouldRedirect
                // decides whether to patch each CheckQuestState. Only skip the
                // additive-load conditional GameObjects (which can be in a weird
                // partial-load state and cause NREs during enumeration).
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
                        PatchPlayerDataBoolTest(pdBoolTest, state.Name, fsmKey);
                        patched = true;
                    }
                    else if (action is BoolTest boolTest)
                    {
                        PatchBoolTest(boolTest, state.Name, fsmKey);
                        patched = true;
                    }
                }
            }

            if (patched)
                PatchedFSMs.Add(fsmKey);
        }

        // CLUSTER N: V2 redirect target is IncompleteEvent (quest accepted but not done),
        // NOT CompletedEvent. The previous behavior redirected both NotTracked and Incomplete
        // to Completed, which made quest-giver NPCs (Plasmium, Shakra, Mr Mushroom, Junilana)
        // jump to their thank-you / wrap-up dialogue branch as soon as the player walked up,
        // breaking quest acceptance and multi-stage quest progression on every whitelisted NPC.
        //
        // Fallback ladder: NotTracked → IncompleteEvent; if IncompleteEvent is null on this
        // particular action, fall back to CompletedEvent so the FSM still moves forward.
        // We deliberately do NOT touch IncompleteEvent or CompletedEvent themselves.
        //
        // CLUSTER Q: now gated by ShouldRedirect (mode + chain + exclusion + BeginQuest
        // reachability). Returns true if the action was patched, false otherwise.
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

        // CLUSTER N: V1 redirect target is TrackedEvent (quest accepted/active), NOT
        // CompletedEvent. CheckQuestState (V1) has fields { NotTrackedEvent, TrackedEvent,
        // CompletedEvent } — there is no IncompleteEvent (V1 collapses incomplete + tracked
        // into TrackedEvent). The old code wrote CompletedEvent into both NotTracked and a
        // nonexistent IncompleteEvent field; the latter was dead code, the former caused
        // quest-giver NPCs to enter the thank-you branch prematurely.
        //
        // Fallback ladder: NotTracked → TrackedEvent; if TrackedEvent is null on this
        // particular action, fall back to CompletedEvent so the FSM still moves forward.
        //
        // CLUSTER Q: gated by ShouldRedirect like V2. Returns true if patched.
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

        private static void PatchPlayerDataBoolTest(PlayerDataBoolTest boolTest, string stateName, string fsmKey)
        {
            try
            {
                var varName = boolTest.boolName != null ? boolTest.boolName.Value : null;
                if (string.IsNullOrEmpty(varName) || !NpcSpawnFlags.Contains(varName))
                    return;

                var trueEvent = boolTest.isTrue;
                if (trueEvent == null)
                    return;

                if (boolTest.isFalse != null)
                {
                    boolTest.isFalse = trueEvent;
                    QuestModPlugin.LogDebugInfo($"  {fsmKey}/{stateName} (PDTest '{varName}'): Redirected isFalse → isTrue");
                }
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogDebug($"  {fsmKey}/{stateName} (PDTest): Patch failed - {ex.Message}");
            }
        }

        private static void PatchBoolTest(BoolTest boolTest, string stateName, string fsmKey)
        {
            try
            {
                var varName = boolTest.boolVariable != null ? boolTest.boolVariable.Name : null;
                if (string.IsNullOrEmpty(varName) || !NpcSpawnFlags.Contains(varName))
                    return;

                var trueEvent = boolTest.isTrue;
                if (trueEvent == null)
                    return;

                if (boolTest.isFalse != null)
                {
                    boolTest.isFalse = trueEvent;
                    QuestModPlugin.LogDebugInfo($"  {fsmKey}/{stateName} (BoolTest '{varName}'): Redirected isFalse → isTrue");
                }
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogDebug($"  {fsmKey}/{stateName} (BoolTest): Patch failed - {ex.Message}");
            }
        }

        // CLUSTER N: Spawn-flag whitelist absorbed from cluster A, with the addition of the
        // quest-giver flags for issues #2/#4/#7 (Shakra, Mr Mushroom, City Merchant, Junilana).
        // metTipp, conclaveHubOpen, belltowerRepaired were dropped — those names do not exist
        // on the live Assembly-CSharp.dll PlayerData (verified via reflection). Using
        // nameof(PlayerData.X) so the build will fail loudly if a field is renamed upstream.
        private static readonly HashSet<string> NpcSpawnFlags = new HashSet<string>
        {
            nameof(PlayerData.metMapper),
            nameof(PlayerData.MapperAppearInBellhart),
            nameof(PlayerData.hasMarker_a),
            nameof(PlayerData.shermaQuestActive),
            nameof(PlayerData.shermaInBellhart),
            nameof(PlayerData.fixerQuestBoardConvo),
            nameof(PlayerData.visitedBellhartSaved),
            nameof(PlayerData.ShakraFinalQuestAppear),
            nameof(PlayerData.hasDoubleJump),
            nameof(PlayerData.MushroomQuestFound1),
            nameof(PlayerData.MushroomQuestFound2),
            nameof(PlayerData.MushroomQuestFound3),
            nameof(PlayerData.MushroomQuestFound4),
            nameof(PlayerData.MushroomQuestFound5),
            nameof(PlayerData.MushroomQuestFound6),
            nameof(PlayerData.MushroomQuestFound7),
            nameof(PlayerData.cityMerchantSaved),
            nameof(PlayerData.MetCityMerchantScavenge),
            nameof(PlayerData.MetCityMerchantEnclave),
            nameof(PlayerData.cityMerchantCanLeaveForBridge),
        };

        private static void SetNpcSpawnFlags()
        {
            if (!AnyQuestAvailable())
                return;

            // CLUSTER P: spawn flags are PD bool writes (false -> true, one-way).
            // Gate behind the save-safety check so legacy saves stay clean.
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                QuestModPlugin.LogDebugInfo("SetNpcSpawnFlags: skipped (legacy save, no override) -- cluster P gate");
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

        // ──────────────────────────────────────────────────────────────────
        // CLUSTER E: granular prerequisite bypasses
        // Independent of AllQuestsAvailable. Each toggle on the save data sets
        // a small cluster of PlayerData bools so individual prereq chains can
        // be opened without flipping the global override.
        // Field names verified against Assembly-CSharp.dll string table.
        // ──────────────────────────────────────────────────────────────────
        private static void ApplyGranularBypasses()
        {
            if (PlayerData.instance == null)
                return;

            // CLUSTER P: granular bypasses are one-way PD writes. Same gate.
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                QuestModPlugin.LogDebugInfo("ApplyGranularBypasses: skipped (legacy save, no override) -- cluster P gate");
                return;
            }

            var prereqs = QuestModPlugin.Prereqs;
            if (prereqs == null)
                return;

            int set = 0;

            if (prereqs.BypassFleatopia)
            {
                set += SetPdBoolIfFalse("visitedFleatopia");
                set += SetPdBoolIfFalse("SeenFleatopiaEmpty");
                set += SetPdBoolIfFalse("SethJoinedFleatopia");
                set += SetPdBoolIfFalse("TroupeLeaderSpokenFleatopiaSearch");
                set += SetPdBoolIfFalse("MetCaravanTroupeLeader");
            }

            if (prereqs.BypassMandatoryWishes)
            {
                set += SetPdBoolIfFalse("promisedFirstWish");
            }

            if (prereqs.BypassFaydownCloak)
            {
                set += SetPdBoolIfFalse("hasDoubleJump");
            }

            if (prereqs.BypassNeedolin)
            {
                set += SetPdBoolIfFalse("hasNeedolin");
            }

            if (prereqs.BypassBonebottomQuestBoard)
            {
                set += SetPdBoolIfFalse("bonebottomQuestBoardFixed");
            }

            // Cluster K-2: BypassAllWishwalls is intentionally NON-destructive.
            // No PD writes here -- flipping defeatedBellBeast / visitedBellhartSaved
            // / metCaretaker would cascade into story/Act milestones (Bell Beast
            // achievement, Bellhart haunted->saved transition, Caretaker's
            // questline). The wiki research found these are the gates, but
            // setting them = "I beat the boss / saved the town / met the NPC",
            // which is way more than "the wishwall is usable". The runtime
            // activation patch (ActivateIfPlayerdataTrueStartPatch) handles
            // wishwall visibility/interactivity without any save mutation.

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
            if (!AnyQuestAvailable()) return;

            QuestModPlugin.Log.LogInfo("=== NPC Diagnostics ===");
            var fsms = Object.FindObjectsByType<PlayMakerFSM>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var fsm in fsms)
            {
                if (fsm == null) continue;
                // CLUSTER Q: walk every FSM (no whitelist). Diagnostic listing
                // only filters by "has CheckQuestState/V2 actions" via the inner
                // loop, so non-quest FSMs still skip cheaply.


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

    // CLUSTER P: StartNewGame postfix sets QuestModInitialized=true on the
    // active QuestModSaveData so the safety gate marks the save as
    // QuestMod-aware. Runs AFTER DataManager's OnceSetupHook (priority Last)
    // so SaveData has already been initialized by the time we touch it.
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
            QuestModPlugin.Log?.LogInfo("StartNewGame: marked save as QuestMod-aware (cluster P)");
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(FullQuestBase), "IsAvailable", HarmonyLib.MethodType.Getter)]
    public static class QuestAvailabilityPatch
    {
        public static void Postfix(FullQuestBase __instance, ref bool __result)
        {
            // Per-quest availability: legacy AllQuestsAvailable forces every
            // quest available; otherwise only quests with an explicit policy.
            if (!QuestPolicyStore.IsAvailable(__instance.name))
                return;

            if (!QuestModPlugin.IsQuestDiscovered(__instance.name))
                return;

            // CLUSTER H: Pure mode ignores chain gating; Adjusted respects it.
            if (QuestModPlugin.IsAdjustedWishes && !QuestAcceptance.IsChainPrereqMet(__instance.name))
                return;

            // CLUSTER Q: Adjusted also respects mutually-exclusive twins
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

            // CLUSTER R: Adjusted also respects custom availableConditions for
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
