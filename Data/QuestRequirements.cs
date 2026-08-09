using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using Newtonsoft.Json.Linq;

namespace QuestMod
{
    // Custom requirements rules engine. See docs/CustomRequirements.md.
    // ApplyActivePreset sweeps matched targets at load. EvaluateExtraConditions
    // gates CompleteQuest for quests listed in perQuest.
    public static class QuestRequirements
    {
        public const string UserFileName = "QuestRequirements.user.json";

        // questId or counter -> tags
        public static Dictionary<string, HashSet<string>> Tags { get; private set; } = new();
        public static HashSet<string> PlayerDataWhitelist { get; private set; } = new();
        public static Dictionary<string, Preset> Presets { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, PerQuestEntry> PerQuest { get; private set; } = new();

        public static bool IsLoaded { get; private set; }

        // Bumps on every tag-map mutation. GUI dirty-check uses RulesVersion.
        public static int RulesVersion { get; private set; }

        // Back-compat alias for older callers / SelfTest.
        public static int TagsVersion => RulesVersion;

        public static void AddTag(string questName, string tag)
        {
            if (string.IsNullOrWhiteSpace(questName) || string.IsNullOrWhiteSpace(tag)) return;
            if (!Tags.ContainsKey(questName))
                Tags[questName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Tags[questName].Add(tag.Trim()))
            {
                RulesVersion++;
                SaveTagsToUserOverlay();
            }
        }

        public static void RemoveTag(string questName, string tag)
        {
            if (Tags.TryGetValue(questName, out var set) && set.Remove(tag))
            {
                if (set.Count == 0) Tags.Remove(questName);
                RulesVersion++;
                SaveTagsToUserOverlay();
            }
        }

        // Snapshot/restore for GUI open→close rollback (rules overlay = tag map).
        // On-disk user JSON still uses the "tags" key (schema; do not rename).
        public static string ExportRulesJson()
        {
            var obj = new JObject();
            foreach (var kvp in Tags)
            {
                if (kvp.Value.Count == 0) continue;
                var arr = new JArray();
                foreach (var t in kvp.Value) arr.Add(t);
                obj[kvp.Key] = arr;
            }
            return obj.ToString(Newtonsoft.Json.Formatting.None);
        }

        public static void ImportRulesJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            var parsed = JObject.Parse(json);
            Tags.Clear();
            foreach (var prop in parsed.Properties())
            {
                if (prop.Value is JArray arr)
                {
                    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var t in arr)
                        if (t.Type == JTokenType.String) set.Add(t.Value<string>()!);
                    Tags[prop.Name] = set;
                }
            }
            RulesVersion++;
            SaveTagsToUserOverlay();
        }

        // Back-compat aliases.
        public static string ExportTagsJson() => ExportRulesJson();
        public static void ImportTagsJson(string json) => ImportRulesJson(json);

        public static void ClearAllTagOverrides()
        {
            try
            {
                string dir = Path.Combine(BepInEx.Paths.ConfigPath, "QuestMod");
                string path = Path.Combine(dir, UserFileName);
                if (File.Exists(path))
                {
                    JObject root;
                    try { root = JObject.Parse(File.ReadAllText(path)); }
                    catch { return; }
                    root.Remove("tags");
                    File.WriteAllText(path, root.ToString(Newtonsoft.Json.Formatting.Indented));
                }
                Reload();
                QuestModPlugin.Log.LogInfo("Cleared all tag overrides; reverted to embedded baseline.");
            }
            catch (Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"ClearAllTagOverrides failed: {ex.Message}");
            }
        }

        public static HashSet<string> GetAllUniqueTags()
        {
            var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in Tags)
                foreach (var t in kvp.Value) all.Add(t);
            return all;
        }

        // Writes Tags to user overlay, preserves other sections.
        private static void SaveTagsToUserOverlay()
        {
            try
            {
                string dir = Path.Combine(BepInEx.Paths.ConfigPath, "QuestMod");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, UserFileName);
                JObject root;
                if (File.Exists(path))
                {
                    try { root = JObject.Parse(File.ReadAllText(path)); }
                    catch { root = new JObject { ["version"] = 1 }; }
                }
                else
                {
                    root = new JObject { ["version"] = 1 };
                }
                var tagsObj = new JObject();
                foreach (var kvp in Tags)
                {
                    if (kvp.Value.Count == 0) continue;
                    var arr = new JArray();
                    foreach (var t in kvp.Value) arr.Add(t);
                    tagsObj[kvp.Key] = arr;
                }
                root["tags"] = tagsObj;
                // Atomic write via tmp + rename. Crash leaves .tmp, not truncated file.
                // netstandard2.1 has no overwrite-Move, so delete-then-move.
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, root.ToString(Newtonsoft.Json.Formatting.Indented));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                QuestModPlugin.LogDebugInfo($"Saved tags to {path}");
            }
            catch (Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"Failed to save tags to user overlay: {ex.Message}");
            }
        }
        public static string ActivePresetName { get; private set; } = "vanilla";

        public sealed class Preset
        {
            public string Name = "";
            public string Description = "";
            public List<Rule> Rules = new();
        }

        public sealed class Rule
        {
            public MatchExpr? Match;
            public int? Set;
            public float? Scale;
            public string Round = "ceil"; // ceil | floor | nearest
            public int? Min;
            public int? Max;
        }

        public sealed class MatchExpr
        {
            public HashSet<string>? QuestId;
            public HashSet<string>? Category;
            public HashSet<string>? AnyTag;
            public HashSet<string>? AllTag;
            public MatchExpr? Not;

            public bool IsEmpty =>
                (QuestId == null || QuestId.Count == 0) &&
                (Category == null || Category.Count == 0) &&
                (AnyTag == null || AnyTag.Count == 0) &&
                (AllTag == null || AllTag.Count == 0) &&
                Not == null;
        }

        public sealed class PerQuestEntry
        {
            public Dictionary<int, int> TargetCounts = new();
            public List<ExtraCondition> ExtraConditions = new();
            // Gates IsAvailable under Adjusted only (Pure/vanilla bypass).
            // Used for arena-reuse wishes like Beastfly Hunt that need another
            // boss defeated.
            public List<ExtraCondition> AvailableConditions = new();
        }

        public sealed class ExtraCondition
        {
            public string Kind = ""; // playerData | questCompleted | tagAccepted
            public string Field = "";
            public string Op = "==";
            public JToken? Value;
            public string Quest = "";
            public HashSet<string>? AnyTag;
            public int Count;
        }

        // ---------- Loading ----------

        public static void Reload()
        {
            IsLoaded = false;
            Load();
        }

        public static void Load()
        {
            if (IsLoaded) return;

            JObject baseline = LoadEmbedded();
            JObject? userOverlay = LoadUserOverlay();
            JObject merged = userOverlay == null ? baseline : MergeObjects(baseline, userOverlay);

            ParseRoot(merged);

            IsLoaded = true;
            QuestModPlugin.Log.LogInfo(
                $"QuestRequirements loaded: {Tags.Count} tagged ids, {Presets.Count} presets, " +
                $"{PerQuest.Count} per-quest overrides (overlay={(userOverlay != null ? "yes" : "no")})");
        }

        private static JObject LoadEmbedded()
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(QuestModConstants.EmbeddedQuestRequirementsJson);
            if (stream == null)
            {
                QuestModPlugin.Log.LogError("Embedded QuestRequirements.json missing");
                return new JObject();
            }
            using var sr = new StreamReader(stream);
            return JObject.Parse(sr.ReadToEnd());
        }

        private static JObject? LoadUserOverlay()
        {
            try
            {
                string dir = Path.Combine(Paths.ConfigPath, "QuestMod");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, UserFileName);
                if (!File.Exists(path))
                {
                    // Seed placeholder so users can find the file.
                    File.WriteAllText(path,
                        "{\n  \"_doc\": \"See docs/CustomRequirements.md. This file overlays the embedded baseline.\",\n  \"version\": 1\n}\n");
                    return null;
                }
                return JObject.Parse(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                QuestModPlugin.Log.LogWarning($"Failed to load user QuestRequirements overlay: {ex.Message}");
                return null;
            }
        }

        // Objects merge recursively, arrays replace.
        private static JObject MergeObjects(JObject baseline, JObject overlay)
        {
            var result = (JObject)baseline.DeepClone();
            foreach (var prop in overlay.Properties())
            {
                if (prop.Value is JObject ovlObj && result[prop.Name] is JObject baseObj)
                    result[prop.Name] = MergeObjects(baseObj, ovlObj);
                else
                    result[prop.Name] = prop.Value.DeepClone();
            }
            return result;
        }

        private static void ParseRoot(JObject root)
        {
            Tags.Clear();
            // Always bump so GUI re-checks.
            RulesVersion++;
            PlayerDataWhitelist.Clear();
            Presets.Clear();
            PerQuest.Clear();

            if (root["playerDataWhitelist"] is JArray wl)
                foreach (var t in wl)
                    if (t.Type == JTokenType.String) PlayerDataWhitelist.Add(t.Value<string>()!);

            if (root["tags"] is JObject tagsObj)
            {
                foreach (var prop in tagsObj.Properties())
                {
                    if (prop.Value is JArray arr)
                    {
                        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var t in arr)
                            if (t.Type == JTokenType.String) set.Add(t.Value<string>()!);
                        Tags[prop.Name] = set;
                    }
                }
            }

            if (root["presets"] is JObject presetsObj)
            {
                foreach (var prop in presetsObj.Properties())
                {
                    if (prop.Value is JObject pObj)
                        Presets[prop.Name] = ParsePreset(prop.Name, pObj);
                }
            }
            // Always provide a vanilla no-op preset.
            if (!Presets.ContainsKey("vanilla"))
                Presets["vanilla"] = new Preset { Name = "vanilla", Description = "no changes" };

            if (root["perQuest"] is JObject pqObj)
            {
                foreach (var prop in pqObj.Properties())
                {
                    if (prop.Value is JObject qObj)
                        PerQuest[prop.Name] = ParsePerQuest(qObj);
                }
            }
        }

        private static Preset ParsePreset(string name, JObject obj)
        {
            var p = new Preset
            {
                Name = name,
                Description = obj["description"]?.Value<string>() ?? "",
            };
            if (obj["rules"] is JArray rules)
                foreach (var r in rules)
                    if (r is JObject ro) p.Rules.Add(ParseRule(ro));
            return p;
        }

        private static Rule ParseRule(JObject obj)
        {
            var r = new Rule
            {
                Match = obj["match"] is JObject m ? ParseMatch(m) : null,
                Round = obj["round"]?.Value<string>() ?? "ceil",
            };
            if (obj["set"]?.Type == JTokenType.Integer) r.Set = obj["set"]!.Value<int>();
            if (obj["scale"]?.Type == JTokenType.Float || obj["scale"]?.Type == JTokenType.Integer)
                r.Scale = obj["scale"]!.Value<float>();
            if (obj["min"]?.Type == JTokenType.Integer) r.Min = obj["min"]!.Value<int>();
            if (obj["max"]?.Type == JTokenType.Integer) r.Max = obj["max"]!.Value<int>();
            return r;
        }

        // Cap `not` nesting -- a malicious overlay could stack-overflow Awake.
        private const int MaxMatchDepth = 16;

        private static MatchExpr ParseMatch(JObject obj) => ParseMatch(obj, 0);

        private static MatchExpr ParseMatch(JObject obj, int depth)
        {
            if (depth >= MaxMatchDepth)
            {
                QuestModPlugin.Log.LogWarning(
                    $"QuestRequirements: match expression nested deeper than {MaxMatchDepth}; treating inner 'not' as empty.");
                return new MatchExpr();
            }
            var m = new MatchExpr();
            m.QuestId = ReadStringSet(obj["questId"]);
            m.Category = ReadStringSet(obj["category"]);
            m.AnyTag = ReadStringSet(obj["anyTag"]);
            m.AllTag = ReadStringSet(obj["allTag"]);
            if (obj["not"] is JObject notObj) m.Not = ParseMatch(notObj, depth + 1);
            return m;
        }

        private static HashSet<string>? ReadStringSet(JToken? t)
        {
            if (t is JArray a)
            {
                var s = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var v in a)
                    if (v.Type == JTokenType.String) s.Add(v.Value<string>()!);
                return s;
            }
            return null;
        }

        private static PerQuestEntry ParsePerQuest(JObject obj)
        {
            var pq = new PerQuestEntry();
            if (obj["targets"] is JObject targets)
            {
                foreach (var prop in targets.Properties())
                {
                    if (!int.TryParse(prop.Name, out int idx)) continue;
                    if (prop.Value is JObject tObj && tObj["count"]?.Type == JTokenType.Integer)
                        pq.TargetCounts[idx] = tObj["count"]!.Value<int>();
                }
            }
            if (obj["extraConditions"] is JArray conds)
            {
                foreach (var c in conds)
                {
                    if (c is not JObject co) continue;
                    pq.ExtraConditions.Add(ParseExtraCondition(co));
                }
            }
            // availableConditions has the same shape as extraConditions
            // but gates IsAvailable instead of CompleteQuest.
            if (obj["availableConditions"] is JArray availConds)
            {
                foreach (var c in availConds)
                {
                    if (c is not JObject co) continue;
                    pq.AvailableConditions.Add(ParseExtraCondition(co));
                }
            }
            return pq;
        }

        private static ExtraCondition ParseExtraCondition(JObject co)
        {
            return new ExtraCondition
            {
                Kind = co["kind"]?.Value<string>() ?? "",
                Field = co["field"]?.Value<string>() ?? "",
                Op = co["op"]?.Value<string>() ?? "==",
                Value = co["value"],
                Quest = co["quest"]?.Value<string>() ?? "",
                AnyTag = ReadStringSet(co["anyTag"]),
                Count = co["count"]?.Value<int>() ?? 0,
            };
        }

        // ---------- Matching ----------

        public static bool MatchQuest(MatchExpr? expr, string questName) => MatchQuest(expr, questName, 0);

        private static bool MatchQuest(MatchExpr? expr, string questName, int depth)
        {
            if (expr == null || expr.IsEmpty) return true;
            if (depth >= MaxMatchDepth) return true;

            if (expr.QuestId != null && expr.QuestId.Count > 0 && !expr.QuestId.Contains(questName))
                return false;

            if (expr.Category != null && expr.Category.Count > 0)
            {
                var cat = QuestCompletionOverrides.GetCategory(questName);
                if (cat == null || !expr.Category.Contains(cat)) return false;
            }

            if (expr.AnyTag != null && expr.AnyTag.Count > 0)
            {
                if (!Tags.TryGetValue(questName, out var qTags)) return false;
                bool any = false;
                foreach (var t in expr.AnyTag) if (qTags.Contains(t)) { any = true; break; }
                if (!any) return false;
            }

            if (expr.AllTag != null && expr.AllTag.Count > 0)
            {
                if (!Tags.TryGetValue(questName, out var qTags)) return false;
                foreach (var t in expr.AllTag) if (!qTags.Contains(t)) return false;
            }

            if (expr.Not != null && MatchQuest(expr.Not, questName, depth + 1)) return false;

            return true;
        }

        // ---------- Apply ----------

        public static void SetActivePreset(string name)
        {
            if (string.IsNullOrEmpty(name)) name = "vanilla";
            // Fall back to vanilla so the GUI never points at a ghost preset.
            if (!Presets.ContainsKey(name))
            {
                QuestModPlugin.Log?.LogWarning($"SetActivePreset: unknown preset '{name}', falling back to vanilla");
                name = "vanilla";
            }
            ActivePresetName = name;
            // Persist on the active save so reload restores the preset.
            var data = QuestModPlugin.Instance?.SaveData;
            if (data != null && data.ActiveDslPreset != name)
                data.ActiveDslPreset = name;
            // Apply now or in-session swaps stay stale.
            if (QuestCompletionOverrides.IsInitialized)
                ApplyActivePreset();
        }

        public static string[] GetPresetNames()
        {
            var names = new List<string>(Presets.Keys);
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names.ToArray();
        }

        // Apply preset + per-quest overrides. Skips slider-touched targets.
        public static void ApplyActivePreset()
        {
            if (!IsLoaded) return;
            if (!QuestCompletionOverrides.IsInitialized) return;

            var saveData = QuestModPlugin.Instance?.SaveData;
            var sliderTouched = saveData?.QuestTargetOverrides ?? new Dictionary<string, int>();

            Preset? preset = null;
            if (!Presets.TryGetValue(ActivePresetName, out preset))
                preset = Presets.TryGetValue("vanilla", out var v) ? v : null;

            int presetApplied = 0;
            int perQuestApplied = 0;

            // Iterate via the public API.
            foreach (var info in QuestCompletionOverrides.GetAllQuestsWithTargets())
            {
                string qName = info.QuestName;
                for (int i = 0; i < info.Targets.Count; i++)
                {
                    string sliderKey = $"{qName}:{i}";
                    bool sliderTouchedThis = sliderTouched.ContainsKey(sliderKey);

                    int original = info.Targets[i].OriginalCount;
                    int newCount = info.Targets[i].CurrentCount;
                    bool changed = false;

                    if (!sliderTouchedThis && preset != null)
                    {
                        foreach (var rule in preset.Rules)
                        {
                            if (!MatchQuest(rule.Match, qName)) continue;
                            int candidate = newCount;
                            if (rule.Set.HasValue) candidate = rule.Set.Value;
                            if (rule.Scale.HasValue) candidate = ApplyRound(original * rule.Scale.Value, rule.Round);
                            if (rule.Min.HasValue && candidate < rule.Min.Value) candidate = rule.Min.Value;
                            if (rule.Max.HasValue && candidate > rule.Max.Value) candidate = rule.Max.Value;
                            if (candidate != newCount)
                            {
                                newCount = candidate;
                                changed = true;
                            }
                        }
                    }

                    // Per-quest override wins.
                    if (PerQuest.TryGetValue(qName, out var pq) && pq.TargetCounts.TryGetValue(i, out int forced))
                    {
                        if (forced != newCount)
                        {
                            newCount = forced;
                            changed = true;
                            perQuestApplied++;
                        }
                    }
                    else if (changed)
                    {
                        presetApplied++;
                    }

                    if (changed)
                    {
                        // Transient -- preset values mustn't mark slots as slider-touched.
                        QuestCompletionOverrides.SetTargetCountTransient(qName, i, newCount);
                    }
                }
            }

            QuestModPlugin.Log.LogInfo(
                $"QuestRequirements: applied preset '{ActivePresetName}' " +
                $"(preset={presetApplied}, perQuest={perQuestApplied})");
        }

        private static int ApplyRound(float v, string mode)
        {
            return mode switch
            {
                "floor" => (int)Math.Floor(v),
                "nearest" => (int)Math.Round(v, MidpointRounding.AwayFromZero),
                _ => (int)Math.Ceiling(v),
            };
        }

        // ---------- Extra-condition evaluation ----------

        public struct ExtraConditionResult
        {
            public bool Pass;
            public string? Reason;
        }

        public static ExtraConditionResult EvaluateExtraConditions(string questName)
        {
            if (!IsLoaded) return new ExtraConditionResult { Pass = true };
            if (!PerQuest.TryGetValue(questName, out var pq)) return new ExtraConditionResult { Pass = true };
            if (pq.ExtraConditions.Count == 0) return new ExtraConditionResult { Pass = true };

            foreach (var c in pq.ExtraConditions)
            {
                if (!EvalCondition(c, out string reason))
                    return new ExtraConditionResult { Pass = false, Reason = reason };
            }
            return new ExtraConditionResult { Pass = true };
        }

        // Evaluates availableConditions for QuestAvailabilityPatch (Adjusted).
        // Empty conditions or unloaded = pass.
        public static ExtraConditionResult EvaluateAvailableConditions(string questName)
        {
            if (!IsLoaded) return new ExtraConditionResult { Pass = true };
            if (!PerQuest.TryGetValue(questName, out var pq)) return new ExtraConditionResult { Pass = true };
            if (pq.AvailableConditions.Count == 0) return new ExtraConditionResult { Pass = true };

            foreach (var c in pq.AvailableConditions)
            {
                if (!EvalCondition(c, out string reason))
                    return new ExtraConditionResult { Pass = false, Reason = reason };
            }
            return new ExtraConditionResult { Pass = true };
        }

        private static bool EvalCondition(ExtraCondition c, out string reason)
        {
            reason = "";
            switch (c.Kind)
            {
                case "playerData":
                    return EvalPlayerData(c, out reason);

                case "questCompleted":
                    {
                        var rt = QuestDataAccess.GetRuntimeData();
                        if (rt == null || !rt.Contains(c.Quest))
                        {
                            reason = $"requires quest '{c.Quest}' completed (not in runtime data)";
                            return false;
                        }
                        if (!QuestDataAccess.IsCompleted(rt[c.Quest]))
                        {
                            reason = $"requires quest '{c.Quest}' completed";
                            return false;
                        }
                        return true;
                    }

                case "tagAccepted":
                    {
                        var rt = QuestDataAccess.GetRuntimeData();
                        if (rt == null || c.AnyTag == null) { reason = "tagAccepted: missing data"; return false; }
                        int hits = 0;
                        foreach (var kvp in Tags)
                        {
                            bool tagMatch = false;
                            foreach (var t in c.AnyTag) if (kvp.Value.Contains(t)) { tagMatch = true; break; }
                            if (!tagMatch) continue;
                            if (rt.Contains(kvp.Key) && QuestDataAccess.IsAccepted(rt[kvp.Key])) hits++;
                        }
                        if (hits < c.Count)
                        {
                            reason = $"requires {c.Count} accepted with tag(s) {string.Join(",", c.AnyTag)} (have {hits})";
                            return false;
                        }
                        return true;
                    }

                default:
                    reason = $"unknown condition kind '{c.Kind}'";
                    return false;
            }
        }

        private static bool EvalPlayerData(ExtraCondition c, out string reason)
        {
            reason = "";
            if (PlayerData.instance == null) { reason = "no PlayerData"; return false; }
            if (!PlayerDataWhitelist.Contains(c.Field))
            {
                reason = $"playerData field '{c.Field}' not in whitelist";
                return false;
            }

            var f = typeof(PlayerData).GetField(c.Field, BindingFlags.Public | BindingFlags.Instance);
            if (f == null) { reason = $"playerData field '{c.Field}' not found"; return false; }

            object? actual = f.GetValue(PlayerData.instance);
            if (actual == null) { reason = $"playerData '{c.Field}' is null"; return false; }

            // Compare numeric or bool.
            if (actual is bool ab)
            {
                if (c.Value == null || c.Value.Type != JTokenType.Boolean)
                {
                    reason = $"playerData '{c.Field}' is bool but condition value is not boolean";
                    return false;
                }
                // Bool ops are == / !=. Reject others instead of silent coerce.
                if (c.Op != "==" && c.Op != "!=" && !string.IsNullOrEmpty(c.Op))
                {
                    reason = $"playerData '{c.Field}' is bool but op '{c.Op}' is not == or !=";
                    return false;
                }
                bool want = c.Value.Value<bool>();
                bool ok = c.Op == "!=" ? ab != want : ab == want;
                if (!ok) reason = $"{c.Field} ({ab}) {c.Op} {want} failed";
                return ok;
            }

            double a = Convert.ToDouble(actual);
            // Catch non-numeric tokens so a bad overlay can't crash IsAvailable.
            double b;
            try { b = c.Value?.ToObject<double>() ?? 0.0; }
            catch
            {
                reason = $"playerData condition '{c.Field}' value not numeric";
                return false;
            }
            bool pass = c.Op switch
            {
                ">=" => a >= b,
                ">" => a > b,
                "<=" => a <= b,
                "<" => a < b,
                "!=" => Math.Abs(a - b) > 0.0001,
                _ => Math.Abs(a - b) <= 0.0001,
            };
            if (!pass) reason = $"{c.Field} ({a}) {c.Op} {b} failed";
            return pass;
        }
    }
}
