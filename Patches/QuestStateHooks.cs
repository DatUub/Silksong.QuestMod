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

        private static readonly HashSet<string> WhitelistedObjects = new(System.StringComparer.OrdinalIgnoreCase)
        {
            "Courier",
            "Tipp",
            "Mapper",
            "Leader",
            "Quest Board",
            "QuestBoard",
            "Wishwall",
            "Sherma",
            "Fixer",
            "Caretaker",
        };

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
            ActivateQuestBoards();
            RefreshQuestBoards();

            QuestModPlugin.Instance.InvokeAfterSeconds(() =>
            {
                QuestModPlugin.LogDebugInfo("Delayed re-patch running...");
                PatchedFSMs.Clear();
                PatchAllQuestFSMs();
                SetNpcSpawnFlags();
                ActivateQuestBoards();
                RefreshQuestBoards();
                DumpNpcDiagnostics();

                if (QuestModPlugin.AllQuestsAccepted)
                {
                    QuestAcceptance.InjectAndAcceptAllQuests();
                }

                if (QuestModPlugin.EnableCompletionOverrides.Value)
                    QuestCompletionOverrides.ApplySavedOverrides();
            }, 0.5f);
        }

        private static bool IsWhitelisted(GameObject go)
        {
            var name = go.name;
            foreach (var entry in WhitelistedObjects)
            {
                if (name.IndexOf(entry, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            var parent = go.transform.parent;
            if (parent != null)
            {
                var parentName = parent.gameObject.name;
                foreach (var entry in WhitelistedObjects)
                {
                    if (parentName.IndexOf(entry, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
        }

        private static void PatchAllQuestFSMs()
        {
            if (!QuestModPlugin.AllQuestsAvailable)
                return;

            var fsms = Object.FindObjectsByType<PlayMakerFSM>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var fsm in fsms)
            {
                if (fsm == null) continue;

                if (!IsWhitelisted(fsm.gameObject))
                    continue;

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
                        PatchCheckAction(checkV2, state.Name, fsmKey, "V2");
                        patched = true;
                    }
                    else if (action.GetType().Name == "CheckQuestState")
                    {
                        PatchCheckActionV1(action, state.Name, fsmKey);
                        patched = true;
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

        private static void PatchCheckAction(CheckQuestStateV2 checkAction, string stateName, string fsmKey, string version)
        {
            try
            {
                var completedEvent = checkAction.CompletedEvent;
                if (completedEvent == null)
                {
                    QuestModPlugin.Log.LogDebug($"  {fsmKey}/{stateName} ({version}): CompletedEvent is null, skipping");
                    return;
                }

                checkAction.NotTrackedEvent = completedEvent;
                checkAction.IncompleteEvent = completedEvent;
                QuestModPlugin.LogDebugInfo($"  {fsmKey}/{stateName} ({version}): Redirected → CompletedEvent");
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogDebug($"  {fsmKey}/{stateName} ({version}): Patch failed - {ex.Message}");
            }
        }

        private static void PatchCheckActionV1(FsmStateAction action, string stateName, string fsmKey)
        {
            try
            {
                var type = action.GetType();
                var notTrackedField = type.GetField("NotTrackedEvent", BindingFlags.Public | BindingFlags.Instance);
                var incompleteField = type.GetField("IncompleteEvent", BindingFlags.Public | BindingFlags.Instance);
                var completedField = type.GetField("CompletedEvent", BindingFlags.Public | BindingFlags.Instance);

                if (completedField == null) return;

                var completedEvent = completedField.GetValue(action);
                if (completedEvent == null) return;

                if (notTrackedField != null)
                    notTrackedField.SetValue(action, completedEvent);
                if (incompleteField != null)
                    incompleteField.SetValue(action, completedEvent);

                QuestModPlugin.LogDebugInfo($"  {fsmKey}/{stateName} (V1): Redirected → CompletedEvent");
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogDebug($"  {fsmKey}/{stateName} (V1): Patch failed - {ex.Message}");
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

        private static readonly HashSet<string> NpcSpawnFlags = new HashSet<string>
        {
            "metMapper",
            "MapperAppearInBellhart",
            "metTipp",
            "hasMarker_a",
            "shermaQuestActive",
            "shermaInBellhart",
            "fixerQuestBoardConvo",
            "conclaveHubOpen",
            "belltowerRepaired",
            "visitedBellhartSaved",
        };

        private static void SetNpcSpawnFlags()
        {
            if (!QuestModPlugin.AllQuestsAvailable)
                return;

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
            if (!QuestModPlugin.AllQuestsAvailable)
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

        private static void DumpNpcDiagnostics()
        {
            if (!QuestModPlugin.AllQuestsAvailable) return;

            QuestModPlugin.Log.LogInfo("=== NPC Diagnostics ===");
            var fsms = Object.FindObjectsByType<PlayMakerFSM>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var fsm in fsms)
            {
                if (fsm == null) continue;
                if (!IsWhitelisted(fsm.gameObject)) continue;


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

    [HarmonyLib.HarmonyPatch(typeof(FullQuestBase), "IsAvailable", HarmonyLib.MethodType.Getter)]
    public static class QuestAvailabilityPatch
    {
        public static void Postfix(FullQuestBase __instance, ref bool __result)
        {
            if (!QuestModPlugin.AllQuestsAvailable)
                return;

            if (!QuestModPlugin.IsQuestDiscovered(__instance.name))
                return;

            if (!QuestAcceptance.IsChainPrereqMet(__instance.name))
                return;

            if (!__result)
            {
                QuestModPlugin.LogDebugInfo($"IsAvailable override: {__instance.name} was False, returning True");
                __result = true;
            }
        }
    }
}
