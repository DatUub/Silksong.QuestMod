using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace QuestMod
{
    public static class QuestRegistry
    {
        public static HashSet<string> ExcludedQuests { get; private set; } = new HashSet<string>();
        public static Dictionary<string, string[]> ChainRegistry { get; private set; } = new Dictionary<string, string[]>();
        public static Dictionary<string, string> ChainDisplayNames { get; private set; } = new Dictionary<string, string>();
        public static HashSet<string> ChainStepNames { get; private set; } = new HashSet<string>();
        public static Dictionary<string, string> MutuallyExclusiveQuests { get; private set; } = new Dictionary<string, string>();
        public static Dictionary<string, string> SharedTargetQuests { get; private set; } = new Dictionary<string, string>();
        public static Dictionary<string, string> DisplayNames { get; private set; } = new Dictionary<string, string>();
        public static Dictionary<string, string> QuestCategories { get; private set; } = new Dictionary<string, string>();
        public static string[] Categories { get; private set; } = Array.Empty<string>();

        public static Dictionary<string, int[]> MaxCaps { get; private set; } = new Dictionary<string, int[]>();
        public static Dictionary<string, string[]> ChecklistQuests { get; private set; } = new Dictionary<string, string[]>();
        public static HashSet<string> SequentialQuests { get; private set; } = new HashSet<string>();
        public static HashSet<string> FarmableExcluded { get; private set; } = new HashSet<string>();

        public static int DefaultThreshold { get; private set; } = 17;
        public static Dictionary<string, float> SilkSoulPointValues { get; private set; } = new Dictionary<string, float>();
        public static string[] SilkSoulRequiredQuests { get; private set; } = Array.Empty<string>();

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

            var root = JObject.Parse(json);

            LoadExcluded(root);
            LoadFarmableExclude(root);
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

        private static void LoadExcluded(JObject root)
        {
            var arr = root["excluded"] as JArray;
            if (arr == null) return;
            foreach (var item in arr)
            {
                var val = item.Value<string>();
                if (val != null) ExcludedQuests.Add(val);
            }
        }

        private static void LoadFarmableExclude(JObject root)
        {
            var arr = root["farmableExclude"] as JArray;
            if (arr == null) return;
            foreach (var item in arr)
            {
                var val = item.Value<string>();
                if (val != null) FarmableExcluded.Add(val);
            }
        }

        private static void LoadMutuallyExclusive(JObject root)
        {
            var obj = root["mutuallyExclusive"] as JObject;
            if (obj == null) return;
            foreach (var prop in obj)
            {
                var val = prop.Value?.Value<string>();
                if (val != null) MutuallyExclusiveQuests[prop.Key] = val;
            }
        }

        private static void LoadSharedTargets(JObject root)
        {
            var obj = root["sharedTargets"] as JObject;
            if (obj == null) return;
            foreach (var prop in obj)
            {
                var val = prop.Value?.Value<string>();
                if (val != null) SharedTargetQuests[prop.Key] = val;
            }
        }

        private static void LoadSilkSoul(JObject root)
        {
            var ss = root["silkSoul"] as JObject;
            if (ss == null) return;

            var threshold = ss["defaultThreshold"];
            if (threshold != null)
                DefaultThreshold = threshold.Value<int>();

            var required = ss["requiredQuests"] as JArray;
            if (required != null)
            {
                var list = new List<string>();
                foreach (var item in required)
                {
                    var val = item.Value<string>();
                    if (val != null) list.Add(val);
                }
                SilkSoulRequiredQuests = list.ToArray();
            }

            var points = ss["pointValues"] as JObject;
            if (points != null)
            {
                foreach (var prop in points)
                    SilkSoulPointValues[prop.Key] = prop.Value!.Value<float>();
            }
        }

        private static void LoadMaxCaps(JObject root)
        {
            var caps = root["maxCaps"] as JObject;
            if (caps == null) return;
            foreach (var prop in caps)
            {
                var arr = prop.Value as JArray;
                if (arr == null) continue;
                var list = new List<int>();
                foreach (var val in arr)
                    list.Add(val.Value<int>());
                MaxCaps[prop.Key] = list.ToArray();
            }
        }

        private static void LoadChecklist(JObject root)
        {
            var checklists = root["checklist"] as JObject;
            if (checklists != null)
            {
                foreach (var prop in checklists)
                {
                    var arr = prop.Value as JArray;
                    if (arr == null) continue;
                    var list = new List<string>();
                    foreach (var item in arr)
                    {
                        var val = item.Value<string>();
                        if (val != null) list.Add(val);
                    }
                    ChecklistQuests[prop.Key] = list.ToArray();
                }
            }

            var sequential = root["sequentialQuests"] as JArray;
            if (sequential != null)
            {
                foreach (var item in sequential)
                {
                    var val = item.Value<string>();
                    if (val != null) SequentialQuests.Add(val);
                }
            }
        }

        private static void LoadChains(JObject root)
        {
            var chains = root["chains"] as JObject;
            if (chains == null) return;
            foreach (var chain in chains)
            {
                var chainName = chain.Key;
                var chainObj = chain.Value as JObject;
                if (chainObj == null) continue;

                var display = chainObj["display"]?.Value<string>();
                if (display != null) ChainDisplayNames[chainName] = display;

                var steps = chainObj["steps"] as JArray;
                if (steps != null)
                {
                    var stepList = new List<string>();
                    foreach (var step in steps)
                    {
                        var val = step.Value<string>();
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

        private static void LoadCategories(JObject root)
        {
            var categories = root["categories"] as JObject;
            if (categories == null) return;

            var categoryList = new List<string>();

            foreach (var category in categories)
            {
                var categoryName = category.Key;
                if (categoryName == "Main Story") continue;
                categoryList.Add(categoryName);

                var categoryObj = category.Value as JObject;
                if (categoryObj == null) continue;
                foreach (var quest in categoryObj)
                    LoadQuest(quest.Key, quest.Value as JObject, categoryName);
            }

            var mainStory = categories["Main Story"] as JObject;
            if (mainStory != null)
            {
                foreach (var quest in mainStory)
                    LoadQuest(quest.Key, quest.Value as JObject, null);
            }

            Categories = categoryList.ToArray();
        }

        private static void LoadQuest(string questName, JObject? questObj, string? categoryName)
        {
            if (categoryName != null)
                QuestCategories[questName] = categoryName;

            if (questObj == null) return;

            var display = questObj["display"]?.Value<string>();
            if (display != null) DisplayNames[questName] = display;
        }
    }
}
