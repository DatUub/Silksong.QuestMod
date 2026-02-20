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
                AllQuestsAvailable = _saveData.AllQuestsAvailable;
                AllQuestsAccepted = _saveData.AllQuestsAccepted;
                Log?.LogInfo($"SaveData loaded: Available={AllQuestsAvailable}, Accepted={AllQuestsAccepted}");
            }
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
                using var sr = new StreamReader(saveFile);
                using var reader = new JsonTextReader(sr);
                var ser = JsonSerializer.CreateDefault(new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                });
                SaveData = ser.Deserialize<QuestModSaveData>(reader);
            }
            catch (System.Exception ex)
            {
                Log.LogWarning($"Failed to deserialize save data (old format?), resetting: {ex.Message}");
                SaveData = new QuestModSaveData();
            }
        }

        public static bool AllQuestsAvailable { get; private set; }
        public static bool AllQuestsAccepted { get; private set; }

        public static void SetAllQuestsAvailable(bool value)
        {
            AllQuestsAvailable = value;
            SyncToSaveData();
        }

        public static void SetAllQuestsAccepted(bool value)
        {
            AllQuestsAccepted = value;
            if (value)
                AllQuestsAvailable = true;
            SyncToSaveData();
        }

        internal static void SyncFromSaveData()
        {
            var data = Instance.SaveData;
            if (data == null) return;
            AllQuestsAvailable = data.AllQuestsAvailable;
            AllQuestsAccepted = data.AllQuestsAccepted;
            LogDebugInfo($"SyncFromSave: Available={AllQuestsAvailable}, Accepted={AllQuestsAccepted}");
        }

        private static void SyncToSaveData()
        {
            if (Instance == null) return;
            var data = Instance.SaveData;
            if (data == null) return;
            data.AllQuestsAvailable = AllQuestsAvailable;
            data.AllQuestsAccepted = AllQuestsAccepted;
            LogDebugInfo($"SyncToSave: Available={AllQuestsAvailable}, Accepted={AllQuestsAccepted}");
        }
        public static ConfigEntry<bool> EnableCompletionOverrides { get; private set; } = null!;
        public static ConfigEntry<bool> OnlyDiscoveredQuests { get; private set; } = null!;
        public static ConfigEntry<bool> QuestItemInvincible { get; private set; } = null!;
        public static ConfigEntry<bool> ShowQuestDisplayNames { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> GuiToggleKey { get; private set; } = null!;
        public static ConfigEntry<float> GuiScale { get; private set; } = null!;
        public static ConfigEntry<bool> GuaranteedSilverBells { get; private set; } = null!;
        public static ConfigEntry<bool> DebugLogging { get; private set; } = null!;
        public static ConfigEntry<bool> DevRemoveLimits { get; private set; } = null!;
        public static ConfigEntry<bool> DevForceOperations { get; private set; } = null!;
        public static ConfigEntry<bool> EnableSilkSoulTab { get; private set; } = null!;


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
                    AllQuestsAvailable = false;
                    AllQuestsAccepted = false;
                    SilkSoulOverrides.Reset();
                    Log.LogInfo("Returned to title — mode reset");
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

            EnableSilkSoulTab = Config.Bind(
                "Features",
                "EnableSilkSoulTab",
                true,
                new ConfigDescription(
                    "Show the Silk & Soul tab in the Quest Manager GUI.",
                    null,
                    new { Order = 1 })
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

            QuestRegistry.Load();
            QuestStateHooks.Initialize();
            QuestAcceptance.Initialize();
            QuestCompletionOverrides.Initialize();


            SilverBellPatch.Initialize();
            
            gameObject.AddComponent<QuestGUI>();
            
            Log.LogInfo($"QuestMod initialized - AllQuestsAvailable={AllQuestsAvailable}, AllQuestsAccepted={AllQuestsAccepted}");

            Log.LogInfo("  F9 = Quest Manager GUI");
        }
    }
}
