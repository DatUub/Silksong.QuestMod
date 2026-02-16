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

        public static void Initialize()
        {
            QuestModPlugin.Log.LogInfo("QuestAcceptance initialized");
        }

        public static void InjectAndAcceptAllQuests()
        {
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

                if (!IsChainPrereqMet(questName))
                {
                    QuestModPlugin.Log.LogInfo($"  SKIP [{questName}]: chain prereq not met");
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

        public static void ForceAcceptAllQuests() => ForceAllQuestsOp(complete: false);
        public static void ForceCompleteAllQuests() => ForceAllQuestsOp(complete: true);

        private static void ForceAllQuestsOp(bool complete)
        {
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

            ModifyAllQuests(rt, complete);

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

        public static void CompleteQuest(string name)
        {
            var (rt, qd) = EnsureQuestEntry(name);
            if (rt == null) return;
            qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: true, wasEver: true);
            rt[name] = qd;
            QuestModPlugin.LogDebugInfo($"Completed: {name}");
        }

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

        private static void ModifyAllQuests(IDictionary rt, bool complete)
        {
            int count = 0;
            var keys = new List<object>();
            foreach (var key in rt.Keys) keys.Add(key);

            foreach (string key in keys)
            {
                if (!IsChainPrereqMet(key))
                    continue;

                var qd = rt[key];
                bool wasCompleted = QuestDataAccess.IsCompleted(qd);
                qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: complete || wasCompleted, wasEver: complete || wasCompleted);
                rt[key] = qd;
                count++;
            }
            var verb = complete ? "Completed" : "Accepted";
            QuestModPlugin.Log.LogInfo($"{verb} {count} quests");
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
