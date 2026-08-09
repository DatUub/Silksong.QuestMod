using HarmonyLib;
using Silksong.UnityHelper.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuestMod
{
    // Wishboard access bypass. ActivateIfPlayerdataTrue.Start() is a no-op
    // when its PD bool is false. Postfix forces the activations when the
    // bypass toggle is on. Pure runtime -- no PD writes.
    [HarmonyPatch(typeof(ActivateIfPlayerdataTrue), "Start")]
    public static class ActivateIfPlayerdataTrueStartPatch
    {
        internal const string WishboardBoolName = "bonebottomQuestBoardFixed";

        // Heuristic match for any wishwall gate -- catches Bellhart + Songclave
        // variants without hardcoding bool names we can't enumerate offline.
        private static bool LooksLikeWishwallGate(ActivateIfPlayerdataTrue inst)
        {
            if (inst == null) return false;
            string boolName = inst.boolName ?? "";
            string goName = inst.gameObject != null ? inst.gameObject.name : "";
            string targetName = inst.objectToActivate != null ? inst.objectToActivate.name : "";
            return WishboardSceneSweep.NameLooksLikeWishwall(goName, targetName)
                || string.Equals(boolName, WishboardBoolName, System.StringComparison.Ordinal);
        }

        internal static bool IsExcludedWishwall(string name)
            => WishboardSceneSweep.IsExcludedWishwallName(name);

        public static void Postfix(ActivateIfPlayerdataTrue __instance)
        {
            bool bonebottomOnly = QuestModPlugin.BypassWishboardLock?.Value == true;
            bool allWishwalls = QuestModPlugin.Instance?.SaveData?.Prereqs?.BypassAllWishwalls == true;
            if (!bonebottomOnly && !allWishwalls) return;
            if (__instance == null) return;
            var go = __instance.gameObject;
            if (go == null) return;

            if (bonebottomOnly && !allWishwalls
                && !string.Equals(__instance.boolName, WishboardBoolName, System.StringComparison.Ordinal))
                return;

            if (allWishwalls)
            {
                if (!LooksLikeWishwallGate(__instance)) return;
                string targetName = __instance.objectToActivate != null
                    ? __instance.objectToActivate.name : "";
                if (IsExcludedWishwall(targetName) || IsExcludedWishwall(go.name))
                    return;
            }

            if (!__instance.gameObject.activeSelf)
                __instance.gameObject.SetActive(true);

            if (__instance.objectToActivate != null && !__instance.objectToActivate.activeSelf)
                __instance.objectToActivate.SetActive(true);

            QuestModPlugin.LogDebugInfo(
                $"WishwallBypass: forced activation on '{__instance.gameObject.name}' " +
                $"(boolName='{__instance.boolName}', objectToActivate='{(__instance.objectToActivate != null ? __instance.objectToActivate.name : "null")}')");
        }
    }

    // Start() fires once. If the user toggles bypass mid-run, dormant gates
    // already loaded never re-run. Re-sweep on every scene load. Also walks
    // wishwall ROOT GameObjects since some interactable children are gated
    // by an FSM action elsewhere, not ActivateIfPlayerdataTrue.
    public static class WishboardSceneSweep
    {
        private static readonly string[] WishwallGoNamePatterns = new[]
        {
            "Quest_Board",
            "Quest Board",
            "Wishwall",
            "Wish_Wall",
            "Wish Wall",
            "QuestBoard",
        };

        // Skip pre-build, in-progress, and post-game variants. Keeps us from
        // lighting up "covered" and "uncovered" siblings simultaneously.
        private static readonly string[] WishwallExcludePatterns = new[]
        {
            // Damage / story-end variants
            "broken", "destroyed", "damaged", "ruined",
            "skull king", "skullking",
            // Pre-progress / not-yet-built variants
            "covered", "unlit", "pre_", "_pre",
            "inactive", "off_state",
            // Builder-in-progress variants
            "scaffold", "construction",
        };

        internal static bool NameLooksLikeWishwall(params string[] names)
        {
            if (names == null) return false;
            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name)) continue;
                foreach (var p in WishwallGoNamePatterns)
                {
                    if (name.IndexOf(p, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            return false;
        }

        internal static bool IsExcludedWishwallName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string l = name.ToLowerInvariant();
            foreach (var ex in WishwallExcludePatterns)
                if (l.Contains(ex)) return true;
            return false;
        }

        private static bool IsBonebottomQuestBoard(params string[] names)
        {
            if (names == null) return false;
            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name)) continue;
                if (name.IndexOf("bonebottom_quest_board", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            QuestModPlugin.Log.LogInfo("WishboardSceneSweep: registered sceneLoaded hook");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (QuestModConstants.IsTransientMenuScene(scene.name))
                return;

            // Two-shot after scene Start()s settle. Stale sweeps bail.
            string queuedScene = scene.name;
            QuestModPlugin.Instance.InvokeAfterSeconds(() => SweepOnce(queuedScene), 0.5f);
            QuestModPlugin.Instance.InvokeAfterSeconds(() => SweepOnce(queuedScene), 2f);
        }

        private static void SweepOnce(string queuedScene)
        {
            if (SceneManager.GetActiveScene().name != queuedScene) return;
            bool bonebottomOnly = QuestModPlugin.BypassWishboardLock?.Value == true;
            bool allWishwalls = QuestModPlugin.Instance?.SaveData?.Prereqs?.BypassAllWishwalls == true;
            if (!bonebottomOnly && !allWishwalls) return;

            int forced = SweepActivateIfPlayerdataTrue(bonebottomOnly, allWishwalls);
            int forcedTga = SweepTestGameObjectActivator(bonebottomOnly, allWishwalls);

            if (forced > 0 || forcedTga > 0)
                QuestModPlugin.Log.LogInfo(
                    $"WishboardSceneSweep: activated {forced} ActivateIfPDTrue + {forcedTga} TestGameObjectActivator gate(s)");

            // Second pass: recursively activate descendants of wishwall ROOTs.
            // Catches children SetActive(false) at scene-build time.
            int activatedGos = 0;
            if (allWishwalls)
            {
                var transforms = Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                // Dedupe roots+descendants so name-matching children don't get walked twice.
                var visited = new System.Collections.Generic.HashSet<int>();
                foreach (var t in transforms)
                {
                    if (t == null || t.gameObject == null) continue;
                    var go = t.gameObject;
                    if (!go.scene.IsValid() || go.scene.name == "DontDestroyOnLoad") continue;

                    string name = go.name ?? "";
                    if (!NameLooksLikeWishwall(name)) continue;
                    if (IsExcludedWishwallName(name)) continue;
                    // ForceActivateRecursive handles dedupe; don't pre-add the root.
                    activatedGos += ForceActivateRecursive(t, visited);
                }
            }
            if (activatedGos > 0)
                QuestModPlugin.Log.LogInfo($"WishboardSceneSweep: force-activated {activatedGos} wishwall descendant GameObject(s)");
        }

        private static int SweepActivateIfPlayerdataTrue(bool bonebottomOnly, bool allWishwalls)
        {
            int forced = 0;
            foreach (var gate in Object.FindObjectsByType<ActivateIfPlayerdataTrue>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (gate == null || gate.gameObject == null) continue;

                string goName = gate.gameObject.name ?? "";
                string targetName = gate.objectToActivate != null ? gate.objectToActivate.name : "";

                bool match = false;
                if (bonebottomOnly && string.Equals(gate.boolName,
                        ActivateIfPlayerdataTrueStartPatch.WishboardBoolName,
                        System.StringComparison.Ordinal))
                    match = true;
                if (allWishwalls && NameLooksLikeWishwall(goName, targetName))
                    match = true;
                if (!match) continue;

                if (allWishwalls && (IsExcludedWishwallName(targetName) || IsExcludedWishwallName(goName)))
                    continue;

                bool acted = false;
                if (!gate.gameObject.activeSelf)
                {
                    gate.gameObject.SetActive(true);
                    acted = true;
                }
                if (gate.objectToActivate != null && !gate.objectToActivate.activeSelf)
                {
                    gate.objectToActivate.SetActive(true);
                    acted = true;
                }

                if (acted)
                {
                    forced++;
                    QuestModPlugin.LogDebugInfo(
                        $"WishboardSceneSweep: forced '{goName}' (objectToActivate='{targetName}')");
                }
            }
            return forced;
        }

        // Sibling MB type: Bonetown wishboard uses TestGameObjectActivator with a
        // (defeatedBellBeast OR visitedShellwood) PlayerDataTest — not ActivateIfPDTrue.
        private static int SweepTestGameObjectActivator(bool bonebottomOnly, bool allWishwalls)
        {
            int forcedTga = 0;
            foreach (var ta in Object.FindObjectsByType<TestGameObjectActivator>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (ta == null || ta.gameObject == null) continue;
                string goName = ta.gameObject.name ?? "";
                string actName = ta.activateGameObject != null ? ta.activateGameObject.name : "";
                string deactName = ta.deactivateGameObject != null ? ta.deactivateGameObject.name : "";

                if (!NameLooksLikeWishwall(goName, actName, deactName)) continue;
                if (IsExcludedWishwallName(goName) || IsExcludedWishwallName(actName)) continue;

                // Bonebottom-only: only the Bonetown board (match go or activate target).
                if (bonebottomOnly && !allWishwalls
                    && !IsBonebottomQuestBoard(goName, actName))
                    continue;

                bool actedTga = false;
                if (ta.activateGameObject != null && !ta.activateGameObject.activeSelf)
                {
                    ta.activateGameObject.SetActive(true);
                    actedTga = true;
                }
                if (ta.deactivateGameObject != null && ta.deactivateGameObject.activeSelf)
                {
                    ta.deactivateGameObject.SetActive(false);
                    actedTga = true;
                }
                if (actedTga)
                {
                    forcedTga++;
                    QuestModPlugin.LogDebugInfo(
                        $"WishboardSceneSweep: forced TGA on '{goName}' " +
                        $"(activate='{actName}', deactivate='{deactName}')");
                }
            }
            return forcedTga;
        }

        private static int ForceActivateRecursive(Transform root,
            System.Collections.Generic.HashSet<int> visited)
        {
            int activated = 0;
            if (root == null || root.gameObject == null) return activated;
            if (!visited.Add(root.gameObject.GetInstanceID())) return activated;

            // Self gated by exclude; children always walked so healthy
            // descendants of an excluded parent still reach.
            string n = root.gameObject.name ?? "";
            if (!IsExcludedWishwallName(n))
            {
                if (!root.gameObject.activeSelf)
                {
                    root.gameObject.SetActive(true);
                    activated++;
                    QuestModPlugin.LogDebugInfo(
                        $"WishboardSceneSweep: force-activated '{n}' (scene={root.gameObject.scene.name})");
                }
            }
            int childCount = root.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var c = root.GetChild(i);
                if (c == null) continue;
                activated += ForceActivateRecursive(c, visited);
            }
            return activated;
        }
    }
}
