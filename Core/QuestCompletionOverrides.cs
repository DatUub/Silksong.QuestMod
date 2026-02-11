using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public List<QuestTargetInfo> Targets;
    }

    public static class QuestCompletionOverrides
    {
        private static FieldInfo? targetsBackingField;
        private static FieldInfo? countField;

        private static Dictionary<string, int[]> originalCounts = new Dictionary<string, int[]>();
        private static Dictionary<string, int[]> overrideCounts = new Dictionary<string, int[]>();

        private static FullQuestBase[]? cachedQuests;

        public static readonly string[] Categories = { "Gather" };

        private static readonly Dictionary<string, string> questCategories = new Dictionary<string, string>
        {
            { "Brolly Get", "Gather" },
            { "Huntress Quest", "Gather" },
            { "Shiny Bell Goomba", "Gather" },
            { "Rock Rollers", "Gather" },
            { "Pilgrim Rags", "Gather" },
            { "Fine Pins", "Gather" },
            { "Song Pilgrim Cloaks", "Gather" },
            { "Roach Killing", "Gather" },
            { "Crow Feathers", "Gather" },
            { "Huntress Quest Runt", "Gather" },

            { "Belltown House Mid", "Craftmetal" },
            { "Belltown House Start", "Craftmetal" },
            { "Songclave Donation 1", "Craftmetal" },
            { "Songclave Donation 2", "Craftmetal" },
            { "Building Materials (Statue)", "Craftmetal" },
            { "Building Materials (Bridge)", "Craftmetal" },
            { "Building Materials", "Craftmetal" },

            { "Courier Delivery Mask Maker", "Courier" },
            { "Courier Delivery Fleatopia", "Courier" },
            { "Courier Delivery Dustpens Slave", "Courier" },
            { "Courier Delivery Pilgrims Rest", "Courier" },
            { "Courier Delivery Bonebottom", "Courier" },
            { "Courier Delivery Fixer", "Courier" },
            { "Courier Delivery Songclave", "Courier" },

            { "Extractor Blue Worms", "Misc" },
            { "Extractor Blue", "Misc" },
            { "Journal", "Misc" },
            { "Save the Fleas", "Misc" },
            { "Destroy Thread Cores", "Misc" },
            { "Shell Flowers", "Misc" },
            { "Mossberry Collection 1", "Misc" }
        };

        public static string? GetCategory(string questName)
        {
            if (questCategories.TryGetValue(questName, out string cat))
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
                        countField = AccessTools.Field(f.FieldType.GetElementType(), "Count");
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

        private static Array? GetTargetsArray(FullQuestBase quest)
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

        public static string DumpAllTargets()
        {
            if (!IsInitialized || countField == null) return null;
            if (cachedQuests == null) CacheQuests();
            if (cachedQuests == null) return null;

            var lines = new List<string>();
            lines.Add("Quest Name | Type | Target Index | Counter | Count");
            lines.Add("--- | --- | --- | --- | ---");

            var detailLines = new List<string>();
            detailLines.Add("");
            detailLines.Add("## Detailed Target Fields");
            detailLines.Add("");

            foreach (var quest in cachedQuests)
            {
                var questName = quest.name ?? "";
                var typeName = quest.GetType().Name;
                var targets = quest.Targets;
                if (targets == null || targets.Count == 0)
                {
                    lines.Add($"{questName} | {typeName} | - | - | 0 targets");
                    continue;
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    var counter = target.Counter?.ToString() ?? "?";
                    lines.Add($"{questName} | {typeName} | {i} | {counter} | {target.Count}");

                    if (counter == "?")
                    {
                        var arr = GetTargetsArray(quest);
                        if (arr != null && i < arr.Length)
                        {
                            var rawTarget = arr.GetValue(i);
                            var targetType = rawTarget.GetType();
                            detailLines.Add($"### {questName} [target {i}]");
                            detailLines.Add($"- **Struct Type:** `{targetType.FullName}`");

                            foreach (var field in AccessTools.GetDeclaredFields(targetType))
                            {
                                try
                                {
                                    var val = field.GetValue(rawTarget);
                                    string valStr;
                                    if (val == null)
                                        valStr = "null";
                                    else if (val is UnityEngine.Object uobj)
                                        valStr = $"{uobj.GetType().Name} \"{uobj.name}\"";
                                    else
                                        valStr = val.ToString();
                                    detailLines.Add($"- `{field.Name}` ({field.FieldType.Name}): {valStr}");
                                }
                                catch
                                {
                                    detailLines.Add($"- `{field.Name}` ({field.FieldType.Name}): [error reading]");
                                }
                            }
                            detailLines.Add("");
                        }
                    }
                }
            }

            lines.AddRange(detailLines);

            var path = Path.Combine(Path.GetDirectoryName(typeof(QuestModPlugin).Assembly.Location), "QuestTargetDump.md");
            File.WriteAllLines(path, lines);
            QuestModPlugin.Log.LogInfo($"Dumped {cachedQuests.Length} quests to {path}");
            return path;
        }

        public static string DumpCompleteTotalGroups()
        {
            var groupType = AccessTools.TypeByName("QuestCompleteTotalGroup");
            if (groupType == null)
            {
                QuestModPlugin.Log.LogWarning("QuestCompleteTotalGroup type not found");
                return null;
            }
            QuestModPlugin.Log.LogInfo($"Found QuestCompleteTotalGroup as {groupType.FullName}");

            var findMethod = typeof(Resources).GetMethod("FindObjectsOfTypeAll", new[] { typeof(System.Type) });
            var allObjects = findMethod.Invoke(null, new object[] { groupType }) as UnityEngine.Object[];
            if (allObjects == null || allObjects.Length == 0)
            {
                QuestModPlugin.Log.LogWarning("No CompleteTotalGroup instances found");
                return null;
            }

            QuestModPlugin.Log.LogInfo($"Found {allObjects.Length} CompleteTotalGroup instances");

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[");

            for (int g = 0; g < allObjects.Length; g++)
            {
                var obj = allObjects[g];
                sb.AppendLine("  {");
                sb.AppendLine($"    \"name\": \"{EscapeJson(obj.name)}\",");
                sb.AppendLine($"    \"type\": \"{obj.GetType().FullName}\",");
                sb.AppendLine("    \"fields\": {");
                SerializeFields(sb, obj, obj.GetType(), "      ");
                sb.AppendLine("    }");
                sb.Append(g < allObjects.Length - 1 ? "  },\n" : "  }\n");
            }

            sb.AppendLine("]");

            var path = Path.Combine(Path.GetDirectoryName(typeof(QuestModPlugin).Assembly.Location), "CompleteTotalGroups.json");
            File.WriteAllText(path, sb.ToString());
            QuestModPlugin.Log.LogInfo($"Dumped {allObjects.Length} CompleteTotalGroup objects to {path}");
            return path;
        }

        public static string DumpQuestAvailability()
        {
            var allQuests = Resources.FindObjectsOfTypeAll<FullQuestBase>();
            if (allQuests.Length == 0)
            {
                QuestModPlugin.Log.LogWarning("No FullQuestBase objects found");
                return null;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[");

            var fqbType = typeof(FullQuestBase);
            var f_playerDataTest = AccessTools.Field(fqbType, "playerDataTest");
            var f_persistentBoolTests = AccessTools.Field(fqbType, "persistentBoolTests");
            var f_requiredCompleteQuests = AccessTools.Field(fqbType, "requiredCompleteQuests");
            var f_requiredUnlockedTools = AccessTools.Field(fqbType, "requiredUnlockedTools");
            var f_requiredCompleteTotalGroups = AccessTools.Field(fqbType, "requiredCompleteTotalGroups");
            var f_previousQuestStep = AccessTools.Field(fqbType, "previousQuestStep");
            var f_nextQuestStep = AccessTools.Field(fqbType, "nextQuestStep");
            var f_markCompleted = AccessTools.Field(fqbType, "markCompleted");
            var f_cancelIfIncomplete = AccessTools.Field(fqbType, "cancelIfIncomplete");
            var f_hideIfComplete = AccessTools.Field(fqbType, "hideIfComplete");
            var f_getTargetCondition = AccessTools.Field(fqbType, "getTargetCondition");

            for (int i = 0; i < allQuests.Length; i++)
            {
                var q = allQuests[i];
                if (string.IsNullOrEmpty(q.name)) continue;

                sb.AppendLine("  {");
                sb.AppendLine($"    \"name\": \"{EscapeJson(q.name)}\",");

                try { sb.AppendLine($"    \"displayName\": \"{EscapeJson(q.DisplayName.ToString())}\","); }
                catch { sb.AppendLine("    \"displayName\": \"\","); }

                try { sb.AppendLine($"    \"questType\": \"{q.QuestType}\","); }
                catch { sb.AppendLine("    \"questType\": \"unknown\","); }

                try { sb.AppendLine($"    \"isAvailable\": {(q.IsAvailable ? "true" : "false")},"); }
                catch { sb.AppendLine("    \"isAvailable\": null,"); }

                try { sb.AppendLine($"    \"isAccepted\": {(q.IsAccepted ? "true" : "false")},"); }
                catch { sb.AppendLine("    \"isAccepted\": null,"); }

                try { sb.AppendLine($"    \"isCompleted\": {(q.IsCompleted ? "true" : "false")},"); }
                catch { sb.AppendLine("    \"isCompleted\": null,"); }

                try { sb.AppendLine($"    \"isHidden\": {(q.IsHidden ? "true" : "false")},"); }
                catch { sb.AppendLine("    \"isHidden\": null,"); }

                WriteQuestRefField(sb, f_previousQuestStep, q, "previousQuestStep");
                WriteQuestRefField(sb, f_nextQuestStep, q, "nextQuestStep");
                WriteQuestArrayField(sb, f_requiredCompleteQuests, q, "requiredCompleteQuests");
                WriteQuestArrayField(sb, f_markCompleted, q, "markCompleted");
                WriteQuestArrayField(sb, f_cancelIfIncomplete, q, "cancelIfIncomplete");
                WriteQuestArrayField(sb, f_hideIfComplete, q, "hideIfComplete");

                try
                {
                    var tools = f_requiredUnlockedTools?.GetValue(q) as UnityEngine.Object[];
                    if (tools != null && tools.Length > 0)
                    {
                        sb.Append("    \"requiredUnlockedTools\": [");
                        for (int t = 0; t < tools.Length; t++)
                        {
                            sb.Append($"\"{EscapeJson(tools[t] == null ? "null" : tools[t].name)}\"");
                            if (t < tools.Length - 1) sb.Append(", ");
                        }
                        sb.AppendLine("],");
                    }
                    else sb.AppendLine("    \"requiredUnlockedTools\": [],");
                }
                catch { sb.AppendLine("    \"requiredUnlockedTools\": [],"); }

                try
                {
                    var groups = f_requiredCompleteTotalGroups?.GetValue(q) as UnityEngine.Object[];
                    if (groups != null && groups.Length > 0)
                    {
                        sb.Append("    \"requiredCompleteTotalGroups\": [");
                        for (int g = 0; g < groups.Length; g++)
                        {
                            sb.Append($"\"{EscapeJson(groups[g] == null ? "null" : groups[g].name)}\"");
                            if (g < groups.Length - 1) sb.Append(", ");
                        }
                        sb.AppendLine("],");
                    }
                    else sb.AppendLine("    \"requiredCompleteTotalGroups\": [],");
                }
                catch { sb.AppendLine("    \"requiredCompleteTotalGroups\": [],"); }

                if (f_playerDataTest != null)
                {
                    try
                    {
                        var pdt = f_playerDataTest.GetValue(q);
                        if (pdt != null)
                        {
                            sb.AppendLine("    \"playerDataTest\": {");
                            SerializeFields(sb, pdt, pdt.GetType(), "      ");
                            sb.AppendLine("    },");
                        }
                        else sb.AppendLine("    \"playerDataTest\": null,");
                    }
                    catch { sb.AppendLine("    \"playerDataTest\": null,"); }
                }

                if (f_getTargetCondition != null)
                {
                    try
                    {
                        var cond = f_getTargetCondition.GetValue(q);
                        if (cond != null)
                        {
                            sb.AppendLine("    \"getTargetCondition\": {");
                            SerializeFields(sb, cond, cond.GetType(), "      ");
                            sb.AppendLine("    },");
                        }
                        else sb.AppendLine("    \"getTargetCondition\": null,");
                    }
                    catch { sb.AppendLine("    \"getTargetCondition\": null,"); }
                }

                var targets = q.Targets;
                sb.AppendLine("    \"targets\": [");
                if (targets != null)
                {
                    for (int t = 0; t < targets.Count; t++)
                    {
                        var target = targets[t];
                        sb.Append($"      {{ \"item\": \"{EscapeJson(target.ItemName)}\", \"count\": {target.Count} }}");
                        sb.AppendLine(t < targets.Count - 1 ? "," : "");
                    }
                }
                sb.AppendLine("    ]");

                sb.AppendLine(i < allQuests.Length - 1 ? "  }," : "  }");
            }

            sb.AppendLine("]");

            var path = Path.Combine(Path.GetDirectoryName(typeof(QuestModPlugin).Assembly.Location), "QuestAvailability.json");
            File.WriteAllText(path, sb.ToString());
            QuestModPlugin.Log.LogInfo($"Dumped {allQuests.Length} quest availability records to {path}");
            return path;
        }

        private static void WriteQuestRefField(System.Text.StringBuilder sb, FieldInfo field, FullQuestBase q, string jsonKey)
        {
            try
            {
                var val = field?.GetValue(q) as FullQuestBase;
                sb.AppendLine($"    \"{jsonKey}\": {(val == null ? "null" : $"\"{EscapeJson(val.name)}\"")},");
            }
            catch { sb.AppendLine($"    \"{jsonKey}\": null,"); }
        }

        private static void WriteQuestArrayField(System.Text.StringBuilder sb, FieldInfo field, FullQuestBase q, string jsonKey)
        {
            try
            {
                var arr = field?.GetValue(q) as FullQuestBase[];
                if (arr != null && arr.Length > 0)
                {
                    sb.Append($"    \"{jsonKey}\": [");
                    for (int j = 0; j < arr.Length; j++)
                    {
                        sb.Append($"\"{EscapeJson(arr[j] == null ? "null" : arr[j].name)}\"");
                        if (j < arr.Length - 1) sb.Append(", ");
                    }
                    sb.AppendLine("],");
                }
                else sb.AppendLine($"    \"{jsonKey}\": [],");
            }
            catch { sb.AppendLine($"    \"{jsonKey}\": [],"); }
        }

        private static void SerializeFields(System.Text.StringBuilder sb, object obj, System.Type type, string indent)
        {
            var fields = AccessTools.GetDeclaredFields(type);
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (field.Name.StartsWith("<")) continue;

                try
                {
                    var val = field.GetValue(obj);
                    var comma = i < fields.Count - 1 ? "," : "";
                    SerializeValue(sb, field.Name, val, indent, comma);
                }
                catch
                {
                    sb.AppendLine($"{indent}\"{field.Name}\": \"[error reading]\"{(i < fields.Count - 1 ? "," : "")}");
                }
            }
        }

        private static void SerializeValue(System.Text.StringBuilder sb, string name, object val, string indent, string comma)
        {
            if (val == null)
            {
                sb.AppendLine($"{indent}\"{name}\": null{comma}");
                return;
            }

            if (val is string s)
            {
                sb.AppendLine($"{indent}\"{name}\": \"{EscapeJson(s)}\"{comma}");
                return;
            }

            if (val is bool b)
            {
                sb.AppendLine($"{indent}\"{name}\": {(b ? "true" : "false")}{comma}");
                return;
            }

            if (val is int || val is float || val is double || val is long || val is short || val is byte)
            {
                sb.AppendLine($"{indent}\"{name}\": {val}{comma}");
                return;
            }

            if (val is System.Enum e)
            {
                sb.AppendLine($"{indent}\"{name}\": \"{val}\"{comma}");
                return;
            }

            if (val is UnityEngine.Object uobj)
            {
                sb.AppendLine($"{indent}\"{name}\": \"{uobj.GetType().Name}:{EscapeJson(uobj.name)}\"{comma}");
                return;
            }

            if (val is System.Collections.IList list)
            {
                sb.AppendLine($"{indent}\"{name}\": [");
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    var itemComma = i < list.Count - 1 ? "," : "";
                    if (item is UnityEngine.Object uo)
                    {
                        sb.AppendLine($"{indent}  \"{uo.GetType().Name}:{EscapeJson(uo.name)}\"{itemComma}");
                    }
                    else if (item != null && !item.GetType().IsPrimitive && item.GetType() != typeof(string))
                    {
                        sb.AppendLine($"{indent}  {{");
                        SerializeFields(sb, item, item.GetType(), indent + "    ");
                        sb.AppendLine($"{indent}  }}{itemComma}");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}  {(item == null ? "null" : $"\"{EscapeJson(item.ToString())}\"")}{itemComma}");
                    }
                }
                sb.AppendLine($"{indent}]{comma}");
                return;
            }

            if (val is System.Array arr)
            {
                sb.AppendLine($"{indent}\"{name}\": [");
                for (int i = 0; i < arr.Length; i++)
                {
                    var item = arr.GetValue(i);
                    var itemComma = i < arr.Length - 1 ? "," : "";
                    if (item is UnityEngine.Object uo)
                    {
                        sb.AppendLine($"{indent}  \"{uo.GetType().Name}:{EscapeJson(uo.name)}\"{itemComma}");
                    }
                    else if (item != null && !item.GetType().IsPrimitive && item.GetType() != typeof(string))
                    {
                        sb.AppendLine($"{indent}  {{");
                        SerializeFields(sb, item, item.GetType(), indent + "    ");
                        sb.AppendLine($"{indent}  }}{itemComma}");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}  {(item == null ? "null" : $"\"{EscapeJson(item.ToString())}\"")}{itemComma}");
                    }
                }
                sb.AppendLine($"{indent}]{comma}");
                return;
            }

            var valType = val.GetType();
            if (valType.IsValueType && !valType.IsPrimitive && !valType.IsEnum)
            {
                sb.AppendLine($"{indent}\"{name}\": {{");
                SerializeFields(sb, val, valType, indent + "  ");
                sb.AppendLine($"{indent}}}{comma}");
                return;
            }

            sb.AppendLine($"{indent}\"{name}\": \"{EscapeJson(val.ToString())}\"{comma}");
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
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
                if (!questCategories.ContainsKey(questName)) continue;
                if (!QuestModPlugin.IsQuestDiscovered(questName)) continue;

                var targets = quest.Targets;
                if (targets == null || targets.Count == 0) continue;

                var info = new QuestOverrideInfo
                {
                    QuestName = questName,
                    QuestTypeName = quest.GetType().Name,
                    Targets = new List<QuestTargetInfo>()
                };

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

                QuestModPlugin.Log.LogInfo($"Set {questName} target[{targetIndex}] count to {newCount}");
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

                QuestModPlugin.Log.LogInfo($"Set all {arr.Length} targets for {questName} to {newCount}");
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

                QuestModPlugin.Log.LogInfo($"Reset {questName} to original counts");
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

        public static readonly Dictionary<string, string[]> ChecklistQuests = new Dictionary<string, string[]>
        {
            { "Grand Gate Bellshrines", new[] { "The Marrow", "Far Fields", "Greymoor", "Bellhart", "Shellwood" } },
            { "Flea Games", new[] { "Juggle", "Dodge", "Bounce" } },
            { "Great Gourmand", new[] { "Mossberry Stew", "Vintage Nectar", "Courier Supplies", "Coral Ingredient", "Pickled Roach Egg" } },
            { "Mr Mushroom", new[] { "Chapel (Monarch)", "Camp (Black Thread)", "Scorched Field", "Towers (Surgeon)", "Cage (Silken Lie)", "Heart of Frost", "Cradle's Peak" } },
            { "Soul Snare", new[] { "Churchkeeper", "Bell Hermit", "Swamp Bug", "Silk Snare" } }
        };

        public static readonly HashSet<string> SequentialQuests = new HashSet<string> { "Mr Mushroom" };

        public static bool[] GetChecklistStatus(string questName)
        {
            if (!ChecklistQuests.ContainsKey(questName)) return new bool[0];
            if (!IsInitialized || cachedQuests == null) return new bool[ChecklistQuests[questName].Length];

            foreach (var quest in cachedQuests)
            {
                if (quest.name != questName) continue;
                var targets = quest.Targets;
                if (targets == null) return new bool[ChecklistQuests[questName].Length];

                var status = new bool[targets.Count];
                for (int i = 0; i < targets.Count; i++)
                    status[i] = targets[i].Count == 0;
                return status;
            }

            return new bool[ChecklistQuests[questName].Length];
        }

        public static void ToggleChecklistTarget(string questName, int index, bool done)
        {
            SetTargetCount(questName, index, done ? 0 : 1);

            if (!done && SequentialQuests.Contains(questName))
            {
                var labels = ChecklistQuests[questName];
                for (int i = index + 1; i < labels.Length; i++)
                    SetTargetCount(questName, i, 1);
            }
        }
    }
}
