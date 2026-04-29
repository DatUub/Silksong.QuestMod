using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace QuestMod
{
    // Cluster L: gates DeliveryQuestItem destroy/hit paths + HeroController
    // tick path on the QuestItemInvincible toggle. Patches via reflection
    // because ActiveItem (a parameter type on TakeHitForItem) isn't in the
    // reference assembly we compile against, so the attribute path can't
    // resolve typeof(ActiveItem).
    public static class QuestItemProtection
    {
        private static bool _applied;

        public static bool InvinciblePrefix() => !QuestModPlugin.QuestItemInvincible.Value;

        public static void Initialize()
        {
            if (_applied) return;
            _applied = true;
            try
            {
                var harmony = new Harmony("com.silkmod.questmod.item-invincible");
                var prefix = new HarmonyMethod(typeof(QuestItemProtection)
                    .GetMethod(nameof(InvinciblePrefix), BindingFlags.Public | BindingFlags.Static));

                int patched = 0;
                var dqiType = AccessTools.TypeByName("DeliveryQuestItem");
                if (dqiType != null)
                {
                    var breakAll = AccessTools.Method(dqiType, "BreakAllInternal",
                        new[] { typeof(bool), typeof(bool) });
                    if (breakAll != null) { harmony.Patch(breakAll, prefix: prefix); patched++; }

                    // Both TakeHitForItem overloads are static. Iterate by name
                    // and take whichever overloads are present at runtime.
                    foreach (var m in dqiType
                        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .Where(m => m.Name == "TakeHitForItem"))
                    {
                        try { harmony.Patch(m, prefix: prefix); patched++; }
                        catch (Exception ex)
                        {
                            QuestModPlugin.Log.LogWarning(
                                $"QuestItemProtection: TakeHitForItem({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name))}) patch failed: {ex.Message}");
                        }
                    }
                }
                else
                {
                    QuestModPlugin.Log.LogWarning("QuestItemProtection: DeliveryQuestItem type missing; invincibility no-op.");
                }

                var hcType = AccessTools.TypeByName("HeroController");
                var tickDelivery = hcType != null ? AccessTools.Method(hcType, "TickDeliveryItems") : null;
                if (tickDelivery != null) { harmony.Patch(tickDelivery, prefix: prefix); patched++; }

                QuestModPlugin.Log.LogInfo($"QuestItemProtection: applied {patched} prefix(es) gating QuestItemInvincible.");
            }
            catch (Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"QuestItemProtection: Initialize threw: {ex.Message}");
            }
        }
    }
}
