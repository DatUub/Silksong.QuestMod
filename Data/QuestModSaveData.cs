using System.Collections.Generic;

namespace QuestMod
{
    // CLUSTER H: 3-way All Wishes mode replaces the legacy AllQuestsAvailable bool.
    public enum AllWishesMode
    {
        Disabled = 0,
        Pure = 1,
        Adjusted = 2,
    }

    // CLUSTER D: per-quest availability + auto-accept policy.
    public class QuestPolicy
    {
        public bool Available { get; set; }
        public bool AutoAccept { get; set; }
    }

    public class QuestModSaveData
    {
        public HashSet<string> InjectedQuests { get; set; } = new();
        public HashSet<string> CompletedQuests { get; set; } = new();
        public Dictionary<string, int> QuestTargetOverrides { get; set; } = new();

        // Legacy field â€” kept for back-compat with saves written by < 1.2.0.
        // On load, true is migrated to AllWishesMode.Adjusted (the safer default,
        // matching prior behaviour where chain gating was already respected).
        public bool AllQuestsAvailable { get; set; }

        public AllWishesMode AllWishesMode { get; set; } = AllWishesMode.Disabled;

        public bool AllQuestsAccepted { get; set; }

        // Per-quest availability and auto-accept policies (keyed by internal quest name).
        public Dictionary<string, QuestPolicy> QuestPolicies { get; set; } = new();

        // CLUSTER E: granular prerequisite toggles.
        public GranularPrereqs Prereqs { get; set; } = new();

        // CLUSTER J: Wish Location Reassignment (stretch goal scaffold).
        // See docs/WishLocationReassignment.md. Not yet enforced by any code path.
        public Dictionary<string, string> WishLocationOverrides { get; set; } = new();
        public HashSet<string> WishLocationTriggersFired { get; set; } = new();

        // CLUSTER P: save-safety gate.
        // QuestModInitialized = "this save was created with QuestMod active" or
        // "this save has been used with QuestMod's destructive features before".
        // Set true by the StartNewGame postfix and grandfathered to true on
        // first load when any QuestMod state is non-default. Default false
        // means "legacy save" -- destructive features (NPC spawn flags,
        // granular bypasses, force-accept, force-complete) are gated off.
        public bool QuestModInitialized { get; set; }

        // OverrideSafetyForThisSave = the user clicked through the
        // "I understand the risk" confirm dialog. Persists per-save; once set
        // there is no off button (intentional -- you opted in, you live with
        // it). Unlocks destructive features even on legacy saves.
        public bool OverrideSafetyForThisSave { get; set; }

        // I-1 per-save requirements preset. Cluster I's QuestRequirements presets
        // (vanilla / farmable-only / farmable-quarter / quick) used to be
        // a static runtime choice; now persisted per save so each save can
        // remember which preset the user picked.
        public string ActiveDslPreset { get; set; } = "vanilla";
        // Per save overrides of destructive ConfigEntries. null = inherit
        // the global ConfigEntry value (the default for new saves). Set
        // explicitly when the user adjusts the toggle in the Tools tab while
        // a save is loaded.
        public bool? EnableCustomRequirements { get; set; } = null;
        public bool? EnableFullRemoteComplete { get; set; } = null;
    }

    public class GranularPrereqs
    {
        public bool BypassFleatopia { get; set; }
        public bool BypassMandatoryWishes { get; set; }
        public bool BypassFaydownCloak { get; set; }
        public bool BypassNeedolin { get; set; }
        public bool BypassBonebottomQuestBoard { get; set; }
        // Cluster K-2: non-destructive "all wishwalls always available" toggle.
        // Per hollowknight.wiki, only 3 wishwalls exist in Pharloom (Bone Bottom,
        // Bellhart, Songclave). When this flag is on, the runtime activation
        // patch (ActivateIfPlayerdataTrueStartPatch) force-activates the kiosk
        // GameObjects in any region by name pattern. We deliberately do NOT
        // flip the PD bools that gate them (defeatedBellBeast, visitedBellhartSaved,
        // metCaretaker, bellShrineEnclave) because those would falsely advance
        // story milestones (boss achievements, Act transitions, NPC questlines).
        public bool BypassAllWishwalls { get; set; }
    }
}
