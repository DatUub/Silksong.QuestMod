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

                // Newtonsoft skips property initializers on explicit nulls. Normalize.
                if (_saveData.QuestPolicies == null)
                    _saveData.QuestPolicies = new System.Collections.Generic.Dictionary<string, QuestPolicy>();
                if (_saveData.QuestTargetOverrides == null)
                    _saveData.QuestTargetOverrides = new System.Collections.Generic.Dictionary<string, int>();
                if (_saveData.InjectedQuests == null)
                    _saveData.InjectedQuests = new System.Collections.Generic.HashSet<string>();
                if (_saveData.CompletedQuests == null)
                    _saveData.CompletedQuests = new System.Collections.Generic.HashSet<string>();
                if (_saveData.WishLocationOverrides == null)
                    _saveData.WishLocationOverrides = new System.Collections.Generic.Dictionary<string, string>();
                if (_saveData.WishLocationTriggersFired == null)
                    _saveData.WishLocationTriggersFired = new System.Collections.Generic.HashSet<string>();
                if (_saveData.Prereqs == null)
                    _saveData.Prereqs = new GranularPrereqs();

                // Migrate legacy bool -> enum and clear so we don't re-grandfather later.
                if (_saveData.AllWishesMode == AllWishesMode.Disabled && _saveData.AllQuestsAvailable)
                {
                    _saveData.AllWishesMode = AllWishesMode.Adjusted;
                    _saveData.AllQuestsAvailable = false;
                }

                MigrateLegacyAvailability();

                // Legacy = 0. Run migrations, then stamp.
                if (_saveData.SchemaVersion < QuestModSaveData.CurrentSchemaVersion)
                {
                    Log?.LogInfo($"SaveData schema migration {_saveData.SchemaVersion} -> {QuestModSaveData.CurrentSchemaVersion}");
                    _saveData.SchemaVersion = QuestModSaveData.CurrentSchemaVersion;
                }

                WishesMode = _saveData.AllWishesMode;
                AllQuestsAccepted = _saveData.AllQuestsAccepted;

                // Grandfather: any non-default QuestMod state means destructive
                // features have already touched this save, so don't lock the user out.
                if (!_saveData.QuestModInitialized && HasAnyQuestModState(_saveData))
                {
                    _saveData.QuestModInitialized = true;
                    Log?.LogInfo("Grandfathered existing QuestMod state on this save -> QuestModInitialized=true");
                }

                // Restore per-save preset. Load() picks it up on first call too.
                if (QuestRequirements.IsLoaded && !string.IsNullOrEmpty(_saveData.ActiveDslPreset))
                    QuestRequirements.SetActivePreset(_saveData.ActiveDslPreset);

                Log?.LogInfo($"SaveData loaded: WishesMode={WishesMode}, Accepted={AllQuestsAccepted}, Policies={_saveData.QuestPolicies.Count}, Initialized={_saveData.QuestModInitialized}, Override={_saveData.OverrideSafetyForThisSave}, DslPreset={_saveData.ActiveDslPreset}");
            }
        }

        // Setter handles the legacy bool. Per-quest seeding would surprise users.
        private void MigrateLegacyAvailability() { }

        void IRawSaveDataMod.ReadSaveData(Stream saveFile)
        {
            if (saveFile == null)
            {
                SaveData = null;
                return;
            }

            string rawJson = null;
            try
            {
                using var sr = new StreamReader(saveFile);
                rawJson = sr.ReadToEnd();
                // TypeNameHandling OFF -- $type on read is a gadget vector.
                var ser = JsonSerializer.CreateDefault();
                using var reader = new JsonTextReader(new System.IO.StringReader(rawJson));
                SaveData = ser.Deserialize<QuestModSaveData>(reader);
            }
            catch (System.Exception ex)
            {
                Log?.LogError($"Failed to deserialize save data (old format or truncated): {ex.Message}");
                TryBackupCorruptSave(rawJson);
                QuestModToast.Show("QuestMod save data corrupted, reset to defaults. Backup written to BepInEx/config/QuestMod/corrupt-saves/.",
                    new UnityEngine.Color(1f, 0.5f, 0.3f), 8f);
                SaveData = new QuestModSaveData();
            }
        }

        private static void TryBackupCorruptSave(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson)) return;
            try
            {
                var dir = Path.Combine(BepInEx.Paths.ConfigPath, "QuestMod", "corrupt-saves");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, $"corrupt-{System.DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
                File.WriteAllText(path, rawJson);
                Log?.LogWarning($"QuestMod: corrupt save data archived to {path}");
            }
            catch (System.Exception ex)
            {
                Log?.LogWarning($"QuestMod: corrupt-save backup failed: {ex.Message}");
            }
        }

        // WishesMode is canonical. AllQuestsAvailable is a back-compat shim.
        public static AllWishesMode WishesMode { get; private set; } = AllWishesMode.Disabled;
        public static bool AllQuestsAvailable => WishesMode != AllWishesMode.Disabled;
        public static bool IsPureWishes => WishesMode == AllWishesMode.Pure;
        public static bool IsAdjustedWishes => WishesMode == AllWishesMode.Adjusted;

        // Save value wins. Falls back to the global ConfigEntry.
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

        // IsQuestModSave: save was created with QuestMod or used destructive features.
        // IsSafetyOverridden: user confirmed past the gate. One-way ratchet.
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
            if (!string.IsNullOrEmpty(d.ActiveDslPreset) && d.ActiveDslPreset != "vanilla") return true;
            if (d.EnableCustomRequirements.HasValue) return true;
            if (d.EnableFullRemoteComplete.HasValue) return true;
            // Reflect so a new GranularPrereqs field doesn't silently miss grandfathering.
            if (d.Prereqs != null)
            {
                foreach (var f in typeof(GranularPrereqs).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (f.FieldType != typeof(bool)) continue;
                    if ((bool)f.GetValue(d.Prereqs)) return true;
                }
                foreach (var p in typeof(GranularPrereqs).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (p.PropertyType != typeof(bool) || !p.CanRead) continue;
                    if ((bool)p.GetValue(d.Prereqs)) return true;
                }
            }
            return false;
        }

        public static void SetSafetyOverride(bool value)
        {
            var data = Instance?.SaveData;
            if (data == null) return;
            // One-way by design.
            if (!value)
            {
                Log?.LogWarning("SetSafetyOverride: ignored value=false (override is one-way by design)");
                return;
            }
            data.OverrideSafetyForThisSave = true;
            Log?.LogMessage("SetSafetyOverride: ON");
        }

        // GUI shows a transient warning when this fires mid-run.
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

        // Bool shim for legacy callers.
        public static void SetAllQuestsAvailable(bool value)
        {
            WishesMode = value ? AllWishesMode.Adjusted : AllWishesMode.Disabled;
            SyncToSaveData();
        }

        public static void SetAllQuestsAccepted(bool value)
        {
            AllQuestsAccepted = value;
            if (value && WishesMode == AllWishesMode.Disabled)
            {
                // Needs non-Disabled to do anything. Toast on flip.
                WishesMode = AllWishesMode.Adjusted;
                LastWishesModeChange = AllWishesMode.Adjusted;
                LastWishesModeChangeRealtime = Time.realtimeSinceStartup;
                Log?.LogInfo("AllQuestsAccepted=true also enabled WishesMode=Adjusted (required coupling)");
                QuestModToast.Show("All Quests Accepted also enabled Adjusted mode",
                    new UnityEngine.Color(0.7f, 0.85f, 1f), 4f);
            }
            SyncToSaveData();
        }

        // Live GranularPrereqs accessor. null if no save.
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

        // null if no save loaded.
        public static string ExportSaveDataToJson()
        {
            if (Instance?.SaveData == null) return null;
            return JsonConvert.SerializeObject(Instance.SaveData, Formatting.Indented);
        }
        // Throws on parse failure. Safety gate fields are preserved from the
        // live save so a shared preset can't flip them on a legacy save.
        public static void ImportSaveDataFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new System.ArgumentException("empty json");
            if (Instance == null) throw new System.InvalidOperationException("plugin not initialized");
            var imported = JsonConvert.DeserializeObject<QuestModSaveData>(json);
            if (imported == null) throw new System.ArgumentException("parsed json was null");

            // Preserve gate fields. Only the Tools-tab override can elevate.
            var live = Instance.SaveData;
            imported.QuestModInitialized = live?.QuestModInitialized ?? false;
            imported.OverrideSafetyForThisSave = live?.OverrideSafetyForThisSave ?? false;

            // Validate before apply -- bad paste must not crash scene loads.
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
            if (Instance == null) return;
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
            // Legacy bool in sync.
            data.AllQuestsAvailable = WishesMode != AllWishesMode.Disabled;
            data.AllQuestsAccepted = AllQuestsAccepted;
            // Never touch QuestModInitialized here -- only StartNewGame or
            // grandfathering can flip it. Otherwise legacy saves would
            // bypass the safety gate on first toggle.
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
        // Pure is hidden by default so it doesn't look like a peer to Adjusted.
        public static ConfigEntry<bool> ShowPureWishesMode { get; private set; } = null!;
        public static ConfigEntry<bool> EnableSilkSoulTab { get; private set; } = null!;
        public static ConfigEntry<string> ActivePreset { get; private set; } = null!;
        public static ConfigEntry<bool> EnableCustomRequirements { get; private set; } = null!;
        public static ConfigEntry<bool> EnableWishLocationReassignment { get; private set; } = null!;
        public static ConfigEntry<bool> EnableFullRemoteComplete { get; private set; } = null!;
        public static ConfigEntry<bool> GourmandStopDecay { get; private set; } = null!;
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
            // Bail on dev sideload + Thunderstore double-Awake.
            if (Instance != null && Instance != this)
            {
                Logger.LogWarning("QuestMod: second Awake detected, destroying duplicate component");
                Destroy(this);
                return;
            }
            Instance = this;
            Log = Logger;
            Log.LogInfo("QuestMod initializing...");

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;

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
                    "Runtime-only override on the ActivateIfPlayerdataTrue gate, does NOT modify PlayerData, " +
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

            // See docs/WishLocationReassignment.md.
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
                    "(legacy behaviour, useful for dev/debug).  in TODO.md.",
                    null,
                    new { Order = 7 })
            );


            // Delivery + Gourmand
            GourmandStopDecay = Config.Bind(
                "Delivery",
                "GourmandStopDecay",
                false,
                new ConfigDescription(
                    "Stop the Courier's Rasher (Great Gourmand quest item) from decaying while carried.",
                    null,
                    new { Order = 1 })
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


            // PatchAll covers attribute patches. QuestItemProtection +
            // GourmandTimerPatch use manual harmony.Patch for types not in
            // the reference assembly.
            var harmony = new HarmonyLib.Harmony(QuestModConstants.PluginGuid);
            // Unwind partials on failure so we never run half-patched.
            try
            {
                harmony.PatchAll(typeof(QuestModPlugin).Assembly);
            }
            catch (System.Exception ex)
            {
                Log?.LogError($"Harmony.PatchAll failed: {ex.Message}\n{ex.StackTrace}");
                try { harmony.UnpatchSelf(); }
                catch (System.Exception unpatchEx)
                {
                    Log?.LogError($"Harmony.UnpatchSelf after PatchAll failure also threw: {unpatchEx.Message}");
                }
            }

            TryStep("QuestItemProtection",     QuestItemProtection.Initialize);
            TryStep("SilverBellPatch",         SilverBellPatch.Initialize);
            TryStep("WishboardSceneSweep",     WishboardSceneSweep.Initialize);
            TryStep("WishwallFsmPatch",        WishwallFsmPatch.Initialize);
            TryStep("GourmandTimerPatch",      GourmandTimerPatch.Initialize);

            // Arena-reuse gating lives in QuestRequirements.json availableConditions now.
            TryStep("EdgeBugPinstressNeedleStrike", EdgeBugPinstressNeedleStrike.Initialize);
            TryStep("EdgeBugDoubleTrobbio",         EdgeBugDoubleTrobbio.Initialize);
            TryStep("QuestGUI",                () => gameObject.AddComponent<QuestGUI>());

            Log.LogInfo($"QuestMod initialized - WishesMode={WishesMode}, AllQuestsAccepted={AllQuestsAccepted}");
            Log.LogInfo("  F9 = Quest Manager GUI");
        }

        private void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene from, UnityEngine.SceneManagement.Scene to)
        {
            if (to.name != "Menu_Title") return;
            // Wipe static state so slot A doesn't leak into slot B.
            WishesMode = AllWishesMode.Disabled;
            AllQuestsAccepted = false;
            LastWishesModeChange = AllWishesMode.Disabled;
            LastWishesModeChangeRealtime = -1f;
            _saveData = new QuestModSaveData();
            SilkSoulOverrides.Reset();
            QuestCompletionOverrides.ResetSlotState();
            Log?.LogInfo("Returned to title, mode reset");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private static void TryStep(string name, System.Action step)
        {
            try { step(); }
            catch (System.Exception ex)
            {
                Log?.LogError($"Initialize {name} failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
