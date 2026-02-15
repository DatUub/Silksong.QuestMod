using System.Collections.Generic;
using System.Reflection;
using HutongGames.PlayMaker;
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
            PatchWhitelistedFSMs();
            RefreshQuestBoards();

            QuestModPlugin.Instance.InvokeAfterSeconds(() =>
            {
                QuestModPlugin.LogDebugInfo("Delayed re-patch running...");
                PatchedFSMs.Clear();
                PatchWhitelistedFSMs();
                RefreshQuestBoards();

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

        private static void PatchWhitelistedFSMs()
        {
            if (!QuestModPlugin.AllQuestsAvailable)
                return;

            var fsms = Object.FindObjectsByType<PlayMakerFSM>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var fsm in fsms)
            {
                if (fsm == null || fsm.FsmStates == null)
                    continue;

                if (!IsWhitelisted(fsm.gameObject))
                    continue;

                var fsmKey = $"{fsm.gameObject.name}/{fsm.FsmName}";
                if (PatchedFSMs.Contains(fsmKey))
                    continue;

                try
                {
                    PatchQuestStateFSM(fsm, fsmKey);
                }
                catch (System.Exception ex)
                {
                    QuestModPlugin.Log.LogDebug($"Error patching FSM {fsmKey}: {ex.Message}");
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
