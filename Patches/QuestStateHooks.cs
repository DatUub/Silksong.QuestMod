using System.Collections.Generic;
using HutongGames.PlayMaker;
using QuestPlaymakerActions;
using Silksong.FsmUtil;
using Silksong.UnityHelper.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuestMod
{
    public static class QuestStateHooks
    {
        private static readonly HashSet<string> PatchedFSMs = new();

        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            QuestModPlugin.Log.LogInfo("QuestStateHooks: Registered sceneLoaded hook");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(scene.name) || scene.name == "Menu_Title")
                return;

            QuestModPlugin.Log.LogDebug($"Scene loaded: {scene.name}");
            
            PatchAllFSMs();
            RefreshQuestBoards();
            
            QuestModPlugin.Instance.InvokeAfterSeconds(() =>
            {
                QuestModPlugin.Log.LogDebug("Delayed re-patch running...");
                PatchedFSMs.Clear();
                PatchAllFSMs();
                RefreshQuestBoards();

                if (QuestModPlugin.AllQuestsAccepted)
                {
                    QuestAcceptance.InjectAndAcceptAllQuests();
                }

                if (QuestModPlugin.EnableCompletionOverrides.Value)
                    QuestCompletionOverrides.ApplySavedOverrides();
            }, 0.5f);
        }

        private static void PatchAllFSMs()
        {
            if (!QuestModPlugin.AllQuestsAvailable)
                return;

            var fsms = UnityEngine.Object.FindObjectsByType<PlayMakerFSM>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var fsm in fsms)
            {
                try
                {
                    PatchQuestStateFSM(fsm);
                }
                catch (System.Exception ex)
                {
                    QuestModPlugin.Log.LogDebug($"Error patching FSM: {ex.Message}");
                }
            }
        }

        private static void RefreshQuestBoards()
        {
            var allObjects = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
                            QuestModPlugin.Log.LogInfo($"Refreshed quest board: {obj.gameObject.name}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        QuestModPlugin.Log.LogDebug($"Failed to refresh quest board: {ex.Message}");
                    }
                }
            }
        }

        private static void PatchQuestStateFSM(PlayMakerFSM fsm)
        {
            if (fsm == null || fsm.Fsm == null || !fsm.Fsm.Initialized || fsm.FsmStates == null)
                return;

            string fsmKey = $"{fsm.gameObject.name}/{fsm.FsmName}";
            if (PatchedFSMs.Contains(fsmKey))
                return;

            bool foundQuestAction = false;

            foreach (var state in fsm.FsmStates)
            {
                if (state == null)
                    continue;

                // V2 actions via FsmUtil
                var v2Actions = FsmUtil.GetActionsOfType<CheckQuestStateV2>(fsm, state.Name);
                foreach (var checkActionV2 in v2Actions)
                {
                    foundQuestAction = true;
                    PatchCheckQuestStateV2(state, checkActionV2);
                }

                // V1 actions via FsmUtil
                var v1Actions = FsmUtil.GetActionsOfType<CheckQuestState>(fsm, state.Name);
                foreach (var checkActionV1 in v1Actions)
                {
                    foundQuestAction = true;
                    PatchCheckQuestStateV1(state, checkActionV1);
                }
            }

            if (foundQuestAction)
            {
                PatchedFSMs.Add(fsmKey);
                QuestModPlugin.Log.LogInfo($"Patched quest FSM: {fsmKey}");
            }
            else
            {
                LogFsmActionTypes(fsm, fsmKey);
            }
        }

        private static void PatchCheckQuestStateV2(FsmState state, CheckQuestStateV2 checkAction)
        {
            if (checkAction == null)
                return;

            try
            {
                var completedEvent = checkAction.CompletedEvent;

                if (completedEvent != null)
                {
                    QuestModPlugin.Log.LogInfo($"  {state.Name}: Redirecting NotTrackedEvent -> CompletedEvent");
                    checkAction.NotTrackedEvent = completedEvent;
                    checkAction.IncompleteEvent = completedEvent;
                }
                else
                {
                    QuestModPlugin.Log.LogWarning($"  {state.Name}: CompletedEvent is null");
                }
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"  {state.Name}: Patch failed - {ex.Message}");
            }
        }

        private static void PatchCheckQuestStateV1(FsmState state, CheckQuestState action)
        {
            try
            {
                var completedEvent = action.CompletedEvent;

                if (completedEvent != null)
                {
                    QuestModPlugin.Log.LogInfo($"  {state.Name} (V1): Redirecting NotTrackedEvent -> CompletedEvent");
                    action.NotTrackedEvent = completedEvent;
                }
                else
                {
                    QuestModPlugin.Log.LogWarning($"  {state.Name} (V1): CompletedEvent is null");
                }
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"  {state.Name} (V1): Patch failed - {ex.Message}");
            }
        }

        private static readonly HashSet<string> LoggedNpcFsms = new();
        private static readonly HashSet<string> InterestingActionTypes = new()
        {
            "PlayerDataBoolTest",
            "GetPlayerDataBool",
            "BoolTest",
            "IntCompare",
            "StringCompare",
            "CheckQuestState",
            "ActivateGameObject",
            "SetActive"
        };

        private static void LogFsmActionTypes(PlayMakerFSM fsm, string fsmKey)
        {
            if (LoggedNpcFsms.Contains(fsmKey))
                return;
            
            bool hasInterestingAction = false;
            List<string> actionList = new();

            foreach (var state in fsm.FsmStates)
            {
                if (state == null || state.Actions == null)
                    continue;

                foreach (var action in state.Actions)
                {
                    if (action == null)
                        continue;

                    string typeName = action.GetType().Name;
                    if (InterestingActionTypes.Contains(typeName))
                    {
                        hasInterestingAction = true;
                        if (!actionList.Contains(typeName))
                            actionList.Add(typeName);
                    }
                }
            }

            if (hasInterestingAction)
            {
                LoggedNpcFsms.Add(fsmKey);
                QuestModPlugin.Log.LogDebug($"Interesting FSM: {fsmKey} - Actions: {string.Join(", ", actionList)}");
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

            if (!__result)
            {
                QuestModPlugin.Log.LogDebug($"IsAvailable override: {__instance.name} was False, returning True");
                __result = true;
            }
        }
    }
}
