using System.Collections.Generic;

namespace QuestMod
{
    // Safe accessor over SaveData.QuestPolicies. Respects legacy AllQuestsAvailable.
    public static class QuestPolicyStore
    {
        public static Dictionary<string, QuestPolicy> Map
        {
            get
            {
                var data = QuestModPlugin.Instance?.SaveData;
                if (data == null) return null;
                if (data.QuestPolicies == null)
                    data.QuestPolicies = new Dictionary<string, QuestPolicy>();
                return data.QuestPolicies;
            }
        }

        public static QuestPolicy Get(string questName)
        {
            var map = Map;
            if (map == null || string.IsNullOrEmpty(questName)) return null;
            return map.TryGetValue(questName, out var p) ? p : null;
        }

        public static QuestPolicy GetOrCreate(string questName)
        {
            var map = Map;
            if (map == null || string.IsNullOrEmpty(questName)) return null;
            if (!map.TryGetValue(questName, out var p))
            {
                p = new QuestPolicy();
                map[questName] = p;
            }
            return p;
        }

        // Falls back to legacy global toggle so old saves still work.
        public static bool IsAvailable(string questName)
        {
            if (QuestModPlugin.AllQuestsAvailable)
                return true;
            var p = Get(questName);
            return p != null && p.Available;
        }

        public static bool IsAutoAccept(string questName)
        {
            if (QuestModPlugin.AllQuestsAccepted)
                return true;
            var p = Get(questName);
            return p != null && p.AutoAccept;
        }

        public static void SetAvailable(string questName, bool value)
        {
            var p = GetOrCreate(questName);
            if (p == null) return;
            p.Available = value;
            // Unavailable can't auto-accept.
            if (!value) p.AutoAccept = false;
        }

        public static void SetAutoAccept(string questName, bool value)
        {
            var p = GetOrCreate(questName);
            if (p == null) return;
            p.AutoAccept = value;
            // Auto-accept implies available.
            if (value) p.Available = true;
        }

        // All auto-accept quest names.
        public static IEnumerable<string> AutoAcceptNames()
        {
            var map = Map;
            if (map == null) yield break;
            foreach (var kvp in map)
            {
                if (kvp.Value != null && kvp.Value.AutoAccept)
                    yield return kvp.Key;
            }
        }
    }
}
