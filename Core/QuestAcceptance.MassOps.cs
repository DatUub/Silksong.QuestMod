using System.Collections;
using System.Collections.Generic;

namespace QuestMod
{
    public static partial class QuestAcceptance
    {
        public static void InjectAndAcceptAllQuests()
        {
            // Destructive (one-way PD writes). Gate behind save-safety.
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                QuestModPlugin.Log.LogInfo("InjectAndAcceptAllQuests: skipped -- legacy save, safety override not set");
                return;
            }

            if (PlayerData.instance == null)
            {
                QuestModPlugin.Log.LogWarning("InjectAndAcceptAllQuests: PlayerData not available yet");
                return;
            }

            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null)
            {
                QuestModPlugin.Log.LogWarning("InjectAndAcceptAllQuests: RuntimeData not available yet");
                return;
            }

            var (injected, skipped) = InjectMissingEntries(rt, applyGates: true);
            QuestModPlugin.Log.LogInfo(
                $"Injected {injected} new quests into RuntimeData (skipped {skipped}, total registered quests: {QuestRegistry.AllQuests.Count})");

            AcceptAllQuests();
        }

        // Accept any AutoAccept-flagged quest on first scene load.
        public static void AutoAcceptFlaggedQuests()
        {
            if (PlayerData.instance == null) return;
            // Permanent flip. Save-safety gate prevents imported presets
            // from quietly accepting on legacy saves.
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                QuestModPlugin.LogDebugInfo("AutoAcceptFlaggedQuests: skipped -- legacy save, safety override not set");
                return;
            }
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;

            int accepted = 0;
            var seen = new System.Collections.Generic.HashSet<string>();

            // Per-quest flagged path. Works in any mode -- IsActuallyAvailable
            // short-circuits in Pure, gates in Adjusted, gates in Disabled too
            // (which is fine: a per-quest opt-in is explicit).
            foreach (var name in QuestPolicyStore.AutoAcceptNames())
            {
                if (TryAutoAcceptOne(name, rt, seen)) accepted++;
            }

            // Global "accept everything Adjusted considers available" path.
            // Only meaningful in Adjusted/Pure -- Disabled would let unlocked
            // gates through that the vanilla game wouldn't yet offer.
            if (QuestModPlugin.AutoAcceptAllAvailable
                && QuestModPlugin.WishesMode != AllWishesMode.Disabled)
            {
                foreach (var questName in QuestRegistry.AllQuests)
                {
                    if (string.IsNullOrEmpty(questName)) continue;
                    if (TryAutoAcceptOne(questName, rt, seen)) accepted++;
                }
            }

            if (accepted > 0)
            {
                QuestModPlugin.Log.LogInfo($"Auto-accepted {accepted} flagged quests");
                // Toast so the player sees what the mod just did.
                QuestModToast.Show($"Auto-accepted {accepted} quest" + (accepted == 1 ? "" : "s"));
            }
        }

        private static bool TryAutoAcceptOne(string name, System.Collections.IDictionary rt, System.Collections.Generic.HashSet<string> seen)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!seen.Add(name)) return false;
            if (QuestRegistry.ExcludedQuests.Contains(name)) return false;
            // Full gate set. Force* paths skip this on purpose.
            if (!IsActuallyAvailable(name)) return false;

            if (rt.Contains(name))
            {
                var existing = rt[name];
                if (QuestDataAccess.IsAccepted(existing) || QuestDataAccess.IsCompleted(existing))
                    return false;
            }

            AcceptQuest(name);
            return true;
        }

        public static void ForceAcceptAllQuests() => ForceAllQuestsOp(complete: false);
        public static void ForceCompleteAllQuests() => ForceAllQuestsOp(complete: true);

        private static void ForceAllQuestsOp(bool complete)
        {
            // Destructive. Same gate.
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                QuestModPlugin.Log.LogInfo($"ForceAllQuestsOp(complete={complete}): skipped -- legacy save, safety override not set");
                return;
            }

            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;

            bool savedActState = PlayerData.instance.blackThreadWorld;

            int injected = InjectMissingEntries(rt, applyGates: false).injected;

            ModifyAllQuests(rt, complete, respectGates: false);

            PlayerData.instance.blackThreadWorld = savedActState;
            var verb = complete ? "completed" : "accepted";
            QuestModPlugin.Log.LogInfo($"Force {verb} ALL quests (injected {injected} new, total registered quests: {QuestRegistry.AllQuests.Count}), act state preserved");
        }

        // Shared RuntimeData slot creation for mass inject. applyGates=true enforces
        // discovery + IsActuallyAvailable (All Quests Accepted path); false is Force*.
        private static (int injected, int skipped) InjectMissingEntries(IDictionary rt, bool applyGates)
        {
            int injected = 0;
            int skipped = 0;

            foreach (var questName in QuestRegistry.AllQuests)
            {
                if (string.IsNullOrEmpty(questName)) continue;

                if (QuestRegistry.ExcludedQuests.Contains(questName))
                {
                    if (applyGates)
                    {
                        QuestModPlugin.Log.LogInfo($"  SKIP [{questName}]: excluded");
                        skipped++;
                    }
                    continue;
                }

                if (rt.Contains(questName)) continue;

                if (applyGates)
                {
                    if (!QuestModPlugin.IsQuestDiscovered(questName))
                    {
                        QuestModPlugin.Log.LogInfo($"  SKIP [{questName}]: not discovered");
                        skipped++;
                        continue;
                    }

                    // All gates under non-Pure. Pure stays raw inside IsActuallyAvailable.
                    if (!IsActuallyAvailable(questName, out string reason))
                    {
                        QuestModPlugin.Log.LogInfo($"  SKIP [{questName}]: {reason}");
                        skipped++;
                        continue;
                    }
                }

                var newData = QuestDataAccess.CreateEntry(rt, seen: true, accepted: true, completed: false, wasEver: false);
                if (newData == null) continue;
                rt[questName] = newData;
                injected++;
            }

            return (injected, skipped);
        }

        public static void AcceptAllQuests()
        {
            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;
            ModifyAllQuests(rt, complete: false);
        }

        public static void CompleteAllQuests()
        {
            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;
            ModifyAllQuests(rt, complete: true);
        }

        private static (IDictionary rt, object qd) EnsureQuestEntry(string name)
        {
            if (PlayerData.instance == null) return (null, null);
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return (null, null);

            if (rt.Contains(name))
                return (rt, rt[name]);

            var qd = QuestDataAccess.CreateEntry(rt, seen: false, accepted: false, completed: false, wasEver: false);
            if (qd == null) return (null, null);
            rt[name] = qd;
            QuestModPlugin.LogDebugInfo($"Injected into RuntimeData: {name}");
            return (rt, qd);
        }

        // respectGates=true => full gate set. Force* paths pass false.
        private static void ModifyAllQuests(IDictionary rt, bool complete, bool respectGates = true)
        {
            int count = 0;
            int skipped = 0;
            var keys = new List<object>();
            foreach (var key in rt.Keys) keys.Add(key);

            foreach (string key in keys)
            {
                var qd = rt[key];
                bool wasAccepted = QuestDataAccess.IsAccepted(qd);
                bool wasCompleted = QuestDataAccess.IsCompleted(qd);
                // Skip excluded even if in rt, or mass-accept re-flips completed couriers.
                if (respectGates && QuestRegistry.ExcludedQuests.Contains((string)key))
                {
                    QuestModPlugin.LogDebugInfo($"  SKIP [{key}]: excluded");
                    skipped++;
                    continue;
                }
                // availableConditions = "may first be offered". Skip re-eval
                // on accepted entries, or Complete All would drop them.
                bool needsGate = respectGates && !wasAccepted && !wasCompleted;
                if (needsGate && !IsActuallyAvailable(key, out string reason))
                {
                    QuestModPlugin.LogDebugInfo($"  SKIP [{key}]: {reason}");
                    skipped++;
                    continue;
                }
                // Mass Complete Available is flag-only. Same BlocksFlagOnlyComplete policy.
                if (complete && respectGates && BlocksFlagOnlyComplete(key))
                {
                    QuestModPlugin.LogDebugInfo($"  SKIP [{key}]: world-state complete requires in-world / Remote Complete");
                    skipped++;
                    continue;
                }
                qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: complete || wasCompleted, wasEver: complete || wasCompleted);
                rt[key] = qd;
                count++;
            }
            var verb = complete ? "Completed" : "Accepted";
            QuestModPlugin.Log.LogInfo($"{verb} {count} quests (skipped {skipped}, gates={(respectGates ? "respected" : "raw")})");
        }

    }
}
