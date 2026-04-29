// CLUSTER M: Bug 3 — Double Trobbio in All-Wishes mode
//
// Diagnosis:
//   Trobbio is a wandering wishboard quest-giver NPC. Under All-Wishes the
//   NPC-spawn-flag whitelist (QuestStateHooks.NpcSpawnFlags) flips Trobbio's
//   discovery flag, but the base game ALSO spawns him via a separate scene-
//   placed prefab once his quest activates. Result: two Trobbio NPCs in
//   the same scene — one walks his patrol path, the other stands at the
//   wishboard, both with conversation FSMs alive. Talking to either can
//   advance the same quest twice or leave him in a desync state.
//
// Fix approach (this file):
//   On scene load (post the QuestStateHooks 0.5s delay so NPC spawns have
//   settled), scan the scene for GameObjects whose name matches Trobbio.
//   If two-or-more roots exist, deactivate all but the one that has the
//   most-active conversation FSM (heuristic: the one whose FSM is currently
//   in a non-idle state, otherwise the one that was instantiated first by
//   InstanceID order — stable + deterministic).
//
// Confidence: MEDIUM-HIGH. Trobbio is a real Silksong NPC and the
//   GameObject naming convention in Silksong is consistent ("Trobbio" or
//   "Trobbio NPC"). The dedup heuristic is conservative — when only one
//   instance exists we are a no-op. Worst case (both instances are needed
//   for a script reason we don't know about) we deactivate the duplicate
//   rather than destroy it, so re-enabling is one line.
//
// Test plan:
//   1. New save, AllWishes ON, travel to a wishboard scene where Trobbio
//      can spawn (Bellhart / Greymoor / Songclave). Confirm exactly ONE
//      Trobbio is visible and his quest can be picked up + completed.
//   2. With AllWishes OFF, scene reload — Trobbio behavior should be
//      unchanged (patch is no-op when AllQuestsAvailable=false).
//   3. Save & reload mid-Trobbio-quest under AllWishes — still one
//      Trobbio, his FSM state preserved (no respawn duplicate).

using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityEngine.SceneManagement;
using Silksong.UnityHelper.Extensions;

namespace QuestMod
{
    public static class EdgeBugDoubleTrobbio
    {
        private const string TrobbioNamePrefix = "Trobbio";

        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            QuestModPlugin.Log.LogInfo("EdgeBugDoubleTrobbio: registered (scene-load dedup)");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(scene.name) || scene.name == "Menu_Title")
                return;
            // CLUSTER Q: edge-case dedup is an Adjusted-only safety net. Pure
            // wants raw chaos -- if the player picks Pure and ends up with two
            // Trobbios spawned, that's the soft-lock tradeoff they signed up for.
            if (!QuestModPlugin.IsAdjustedWishes)
                return;

            // Run after QuestStateHooks (0.5s) so any spawn flag-driven
            // instantiations have already happened.
            QuestModPlugin.Instance.InvokeAfterSeconds(DedupTrobbio, 0.75f);
            // Second pass in case a wave-spawn or wishboard-refresh creates
            // a duplicate later (matches SilverBellPatch's two-shot pattern).
            QuestModPlugin.Instance.InvokeAfterSeconds(DedupTrobbio, 2.25f);
        }

        private static void DedupTrobbio()
        {
            var matches = new List<GameObject>();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (go == null) continue;
                if (!IsTrobbio(go)) continue;
                // Only consider scene roots-or-NPCs (skip child decorations
                // like "TrobbioHat" that aren't standalone NPCs).
                if (go.transform.parent != null)
                {
                    // Accept if our parent isn't itself a Trobbio (i.e. we
                    // are an actual NPC root attached to a scene container).
                    if (IsTrobbio(go.transform.parent.gameObject))
                        continue;
                }
                matches.Add(go);
            }

            if (matches.Count <= 1) return;

            // Score: the GameObject with an active PlayMakerFSM not in a
            // default-named idle state wins. Tie-break by lowest InstanceID
            // (oldest = the legitimately-spawned one).
            GameObject keep = matches[0];
            int keepScore = ScoreForActivity(keep);
            for (int i = 1; i < matches.Count; i++)
            {
                int s = ScoreForActivity(matches[i]);
                if (s > keepScore || (s == keepScore && matches[i].GetInstanceID() < keep.GetInstanceID()))
                {
                    keep = matches[i];
                    keepScore = s;
                }
            }

            int suppressed = 0;
            foreach (var go in matches)
            {
                if (go == keep) continue;
                try
                {
                    go.SetActive(false);
                    suppressed++;
                    QuestModPlugin.LogDebugInfo($"EdgeBugDoubleTrobbio: suppressed duplicate '{go.name}' (id={go.GetInstanceID()})");
                }
                catch (System.Exception ex)
                {
                    QuestModPlugin.Log.LogWarning($"EdgeBugDoubleTrobbio: could not deactivate '{go.name}': {ex.Message}");
                }
            }

            if (suppressed > 0)
                QuestModPlugin.Log.LogInfo($"EdgeBugDoubleTrobbio: kept '{keep.name}' (id={keep.GetInstanceID()}), suppressed {suppressed} duplicate(s)");
        }

        private static bool IsTrobbio(GameObject go)
        {
            // Substring-only would match decoration / FX / audio objects whose
            // name happens to contain "Trobbio" (e.g. "TrobbioPoster",
            // "Trobbio_corpse"). Require at least one PlayMakerFSM in the
            // hierarchy so we only consider real NPC instances.
            if (go.name.IndexOf(TrobbioNamePrefix, System.StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            return go.GetComponentInChildren<PlayMakerFSM>(includeInactive: true) != null;
        }

        private static int ScoreForActivity(GameObject go)
        {
            int score = 0;
            var fsms = go.GetComponentsInChildren<PlayMakerFSM>(includeInactive: false);
            foreach (var fsm in fsms)
            {
                if (fsm == null || fsm.Fsm == null) continue;
                score++;
                var stateName = fsm.Fsm.ActiveStateName;
                if (!string.IsNullOrEmpty(stateName) &&
                    stateName.IndexOf("idle", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                    stateName.IndexOf("init", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    score += 2;
                }
            }
            return score;
        }
    }
}
