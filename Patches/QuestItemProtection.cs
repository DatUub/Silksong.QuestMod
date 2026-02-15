using HarmonyLib;

namespace QuestMod
{
    [HarmonyPatch(typeof(DeliveryQuestItem), nameof(DeliveryQuestItem.BreakAllInternal))]
    public static class BreakAllPatch
    {
        public static bool Prefix() => !QuestModPlugin.QuestItemInvincible.Value;
    }

    [HarmonyPatch(typeof(DeliveryQuestItem), "TakeHitForItem")]
    public static class TakeHitPatch
    {
        public static bool Prefix() => !QuestModPlugin.QuestItemInvincible.Value;
    }

    [HarmonyPatch(typeof(HeroController), nameof(HeroController.TickDeliveryItems))]
    public static class TickDeliveryPatch
    {
        public static bool Prefix() => !QuestModPlugin.QuestItemInvincible.Value;
    }
}
