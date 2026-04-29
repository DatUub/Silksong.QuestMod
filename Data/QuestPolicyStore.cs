using System.Collections.Generic;

namespace QuestMod
{
    // Wrapper around QuestModSaveData.QuestPolicies that provides safe access
    // and respects the legacy AllQuestsAvailable bool for backwards compatibility.
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

        // Per-quest availability decision. Falls back to legacy global toggle so
        // existing saves keep behaving as before.
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
            // If a quest is no longer available, it cannot be auto-accepted either.
            if (!value) p.AutoAccept = false;
        }

        public static void SetAutoAccept(string questName, bool value)
        {
            var p = GetOrCreate(questName);
            if (p == null) return;
            p.AutoAccept = value;
            // Auto-accept implies the quest must be available.
            if (value) p.Available = true;
        }

        // Returns every quest name currently flagged for auto-accept.
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
