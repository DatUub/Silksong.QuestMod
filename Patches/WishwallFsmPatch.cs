using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.UnityHelper.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuestMod
{
    // Rewrites PlayerDataBoolTest actions in Flick / wishwall-builder FSMs so
    // the "true" branch fires regardless of the PD value. Cosmetic only -- no
    // PD writes (those would falsely advance milestones).
    // Addressables loads the NPCs on approach, so re-sweep for 30s post-scene.
    public static class WishwallFsmPatch
    {
        // Only touched in FSMs whose name matches a builder pattern -- the
        // name filter prevents collateral damage on general progression bools.
        private static readonly HashSet<string> _wishwallBoolNames = new HashSet<string>
        {
            nameof(PlayerData.defeatedBellBeast),
            nameof(PlayerData.fixerQuestBoardConvo),
            nameof(PlayerData.bonebottomQuestBoardFixed),
            nameof(PlayerData.visitedBellhartSaved),
            nameof(PlayerData.metCaretaker),
            nameof(PlayerData.bellShrineEnclave),
        };

        // Bools that uniquely identify a builder FSM. Used as a structural
        // fallback when the name patterns don't match.
        private static readonly HashSet<string> _structurallyUniqueBools = new HashSet<string>
        {
            nameof(PlayerData.fixerQuestBoardConvo),
            nameof(PlayerData.bonebottomQuestBoardFixed),
        };

        private static readonly string[] _fsmNamePatterns = new[]
        {
            // Fixer is the in-engine name; "Flick" is a wiki nickname.
            "fixer", "flick",
            "wishwall", "wish_wall", "wisher",
            "questboard", "quest_board",
            // Narrow forms only -- broader "build/hammer" false-matches smiths.
            "builder", "construction", "hammering",
        };

        // Per-scene patched-action set. Keyed on (fsm,state,index) tuples so
        // GC'd action hash reuse can't skip a freshly-loaded action.
        private static readonly HashSet<string> _patchedActions = new HashSet<string>();

        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            QuestModPlugin.Log.LogInfo("WishwallFsmPatch: registered sceneLoaded hook");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(scene.name) || scene.name == "Menu_Title") return;
            _patchedActions.Clear();
            // 30s window catches Addressables-loaded NPCs. Stale sweeps bail.
            string queuedScene = scene.name;
            for (float t = 0.5f; t <= 30f; t += 2f)
                QuestModPlugin.Instance.InvokeAfterSeconds(() => SweepOnce(queuedScene), t);
        }

        private static void SweepOnce(string queuedScene)
        {
            string activeScene = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(activeScene) || activeScene != queuedScene) return;

            bool allWishwalls = QuestModPlugin.Instance?.SaveData?.Prereqs?.BypassAllWishwalls == true;
            bool bonebottomOnly = QuestModPlugin.BypassWishboardLock?.Value == true;
            if (!allWishwalls && !bonebottomOnly) return;

            int patched = 0;
            var fsms = Object.FindObjectsByType<PlayMakerFSM>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var fsm in fsms)
            {
                if (fsm == null || fsm.FsmStates == null) continue;
                if (!LooksLikeWishwallBuilder(fsm)) continue;
                if (PatchFsm(fsm, allWishwalls, bonebottomOnly)) patched++;
            }

            if (patched > 0)
                QuestModPlugin.Log.LogInfo(
                    $"WishwallFsmPatch: rewrote {patched} wishwall-builder FSM action(s)");
        }

        private static bool LooksLikeWishwallBuilder(PlayMakerFSM fsm)
        {
            string goName = (fsm.gameObject != null ? fsm.gameObject.name : "").ToLowerInvariant();
            string fsmName = (fsm.FsmName ?? "").ToLowerInvariant();
            foreach (var p in _fsmNamePatterns)
                if (goName.Contains(p) || fsmName.Contains(p)) return true;

            // Structural match on a wishwall-unique PD bool test.
            foreach (var state in fsm.FsmStates)
            {
                if (state?.Actions == null) continue;
                foreach (var act in state.Actions)
                {
                    if (act is PlayerDataBoolTest pdt)
                    {
                        string b = pdt.boolName?.Value ?? "";
                        if (_structurallyUniqueBools.Contains(b)) return true;
                    }
                }
            }
            return false;
        }

        private static bool PatchFsm(PlayMakerFSM fsm, bool allWishwalls, bool bonebottomOnly)
        {
            bool changed = false;
            foreach (var state in fsm.FsmStates)
            {
                if (state?.Actions == null) continue;
                for (int i = 0; i < state.Actions.Length; i++)
                {
                    var act = state.Actions[i];
                    if (act is PlayerDataBoolTest pdt)
                    {
                        string boolName = pdt.boolName?.Value ?? "";
                        if (bonebottomOnly && !allWishwalls
                            && boolName != "bonebottomQuestBoardFixed") continue;
                        if (!_wishwallBoolNames.Contains(boolName)) continue;

                        string actKey = fsm.GetInstanceID() + "|" + state.Name + "|" + i;
                        if (_patchedActions.Contains(actKey)) continue;

                        var trueEvent = pdt.isTrue;
                        var falseEvent = pdt.isFalse;
                        if (trueEvent == null && falseEvent == null) continue;

                        // Mutate in place -- new SendEvent loses Fsm/State wiring.
                        if (trueEvent != null && falseEvent != null)
                        {
                            pdt.isFalse = trueEvent;
                            QuestModPlugin.LogDebugInfo(
                                $"WishwallFsmPatch: redirected isFalse -> isTrue('{trueEvent.Name}') on PlayerDataBoolTest('{boolName}') in {fsm.gameObject.name}/{fsm.FsmName}/{state.Name}");
                        }
                        else if (trueEvent != null)
                        {
                            // isTrue only: already a no-op on false. Mark visited.
                        }
                        else
                        {
                            // isFalse only: Fixer Init sends "NO WISHWALL" if
                            // defeatedBellBeast is false. Null it out.
                            pdt.isFalse = null;
                            QuestModPlugin.LogDebugInfo(
                                $"WishwallFsmPatch: cleared isFalse('{falseEvent.Name}') on PlayerDataBoolTest('{boolName}') in {fsm.gameObject.name}/{fsm.FsmName}/{state.Name}");
                        }
                        _patchedActions.Add(actKey);
                        changed = true;
                    }
                }
            }
            return changed;
        }
    }
}
