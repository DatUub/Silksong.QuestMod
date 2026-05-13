namespace QuestMod
{
    internal static class QuestModConstants
    {
        public const string PluginGuid = "com.silkmod.questmod";
        public const string PluginName = "QuestMod";

        public const string HarmonyId_GourmandTimer = PluginGuid + ".gourmand-timer";
        public const string HarmonyId_ItemInvincible = PluginGuid + ".item-invincible";

        public const string EmbeddedQuestCapabilitiesJson = "QuestMod.Data.QuestCapabilities.json";
        public const string EmbeddedQuestRequirementsJson = "QuestMod.Data.QuestRequirements.json";

        public const int GuiWindowId = 12345;
        public const float ConfirmArmWindow = 4f;
        public const float ModeChangeHintDurationSec = 12f;

        // Pre-gameplay scenes where FSMs aren't initialized yet. Sweeps that
        // FindObjectsByType(FindObjectsInactive.Include) here trigger PlayMaker
        // lazy init and NPE on state.Fsm. Prefix match catches Pre_Menu_Intro,
        // Pre_Menu_Loader, etc. without needing to enumerate every variant.
        public static bool IsTransientMenuScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return true;
            if (sceneName.StartsWith("Pre_Menu_", System.StringComparison.Ordinal)) return true;
            return sceneName == "Menu_Title"
                || sceneName == "Quit_To_Menu"
                || sceneName == "Opening_Sequence"
                || sceneName == "PermaDeath"
                || sceneName == "PermaDeath_Unlocked";
        }
    }
}
