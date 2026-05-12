using System.Collections.Generic;

namespace QuestMod
{
    public enum AllWishesMode
    {
        Disabled = 0,
        Pure = 1,
        Adjusted = 2,
    }

    public class QuestPolicy
    {
        public bool Available { get; set; }
        public bool AutoAccept { get; set; }
    }

    public class QuestModSaveData
    {
        // 0 default so legacy saves are detectable. Bumps on migration.
        public const int CurrentSchemaVersion = 2;
        public int SchemaVersion { get; set; } = 0;

        public HashSet<string> InjectedQuests { get; set; } = new();
        public HashSet<string> CompletedQuests { get; set; } = new();
        public Dictionary<string, int> QuestTargetOverrides { get; set; } = new();

        // Legacy <1.2.0. true migrates to AllWishesMode.Adjusted on load.
        public bool AllQuestsAvailable { get; set; }

        public AllWishesMode AllWishesMode { get; set; } = AllWishesMode.Disabled;

        public bool AllQuestsAccepted { get; set; }

        public Dictionary<string, QuestPolicy> QuestPolicies { get; set; } = new();

        public GranularPrereqs Prereqs { get; set; } = new();

        // Stretch-goal scaffold. Not enforced yet.
        public Dictionary<string, string> WishLocationOverrides { get; set; } = new();
        public HashSet<string> WishLocationTriggersFired { get; set; } = new();

        // Save-safety gate. true = save was created with QuestMod or already
        // used destructive features. Legacy saves stay false until the user
        // opts in via OverrideSafetyForThisSave.
        public bool QuestModInitialized { get; set; }

        // One-way ratchet. Once set, destructive features unlock for this save.
        public bool OverrideSafetyForThisSave { get; set; }

        public string ActiveDslPreset { get; set; } = "vanilla";

        // null = inherit global ConfigEntry. Set when toggled with a save loaded.
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
        // Force-activates all 3 wishwall kiosks by name pattern. No PD writes
        // -- flipping defeatedBellBeast etc. would falsely advance story.
        public bool BypassAllWishwalls { get; set; }
    }
}
