using System.Collections;
using System.Collections.Generic;

namespace QuestMod
{
    public static partial class QuestAcceptance
    {
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
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                LastCompletionRefusal = $"chain '{chainName}': legacy save (safety override not set)";
                QuestModPlugin.Log.LogInfo($"AdvanceChain({chainName}): skipped (safety gate)");
                return;
            }
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
                LastCompletionRefusal = null;
                CompleteQuest(steps[current]);
                if (!string.IsNullOrEmpty(LastCompletionRefusal))
                {
                    QuestModPlugin.Log.LogInfo($"Chain '{chainName}': step complete refused — {LastCompletionRefusal}");
                    return;
                }
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
            if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                LastCompletionRefusal = $"chain '{chainName}': legacy save (safety override not set)";
                QuestModPlugin.Log.LogInfo($"RewindChain({chainName}): skipped (safety gate)");
                return;
            }
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

    }
}
