using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Newtonsoft.Json;
using Silksong.DataManager;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace QuestMod
{
    [BepInAutoPlugin(id: "com.silkmod.questmod")]
    [BepInDependency("org.silksong-modding.fsmutil")]
    [BepInDependency("org.silksong-modding.datamanager")]
    [BepInDependency("org.silksong-modding.unityhelper")]
    public partial class QuestModPlugin : BaseUnityPlugin, ISaveDataMod<QuestModSaveData>
    {
        internal static ManualLogSource Log { get; private set; } = null!;
        internal static QuestModPlugin Instance { get; private set; } = null!;

        private QuestModSaveData _saveData = new();
        public QuestModSaveData? SaveData
        {
            get => _saveData;
            set
            {
                _saveData = value ?? new();
                // CLUSTER H: migrate legacy AllQuestsAvailable bool to AllWishesMode enum.
                // If the new field is at its default but the legacy bool is true,
                // upgrade to Adjusted (the safer mode - chain gating respected).
                if (_saveData.AllWishesMode == AllWishesMode.Disabled && _saveData.AllQuestsAvailable)
                    _saveData.AllWishesMode = AllWishesMode.Adjusted;

                // CLUSTER D: ensure per-quest policy dict exists; migrate legacy bool below.
                if (_saveData.QuestPolicies == null)
                    _saveData.QuestPolicies = new System.Collections.Generic.Dictionary<string, QuestPolicy>();

                WishesMode = _saveData.AllWishesMode;
                AllQuestsAccepted = _saveData.AllQuestsAccepted;
                MigrateLegacyAvailability();

                // CLUSTER P: grandfather existing QuestMod users.
                // If this save has any non-default QuestMod state (mode set,
                // accept toggle on, per-quest policies, granular bypasses),
                // the user has been using destructive features on this save
                // already. Treat as QuestMod-aware so the safety gate doesn't
                // suddenly lock them out.
                if (!_saveData.QuestModInitialized && HasAnyQuestModState(_saveData))
                {
                    _saveData.QuestModInitialized = true;
                    Log?.LogInfo("Grandfathered existing QuestMod state on this save -> QuestModInitialized=true");
                }

                // I-1: restore the per-save requirements preset. Skip if the
                // rules haven't loaded yet (Awake order); the initial Load()
                // picks up the saved value via the same path.
                if (QuestRequirements.IsLoaded && !string.IsNullOrEmpty(_saveData.ActiveDslPreset))
                    QuestRequirements.SetActivePreset(_saveData.ActiveDslPreset);

                Log?.LogInfo($"SaveData loaded: WishesMode={WishesMode}, Accepted={AllQuestsAccepted}, Policies={_saveData.QuestPolicies.Count}, Initialized={_saveData.QuestModInitialized}, Override={_saveData.OverrideSafetyForThisSave}, DslPreset={_saveData.ActiveDslPreset}");
            }
        }

        // Backwards compat: when migrating from the legacy AllQuestsAvailable
        // bool, the cluster H setter above already promoted WishesMode to
        // Adjusted. Adjusted is the global "all wishes available" mode, so
        // there's no need to ALSO seed per-quest Available=true policies for
        // every quest in the registry. Doing that would surprise the user
        // (every per-quest toggle in the Quests tab would read as on, even
        // ones they never opted into) and would also flip
        // QuestModInitialized=true via the grandfather path even if the user
        // had only ever used the legacy global toggle. Leaving QuestPolicies
        // empty means the user can disable Adjusted and get vanilla back
        // cleanly.
        private void MigrateLegacyAvailability()
        {
            // Intentionally empty. The cluster H mode promotion in the SaveData
            // setter handles legacy AllQuestsAvailable=true on its own.
        }

        void IRawSaveDataMod.ReadSaveData(Stream saveFile)
        {
            if (saveFile == null)
            {
                SaveData = null;
                return;
            }

            try
            {
                // Plain typed deserialize. We do NOT enable TypeNameHandling
                // here because the model is concrete and we never emit $type
                // on the write side; allowing $type on read would let any
                // file with edit access to the save embed a deserialization
                // gadget that runs arbitrary code at game launch.
                using var sr = new StreamReader(saveFile);
                using var reader = new JsonTextReader(sr);
                var ser = JsonSerializer.CreateDefault();
                SaveData = ser.Deserialize<QuestModSaveData>(reader);
            }
            catch (System.Exception ex)
            {
                Log.LogWarning($"Failed to deserialize save data (old format?), resetting: {ex.Message}");
                SaveData = new QuestModSaveData();
            }
        }

        // CLUSTER H: WishesMode is the canonical state. AllQuestsAvailable is a
        // derived bool kept for back-compat with code that only cared "is the
        // bypass on?" â€” true for both Pure and Adjusted.
        public static AllWishesMode WishesMode { get; private set; } = AllWishesMode.Disabled;
        public static bool AllQuestsAvailable => WishesMode != AllWishesMode.Disabled;
        public static bool IsPureWishes => WishesMode == AllWishesMode.Pure;
        public static bool IsAdjustedWishes => WishesMode == AllWishesMode.Adjusted;
        // Cluster Y / per save migration: runtime view of the destructive
        // ConfigEntries. Reads the per save SaveData override when set;
        // otherwise falls back to the global ConfigEntry. Setter writes to
        // SaveData when a save is loaded, else writes to the ConfigEntry
        // (so the user setting the value with no save loaded sets the
        // default for new saves).
        public static bool IsCustomRequirementsEnabled
        {
            get
            {
                var sd = Instance?.SaveData;
                if (sd != null && sd.EnableCustomRequirements.HasValue)
                    return sd.EnableCustomRequirements.Value;
                return EnableCustomRequirements?.Value ?? true;
            }
            set
            {
                var sd = Instance?.SaveData;
                if (sd != null) sd.EnableCustomRequirements = value;
                else if (EnableCustomRequirements != null) EnableCustomRequirements.Value = value;
            }
        }
        public static bool IsFullRemoteCompleteEnabled
        {
            get
            {
                var sd = Instance?.SaveData;
                if (sd != null && sd.EnableFullRemoteComplete.HasValue)
                    return sd.EnableFullRemoteComplete.Value;
                return EnableFullRemoteComplete?.Value ?? false;
            }
            set
            {
                var sd = Instance?.SaveData;
                if (sd != null) sd.EnableFullRemoteComplete = value;
                else if (EnableFullRemoteComplete != null) EnableFullRemoteComplete.Value = value;
            }
        }
        public static bool AllQuestsAccepted { get; private set; }

        // CLUSTER P: save-safety gate.
        // IsQuestModSave  = save was created with QuestMod active OR has been
        //                   used with QuestMod's destructive features before.
        // IsSafetyOverridden = user clicked through the "I understand the risk"
        //                   confirm. Persists per-save, no off button.
        // AreDestructiveFeaturesAllowed = the gate every PD-mutating code path
        //                   reads to decide whether to fire.
        public static bool IsQuestModSave => Instance?.SaveData?.QuestModInitialized ?? false;
        public static bool IsSafetyOverridden => Instance?.SaveData?.OverrideSafetyForThisSave ?? false;
        public static bool AreDestructiveFeaturesAllowed => IsQuestModSave || IsSafetyOverridden;

        private static bool HasAnyQuestModState(QuestModSaveData d)
        {
            if (d == null) return false;
            if (d.AllWishesMode != AllWishesMode.Disabled) return true;
            if (d.AllQuestsAccepted) return true;
            if (d.AllQuestsAvailable) return true;
            if (d.QuestPolicies != null && d.QuestPolicies.Count > 0) return true;
            if (d.InjectedQuests != null && d.InjectedQuests.Count > 0) return true;
            if (d.CompletedQuests != null && d.CompletedQuests.Count > 0) return true;
            if (d.QuestTargetOverrides != null && d.QuestTargetOverrides.Count > 0) return true;
            if (d.WishLocationOverrides != null && d.WishLocationOverrides.Count > 0) return true;
            var p = d.Prereqs;
            if (p != null && (p.BypassFleatopia || p.BypassMandatoryWishes
                || p.BypassFaydownCloak || p.BypassNeedolin || p.BypassBonebottomQuestBoard))
                return true;
            return false;
        }

        public static void SetSafetyOverride(bool value)
        {
            var data = Instance?.SaveData;
            if (data == null) return;
            data.OverrideSafetyForThisSave = value;
            Log?.LogInfo($"SetSafetyOverride: {value}");
        }

        // I-5: surfaced via GUI hint when this fires for a mode that turns
        // gates ON or OFF mid-playthrough. Tracks the last logged change so
        // the GUI can show a transient warning. Set by SetWishesMode below.
        public static AllWishesMode LastWishesModeChange { get; private set; } = AllWishesMode.Disabled;
        public static float LastWishesModeChangeRealtime { get; private set; } = -1f;

        public static void SetWishesMode(AllWishesMode mode)
        {
            var prev = WishesMode;
            WishesMode = mode;
            SyncToSaveData();
            if (prev != mode)
            {
                LastWishesModeChange = mode;
                LastWishesModeChangeRealtime = Time.realtimeSinceStartup;
                Log?.LogInfo($"WishesMode changed: {prev} -> {mode}. State already in flight (FSM patches, accepted quests, NPC spawn flags) is NOT rewound; only future scene loads see the new mode.");
                QuestModToast.Show($"All Wishes Mode: {prev} -> {mode}");
            }
        }

        // Back-compat shim: legacy callers (and the GUI's existing checkbox) used a bool.
        public static void SetAllQuestsAvailable(bool value)
        {
            WishesMode = value ? AllWishesMode.Adjusted : AllWishesMode.Disabled;
            SyncToSaveData();
        }

        public static void SetAllQuestsAccepted(bool value)
        {
            AllQuestsAccepted = value;
            if (value && WishesMode == AllWishesMode.Disabled)
                WishesMode = AllWishesMode.Adjusted;
            SyncToSaveData();
        }

        // CLUSTER E: granular prerequisite bypass accessor. Returns the live
        // GranularPrereqs object on the save data so the GUI can mutate fields
        // directly; null when no save is loaded.
        public static GranularPrereqs? Prereqs
        {
            get
            {
                var data = Instance?.SaveData;
                if (data == null) return null;
                if (data.Prereqs == null) data.Prereqs = new GranularPrereqs();
                return data.Prereqs;
            }
        }

                // Export the current per save QuestModSaveData to a JSON string.
        // Returns null if no save is loaded. Used by the Tools tab clipboard
        // export and by the smoke roundtrip assertion.
        public static string ExportSaveDataToJson()
        {
            if (Instance?.SaveData == null) return null;
            return JsonConvert.SerializeObject(Instance.SaveData, Formatting.Indented);
        }
        // Import a JSON string into the current save's QuestModSaveData.
        // Replaces the live SaveData (which fires the setter and propagates
        // to the runtime via SyncFromSaveData). Throws on parse failure so
        // the caller can surface the error.
        //
        // Safety: the imported JSON cannot elevate the save-safety gate.
        // The current values of QuestModInitialized and OverrideSafetyForThisSave
        // are preserved from the live save before the import lands. Otherwise
        // a shared "preset" JSON could flip both to true and bypass cluster P
        // entirely on a legacy save.
        public static void ImportSaveDataFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new System.ArgumentException("empty json");
            if (Instance == null) throw new System.InvalidOperationException("plugin not initialized");
            var imported = JsonConvert.DeserializeObject<QuestModSaveData>(json);
            if (imported == null) throw new System.ArgumentException("parsed json was null");

            // Preserve the gate fields from the live save. Imports cannot
            // elevate safety, only the user clicking the override-with-confirm
            // in the Tools tab can.
            var live = Instance.SaveData;
            imported.QuestModInitialized = live?.QuestModInitialized ?? false;
            imported.OverrideSafetyForThisSave = live?.OverrideSafetyForThisSave ?? false;

            // Validate the imported state before applying. Out-of-range enum
            // values, oversized collections, and weird key chars all get
            // rejected so a malformed paste cannot cause downstream NRE
            // chains or hang scene loads with thousands of overrides.
            if ((int)imported.AllWishesMode < 0 || (int)imported.AllWishesMode > 2)
                throw new System.ArgumentException($"invalid AllWishesMode {imported.AllWishesMode}");
            const int maxEntries = 5000;
            if (imported.QuestPolicies != null && imported.QuestPolicies.Count > maxEntries)
                throw new System.ArgumentException($"QuestPolicies too large ({imported.QuestPolicies.Count} > {maxEntries})");
            if (imported.InjectedQuests != null && imported.InjectedQuests.Count > maxEntries)
                throw new System.ArgumentException($"InjectedQuests too large ({imported.InjectedQuests.Count} > {maxEntries})");
            if (imported.CompletedQuests != null && imported.CompletedQuests.Count > maxEntries)
                throw new System.ArgumentException($"CompletedQuests too large ({imported.CompletedQuests.Count} > {maxEntries})");
            if (imported.QuestTargetOverrides != null && imported.QuestTargetOverrides.Count > maxEntries)
                throw new System.ArgumentException($"QuestTargetOverrides too large ({imported.QuestTargetOverrides.Count} > {maxEntries})");
            if (imported.WishLocationOverrides != null && imported.WishLocationOverrides.Count > maxEntries)
                throw new System.ArgumentException($"WishLocationOverrides too large ({imported.WishLocationOverrides.Count} > {maxEntries})");

            Instance.SaveData = imported;
            Log.LogInfo("ImportSaveDataFromJson: applied imported save state (safety gate preserved from live save)");
        }
        internal static void SyncFromSaveData()
        {
            var data = Instance.SaveData;
            if (data == null) return;
            WishesMode = data.AllWishesMode;
            AllQuestsAccepted = data.AllQuestsAccepted;
            if (data.Prereqs == null) data.Prereqs = new GranularPrereqs();
            LogDebugInfo($"SyncFromSave: WishesMode={WishesMode}, Accepted={AllQuestsAccepted}");
        }

        private static void SyncToSaveData()
        {
            if (Instance == null) return;
            var data = Instance.SaveData;
            if (data == null) return;
            data.AllWishesMode = WishesMode;
            // Keep legacy bool in sync so older builds reading the file still work.
            data.AllQuestsAvailable = WishesMode != AllWishesMode.Disabled;
            data.AllQuestsAccepted = AllQuestsAccepted;
            // CLUSTER P note: SyncToSaveData deliberately does NOT touch
            // QuestModInitialized. Auto-promoting on any state change would
            // defeat the safety gate (legacy saves would be silently marked
            // initialised on first toggle). Initialised flips only via
            // StartNewGame postfix (genuinely new saves) or the SaveData
            // setter's grandfather (existing QuestMod state on first load).
            // To enable destructive features on a legacy save, the user
            // explicitly clicks the override-with-confirm in the Tools tab.
            LogDebugInfo($"SyncToSave: WishesMode={WishesMode}, Accepted={AllQuestsAccepted}");
        }
        public static ConfigEntry<bool> EnableCompletionOverrides { get; private set; } = null!;
        public static ConfigEntry<bool> OnlyDiscoveredQuests { get; private set; } = null!;
        public static ConfigEntry<bool> QuestItemInvincible { get; private set; } = null!;
        public static ConfigEntry<bool> ShowQuestDisplayNames { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> GuiToggleKey { get; private set; } = null!;
        public static ConfigEntry<float> GuiScale { get; private set; } = null!;
        public static ConfigEntry<bool> GuaranteedSilverBells { get; private set; } = null!;
        public static ConfigEntry<bool> BypassWishboardLock { get; private set; } = null!;
        public static ConfigEntry<bool> DebugLogging { get; private set; } = null!;
        public static ConfigEntry<bool> DevRemoveLimits { get; private set; } = null!;
        public static ConfigEntry<bool> DevForceOperations { get; private set; } = null!;
        // CLUSTER P: Pure mode is the "raw chaos" option -- hidden by default
        // so the Tools tab doesnt make it look like a peer choice with Adjusted
        // (the recommended default per docs/AllWishesModes.md).
        public static ConfigEntry<bool> ShowPureWishesMode { get; private set; } = null!;
        public static ConfigEntry<bool> EnableSilkSoulTab { get; private set; } = null!;
        public static ConfigEntry<string> ActivePreset { get; private set; } = null!;
        public static ConfigEntry<bool> EnableCustomRequirements { get; private set; } = null!;
        public static ConfigEntry<bool> EnableWishLocationReassignment { get; private set; } = null!;
        public static ConfigEntry<bool> EnableFullRemoteComplete { get; private set; } = null!;
        public static ConfigEntry<bool> GourmandStopDecay { get; private set; } = null!;
        public static ConfigEntry<int> GourmandSegmentCount { get; private set; } = null!;
        public static ConfigEntry<float> GourmandDecaySeconds { get; private set; } = null!;


        internal static void LogDebugInfo(string message)
        {
            if (DebugLogging.Value)
                Log.LogInfo(message);
        }

        internal static bool IsQuestDiscovered(string questName)
        {
            if (!OnlyDiscoveredQuests.Value) return true;
            if (PlayerData.instance == null) return false;
            var rt = QuestDataAccess.GetRuntimeData();
            return rt != null && rt.Contains(questName);
        }

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo("QuestMod initializing...");

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (from, to) =>
            {
                if (to.name == "Menu_Title")
                {
                    WishesMode = AllWishesMode.Disabled;
                    AllQuestsAccepted = false;
                    SilkSoulOverrides.Reset();
                    Log.LogInfo("Returned to title â€” mode reset");
                }
            };

            EnableCompletionOverrides = Config.Bind(
                "General",
                "EnableCompletionOverrides",
                true,
                new ConfigDescription(
                    "Apply your custom quest target counts when loading a save. Disable to use the game's default target counts.",
                    null,
                    new { Order = 1 })
            );

            OnlyDiscoveredQuests = Config.Bind(
                "General",
                "OnlyDiscoveredQuests",
                true,
                new ConfigDescription(
                    "Only show and modify quests you have already discovered in your current save.",
                    null,
                    new { Order = 2 })
            );

            QuestItemInvincible = Config.Bind(
                "General",
                "QuestItemInvincible",
                false,
                new ConfigDescription(
                    "Prevent delivery quest items from being destroyed by enemy attacks.",
                    null,
                    new { Order = 3 })
            );

            ShowQuestDisplayNames = Config.Bind(
                "GUI",
                "ShowQuestDisplayNames",
                true,
                new ConfigDescription(
                    "Show in-game display names (e.g. 'Flexile Spines') instead of internal asset names (e.g. 'Brolly Get').",
                    null,
                    new { Order = 1 })
            );

            GuiToggleKey = Config.Bind(
                "GUI",
                "GuiToggleKey",
                new KeyboardShortcut(KeyCode.F9),
                new ConfigDescription(
                    "Key to open/close the Quest Manager window.",
                    null,
                    new { Order = 2 })
            );

            GuiScale = Config.Bind(
                "GUI",
                "GuiScale",
                0f,
                new ConfigDescription(
                    "GUI scale override. 0 = auto-detect from system DPI. Set manually (e.g. 1.5) to override.",
                    new AcceptableValueRange<float>(0f, 3f),
                    new { Order = 3 })
            );

            GuaranteedSilverBells = Config.Bind(
                "Quest Tweaks",
                "GuaranteedSilverBells",
                false,
                new ConfigDescription(
                    "Make every bell drop a Silver Bell (overrides the 75/25 normal/silver split for the Silver Bells quest).",
                    null,
                    new { Order = 1 })
            );

            BypassWishboardLock = Config.Bind(
                "Quest Tweaks",
                "BypassWishboardLock",
                false,
                new ConfigDescription(
                    "Force-activate the Bonetown wishboard without defeating the Bell Beast. " +
                    "Runtime-only override on the ActivateIfPlayerdataTrue gate — does NOT modify PlayerData, " +
                    "so disabling the toggle restores the vanilla lock immediately.",
                    null,
                    new { Order = 2 })
            );

            EnableSilkSoulTab = Config.Bind(
                "Features",
                "EnableSilkSoulTab",
                true,
                new ConfigDescription(
                    "Show the Silk & Soul tab in the Quest Manager GUI.",
                    null,
                    new { Order = 1 })
            );

            EnableCustomRequirements = Config.Bind(
                "Custom Requirements",
                "Enable",
                true,
                new ConfigDescription(
                    "Enable the custom requirements rules (presets + per-quest extra conditions). " +
                    "See docs/CustomRequirements.md and BepInEx/config/QuestMod/QuestRequirements.user.json.",
                    null,
                    new { Order = 1 })
            );

            ActivePreset = Config.Bind(
                "Custom Requirements",
                "ActivePreset",
                "vanilla",
                new ConfigDescription(
                    "Name of the preset to apply on save load. Built-ins: 'vanilla', 'farmable-only', " +
                    "'farmable-quarter', 'quick'.",
                    null,
                    new { Order = 2 })
            );

            // CLUSTER J scaffold - see docs/WishLocationReassignment.md
            EnableWishLocationReassignment = Config.Bind(
                "Features",
                "EnableWishLocationReassignment",
                false,
                new ConfigDescription(
                    "[STRETCH - NOT YET IMPLEMENTED] Allow quests to be accepted from non-default sources " +
                    "(NPC wishes from wishboards; wishboard wishes from world locations). " +
                    "This flag is a placeholder; no behavior is wired up yet.",
                    null,
                    new { Order = 99 })
            );

            EnableFullRemoteComplete = Config.Bind(
                "Features",
                "EnableFullRemoteComplete",
                false,
                new ConfigDescription(
                    "When ON, the per quest Complete button in the Quests tab routes through " +
                    "QuestManager so vanilla side effects (item deductions, rewards, dialogue " +
                    "flags) fire. When OFF (default), the button only flips QuestData flags " +
                    "(legacy behaviour, useful for dev/debug). See cluster Y in TODO.md.",
                    null,
                    new { Order = 7 })
            );


            // CLUSTER L: Delivery + Gourmand timer
            GourmandStopDecay = Config.Bind(
                "Delivery",
                "GourmandStopDecay",
                false,
                new ConfigDescription(
                    "Stop the Courier's Rasher (Great Gourmand quest item) from decaying while carried.",
                    null,
                    new { Order = 1 })
            );

            GourmandSegmentCount = Config.Bind(
                "Delivery",
                "GourmandSegmentCount",
                8,
                new ConfigDescription(
                    "Number of durability segments on the Courier's Rasher. Default is 8.",
                    new AcceptableValueRange<int>(1, 32),
                    new { Order = 2 })
            );

            GourmandDecaySeconds = Config.Bind(
                "Delivery",
                "GourmandDecaySeconds",
                47f,
                new ConfigDescription(
                    "Seconds per segment before the Courier's Rasher loses one durability tick. Default is 47s.",
                    new AcceptableValueRange<float>(1f, 600f),
                    new { Order = 3 })
            );



            DebugLogging = Config.Bind(
                "Advanced",
                "DebugLogging",
                false,
                new ConfigDescription(
                    "Log detailed quest operations to BepInEx console. Enable when troubleshooting.",
                    null,
                    new { Order = 1 })
            );

            DevRemoveLimits = Config.Bind(
                "Advanced",
                "DevRemoveLimits",
                false,
                new ConfigDescription(
                    "Remove all count limits (min/max caps) in the Targets tab. Values outside normal ranges may break quest state.",
                    null,
                    new { Order = 2 })
            );

            DevForceOperations = Config.Bind(
                "Advanced",
                "DevForceOperations",
                false,
                new ConfigDescription(
                    "Show Force Accept ALL / Force Complete ALL buttons. These directly inject and modify quest state and can irreversibly break saves.",
                    null,
                    new { Order = 3 })
            );

            ShowPureWishesMode = Config.Bind(
                "Advanced",
                "ShowPureWishesMode",
                false,
                new ConfigDescription(
                    "Show the 'Pure' All Wishes option in the Tools tab. Pure bypasses chain gating + mutually-exclusive + edge-case patches -- raw chaos with soft-locks possible. Hidden by default; enable only if you want the option alongside Adjusted.",
                    null,
                    new { Order = 4 })
            );

            QuestRegistry.Load();
            QuestRequirements.Load();
            QuestRequirements.SetActivePreset(ActivePreset.Value);
            ActivePreset.SettingChanged += (_, _) => QuestRequirements.SetActivePreset(ActivePreset.Value);
            QuestStateHooks.Initialize();
            QuestAcceptance.Initialize();
            QuestCompletionOverrides.Initialize();


            // Apply every [HarmonyPatch]-decorated class in this assembly
            // (PinstressIsAvailableNarrowingPatch for cluster M, etc).
            // QuestItemProtection + GourmandTimerPatch use manual harmony.Patch
            // because their target methods take types that aren't in the
            // reference assembly we compile against (ActiveItem, etc).
            new HarmonyLib.Harmony("com.silkmod.questmod").PatchAll(typeof(QuestModPlugin).Assembly);

            QuestItemProtection.Initialize();
            SilverBellPatch.Initialize();
            WishboardSceneSweep.Initialize();
            WishwallFsmPatch.Initialize();
            GourmandTimerPatch.Initialize();

            // CLUSTER M: All Wishes Mode edge case bug fixes.
            // SB2_4CArena handler retired in cluster R; arena reuse gating is
            // now expressed declaratively via QuestRequirements.json
            // availableConditions (e.g. Beastfly Hunt requires defeatedSongGolem).
            EdgeBugPinstressNeedleStrike.Initialize();
            EdgeBugDoubleTrobbio.Initialize();
            gameObject.AddComponent<QuestGUI>();

            Log.LogInfo($"QuestMod initialized - WishesMode={WishesMode}, AllQuestsAccepted={AllQuestsAccepted}");

            Log.LogInfo("  F9 = Quest Manager GUI");
        }
    }
}
