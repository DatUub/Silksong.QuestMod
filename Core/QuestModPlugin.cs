using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Silksong.DataManager;
using System.Collections.Generic;
using System.Linq;
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

        public QuestModSaveData? SaveData { get; set; } = new();

        public static bool AllQuestsAvailable { get; set; }
        public static bool AllQuestsAccepted { get; set; }
        public static ConfigEntry<bool> EnableCompletionOverrides { get; private set; } = null!;
        public static ConfigEntry<bool> OnlyDiscoveredQuests { get; private set; } = null!;
        public static ConfigEntry<bool> QuestItemInvincible { get; private set; } = null!;
        public static ConfigEntry<bool> ShowQuestDisplayNames { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> GuiToggleKey { get; private set; } = null!;
        public static ConfigEntry<bool> GuaranteedSilverBells { get; private set; } = null!;

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
                    Log.LogInfo("Returned to title — AllQuests mode reset");
                }
            };

            EnableCompletionOverrides = Config.Bind(
                "General",
                "EnableCompletionOverrides",
                true,
                "Restore your custom quest target counts (set via the F9 menu) when loading a save. Turn off to use the game's original target counts"
            );

            OnlyDiscoveredQuests = Config.Bind(
                "General",
                "OnlyDiscoveredQuests",
                true,
                "When enabled, the mod only affects quests you have already discovered in your current save"
            );

            QuestItemInvincible = Config.Bind(
                "General",
                "QuestItemInvincible",
                false,
                "Prevent delivery quest items from being destroyed by enemy attacks"
            );

            ShowQuestDisplayNames = Config.Bind(
                "GUI",
                "ShowQuestDisplayNames",
                true,
                "Show in-game display names (e.g. 'Flexile Spines') instead of internal names (e.g. 'Brolly Get') in the F9 menu"
            );

            GuiToggleKey = Config.Bind(
                "GUI",
                "GuiToggleKey",
                new KeyboardShortcut(KeyCode.F9),
                "Key to open/close the Quest Manager GUI"
            );

            GuaranteedSilverBells = Config.Bind(
                "Quest Tweaks",
                "GuaranteedSilverBells",
                false,
                "Force every bell drop to be a Silver Bell (overrides the 75/25 normal/silver split)"
            );

            QuestStateHooks.Initialize();
            QuestAcceptance.Initialize();
            QuestCompletionOverrides.Initialize();
            CheatManagerToggle.Initialize();

            SilverBellPatch.Initialize();
            
            gameObject.AddComponent<QuestGUI>();
            
            Log.LogInfo($"QuestMod initialized - AllQuestsAvailable={AllQuestsAvailable}, AllQuestsAccepted={AllQuestsAccepted}");

            Log.LogInfo("  F9 = Quest Manager GUI");
        }
    }
}
