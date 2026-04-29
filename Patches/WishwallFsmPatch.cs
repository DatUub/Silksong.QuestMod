using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.UnityHelper.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuestMod
{
    // Cluster K-3: cosmetic Flick / wishwall builder FSM rewrite.
    //
    // The runtime ActivateIfPlayerdataTrueStartPatch already makes wishwall
    // kiosks interactable without touching PlayerData. This patch handles the
    // *visual* side: Flick's "build" FSM keeps him hammering until
    // bonebottomQuestBoardFixed flips. Without this patch he hammers forever
    // when BypassAllWishwalls is on. Same idea for any other wishwall-builder
    // NPC (e.g. Bellhart's covered-wall reveal NPC) — we identify them
    // structurally rather than by hardcoded names.
    //
    // Approach: rewrite PlayerDataBoolTest actions in matched FSMs so that
    // the "bool is true" branch fires immediately on state entry. We do NOT
    // flip the underlying PD bool (that would falsely advance milestones —
    // see WishboardActivationPatch.cs comments for full reasoning).
    //
    // Because Flick's FSM is Addressables-loaded on player approach, scene
    // sweeps catch him only after he spawns. We re-sweep on a coroutine for
    // 30s after each scene load, then stop. Idempotent: already-rewritten
    // actions are tagged via a custom marker so we don't double-process.
    public static class WishwallFsmPatch
    {
        // Bool names we patch ONLY inside FSMs whose GO/FSM name already
        // matches a builder pattern (Fixer*, Wishwall*, etc). This includes
        // general progression bools that other FSMs also test for unrelated
        // reasons; the name filter prevents collateral damage.
        private static readonly HashSet<string> _wishwallBoolNames = new HashSet<string>
        {
            // Confirmed via Bonetown FSM dump on slot 4 (Fixer NPC Standing /
            // Conversation / Init): PlayerDataBoolTest(defeatedBellBeast)
            // sends "NO WISHWALL" event when false -> wishwall not yet built.
            "defeatedBellBeast",
            // Has-Spoken-To-Fixer-About-Wishwall flag.
            "fixerQuestBoardConvo",
            // Legacy / theoretical wishwall gates kept for forward compat.
            "bonebottomQuestBoardFixed",
            "visitedBellhartSaved",
            "metCaretaker",
            "bellShrineEnclave",
        };

        // Bool names that UNIQUELY identify a builder FSM (used by
        // LooksLikeWishwallBuilder for the structural match fallback when
        // GO/FSM name doesn't match a known pattern). We deliberately
        // exclude defeatedBellBeast here because Mapper, Churchkeeper, and
        // others also test it for unrelated dialogue branches.
        private static readonly HashSet<string> _structurallyUniqueBools = new HashSet<string>
        {
            "fixerQuestBoardConvo",
            "bonebottomQuestBoardFixed",
            // visitedBellhartSaved/metCaretaker also tested by unrelated NPCs;
            // leave them out of the structural-id list, keep them in the
            // patchable list.
        };

        private static readonly string[] _fsmNamePatterns = new[]
        {
            // Silksong NPC name: "Fixer" (the Hornet wiki's "Flick" is a fan
            // nickname; in Assembly-CSharp the GameObjects are "Fixer Pilgrim",
            // "BG Fixer", "Fixer Bridge", "Fixer NPC Standing" etc.)
            "fixer", "flick",
            "wishwall", "wish_wall", "wisher", "build", "construct", "hammer",
            "questboard", "quest_board",
        };

        // Tag stored in the FsmStateAction.Active flag's mirror via an
        // Per-scene patched-action set. Cleared on every sceneLoaded so an
        // Addressables-driven unload/reload of the Fixer NPC's FSM produces
        // a fresh tracking pass. Keyed on (fsmInstanceId, stateName, actionIndex)
        // strings so identity-hash reuse after GC cannot make us skip a
        // legitimately new action.
        private static readonly HashSet<string> _patchedActions = new HashSet<string>();

        // Capture the active scene at sweep-schedule time so a delayed sweep
        // that fires after the player has left the scene (rapid title cycle,
        // back-to-back warps) short-circuits instead of scanning the new
        // scene's FSMs with stale intent.
        private static string _scheduledScene;

        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            QuestModPlugin.Log.LogInfo("WishwallFsmPatch: registered sceneLoaded hook");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(scene.name) || scene.name == "Menu_Title") return;
            // Reset the per-scene tracking set so a returning Fixer FSM gets
            // re-evaluated against the current toggle state.
            _patchedActions.Clear();
            _scheduledScene = scene.name;
            // Spread sweeps over 30s to catch Addressables-loaded NPC FSMs as
            // the player approaches them. The QuestModPlugin coroutine helper
            // is fire-and-forget so each call is independent.
            for (float t = 0.5f; t <= 30f; t += 2f)
                QuestModPlugin.Instance.InvokeAfterSeconds(SweepOnce, t);
        }

        private static void SweepOnce()
        {
            // Bail if the player has changed scenes since this sweep was
            // scheduled. Avoids paying the FindObjectsByType cost on a stale
            // scene and avoids accidental cross-scene patching during fast
            // title-to-save cycles.
            string activeScene = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(activeScene) || activeScene != _scheduledScene) return;

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

            // Structural match: any state has a PlayerDataBoolTest whose
            // boolName is wishwall-unique (excludes general progression bools
            // like defeatedBellBeast that other FSMs also test).
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
                        // Bonebottom-only mode keeps strict bool-name match.
                        if (bonebottomOnly && !allWishwalls
                            && boolName != "bonebottomQuestBoardFixed") continue;
                        if (!_wishwallBoolNames.Contains(boolName)) continue;

                        // Idempotency tag: scoped per-scene (cleared on
                        // sceneLoaded) to survive Addressables unload/reload of
                        // the FSM owner. Keyed on the (fsm, state, action index)
                        // tuple rather than action identity hash so we don't
                        // skip a freshly-instantiated action whose hash happens
                        // to collide with a long-dead one.
                        string actKey = fsm.GetInstanceID() + "|" + state.Name + "|" + i;
                        if (_patchedActions.Contains(actKey)) continue;

                        var trueEvent = pdt.isTrue;
                        var falseEvent = pdt.isFalse;
                        if (trueEvent == null && falseEvent == null) continue;

                        // Mutate the action in place rather than replacing
                        // it with a freshly-constructed SendEvent (a bare
                        // `new` does not get the Fsm/State/Owner wiring that
                        // FsmState.ActivateActions normally applies, and can
                        // NRE on first run). Mirrors the existing pattern in
                        // QuestStateHooks.PatchPlayerDataBoolTest.
                        if (trueEvent != null && falseEvent != null)
                        {
                            // Both branches present: route the false branch
                            // to whatever the true branch goes to so either
                            // bool value falls through the same transition.
                            pdt.isFalse = trueEvent;
                            QuestModPlugin.LogDebugInfo(
                                $"WishwallFsmPatch: redirected isFalse -> isTrue('{trueEvent.Name}') on PlayerDataBoolTest('{boolName}') in {fsm.gameObject.name}/{fsm.FsmName}/{state.Name}");
                        }
                        else if (trueEvent != null)
                        {
                            // Only isTrue is set: nothing to redirect. Action
                            // already does the right thing on bool=true and
                            // is a no-op on bool=false. Mark as visited.
                        }
                        else
                        {
                            // Only isFalse is set: this is the Fixer Init
                            // case (sends "NO WISHWALL" when defeatedBellBeast
                            // is false). Nulling isFalse makes the action
                            // a no-op so the false branch never fires.
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
