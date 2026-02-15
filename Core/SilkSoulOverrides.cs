using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace QuestMod
{
    public static class SilkSoulOverrides
    {
        private static Type groupType;
        private static FieldInfo f_target;
        private static PropertyInfo p_currentValue;
        private static FieldInfo f_requiredCompleteTotalGroups;
        private static object cachedGroup;
        private static bool resolved;

        private static FieldInfo f_entries;
        private static FieldInfo f_entryQuest;
        private static FieldInfo f_entryValue;
        private static Array cachedEntries;
        private static bool entriesResolved;



        private static int? thresholdOverride;
        private static Dictionary<string, float> pointOverrides = new Dictionary<string, float>();

        public static bool TryResolve()
        {
            if (resolved) return cachedGroup != null;

            resolved = true;

            groupType = ReflectionCache.GetType("QuestCompleteTotalGroup");
            if (groupType == null) return false;

            f_target = ReflectionCache.GetField(groupType, "target");
            p_currentValue = ReflectionCache.GetProperty(groupType, "CurrentValueCount");

            var fqbType = ReflectionCache.GetType("FullQuestBase");
            if (fqbType != null)
                f_requiredCompleteTotalGroups = ReflectionCache.GetField(fqbType, "requiredCompleteTotalGroups");

            var questManagerType = ReflectionCache.GetType("QuestManager");
            if (questManagerType == null) return false;

            var getQuest = AccessTools.Method(questManagerType, "GetQuest", new[] { typeof(string) });
            if (getQuest == null) return false;

            var soulSnarePre = getQuest.Invoke(null, new object[] { "Soul Snare Pre" });
            if (soulSnarePre == null) return false;

            if (f_requiredCompleteTotalGroups != null)
            {
                var groups = f_requiredCompleteTotalGroups.GetValue(soulSnarePre) as Array;
                if (groups != null && groups.Length > 0)
                {
                    cachedGroup = groups.GetValue(0);
                    QuestModPlugin.Log.LogInfo("SilkSoul: resolved CompleteTotalGroup");
                    TryResolveEntries();

                    return true;
                }
            }

            QuestModPlugin.Log.LogWarning("SilkSoul: could not resolve group from Soul Snare Pre");
            return false;
        }

        private static void TryResolveEntries()
        {
            if (entriesResolved || cachedGroup == null) return;
            entriesResolved = true;

            foreach (var field in AccessTools.GetDeclaredFields(groupType))
            {
                if (!field.FieldType.IsArray) continue;

                var arr = field.GetValue(cachedGroup) as Array;
                if (arr == null || arr.Length == 0) continue;

                var elemType = arr.GetValue(0).GetType();
                FieldInfo questField = null;
                FieldInfo valueField = null;

                foreach (var ef in AccessTools.GetDeclaredFields(elemType))
                {
                    if (ef.FieldType.Name.Contains("Quest") || ef.FieldType.Name.Contains("FullQuestBase"))
                        questField = ef;
                    else if (ef.FieldType == typeof(float) || ef.FieldType == typeof(int))
                        valueField = ef;
                }

                if (questField != null && valueField != null)
                {
                    f_entries = field;
                    f_entryQuest = questField;
                    f_entryValue = valueField;
                    cachedEntries = arr;
                    QuestModPlugin.Log.LogInfo($"SilkSoul: resolved {arr.Length} entries via field '{field.Name}' (quest={questField.Name}, value={valueField.Name})");
                    return;
                }
            }

            QuestModPlugin.Log.LogInfo("SilkSoul: no entry array found — per-quest overrides not available");
        }

        public static bool HasEntries => cachedEntries != null && f_entryQuest != null && f_entryValue != null;

        public struct PointEntry
        {
            public string QuestName;
            public float Value;
            public float DefaultValue;
            public int EntryIndex;
        }

        public static List<PointEntry> GetPointEntries()
        {
            var result = new List<PointEntry>();
            if (!HasEntries) return result;

            for (int i = 0; i < cachedEntries.Length; i++)
            {
                var entry = cachedEntries.GetValue(i);
                var questObj = f_entryQuest.GetValue(entry) as UnityEngine.Object;
                if (questObj == null) continue;

                string name = questObj.name;
                float val;
                if (f_entryValue.FieldType == typeof(float))
                    val = (float)f_entryValue.GetValue(entry);
                else
                    val = (int)f_entryValue.GetValue(entry);

                float defaultVal = QuestRegistry.SilkSoulPointValues.ContainsKey(name) ? QuestRegistry.SilkSoulPointValues[name] : val;

                result.Add(new PointEntry
                {
                    QuestName = name,
                    Value = val,
                    DefaultValue = defaultVal,
                    EntryIndex = i
                });
            }

            return result;
        }

        public static bool SetPointValue(int entryIndex, float value)
        {
            if (!HasEntries || entryIndex < 0 || entryIndex >= cachedEntries.Length)
                return false;

            var entry = cachedEntries.GetValue(entryIndex);
            var questObj = f_entryQuest.GetValue(entry) as UnityEngine.Object;
            string questName = questObj != null ? questObj.name : "";

            object typedValue = f_entryValue.FieldType == typeof(float) ? (object)value : (object)(int)value;
            ReflectionCache.WriteToArray(cachedEntries, entryIndex, entry, f_entryValue, typedValue);

            pointOverrides[questName] = value;
            QuestModPlugin.LogDebugInfo($"SilkSoul: set {questName} value to {value}");
            return true;
        }

        public static void ResetAllPointValues()
        {
            if (!HasEntries) return;

            for (int i = 0; i < cachedEntries.Length; i++)
            {
                var entry = cachedEntries.GetValue(i);
                var questObj = f_entryQuest.GetValue(entry) as UnityEngine.Object;
                if (questObj == null) continue;

                string name = questObj.name;
                if (QuestRegistry.SilkSoulPointValues.ContainsKey(name))
                {
                    float def = QuestRegistry.SilkSoulPointValues[name];
                    object typedValue = f_entryValue.FieldType == typeof(float) ? (object)def : (object)(int)def;
                    ReflectionCache.WriteToArray(cachedEntries, i, entry, f_entryValue, typedValue);
                }
            }

            pointOverrides.Clear();
        }

        public static int GetThreshold()
        {
            if (thresholdOverride.HasValue) return thresholdOverride.Value;
            return ReflectionCache.Read(cachedGroup, f_target, QuestRegistry.DefaultThreshold);
        }

        public static void SetThreshold(int value)
        {
            if (value < 0) value = 0;
            thresholdOverride = value;
            ReflectionCache.Write(cachedGroup, f_target, value);
            QuestModPlugin.LogDebugInfo($"SilkSoul: threshold set to {value}");
        }

        public static void ResetThreshold()
        {
            thresholdOverride = null;
            ReflectionCache.Write(cachedGroup, f_target, QuestRegistry.DefaultThreshold);
        }

        public static int GetCurrentPoints()
        {
            return ReflectionCache.Read(cachedGroup, p_currentValue, -1);
        }

        public static void Reset()
        {
            cachedGroup = null;
            cachedEntries = null;
            resolved = false;
            entriesResolved = false;
            thresholdOverride = null;
            pointOverrides.Clear();
        }
    }
}
