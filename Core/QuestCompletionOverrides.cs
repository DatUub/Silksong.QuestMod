using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace QuestMod
{
    public struct QuestTargetInfo
    {
        public string CounterName;
        public string DisplayName;
        public int CurrentCount;
        public int OriginalCount;
        public int TargetIndex;
    }

    public struct QuestOverrideInfo
    {
        public string QuestName;
        public string QuestTypeName;
        public int CompletedCount;
        public List<QuestTargetInfo> Targets;
    }

    public static class QuestCompletionOverrides
    {
        private static FieldInfo? targetsBackingField;
        private static FieldInfo? countField;

        private static Dictionary<string, int[]> originalCounts = new Dictionary<string, int[]>();
        private static Dictionary<string, int[]> overrideCounts = new Dictionary<string, int[]>();

        private static FullQuestBase[]? cachedQuests;



        public static int GetMaxCap(string questName, int targetIndex)
        {
            if (QuestRegistry.MaxCaps.TryGetValue(questName, out int[] caps) && targetIndex < caps.Length)
                return caps[targetIndex];
            return -1;
        }

        public static int ClampCount(string questName, int targetIndex, int value)
        {
            if (value < 0) value = 0;

            if (QuestModPlugin.DevRemoveLimits.Value)
                return value;

            int cap = GetMaxCap(questName, targetIndex);
            if (cap > 0 && value > cap) value = cap;
            return value;
        }

        public static int GetQoLCount(int originalCount)
        {
            return (originalCount + 1) / 2;
        }

        public static void ApplyPresetAll(string preset)
        {
            if (!IsInitialized) return;
            if (cachedQuests == null) CacheQuests();
            if (cachedQuests == null || countField == null) return;

            foreach (var quest in cachedQuests)
            {
                var questName = quest.name ?? "";
            if (!QuestRegistry.QuestCategories.ContainsKey(questName)) continue;
                if (!originalCounts.ContainsKey(questName)) continue;

                var orig = originalCounts[questName];
                for (int i = 0; i < orig.Length; i++)
                {
                    int newCount;
                    switch (preset)
                    {
                        case "set1":
                            newCount = 1;
                            break;
                        case "qol":
                            newCount = GetQoLCount(orig[i]);
                            break;
                        default:
                            newCount = orig[i];
                            break;
                    }
                    SetTargetCount(questName, i, newCount);
                }
            }

            QuestModPlugin.LogDebugInfo($"Applied preset '{preset}' to all quests");
        }

        public static string? GetCategory(string questName)
        {
            if (QuestRegistry.QuestCategories.TryGetValue(questName, out string cat))
                return cat;
            return null;
        }

        public static List<QuestOverrideInfo> GetQuestsByCategory(string category)
        {
            var all = GetAllQuestsWithTargets();
            var filtered = new List<QuestOverrideInfo>();
            foreach (var q in all)
            {
                if (GetCategory(q.QuestName) == category)
                    filtered.Add(q);
            }
            return filtered;
        }

        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            var type = typeof(FullQuestBase);
            while (type != null && type != typeof(UnityEngine.Object))
            {
                foreach (var f in AccessTools.GetDeclaredFields(type))
                {
                    if (f.FieldType.IsArray && f.FieldType.GetElementType()?.Name == "QuestTarget")
                    {
                        targetsBackingField = f;
                        countField = ReflectionCache.GetField(f.FieldType.GetElementType(), "Count");
                        QuestModPlugin.Log.LogInfo($"Found targets backing field: {f.Name} on {type.Name}");
                        break;
                    }
                }
                if (targetsBackingField != null) break;
                type = type.BaseType;
            }

            if (targetsBackingField == null || countField == null)
            {
                QuestModPlugin.Log.LogWarning("QuestCompletionOverrides: Could not find targets backing field");
                return;
            }

            IsInitialized = true;
            QuestModPlugin.Log.LogInfo("QuestCompletionOverrides initialized");
        }

        internal static Array GetTargetsArray(FullQuestBase quest)
        {
            return targetsBackingField?.GetValue(quest) as Array;
        }

        public static void CacheQuests()
        {
            if (!IsInitialized) return;
            cachedQuests = Resources.FindObjectsOfTypeAll<FullQuestBase>();
            QuestModPlugin.Log.LogInfo($"Cached {cachedQuests.Length} FullQuestBase objects");

            originalCounts.Clear();
            foreach (var quest in cachedQuests)
            {
                var questName = quest.name ?? "";
                var targets = quest.Targets;
                if (targets == null || targets.Count == 0) continue;

                var counts = new int[targets.Count];
                for (int i = 0; i < targets.Count; i++)
                {
                    counts[i] = targets[i].Count;
                }
                originalCounts[questName] = counts;
            }
        }


        public static List<QuestOverrideInfo> GetAllQuestsWithTargets()
        {
            var result = new List<QuestOverrideInfo>();
            if (!IsInitialized) return result;
            if (cachedQuests == null) CacheQuests();
            if (cachedQuests == null || countField == null) return result;

            foreach (var quest in cachedQuests)
            {
                var questName = quest.name ?? "";
                if (!QuestRegistry.QuestCategories.ContainsKey(questName)) continue;
                if (!QuestModPlugin.IsQuestDiscovered(questName)) continue;

                var targets = quest.Targets;
                if (targets == null || targets.Count == 0) continue;

                var info = new QuestOverrideInfo
                {
                    QuestName = questName,
                    QuestTypeName = quest.GetType().Name,
                    CompletedCount = 0,
                    Targets = new List<QuestTargetInfo>()
                };

                var rt = QuestDataAccess.GetRuntimeData();
                if (rt != null && rt.Contains(questName))
                {
                    var qd = rt[questName];
                    info.CompletedCount = QuestDataAccess.GetCompletedCount(qd);
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    var currentCount = target.Count;
                    var counterName = target.Counter?.ToString() ?? "?";
                    var displayName = counterName;

                    int origCount = currentCount;
                    if (originalCounts.ContainsKey(questName) && i < originalCounts[questName].Length)
                        origCount = originalCounts[questName][i];

                    info.Targets.Add(new QuestTargetInfo
                    {
                        CounterName = counterName,
                        DisplayName = displayName,
                        CurrentCount = currentCount,
                        OriginalCount = origCount,
                        TargetIndex = i
                    });
                }

                result.Add(info);
            }

            return result;
        }

        public static bool SetTargetCount(string questName, int targetIndex, int newCount)
        {
            if (!IsInitialized) return false;
            if (cachedQuests == null) CacheQuests();
            if (cachedQuests == null || countField == null) return false;

            newCount = ClampCount(questName, targetIndex, newCount);

            foreach (var quest in cachedQuests)
            {
                if (quest.name != questName) continue;

                var arr = GetTargetsArray(quest);
                if (arr == null || targetIndex >= arr.Length) return false;

                var target = arr.GetValue(targetIndex);
                countField.SetValue(target, newCount);
                arr.SetValue(target, targetIndex);

                if (!overrideCounts.ContainsKey(questName))
                {
                    var counts = new int[arr.Length];
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var t = arr.GetValue(i);
                        counts[i] = (int)countField.GetValue(t);
                    }
                    overrideCounts[questName] = counts;
                }
                else
                {
                    overrideCounts[questName][targetIndex] = newCount;
                }

                var saveData = QuestModPlugin.Instance.SaveData;
                if (saveData != null)
                    saveData.QuestTargetOverrides[$"{questName}:{targetIndex}"] = newCount;

                QuestModPlugin.LogDebugInfo($"Set {questName} target[{targetIndex}] count to {newCount}");

                if (QuestRegistry.SharedTargetQuests.TryGetValue(questName, out string linkedQuest))
                {
                    foreach (var q in cachedQuests)
                    {
                        if (q.name != linkedQuest) continue;
                        var linkedArr = GetTargetsArray(q);
                        if (linkedArr != null && targetIndex < linkedArr.Length)
                        {
                            var linkedTarget = linkedArr.GetValue(targetIndex);
                            countField.SetValue(linkedTarget, newCount);
                            linkedArr.SetValue(linkedTarget, targetIndex);

                            if (!overrideCounts.ContainsKey(linkedQuest))
                            {
                                var lCounts = new int[linkedArr.Length];
                                for (int i = 0; i < linkedArr.Length; i++)
                                {
                                    var lt = linkedArr.GetValue(i);
                                    lCounts[i] = (int)countField.GetValue(lt);
                                }
                                overrideCounts[linkedQuest] = lCounts;
                            }
                            else
                            {
                                overrideCounts[linkedQuest][targetIndex] = newCount;
                            }

                            var linkedSaveData = QuestModPlugin.Instance.SaveData;
                            if (linkedSaveData != null)
                                linkedSaveData.QuestTargetOverrides[$"{linkedQuest}:{targetIndex}"] = newCount;
                        }
                        break;
                    }
                }

                return true;
            }

            return false;
        }

        public static void SetAllTargetCounts(string questName, int newCount)
        {
            if (!IsInitialized) return;
            if (cachedQuests == null) CacheQuests();
            if (cachedQuests == null || countField == null) return;

            foreach (var quest in cachedQuests)
            {
                if (quest.name != questName) continue;

                var arr = GetTargetsArray(quest);
                if (arr == null) return;

                for (int i = 0; i < arr.Length; i++)
                {
                    var target = arr.GetValue(i);
                    countField.SetValue(target, newCount);
                    arr.SetValue(target, i);
                }

                QuestModPlugin.LogDebugInfo($"Set all {arr.Length} targets for {questName} to {newCount}");
                return;
            }
        }

        public static void ResetToOriginal(string questName)
        {
            if (!IsInitialized || !originalCounts.ContainsKey(questName)) return;
            if (cachedQuests == null || countField == null) return;

            foreach (var quest in cachedQuests)
            {
                if (quest.name != questName) continue;

                var arr = GetTargetsArray(quest);
                if (arr == null) return;

                var orig = originalCounts[questName];
                for (int i = 0; i < arr.Length && i < orig.Length; i++)
                {
                    var target = arr.GetValue(i);
                    countField.SetValue(target, orig[i]);
                    arr.SetValue(target, i);
                }

                overrideCounts.Remove(questName);

                var saveData = QuestModPlugin.Instance.SaveData;
                if (saveData != null)
                {
                    var keysToRemove = new List<string>();
                    foreach (var key in saveData.QuestTargetOverrides.Keys)
                    {
                        if (key.StartsWith(questName + ":"))
                            keysToRemove.Add(key);
                    }
                    foreach (var key in keysToRemove)
                        saveData.QuestTargetOverrides.Remove(key);
                }

                QuestModPlugin.LogDebugInfo($"Reset {questName} to original counts");
                return;
            }
        }

        public static void ResetAll()
        {
            foreach (var questName in new List<string>(originalCounts.Keys))
                ResetToOriginal(questName);
        }

        public static void ApplySavedOverrides()
        {
            var saveData = QuestModPlugin.Instance.SaveData;
            if (saveData == null || saveData.QuestTargetOverrides.Count == 0) return;

            if (cachedQuests == null) CacheQuests();

            int applied = 0;
            foreach (var kvp in saveData.QuestTargetOverrides)
            {
                var parts = kvp.Key.Split(':');
                if (parts.Length != 2) continue;
                string questName = parts[0];
                if (!int.TryParse(parts[1], out int targetIndex)) continue;

                if (SetTargetCount(questName, targetIndex, kvp.Value))
                    applied++;
            }

            QuestModPlugin.Log.LogInfo($"Applied {applied} saved quest target overrides");
        }



        public static bool[] GetChecklistStatus(string questName)
        {
            if (!QuestRegistry.ChecklistQuests.ContainsKey(questName)) return new bool[0];
            if (!IsInitialized || cachedQuests == null) return new bool[QuestRegistry.ChecklistQuests[questName].Length];

            foreach (var quest in cachedQuests)
            {
                if (quest.name != questName) continue;
                var targets = quest.Targets;
                if (targets == null) return new bool[QuestRegistry.ChecklistQuests[questName].Length];

                var status = new bool[targets.Count];
                for (int i = 0; i < targets.Count; i++)
                    status[i] = targets[i].Count == 0;
                return status;
            }

            return new bool[QuestRegistry.ChecklistQuests[questName].Length];
        }

        public static void ToggleChecklistTarget(string questName, int index, bool done)
        {
            SetTargetCount(questName, index, done ? 0 : 1);

            if (!done && QuestRegistry.SequentialQuests.Contains(questName))
            {
                var labels = QuestRegistry.ChecklistQuests[questName];
                for (int i = index + 1; i < labels.Length; i++)
                    SetTargetCount(questName, i, 1);
            }
        }
    }
}
