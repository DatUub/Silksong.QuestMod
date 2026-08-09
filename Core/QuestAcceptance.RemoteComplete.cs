using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuestMod
{
    public static partial class QuestAcceptance
    {
        // Mirrors NPC turn-in: deduct targets, grant reward, cascade
        // markCompleted, fire ShowQuestCompleted, then flip QuestData flags.
        public static bool RemoteComplete(string name)
        {
            return RemoteComplete(name, new HashSet<string>());
        }
        private static bool RemoteComplete(string name, HashSet<string> visited)
        {
            if (visited.Contains(name)) return false;
            // Gate before visited.Add or a refused call poisons the set.
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                LastCompletionRefusal = $"{name}: legacy save (safety override not set)";
                QuestModPlugin.Log.LogInfo($"RemoteComplete({name}): skipped -- legacy save, safety override not set");
                return false;
            }
            visited.Add(name);
            // Custom requirements gate. Same as CompleteQuest.
            if (QuestModPlugin.IsCustomRequirementsEnabled)
            {
                var res = QuestRequirements.EvaluateExtraConditions(name);
                if (!res.Pass)
                {
                    LastCompletionRefusal = $"{name}: {res.Reason}";
                    QuestModPlugin.LogDebugInfo($"RemoteComplete refused: {LastCompletionRefusal}");
                    return false;
                }
            }
            bool sideEffectsFired = false;
            try
            {
                var qmType = ReflectionCache.GetType("QuestManager");
                var fqbType = ReflectionCache.GetType("FullQuestBase");
                if (qmType != null && fqbType != null)
                {
                    var getQuest = HarmonyLib.AccessTools.Method(qmType, "GetQuest", new[] { typeof(string) });
                    var quest = getQuest?.Invoke(null, new object[] { name });
                    if (quest != null)
                    {
                        sideEffectsFired |= TryDeductTargets(quest, fqbType, name);
                        sideEffectsFired |= TryGrantReward(quest, fqbType, name);
                        sideEffectsFired |= TryAwardAchievement(quest, fqbType, name);
                        TryCascadeMarkCompleted(quest, fqbType, visited);
                        // In-game toast.
                        try
                        {
                            var showCompleted = HarmonyLib.AccessTools.Method(qmType, "ShowQuestCompleted",
                                new[] { fqbType, typeof(System.Action) });
                            showCompleted?.Invoke(null, new object[] { quest, null });
                        }
                        catch { }
                    }
                }
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"RemoteComplete: side effect path threw for {name}: {ex.Message}");
            }
            // Flag flip last. Skip re-eval -- side effects may invalidate
            // conditions tied to a just-consumed counter.
            CompleteQuest(name, skipExtraConditions: true);
            return sideEffectsFired;
        }
        // True if Consume/Take mutates inventory. Boss-kill / encounter
        // counters share the API but back gameplay progression, not inventory.
        private static bool IsInventoryDeductibleCounter(object counter)
        {
            if (counter == null) return false;
            var t = counter.GetType();
            // Inventory-backed counters inherit CollectableItem; boss-kill
            // subclasses don't, so this filters them out.
            while (t != null && t != typeof(object))
            {
                if (t.Name == "CollectableItem") return true;
                t = t.BaseType;
            }
            // No substring fallback -- false-positives boss-kill trackers.
            QuestModPlugin.LogDebugInfo($"IsInventoryDeductibleCounter: type '{counter.GetType().Name}' not a CollectableItem, skip");
            return false;
        }

        private static bool TryDeductTargets(object quest, System.Type fqbType, string questName)
        {
            try
            {
                System.Reflection.FieldInfo targetsField = null;
                var t = fqbType;
                while (t != null && targetsField == null && t != typeof(object))
                {
                    targetsField = t.GetField("targets",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
                    t = t.BaseType;
                }
                var arr = targetsField?.GetValue(quest) as System.Array;
                if (arr == null) return false;
                bool anyDeducted = false;
                for (int i = 0; i < arr.Length; i++)
                {
                    var elem = arr.GetValue(i);
                    if (elem == null) continue;
                    var counterField = elem.GetType().GetField("Counter");
                    var countField = elem.GetType().GetField("Count");
                    var counter = counterField?.GetValue(elem);
                    int count = 0;
                    var rawCount = countField?.GetValue(elem);
                    if (rawCount is int ic) count = ic;
                    if (counter == null || count <= 0) continue;

                    // Inventory counters only. Boss-kill/event counters share
                    // the API but back shared progression that other quests gate on.
                    if (!IsInventoryDeductibleCounter(counter))
                    {
                        QuestModPlugin.LogDebugInfo($"RemoteComplete {questName}: target {i} counter {counter.GetType().Name} is not inventory-backed; skipping deduct");
                        continue;
                    }

                    // Consume first, then Take. false=suppress per-item popup.
                    var consume = HarmonyLib.AccessTools.Method(counter.GetType(), "Consume",
                        new[] { typeof(int), typeof(bool) });
                    if (consume != null)
                    {
                        consume.Invoke(counter, new object[] { count, false });
                        QuestModPlugin.Log.LogInfo($"RemoteComplete {questName}: deducted {count} via {counter.GetType().Name}.Consume(int, bool)");
                        anyDeducted = true;
                        continue;
                    }
                    var take = HarmonyLib.AccessTools.Method(counter.GetType(), "Take",
                        new[] { typeof(int), typeof(bool) });
                    if (take != null)
                    {
                        take.Invoke(counter, new object[] { count, false });
                        QuestModPlugin.Log.LogInfo($"RemoteComplete {questName}: deducted {count} via {counter.GetType().Name}.Take(int, bool)");
                        anyDeducted = true;
                        continue;
                    }
                    QuestModPlugin.LogDebugInfo($"RemoteComplete {questName}: counter {counter.GetType().Name} has no Consume/Take; skipping target {i}");
                }
                return anyDeducted;
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"RemoteComplete {questName}: deduction threw: {ex.Message}");
                return false;
            }
        }
        private static bool TryGrantReward(object quest, System.Type fqbType, string questName)
        {
            try
            {
                var rewardItemField = fqbType.GetField("rewardItem");
                var rewardCountField = fqbType.GetField("rewardCount");
                if (rewardItemField == null || rewardCountField == null) return false;
                var item = rewardItemField.GetValue(quest);
                int count = 0;
                var rawCount = rewardCountField.GetValue(quest);
                if (rawCount is int ic) count = ic;
                if (item == null || count <= 0) return false;
                var itemType = item.GetType();
                // SavedItem Get/GetMultiple(int, bool). bool=show popup.
                foreach (var mn in new[] { "GetMultiple", "Get" })
                {
                    var m = HarmonyLib.AccessTools.Method(itemType, mn, new[] { typeof(int), typeof(bool) });
                    if (m != null)
                    {
                        m.Invoke(item, new object[] { count, true });
                        QuestModPlugin.Log.LogInfo($"RemoteComplete {questName}: granted {count} via {itemType.Name}.{mn}(int, bool)");
                        return true;
                    }
                }
                // Fallback: Get(bool) called count times.
                var single = HarmonyLib.AccessTools.Method(itemType, "Get", new[] { typeof(bool) });
                if (single != null)
                {
                    for (int i = 0; i < count; i++) single.Invoke(item, new object[] { true });
                    QuestModPlugin.Log.LogInfo($"RemoteComplete {questName}: granted {count} via {count} calls to {itemType.Name}.Get(bool)");
                    return true;
                }
                QuestModPlugin.Log.LogWarning($"RemoteComplete {questName}: no Get/GetMultiple method found on {itemType.Name}");
                return false;
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"RemoteComplete {questName}: grant threw: {ex.Message}");
                return false;
            }
        }
        private static bool TryAwardAchievement(object quest, System.Type fqbType, string questName)
        {
            try
            {
                var achField = fqbType.GetField("awardAchievementOnComplete");
                var achId = achField?.GetValue(quest) as string;
                if (string.IsNullOrEmpty(achId)) return false;
                var achHandler = ReflectionCache.GetType("AchievementHandler")
                    ?? ReflectionCache.GetType("Platform")
                    ?? ReflectionCache.GetType("GameManager");
                if (achHandler == null) return false;
                foreach (var mn in new[] { "AwardAchievement", "AwardAchievementToPlayer", "UnlockAchievement" })
                {
                    var m = HarmonyLib.AccessTools.Method(achHandler, mn, new[] { typeof(string) });
                    if (m == null) continue;
                    if (m.IsStatic)
                    {
                        m.Invoke(null, new object[] { achId });
                    }
                    else
                    {
                        var inst = HarmonyLib.AccessTools.Property(achHandler, "Instance")?.GetValue(null);
                        if (inst == null) continue;
                        m.Invoke(inst, new object[] { achId });
                    }
                    QuestModPlugin.Log.LogInfo($"RemoteComplete {questName}: awarded achievement {achId} via {achHandler.Name}.{mn}");
                    return true;
                }
                QuestModPlugin.Log.LogInfo($"RemoteComplete {questName}: achievement {achId} skipped (no handler)");
                return false;
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"RemoteComplete {questName}: achievement threw: {ex.Message}");
                return false;
            }
        }
        private static void TryCascadeMarkCompleted(object quest, System.Type fqbType, HashSet<string> visited)
        {
            try
            {
                var markField = fqbType.GetField("markCompleted");
                var markArr = markField?.GetValue(quest) as System.Array;
                if (markArr == null) return;
                foreach (var sub in markArr)
                {
                    if (sub == null) continue;
                    var nameProp = sub.GetType().GetProperty("name", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    var subName = nameProp?.GetValue(sub)?.ToString();
                    if (string.IsNullOrEmpty(subName)) continue;
                    if (!visited.Add(subName)) continue;
                    // Flag-only. Recursing RemoteComplete would re-grant rewards.
                    CompleteQuest(subName, skipExtraConditions: true);
                }
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"RemoteComplete cascade threw: {ex.Message}");
            }
        }
    }
}
