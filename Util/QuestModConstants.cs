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
    }
}
