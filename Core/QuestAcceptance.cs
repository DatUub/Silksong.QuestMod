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

    public static partial class QuestAcceptance
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
        // Returns true (no gate) for non-sequential quests. stageIndex is
        // 0-based; the PD pattern uses {stage} as 1-indexed.
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

        // Single source of truth for accept-eligibility under the current mode.
        // Pure: always true. Else: chain + exclusion + availableConditions.
        // `reason` is the rejection cause, empty on pass.
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

            foreach (var questName in QuestRegistry.AllQuests)
            {
                if (string.IsNullOrEmpty(questName)) continue;
                if (seen.Contains(questName)) continue;

                result.Add(new QuestInfo
                {
                    Name = questName,
                    DisplayName = GetDisplayName(questName),
                    IsAccepted = false,
                    IsCompleted = false
                });
            }

            result.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public static void AcceptQuest(string name)
        {
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                LastCompletionRefusal = $"{name}: legacy save (safety override not set)";
                QuestModPlugin.Log.LogInfo($"AcceptQuest({name}): skipped (safety gate)");
                return;
            }
            var (rt, qd) = EnsureQuestEntry(name);
            if (rt == null) return;
            qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: QuestDataAccess.IsCompleted(qd), wasEver: QuestDataAccess.IsCompleted(qd));
            rt[name] = qd;
            QuestModPlugin.LogDebugInfo($"Accepted: {name}");
        }

        public static void CompleteQuest(string name) => CompleteQuest(name, skipExtraConditions: false);

        /// <summary>
        /// Flag-only Complete would look like success but not run minigame/world FSM.
        /// Single product rule for CompleteQuest, mass complete, and GUI lock styling.
        /// Remote Complete opt-in (or skipExtraConditions after remote side-effects) bypasses.
        /// </summary>
        public static bool BlocksFlagOnlyComplete(string questName)
            => QuestRegistry.IsWorldStateComplete(questName)
               && !QuestModPlugin.IsFullRemoteCompleteEnabled;

        // skipExtraConditions=true bypasses custom-req + world-state gates.
        // RemoteComplete uses it after side effects so a just-consumed counter can't fail re-eval.
        internal static void CompleteQuest(string name, bool skipExtraConditions)
        {
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                LastCompletionRefusal = $"{name}: legacy save (safety override not set)";
                QuestModPlugin.Log.LogInfo($"CompleteQuest({name}): skipped (safety gate)");
                return;
            }
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

            if (!skipExtraConditions && BlocksFlagOnlyComplete(name))
            {
                string shown = GetDisplayName(name);
                LastCompletionRefusal =
                    $"{shown}: needs in-world progress (minigame/world state). " +
                    "Flag-only Complete will not finish it. Play it in the world, or enable Tools → Remote Complete " +
                    "(still may not drive minigame scores). If you already flag-completed: Undo on the completed row.";
                QuestModPlugin.Log.LogInfo($"CompleteQuest refused (world-state): {name}");
                return;
            }

            var (rt, qd) = EnsureQuestEntry(name);
            if (rt == null) return;
            qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: true, wasEver: true);
            rt[name] = qd;
            QuestModPlugin.LogDebugInfo($"Completed: {name}");
        }
        // Last refusal reason. GUI shows it as a tooltip.
        public static string? LastCompletionRefusal { get; internal set; }

        public static void UnacceptQuest(string name)
        {
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                LastCompletionRefusal = $"{name}: legacy save (safety override not set)";
                QuestModPlugin.Log.LogInfo($"UnacceptQuest({name}): skipped (safety gate)");
                return;
            }
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
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                LastCompletionRefusal = $"{name}: legacy save (safety override not set)";
                QuestModPlugin.Log.LogInfo($"UncompleteQuest({name}): skipped (safety gate)");
                return;
            }
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

    }
}
