using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuestMod
{
    public struct QuestInfo
    {
        public string Name;
        public string DisplayName;
        public bool IsAccepted;
        public bool IsCompleted;
    }

    public struct ChainInfo
    {
        public string ChainName;
        public string DisplayName;
        public string[] Steps;
        public int CurrentStep;
        public int TotalSteps;
        public bool IsFullyComplete;
    }

    public static class QuestAcceptance
    {
        public static string GetExclusionConflict(string questName)
        {
            if (!QuestRegistry.MutuallyExclusiveQuests.TryGetValue(questName, out string conflicting))
                return null;

            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null || !rt.Contains(conflicting))
                return null;

            var qd = rt[conflicting];
            if (QuestDataAccess.IsAccepted(qd) || QuestDataAccess.IsCompleted(qd))
                return conflicting;

            return null;
        }
        // Cluster T: stage N of a sequential quest is "encountered" iff the
        // PD bool from QuestRegistry.SequentialStagePdPatterns evaluates to
        // true. stageIndex is 0-based; the pattern uses {stage} as 1-indexed.
        // Returns true (no gate) for non-sequential quests or quests without
        // a registered pattern, so callers can use this as a permissive gate.
        public static bool IsSequentialStageEncountered(string questName, int stageIndex)
        {
            if (!QuestRegistry.SequentialStagePdPatterns.TryGetValue(questName, out var pattern))
                return true;
            if (PlayerData.instance == null) return false;
            var fieldName = pattern.Replace("{stage}", (stageIndex + 1).ToString());
            var fi = typeof(PlayerData).GetField(fieldName);
            if (fi == null) return false;
            var val = fi.GetValue(PlayerData.instance);
            return val is bool b && b;
        }

        public static bool IsChainPrereqMet(string questName)
        {
            foreach (var chain in QuestRegistry.ChainRegistry.Values)
            {
                for (int i = 0; i < chain.Length; i++)
                {
                    if (chain[i] != questName)
                        continue;

                    if (i == 0)
                        return true;

                    var rt = QuestDataAccess.GetRuntimeData();
                    if (rt == null)
                        return false;

                    for (int j = 0; j < i; j++)
                    {
                        if (!rt.Contains(chain[j]))
                            return false;
                        if (!QuestDataAccess.IsCompleted(rt[chain[j]]))
                            return false;
                    }
                    return true;
                }
            }

            return true;
        }


        public static string GetDisplayName(string internalName)
        {
            if (QuestRegistry.DisplayNames.TryGetValue(internalName, out string name))
                return name;
            return internalName;
        }

        // CLUSTER S: single source of truth for "should this quest be
        // accept-eligible right now under the current mode". Centralises
        // the gate set so every code path that mass-accepts (AcceptAllQuests,
        // InjectAndAcceptAllQuests, AutoAcceptFlaggedQuests) respects the
        // same rules as the IsAvailable postfix.
        //
        // Pure: always true (raw chaos).
        // Adjusted/Disabled: chain prereq met AND no mutually-exclusive twin
        //   accepted/completed AND cluster R availableConditions pass.
        //
        // Returns the failure reason via out param so callers can log why
        // a specific quest was skipped. Empty string on pass.
        public static bool IsActuallyAvailable(string questName, out string reason)
        {
            reason = "";
            if (QuestModPlugin.IsPureWishes) return true;

            if (!IsChainPrereqMet(questName))
            {
                reason = "chain prereq unmet";
                return false;
            }
            if (GetExclusionConflict(questName) != null)
            {
                reason = "mutually-exclusive twin active";
                return false;
            }
            var avail = QuestRequirements.EvaluateAvailableConditions(questName);
            if (!avail.Pass)
            {
                reason = "availableConditions: " + (avail.Reason ?? "(no reason)");
                return false;
            }
            return true;
        }

        public static bool IsActuallyAvailable(string questName)
            => IsActuallyAvailable(questName, out _);

        public static void Initialize()
        {
            QuestModPlugin.Log.LogInfo("QuestAcceptance initialized");
        }

        public static void InjectAndAcceptAllQuests()
        {
            // CLUSTER P: force-accept-all is destructive (every quest accepted
            // permanently). Gate behind the save-safety check.
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                QuestModPlugin.Log.LogInfo("InjectAndAcceptAllQuests: skipped -- legacy save, safety override not set (cluster P)");
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

            var allQuests = Resources.FindObjectsOfTypeAll<FullQuestBase>();
            int injected = 0;
            int skipped = 0;

            foreach (var quest in allQuests)
            {
                var questName = quest.name;
                if (string.IsNullOrEmpty(questName))
                    continue;

                if (QuestRegistry.ExcludedQuests.Contains(questName))
                {
                    QuestModPlugin.Log.LogInfo($"  SKIP [{questName}]: excluded");
                    skipped++;
                    continue;
                }

                if (rt.Contains(questName))
                    continue;

                if (!QuestModPlugin.IsQuestDiscovered(questName))
                {
                    QuestModPlugin.Log.LogInfo($"  SKIP [{questName}]: not discovered");
                    skipped++;
                    continue;
                }

                // CLUSTER S: respect every gate (chain + exclusion + cluster R
                // availableConditions) under non-Pure. Pure stays raw.
                if (!IsActuallyAvailable(questName, out string reason))
                {
                    QuestModPlugin.Log.LogInfo($"  SKIP [{questName}]: {reason}");
                    skipped++;
                    continue;
                }

                var template = GetAnyValue(rt);
                if (template == null) continue;
                var newData = QuestDataAccess.SetFields(template, seen: true, accepted: true, completed: false, wasEver: false);
                rt[questName] = newData;
                injected++;
            }

            QuestModPlugin.Log.LogInfo($"Injected {injected} new quests into RuntimeData (skipped {skipped} excluded, total ScriptableObjects: {allQuests.Length})");

            AcceptAllQuests();
        }

        // Accept every quest flagged with QuestPolicy.AutoAccept that isn't
        // already accepted/completed. Called on first scene load when the
        // global AllQuestsAccepted toggle is OFF.
        public static void AutoAcceptFlaggedQuests()
        {
            if (PlayerData.instance == null) return;
            // Cluster P: AutoAccept flips IsAccepted on quests permanently.
            // Same gate as the other mass-ops here. Without this, an imported
            // policy preset could quietly accept quests on a legacy save.
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                QuestModPlugin.LogDebugInfo("AutoAcceptFlaggedQuests: skipped -- legacy save, safety override not set");
                return;
            }
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;

            int accepted = 0;
            foreach (var name in QuestPolicyStore.AutoAcceptNames())
            {
                if (string.IsNullOrEmpty(name)) continue;
                if (QuestRegistry.ExcludedQuests.Contains(name)) continue;
                // CLUSTER S: respect chain + exclusion + availableConditions
                // (was chain-only). Force* paths intentionally skip this.
                if (!IsActuallyAvailable(name)) continue;

                // Skip if already accepted or completed.
                if (rt.Contains(name))
                {
                    var existing = rt[name];
                    if (QuestDataAccess.IsAccepted(existing) || QuestDataAccess.IsCompleted(existing))
                        continue;
                }

                AcceptQuest(name);
                accepted++;
            }

            if (accepted > 0)
            {
                QuestModPlugin.Log.LogInfo($"Auto-accepted {accepted} flagged quests");
                // I-2: surface in a toast so the player knows the mod just
                // accepted quests on their behalf.
                QuestModToast.Show($"Auto-accepted {accepted} quest" + (accepted == 1 ? "" : "s"));
            }
        }

        public static void ForceAcceptAllQuests() => ForceAllQuestsOp(complete: false);
        public static void ForceCompleteAllQuests() => ForceAllQuestsOp(complete: true);

        private static void ForceAllQuestsOp(bool complete)
        {
            // CLUSTER P: force-{accept,complete}-all is destructive. Same gate.
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                QuestModPlugin.Log.LogInfo($"ForceAllQuestsOp(complete={complete}): skipped -- legacy save, safety override not set (cluster P)");
                return;
            }

            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;

            bool savedActState = PlayerData.instance.blackThreadWorld;

            var allQuests = Resources.FindObjectsOfTypeAll<FullQuestBase>();
            int injected = 0;

            foreach (var quest in allQuests)
            {
                var questName = quest.name;
                if (string.IsNullOrEmpty(questName)) continue;
                if (QuestRegistry.ExcludedQuests.Contains(questName)) continue;
                if (rt.Contains(questName)) continue;

                var template = GetAnyValue(rt);
                if (template == null) continue;
                var newData = QuestDataAccess.SetFields(template, seen: true, accepted: true, completed: false, wasEver: false);
                rt[questName] = newData;
                injected++;
            }

            ModifyAllQuests(rt, complete, respectGates: false);

            PlayerData.instance.blackThreadWorld = savedActState;
            var verb = complete ? "completed" : "accepted";
            QuestModPlugin.Log.LogInfo($"Force {verb} ALL quests (injected {injected} new, total ScriptableObjects: {allQuests.Length}), act state preserved");
        }

        public static void AcceptAllQuests()
        {
            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;
            ModifyAllQuests(rt, complete: false);
        }

        public static List<QuestInfo> GetQuestList()
        {
            var result = new List<QuestInfo>();
            if (PlayerData.instance == null) return result;
            var rt = QuestDataAccess.GetRuntimeData();

            var seen = new HashSet<string>();

            if (rt != null)
            {
                foreach (DictionaryEntry kvp in rt)
                {
                    var key = kvp.Key as string;
                    if (key == null) continue;
                    seen.Add(key);
                    result.Add(new QuestInfo
                    {
                        Name = key,
                        DisplayName = GetDisplayName(key),
                        IsAccepted = QuestDataAccess.IsAccepted(kvp.Value),
                        IsCompleted = QuestDataAccess.IsCompleted(kvp.Value)
                    });
                }
            }

            var allQuests = Resources.FindObjectsOfTypeAll<FullQuestBase>();
            foreach (var quest in allQuests)
            {
                if (string.IsNullOrEmpty(quest.name)) continue;
                if (seen.Contains(quest.name)) continue;

                result.Add(new QuestInfo
                {
                    Name = quest.name,
                    DisplayName = GetDisplayName(quest.name),
                    IsAccepted = false,
                    IsCompleted = false
                });
            }

            result.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public static void AcceptQuest(string name)
        {
            var (rt, qd) = EnsureQuestEntry(name);
            if (rt == null) return;
            qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: QuestDataAccess.IsCompleted(qd), wasEver: QuestDataAccess.IsCompleted(qd));
            rt[name] = qd;
            QuestModPlugin.LogDebugInfo($"Accepted: {name}");
        }

        public static void CompleteQuest(string name) => CompleteQuest(name, skipExtraConditions: false);

        // skipExtraConditions: when true, bypass the custom-requirements gate.
        // RemoteComplete passes true here because it already evaluated the gate
        // at the top of its own flow before firing side effects (deduct + grant
        // + cascade). Re-evaluating after consuming items would let a condition
        // that references a just-consumed counter spuriously refuse the flag
        // flip, leaving the player with rewards but no IsCompleted.
        internal static void CompleteQuest(string name, bool skipExtraConditions)
        {
            if (!skipExtraConditions && QuestModPlugin.IsCustomRequirementsEnabled)
            {
                var res = QuestRequirements.EvaluateExtraConditions(name);
                if (!res.Pass)
                {
                    LastCompletionRefusal = $"{name}: {res.Reason}";
                    QuestModPlugin.LogDebugInfo($"CompleteQuest refused: {LastCompletionRefusal}");
                    return;
                }
            }

            var (rt, qd) = EnsureQuestEntry(name);
            if (rt == null) return;
            qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: true, wasEver: true);
            rt[name] = qd;
            QuestModPlugin.LogDebugInfo($"Completed: {name}");
        }
        // Cluster Y: Remote complete that mirrors what the NPC/wishboard does
        // when the player turns a wish in. Walks FullQuestBase reward fields
        // (rewardItem + rewardCount) and grants them via reflection on
        // SavedItem; cascades through markCompleted so dependent quests also
        // close; triggers QuestManager.ShowQuestCompleted for the in game
        // toast. Always flips QuestData flags last so IsCompleted is correct
        // even if the reward path was a no op (no rewards).
        //
        // Item DEDUCTION still lives in the per NPC dialogue FSMs and is not
        // covered here; those are scoped under cluster Y phase 2.
        public static bool RemoteComplete(string name)
        {
            return RemoteComplete(name, new HashSet<string>());
        }
        private static bool RemoteComplete(string name, HashSet<string> visited)
        {
            if (visited.Contains(name)) return false;
            visited.Add(name);
            // Cluster P: routing through QuestManager mutates real save state.
            // Gate behind the save safety check so legacy saves stay vanilla.
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                QuestModPlugin.Log.LogInfo($"RemoteComplete({name}): skipped -- legacy save, safety override not set (cluster P)");
                return false;
            }
            // Custom requirements gate (matches CompleteQuest semantics).
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
                        // Trigger the in game toast.
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
            // Always flip QuestData flags so IsCompleted is correct. Skip
            // re-evaluating the custom-requirements gate; we already passed it
            // at the top of this flow, and side effects (counter consume,
            // cascade) may have invalidated conditions that reference the
            // just-consumed state.
            CompleteQuest(name, skipExtraConditions: true);
            return sideEffectsFired;
        }
        // Cluster Y phase 2: deduct items the wish requires the player to
        // hand over. Confirmed via cluster_Y_phase2_6 smoke probe that
        // QuestTargetCounter inherits from CollectableItem, which exposes
        // Consume(int, bool) and Take(int, bool). For courier wishes, the
        // counter itself IS the deliverable item (e.g. Mossberry, Courier
        // Supplies); calling Consume removes the count from inventory the
        // way the NPC dialogue would.
        //
        // Boss kill / encounter counters are also QuestTargetCounters but
        // they do not back inventory state, so Consume on them is harmless
        // (the API is shared but the side effect surface differs).
        // True when the counter is an inventory-backed collectable whose
        // Consume/Take call mutates the player's inventory. Boss kill /
        // encounter / event counters share the API but should not be
        // deducted by RemoteComplete because their count is read by other
        // gameplay systems as progression state.
        private static bool IsInventoryDeductibleCounter(object counter)
        {
            if (counter == null) return false;
            var t = counter.GetType();
            // Walk the inheritance chain looking for CollectableItem.
            // QuestTargetCounter inherits from CollectableItem only when it
            // backs an inventory item; boss-kill subclasses derive from a
            // sibling base so the check filters them out automatically.
            while (t != null && t != typeof(object))
            {
                if (t.Name == "CollectableItem") return true;
                t = t.BaseType;
            }
            // Fallback: any type whose name contains "Item" or "Collectable"
            // is treated as inventory-backed.
            string name = counter.GetType().Name;
            return name.IndexOf("Item", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Collectable", System.StringComparison.OrdinalIgnoreCase) >= 0;
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

                    // Skip counters whose backing type isn't an inventory
                    // collectable. Boss-kill / encounter / event counters
                    // also expose Consume/Take but they aren't backed by
                    // inventory state, so calling them would decrement a
                    // shared progression counter (e.g. defeated-Beastfly
                    // count) that other quests gate on.
                    if (!IsInventoryDeductibleCounter(counter))
                    {
                        QuestModPlugin.LogDebugInfo($"RemoteComplete {questName}: target {i} counter {counter.GetType().Name} is not inventory-backed; skipping deduct");
                        continue;
                    }

                    // Try Consume(int, bool) first (clearer deduction
                    // semantics). Fall back to Take(int, bool). Pass false
                    // for the bool to suppress per item popups (we already
                    // show a single ShowQuestCompleted toast).
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
                // SavedItem exposes Get(int, bool) and GetMultiple(int, bool)
                // where the bool is "show popup". Pass true so the player sees
                // the standard pickup toast like a vanilla NPC handoff.
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
        }        private static bool TryAwardAchievement(object quest, System.Type fqbType, string questName)
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
                    if (!string.IsNullOrEmpty(subName))
                        RemoteComplete(subName, visited);
                }
            }
            catch (System.Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"RemoteComplete cascade threw: {ex.Message}");
            }
        }
        /// <summary>
        /// Most recent CompleteQuest refusal reason from the custom requirements rules.
        /// GUI surfaces this as a tooltip on the quest row.
        /// </summary>
        public static string? LastCompletionRefusal { get; internal set; }

        public static void UnacceptQuest(string name)
        {
            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;
            if (rt.Contains(name))
            {
                var qd = rt[name];
                qd = QuestDataAccess.SetFields(qd, seen: true, accepted: false, completed: false, wasEver: QuestDataAccess.IsCompleted(qd));
                rt[name] = qd;
                QuestModPlugin.LogDebugInfo($"Unaccepted: {name}");
            }
        }

        public static void UncompleteQuest(string name)
        {
            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;
            if (rt.Contains(name))
            {
                var qd = rt[name];
                qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: false, wasEver: true);
                rt[name] = qd;
                QuestModPlugin.LogDebugInfo($"Uncompleted: {name}");
            }
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

            var template = GetAnyValue(rt);
            if (template == null) return (null, null);
            var qd = QuestDataAccess.SetFields(template, seen: false, accepted: false, completed: false, wasEver: false);
            rt[name] = qd;
            QuestModPlugin.LogDebugInfo($"Injected into RuntimeData: {name}");
            return (rt, qd);
        }

        // CLUSTER S fix: respectGates=true (default) means honour the same
        // chain + exclusion + availableConditions checks as the IsAvailable
        // postfix. AcceptAllQuests / CompleteAllQuests (the user-facing GUI
        // buttons) pass true. ForceAllQuestsOp (the dev Force* buttons,
        // gated behind the DevForceOperations advanced flag) passes false
        // for raw acceptance.
        private static void ModifyAllQuests(IDictionary rt, bool complete, bool respectGates = true)
        {
            int count = 0;
            int skipped = 0;
            var keys = new List<object>();
            foreach (var key in rt.Keys) keys.Add(key);

            foreach (string key in keys)
            {
                if (respectGates && !IsActuallyAvailable(key, out string reason))
                {
                    QuestModPlugin.LogDebugInfo($"  SKIP [{key}]: {reason}");
                    skipped++;
                    continue;
                }
                var qd = rt[key];
                bool wasCompleted = QuestDataAccess.IsCompleted(qd);
                qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: complete || wasCompleted, wasEver: complete || wasCompleted);
                rt[key] = qd;
                count++;
            }
            var verb = complete ? "Completed" : "Accepted";
            QuestModPlugin.Log.LogInfo($"{verb} {count} quests (skipped {skipped}, gates={(respectGates ? "respected" : "raw")})");
        }

        public static bool IsChainStep(string name) => QuestRegistry.ChainStepNames.Contains(name);

        public static List<ChainInfo> GetChainList()
        {
            var result = new List<ChainInfo>();
            var rt = QuestDataAccess.GetRuntimeData();

            foreach (var kvp in QuestRegistry.ChainRegistry)
            {
                string chainName = kvp.Key;
                string[] steps = kvp.Value;
                int currentStep = -1;

                for (int i = steps.Length - 1; i >= 0; i--)
                {
                    if (rt != null && rt.Contains(steps[i]))
                    {
                        var qd = rt[steps[i]];
                        if (QuestDataAccess.IsCompleted(qd))
                        {
                            currentStep = i;
                            break;
                        }
                        if (QuestDataAccess.IsAccepted(qd))
                        {
                            currentStep = i;
                            break;
                        }
                    }
                }

                string display = QuestRegistry.ChainDisplayNames.TryGetValue(chainName, out var d) ? d : chainName;

                bool fullyComplete = currentStep == steps.Length - 1
                    && rt != null && rt.Contains(steps[currentStep])
                    && QuestDataAccess.IsCompleted(rt[steps[currentStep]]);

                result.Add(new ChainInfo
                {
                    ChainName = chainName,
                    DisplayName = display,
                    Steps = steps,
                    CurrentStep = currentStep,
                    TotalSteps = steps.Length,
                    IsFullyComplete = fullyComplete,
                });
            }

            return result;
        }

        public static void AdvanceChain(string chainName)
        {
            if (!QuestRegistry.ChainRegistry.TryGetValue(chainName, out var steps)) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;

            int current = -1;
            for (int i = steps.Length - 1; i >= 0; i--)
            {
                if (rt.Contains(steps[i]))
                {
                    var qd = rt[steps[i]];
                    if (QuestDataAccess.IsAccepted(qd) && !QuestDataAccess.IsCompleted(qd))
                    {
                        current = i;
                        break;
                    }
                    if (QuestDataAccess.IsCompleted(qd))
                    {
                        current = i;
                        break;
                    }
                }
            }

            if (current >= 0 && !IsStepCompleted(rt, steps[current]))
            {
                CompleteQuest(steps[current]);
                QuestModPlugin.Log.LogInfo($"Chain '{chainName}': completed step {current + 1}/{steps.Length} ({steps[current]})");
            }

            int next = current + 1;
            if (next < steps.Length)
            {
                AcceptQuest(steps[next]);
                QuestModPlugin.Log.LogInfo($"Chain '{chainName}': accepted step {next + 1}/{steps.Length} ({steps[next]})");
            }
        }

        public static void RewindChain(string chainName)
        {
            if (!QuestRegistry.ChainRegistry.TryGetValue(chainName, out var steps)) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;

            int current = -1;
            for (int i = steps.Length - 1; i >= 0; i--)
            {
                if (rt.Contains(steps[i]))
                {
                    var qd = rt[steps[i]];
                    if (QuestDataAccess.IsAccepted(qd) || QuestDataAccess.IsCompleted(qd))
                    {
                        current = i;
                        break;
                    }
                }
            }

            if (current < 0) return;

            if (!IsStepCompleted(rt, steps[current]))
            {
                UnacceptQuest(steps[current]);
                QuestModPlugin.Log.LogInfo($"Chain '{chainName}': unaccepted step {current + 1}/{steps.Length} ({steps[current]})");
            }
            else
            {
                UncompleteQuest(steps[current]);
                QuestModPlugin.Log.LogInfo($"Chain '{chainName}': uncompleted step {current + 1}/{steps.Length} ({steps[current]})");
            }
        }

        private static bool IsStepCompleted(IDictionary rt, string name)
        {
            if (!rt.Contains(name)) return false;
            return QuestDataAccess.IsCompleted(rt[name]);
        }

        private static object GetAnyValue(IDictionary dict)
        {
            foreach (DictionaryEntry entry in dict)
                return entry.Value;
            return null;
        }
    }
}
