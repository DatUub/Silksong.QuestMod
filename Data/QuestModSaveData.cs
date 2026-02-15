using System.Collections.Generic;

namespace QuestMod
{
    public class QuestModSaveData
    {
        public HashSet<string> InjectedQuests { get; set; } = new();
        public HashSet<string> CompletedQuests { get; set; } = new();
        public Dictionary<string, int> QuestTargetOverrides { get; set; } = new();
        public bool AllQuestsAvailable { get; set; }
        public bool AllQuestsAccepted { get; set; }
    }
}
