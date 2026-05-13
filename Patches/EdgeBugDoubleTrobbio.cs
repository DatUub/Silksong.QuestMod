// Under AllWishes the wandering Trobbio NPC can end up in the same scene as
// the boss arena. Suppress wandering instances when a boss is present.

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
            if (QuestModConstants.IsTransientMenuScene(scene.name))
                return;
            // Adjusted only. Pure keeps raw chaos.
            if (!QuestModPlugin.IsAdjustedWishes)
                return;

            // After QuestStateHooks (0.5s) so spawn flags settle. Two-shot.
            string queuedScene = scene.name;
            QuestModPlugin.Instance.InvokeAfterSeconds(() => DedupTrobbio(queuedScene), 0.75f);
            QuestModPlugin.Instance.InvokeAfterSeconds(() => DedupTrobbio(queuedScene), 2.25f);
        }

        private static void DedupTrobbio(string queuedScene)
        {
            if (SceneManager.GetActiveScene().name != queuedScene) return;
            // Split so dedup never touches the boss.
            var wandering = new List<GameObject>();
            GameObject boss = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (go == null) continue;
                if (!IsTrobbio(go)) continue;
                if (go.transform.parent != null && IsTrobbio(go.transform.parent.gameObject))
                    continue;

                if (LooksLikeBoss(go))
                {
                    if (boss == null || ScoreForActivity(go) > ScoreForActivity(boss))
                        boss = go;
                }
                else
                {
                    wandering.Add(go);
                }
            }

            if (wandering.Count == 0) return;
            if (boss == null && wandering.Count <= 1) return;

            // Boss present -> suppress all wandering. No boss -> keep most
            // active. Tiebreak on scene root index (InstanceID isn't stable).
            GameObject keep;
            if (boss != null)
            {
                keep = boss;
            }
            else
            {
                keep = wandering[0];
                int keepScore = ScoreForActivity(keep);
                int keepRootIx = SceneRootIndex(keep);
                for (int i = 1; i < wandering.Count; i++)
                {
                    int s = ScoreForActivity(wandering[i]);
                    int rootIx = SceneRootIndex(wandering[i]);
                    if (s > keepScore || (s == keepScore && rootIx < keepRootIx))
                    {
                        keep = wandering[i];
                        keepScore = s;
                        keepRootIx = rootIx;
                    }
                }
            }

            int suppressed = 0;
            foreach (var go in wandering)
            {
                if (go == keep) continue;
                try
                {
                    go.SetActive(false);
                    suppressed++;
                    QuestModPlugin.LogDebugInfo($"EdgeBugDoubleTrobbio: suppressed wandering '{go.name}' (id={go.GetInstanceID()})");
                }
                catch (System.Exception ex)
                {
                    QuestModPlugin.Log.LogWarning($"EdgeBugDoubleTrobbio: could not deactivate '{go.name}': {ex.Message}");
                }
            }

            if (suppressed > 0)
                QuestModPlugin.Log.LogInfo($"EdgeBugDoubleTrobbio: kept '{keep.name}' (id={keep.GetInstanceID()}), suppressed {suppressed} wandering duplicate(s)");
        }

        private static bool IsTrobbio(GameObject go)
        {
            if (go.name.IndexOf(TrobbioNamePrefix, System.StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            return go.GetComponentInChildren<PlayMakerFSM>(includeInactive: true) != null;
        }

        // Narrow hints -- wandering Trobbios have Death/Hit/Attack states too.
        private static readonly string[] BossFsmHints = new[]
        {
            "phase", "hit_state", "boss",
        };
        private static bool LooksLikeBoss(GameObject go)
        {
            var fsms = go.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
            foreach (var fsm in fsms)
            {
                if (fsm == null) continue;
                var fsmName = fsm.FsmName ?? string.Empty;
                foreach (var hint in BossFsmHints)
                    if (fsmName.IndexOf(hint, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                if (fsm.Fsm != null && fsm.Fsm.States != null)
                {
                    foreach (var state in fsm.Fsm.States)
                    {
                        var sn = state?.Name;
                        if (string.IsNullOrEmpty(sn)) continue;
                        foreach (var hint in BossFsmHints)
                            if (sn.IndexOf(hint, System.StringComparison.OrdinalIgnoreCase) >= 0)
                                return true;
                    }
                }
            }
            return false;
        }

        private static int SceneRootIndex(GameObject go)
        {
            var t = go.transform;
            while (t.parent != null) t = t.parent;
            return t.GetSiblingIndex();
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
