using HarmonyLib;
using Silksong.UnityHelper.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuestMod
{
    // Cluster K — Wishboard access bypass (runtime-only, no PlayerData mutation).
    //
    // Problem:
    //   The Bonetown assembled quest board is gated by an `ActivateIfPlayerdataTrue`
    //   component bound to the PD bool `bonebottomQuestBoardFixed`. Without defeating
    //   the Bell Beast, the bool stays false, the component's Start() is a no-op, and
    //   the board's `objectToActivate` (the interactable child) is never enabled.
    //
    // Why not flip the PD bool?
    //   The previous antigravity attempt added `bonebottomQuestBoardFixed` to
    //   NpcSpawnFlags + tried to reparent the board GameObject. That softlocked
    //   (duplicate NPCs, broken scene state) AND persisted to save data even after
    //   the user disabled the toggle. We avoid both by leaving PlayerData untouched
    //   and intercepting the activation check at runtime.
    //
    // Approach (b) per cluster brief:
    //   Postfix `ActivateIfPlayerdataTrue.Start()` — when our config bypass is on
    //   and the bound bool is the wishboard one, force the same activations the
    //   real branch performs. No GameObject reparenting, no PD writes.
    //
    // Confirmed against Assembly-CSharp.dll (Silksong 1.x):
    //   public class ActivateIfPlayerdataTrue : MonoBehaviour {
    //     public string boolName;
    //     public GameObject objectToActivate;
    //     private void Start() {
    //       if (pd.GetBool(boolName)) {
    //         base.gameObject.SetActive(true);
    //         if ((bool)objectToActivate) objectToActivate.SetActive(true);
    //       }
    //     }
    //   }
    //   Type lives in the global namespace.
    [HarmonyPatch(typeof(ActivateIfPlayerdataTrue), "Start")]
    public static class ActivateIfPlayerdataTrueStartPatch
    {
        internal const string WishboardBoolName = "bonebottomQuestBoardFixed";

        // Cluster K-2: heuristic match for any wishwall-related gate. Catches
        // Bellhart's covered-wall reveal + Songclave's wishwall activation
        // without hardcoding bool names we can't enumerate offline.
        // GameObject names like "Wishwall_Bone_Bottom", "Wishwall_Bellhart_Covered",
        // and Songclave's wishwall variant are public per hollowknight.wiki.
        private static bool LooksLikeWishwallGate(ActivateIfPlayerdataTrue inst)
        {
            if (inst == null) return false;
            string boolName = inst.boolName ?? "";
            string goName = inst.gameObject != null ? inst.gameObject.name : "";
            string targetName = inst.objectToActivate != null ? inst.objectToActivate.name : "";
            return goName.IndexOf("wishwall", System.StringComparison.OrdinalIgnoreCase) >= 0
                || goName.IndexOf("wish_wall", System.StringComparison.OrdinalIgnoreCase) >= 0
                || goName.IndexOf("questboard", System.StringComparison.OrdinalIgnoreCase) >= 0
                || goName.IndexOf("quest_board", System.StringComparison.OrdinalIgnoreCase) >= 0
                || targetName.IndexOf("wishwall", System.StringComparison.OrdinalIgnoreCase) >= 0
                || targetName.IndexOf("wish_wall", System.StringComparison.OrdinalIgnoreCase) >= 0
                || targetName.IndexOf("questboard", System.StringComparison.OrdinalIgnoreCase) >= 0
                || targetName.IndexOf("quest_board", System.StringComparison.OrdinalIgnoreCase) >= 0
                || string.Equals(boolName, WishboardBoolName, System.StringComparison.Ordinal);
        }

        // Names we should NEVER force-activate even if they match wishwall patterns:
        // post-game / damaged / story-progressed states whose appearance would
        // mis-tell the player about world state (e.g. "broken by skull king").
        internal static readonly string[] WishwallExcludeSubstrings = new[]
        {
            "broken", "destroyed", "damaged", "ruined",
            "skull king", "skullking",
        };

        internal static bool IsExcludedWishwall(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string l = name.ToLowerInvariant();
            foreach (var ex in WishwallExcludeSubstrings)
                if (l.Contains(ex)) return true;
            return false;
        }

        public static void Postfix(ActivateIfPlayerdataTrue __instance)
        {
            // Bonebottom-only legacy toggle still respected.
            bool bonebottomOnly = QuestModPlugin.BypassWishboardLock?.Value == true;
            // Per-save blanket toggle covers all 3 wishwalls.
            bool allWishwalls = QuestModPlugin.Instance?.SaveData?.Prereqs?.BypassAllWishwalls == true;
            if (!bonebottomOnly && !allWishwalls) return;
            if (__instance == null) return;

            // Bonebottom-only path keeps the strict bool-name match for back compat.
            if (bonebottomOnly && !allWishwalls
                && !string.Equals(__instance.boolName, WishboardBoolName, System.StringComparison.Ordinal))
                return;

            // BypassAllWishwalls uses the broader heuristic so Bellhart + Songclave
            // gates (different bool names) also activate. Skip post-game / broken
            // variants to avoid mis-telling story state.
            if (allWishwalls)
            {
                if (!LooksLikeWishwallGate(__instance)) return;
                string targetName = __instance.objectToActivate != null
                    ? __instance.objectToActivate.name : "";
                if (IsExcludedWishwall(targetName) || IsExcludedWishwall(__instance.gameObject.name))
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

    // Scene-load sweep:
    //   Start() only fires once per component instance. If the user toggles the
    //   bypass mid-run, components in already-loaded scenes have already executed
    //   their Start with the bool false and are now permanently dormant. We sweep
    //   for any matching ActivateIfPlayerdataTrue whose target is still inactive
    //   and force-activate it without touching PlayerData. Same on every scene
    //   load so re-entries also work.
    //
    //   We ALSO sweep for inactive GameObjects matching wishwall name patterns
    //   directly — Silksong's actual interactable Quest_Board / Quest Board Pivot
    //   children of bonebottom_quest_board are gated by something other than
    //   ActivateIfPlayerdataTrue (likely an FSM action elsewhere). Force-activating
    //   them by name pattern bypasses that gate without PD writes.
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

        // Names we should NEVER force-activate even if they match wishwall patterns:
        // post-game / damaged / story-progressed states whose appearance would
        // mis-tell the player about world state. Includes both forward-progress
        // variants (covered, unlit, pre-built) and post-progress damaged
        // variants. The wishwall ROOT is force-activated, but children matching
        // any of these substrings are skipped so we don't simultaneously light
        // up "covered wall" and "uncovered wall" siblings.
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

        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            QuestModPlugin.Log.LogInfo("WishboardSceneSweep: registered sceneLoaded hook");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(scene.name) || scene.name == "Menu_Title")
                return;

            // Run shortly after load so the scene's own Start()s have settled.
            QuestModPlugin.Instance.InvokeAfterSeconds(SweepOnce, 0.5f);
            QuestModPlugin.Instance.InvokeAfterSeconds(SweepOnce, 2f);
        }

        private static void SweepOnce()
        {
            bool bonebottomOnly = QuestModPlugin.BypassWishboardLock?.Value == true;
            bool allWishwalls = QuestModPlugin.Instance?.SaveData?.Prereqs?.BypassAllWishwalls == true;
            if (!bonebottomOnly && !allWishwalls) return;

            var gates = Object.FindObjectsByType<ActivateIfPlayerdataTrue>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int forced = 0;
            foreach (var gate in gates)
            {
                if (gate == null) continue;
                bool match = false;
                if (bonebottomOnly && string.Equals(gate.boolName,
                        ActivateIfPlayerdataTrueStartPatch.WishboardBoolName,
                        System.StringComparison.Ordinal))
                    match = true;
                if (allWishwalls)
                {
                    string goName = gate.gameObject != null ? gate.gameObject.name : "";
                    string targetName = gate.objectToActivate != null ? gate.objectToActivate.name : "";
                    if (goName.IndexOf("wishwall", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || goName.IndexOf("wish_wall", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || goName.IndexOf("questboard", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || goName.IndexOf("quest_board", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || targetName.IndexOf("wishwall", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || targetName.IndexOf("wish_wall", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || targetName.IndexOf("questboard", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || targetName.IndexOf("quest_board", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        match = true;
                }
                if (!match) continue;

                // Don't activate broken / post-game variants.
                string targetName2 = gate.objectToActivate != null ? gate.objectToActivate.name : "";
                if (allWishwalls && (ActivateIfPlayerdataTrueStartPatch.IsExcludedWishwall(targetName2)
                    || ActivateIfPlayerdataTrueStartPatch.IsExcludedWishwall(gate.gameObject.name)))
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
                        $"WishboardSceneSweep: forced '{gate.gameObject.name}' " +
                        $"(objectToActivate='{(gate.objectToActivate != null ? gate.objectToActivate.name : "null")}')");
                }
            }

            if (forced > 0)
                QuestModPlugin.Log.LogInfo($"WishboardSceneSweep: activated {forced} dormant wishboard gate(s)");

            // Second pass: find every wishwall ROOT GameObject (parents like
            // bonebottom_quest_board) and recursively activate ALL descendants
            // except excluded names. This catches Quest_Board / Quest Board
            // Pivot / etc. that are children of an active parent but were
            // explicitly SetActive(false) at scene-build time.
            int activatedGos = 0;
            if (allWishwalls)
            {
                var transforms = Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                // visited tracks every GameObject we've already touched, both
                // wishwall ROOTS we've started recursion from and DESCENDANTS
                // already activated. Without this, a child whose name also
                // matches a wishwall pattern (e.g. parent "bonebottom_quest_board"
                // and child "Quest_Board" both match) would be visited twice
                // by the outer loop, doubling the recursion work.
                var visited = new System.Collections.Generic.HashSet<int>();
                foreach (var t in transforms)
                {
                    if (t == null || t.gameObject == null) continue;
                    var go = t.gameObject;
                    if (!go.scene.IsValid() || go.scene.name == "DontDestroyOnLoad") continue;

                    string name = go.name ?? "";
                    bool nameMatch = false;
                    foreach (var p in WishwallGoNamePatterns)
                        if (name.IndexOf(p, System.StringComparison.OrdinalIgnoreCase) >= 0) { nameMatch = true; break; }
                    if (!nameMatch) continue;
                    if (ActivateIfPlayerdataTrueStartPatch.IsExcludedWishwall(name)) continue;
                    // ForceActivateRecursive handles the visited dedupe itself
                    // (including for the root). Don't pre-add here or the
                    // recursive call will see the root as already-visited and
                    // bail on the first line, never touching descendants.
                    activatedGos += ForceActivateRecursive(t, visited);
                }
            }
            if (activatedGos > 0)
                QuestModPlugin.Log.LogInfo($"WishboardSceneSweep: force-activated {activatedGos} wishwall descendant GameObject(s)");
        }

        private static int ForceActivateRecursive(Transform root,
            System.Collections.Generic.HashSet<int> visited)
        {
            int activated = 0;
            if (root == null || root.gameObject == null) return activated;
            if (!visited.Add(root.gameObject.GetInstanceID())) return activated;

            // Activate self first if needed (excluded names skipped).
            string n = root.gameObject.name ?? "";
            if (!ActivateIfPlayerdataTrueStartPatch.IsExcludedWishwall(n))
            {
                if (!root.gameObject.activeSelf)
                {
                    root.gameObject.SetActive(true);
                    activated++;
                    QuestModPlugin.LogDebugInfo(
                        $"WishboardSceneSweep: force-activated '{n}' (scene={root.gameObject.scene.name})");
                }
                // Walk children
                int childCount = root.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var c = root.GetChild(i);
                    if (c == null) continue;
                    activated += ForceActivateRecursive(c, visited);
                }
            }
            return activated;
        }
    }
}
