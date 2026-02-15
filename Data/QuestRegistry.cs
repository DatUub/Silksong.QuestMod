using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace QuestMod
{
    public static class QuestRegistry
    {
        public static HashSet<string> ExcludedQuests { get; private set; } = [];
        public static Dictionary<string, string[]> ChainRegistry { get; private set; } = [];
        public static Dictionary<string, string> ChainDisplayNames { get; private set; } = [];
        public static HashSet<string> ChainStepNames { get; private set; } = [];
        public static Dictionary<string, string> MutuallyExclusiveQuests { get; private set; } = [];
        public static Dictionary<string, string> SharedTargetQuests { get; private set; } = [];
        public static Dictionary<string, string> DisplayNames { get; private set; } = [];
        public static Dictionary<string, string> QuestCategories { get; private set; } = [];
        public static string[] Categories { get; private set; } = [];

        public static Dictionary<string, int[]> MaxCaps { get; private set; } = [];
        public static Dictionary<string, string[]> ChecklistQuests { get; private set; } = [];
        public static HashSet<string> SequentialQuests { get; private set; } = [];

        public static int DefaultThreshold { get; private set; } = 17;
        public static Dictionary<string, float> SilkSoulPointValues { get; private set; } = [];
        public static string[] SilkSoulRequiredQuests { get; private set; } = [];

        public static bool IsLoaded { get; private set; }

        public static void Load()
        {
            if (IsLoaded) return;

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "QuestMod.Data.QuestCapabilities.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                QuestModPlugin.Log.LogError($"Failed to load embedded resource: {resourceName}");
                return;
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            LoadExcluded(root);
            LoadMutuallyExclusive(root);
            LoadSharedTargets(root);
            LoadSilkSoul(root);
            LoadMaxCaps(root);
            LoadChecklist(root);
            LoadChains(root);
            LoadCategories(root);

            IsLoaded = true;
            QuestModPlugin.Log.LogInfo($"QuestRegistry loaded: {DisplayNames.Count} quests, {ChainRegistry.Count} chains, {ChecklistQuests.Count} checklists, {MaxCaps.Count} max caps");
        }

        private static void LoadExcluded(JsonElement root)
        {
            if (!root.TryGetProperty("excluded", out var arr)) return;
            foreach (var item in arr.EnumerateArray())
            {
                var val = item.GetString();
                if (val != null) ExcludedQuests.Add(val);
            }
        }

        private static void LoadMutuallyExclusive(JsonElement root)
        {
            if (!root.TryGetProperty("mutuallyExclusive", out var obj)) return;
            foreach (var prop in obj.EnumerateObject())
            {
                var val = prop.Value.GetString();
                if (val != null) MutuallyExclusiveQuests[prop.Name] = val;
            }
        }

        private static void LoadSharedTargets(JsonElement root)
        {
            if (!root.TryGetProperty("sharedTargets", out var obj)) return;
            foreach (var prop in obj.EnumerateObject())
            {
                var val = prop.Value.GetString();
                if (val != null) SharedTargetQuests[prop.Name] = val;
            }
        }

        private static void LoadSilkSoul(JsonElement root)
        {
            if (!root.TryGetProperty("silkSoul", out var ss)) return;

            if (ss.TryGetProperty("defaultThreshold", out var threshold))
                DefaultThreshold = threshold.GetInt32();

            if (ss.TryGetProperty("requiredQuests", out var required))
            {
                var list = new List<string>();
                foreach (var item in required.EnumerateArray())
                {
                    var val = item.GetString();
                    if (val != null) list.Add(val);
                }
                SilkSoulRequiredQuests = list.ToArray();
            }

            if (ss.TryGetProperty("pointValues", out var points))
            {
                foreach (var prop in points.EnumerateObject())
                    SilkSoulPointValues[prop.Name] = (float)prop.Value.GetDouble();
            }
        }

        private static void LoadMaxCaps(JsonElement root)
        {
            if (!root.TryGetProperty("maxCaps", out var caps)) return;
            foreach (var prop in caps.EnumerateObject())
            {
                var list = new List<int>();
                foreach (var val in prop.Value.EnumerateArray())
                    list.Add(val.GetInt32());
                MaxCaps[prop.Name] = list.ToArray();
            }
        }

        private static void LoadChecklist(JsonElement root)
        {
            if (root.TryGetProperty("checklist", out var checklists))
            {
                foreach (var prop in checklists.EnumerateObject())
                {
                    var list = new List<string>();
                    foreach (var item in prop.Value.EnumerateArray())
                    {
                        var val = item.GetString();
                        if (val != null) list.Add(val);
                    }
                    ChecklistQuests[prop.Name] = list.ToArray();
                }
            }

            if (root.TryGetProperty("sequentialQuests", out var sequential))
            {
                foreach (var item in sequential.EnumerateArray())
                {
                    var val = item.GetString();
                    if (val != null) SequentialQuests.Add(val);
                }
            }
        }

        private static void LoadChains(JsonElement root)
        {
            if (!root.TryGetProperty("chains", out var chains)) return;
            foreach (var chain in chains.EnumerateObject())
            {
                var chainName = chain.Name;
                var chainObj = chain.Value;

                if (chainObj.TryGetProperty("display", out var display))
                {
                    var displayVal = display.GetString();
                    if (displayVal != null) ChainDisplayNames[chainName] = displayVal;
                }

                if (chainObj.TryGetProperty("steps", out var steps))
                {
                    var stepList = new List<string>();
                    foreach (var step in steps.EnumerateArray())
                    {
                        var val = step.GetString();
                        if (val != null)
                        {
                            stepList.Add(val);
                            ChainStepNames.Add(val);
                        }
                    }
                    ChainRegistry[chainName] = stepList.ToArray();
                }
            }
        }

        private static void LoadCategories(JsonElement root)
        {
            if (!root.TryGetProperty("categories", out var categories)) return;

            var categoryList = new List<string>();

            foreach (var category in categories.EnumerateObject())
            {
                var categoryName = category.Name;
                if (categoryName == "Main Story") continue;
                categoryList.Add(categoryName);

                foreach (var quest in category.Value.EnumerateObject())
                    LoadQuest(quest, categoryName);
            }

            if (categories.TryGetProperty("Main Story", out var mainStory))
            {
                foreach (var quest in mainStory.EnumerateObject())
                    LoadQuest(quest, null);
            }

            Categories = categoryList.ToArray();
        }

        private static void LoadQuest(JsonProperty quest, string? categoryName)
        {
            var questName = quest.Name;

            if (categoryName != null)
                QuestCategories[questName] = categoryName;

            if (quest.Value.TryGetProperty("display", out var display))
            {
                var displayVal = display.GetString();
                if (displayVal != null) DisplayNames[questName] = displayVal;
            }
        }
    }
}
