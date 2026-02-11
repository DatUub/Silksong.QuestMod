using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuestMod
{
    public struct QuestInfo
    {
        public string Name;
        public string DisplayName;
        public bool IsAccepted;
        public bool IsCompleted;
    }

    public struct ChainInfo
    {
        public string ChainName;
        public string DisplayName;
        public string[] Steps;
        public int CurrentStep;
        public int TotalSteps;
        public bool IsFullyComplete;
    }

    public static class QuestAcceptance
    {
        private static readonly HashSet<string> ExcludedQuests = new HashSet<string>
        {
            "Courier Delivery Bonebottom",
            "Courier Delivery Pilgrims Rest",
            "Courier Delivery Songclave",
            "Courier Delivery Fleatopia",
            "Courier Delivery Mask Maker",
            "Courier Delivery Dustpens Slave",
            "Courier Delivery Fixer",
        };

        private static readonly Dictionary<string, string[]> ChainRegistry = new Dictionary<string, string[]>
        {
            { "Main Story", new[] { "Black Thread Pt0", "Black Thread Pt1 Shamans", "Diving Bell Pt1 Inspect", "Diving Bell Pt2 Ballow", "Diving Bell Pt3 Descend", "Black Thread Pt2 Abyss", "Black Thread Pt3 Escape", "Black Thread Pt4 Return", "Black Thread Pt5 Heart", "Black Thread Pt6 Flower" } },
            { "Citadel", new[] { "Citadel Seeker", "Citadel Investigate", "Citadel Ascent", "Citadel Ascent Melodies", "Citadel Ascent Lift", "Citadel Ascent Silk Defeat" } },
            { "Sprintmaster", new[] { "Sprintmaster Pre", "Sprintmaster Race" } },
            { "Soul Snare", new[] { "Soul Snare Pre", "Soul Snare" } },
            { "Crow Feathers", new[] { "Crow Feathers Pre", "Crow Feathers" } },
            { "Mossberry", new[] { "Mossberry Collection Pre", "Mossberry Collection 1" } },
            { "Save the Fleas", new[] { "Save the Fleas Pre", "Save the Fleas" } },
            { "Flea Games", new[] { "Flea Games Pre", "Flea Games" } },
            { "Steel Sentinel", new[] { "Steel Sentinel", "Steel Sentinel Pt2" } },
            { "Pinstress Battle", new[] { "Pinstress Battle Pre", "Pinstress Battle" } },
        };

        private static readonly Dictionary<string, string> ChainDisplayNames = new Dictionary<string, string>
        {
            { "Main Story", "Main Story" },
            { "Citadel", "Pharloom's Crown" },
            { "Sprintmaster", "Fastest in Pharloom" },
            { "Soul Snare", "Silk and Soul" },
            { "Crow Feathers", "Crawbug Clearing" },
            { "Mossberry", "Berry Picking" },
            { "Save the Fleas", "The Lost Fleas" },
            { "Flea Games", "Ecstasy of the End" },
            { "Steel Sentinel", "A Vassal Lost" },
            { "Pinstress Battle", "Fatal Resolve" },
        };

        private static readonly HashSet<string> ChainStepNames;

        static QuestAcceptance()
        {
            ChainStepNames = new HashSet<string>();
            foreach (var chain in ChainRegistry.Values)
            {
                foreach (var step in chain)
                    ChainStepNames.Add(step);
            }
        }

        private static readonly Dictionary<string, string> displayNames = new Dictionary<string, string>
        {
            { "Grand Gate Bellshrines", "Grand Gate" },
            { "Bellbeast Rescue", "Beast in the Bells" },
            { "Citadel Investigate", "Silent Halls" },
            { "Citadel Seeker", "The Great Citadel" },
            { "Citadel Ascent", "Pharloom's Crown" },
            { "Citadel Ascent Melodies", "Pharloom's Crown" },
            { "Citadel Ascent Lift", "Pharloom's Crown" },
            { "Citadel Ascent Silk Defeat", "Pale Monarch" },
            { "Silk Defeat Snare", "Soul Snare" },
            { "The Threadspun Town", "The Threadspun Town" },
            { "Diving Bell Pt1 Inspect", "The Dark Below" },
            { "Diving Bell Pt2 Ballow", "The Dark Below" },
            { "Diving Bell Pt3 Descend", "The Dark Below" },
            { "Black Thread Pt0", "After the Fall" },
            { "Black Thread Pt1 Shamans", "Awaiting the End" },
            { "Black Thread Pt2 Abyss", "The Dark Below" },
            { "Black Thread Pt3 Escape", "Return to Pharloom" },
            { "Black Thread Pt4 Return", "Spell Seeker" },
            { "Black Thread Pt5 Heart", "The Old Hearts" },
            { "Black Thread Pt6 Flower", "Last Dive" },
            { "Brolly Get", "Flexile Spines" },
            { "Huntress Quest", "Broodfeast" },
            { "Huntress Quest Runt", "Runtfeast" },
            { "Shiny Bell Goomba", "Silver Bells" },
            { "Rock Rollers", "Volatile Flintbeetles" },
            { "Pilgrim Rags", "Garb of the Pilgrims" },
            { "Fine Pins", "Fine Pins" },
            { "Song Pilgrim Cloaks", "Cloaks of the Choir" },
            { "Roach Killing", "Roach Guts" },
            { "Crow Feathers", "Crawbug Clearing" },
            { "Crow Feathers Pre", "Crawbug Clearing" },
            { "Belltown House Start", "Restoration of Bellhart" },
            { "Belltown House Mid", "Bellhart's Glory" },
            { "Songclave Donation 1", "Building Up Songclave" },
            { "Songclave Donation 2", "Strengthening Songclave" },
            { "Building Materials", "Bone Bottom Repairs" },
            { "Building Materials (Bridge)", "A Lifesaving Bridge" },
            { "Building Materials (Statue)", "An Icon of Hope" },
            { "Courier Delivery Bonebottom", "Bone Bottom Supplies" },
            { "Courier Delivery Dustpens Slave", "Queen's Egg" },
            { "Courier Delivery Fleatopia", "Fleatopia Supplies" },
            { "Courier Delivery Fixer", "Survivor's Camp Supplies" },
            { "Courier Delivery Mask Maker", "Liquid Lacquer" },
            { "Courier Delivery Pilgrims Rest", "Pilgrim's Rest Supplies" },
            { "Courier Delivery Songclave", "Songclave Supplies" },
            { "Save Courier Short", "My Missing Courier" },
            { "Save Courier Tall", "My Missing Brother" },
            { "Great Gourmand", "Great Taste of Pharloom" },
            { "Soul Snare", "Silk and Soul" },
            { "Soul Snare Pre", "Silk and Soul" },
            { "Flea Games", "Ecstasy of the End" },
            { "Flea Games Pre", "Ecstasy of the End" },
            { "Mr Mushroom", "Passing of the Age" },
            { "Extractor Blue", "Alchemist's Assistant" },
            { "Extractor Blue Worms", "Advanced Alchemy" },
            { "Shell Flowers", "Rite of the Pollip" },
            { "Mossberry Collection 1", "Berry Picking" },
            { "Mossberry Collection Pre", "Berry Picking" },
            { "Save the Fleas", "The Lost Fleas" },
            { "Save the Fleas Pre", "The Lost Fleas" },
            { "Destroy Thread Cores", "Dark Hearts" },
            { "Journal", "Bugs of Pharloom" },
            { "A Pinsmiths Tools", "Pinmaster's Oil" },
            { "Ant Trapper", "The Hidden Hunter" },
            { "Beastfly Hunt", "Savage Beastfly" },
            { "Broodmother Hunt", "The Wailing Mother" },
            { "Doctor Curse Cure", "Infestation Operation" },
            { "Skull King", "The Terrible Tyrant" },
            { "Sprintmaster Race", "Fastest in Pharloom" },
            { "Steel Sentinel", "A Vassal Lost" },
            { "Steel Sentinel Pt2", "A Vassal Lost" },
            { "Garmond Black Threaded", "Hero's Call" },
            { "Pinstress Battle", "Fatal Resolve" },
            { "Pinstress Battle Pre", "Fatal Resolve" },
            { "Save City Merchant", "The Wandering Merchant" },
            { "Save City Merchant Bridge", "The Lost Merchant" },
            { "Save Sherma", "Balm for the Wounded" },
            { "Shakra Final Quest", "Trail's End" },
            { "Song Knight", "Final Audience" },
            { "Sprintmaster Pre", "Fastest in Pharloom" },
            { "Tormented Trobbio", "Pain, Anguish and Misery" },
            { "Wood Witch Curse", "Rite of Rebirth" },
        };

        public static string GetDisplayName(string internalName)
        {
            if (displayNames.TryGetValue(internalName, out string name))
                return name;
            return internalName;
        }

        public static void Initialize()
        {
            QuestModPlugin.Log.LogInfo("QuestAcceptance initialized");
        }

        public static void InjectAndAcceptAllQuests()
        {
            if (PlayerData.instance == null)
            {
                QuestModPlugin.Log.LogWarning("InjectAndAcceptAllQuests: PlayerData not available yet");
                return;
            }

            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null)
            {
                QuestModPlugin.Log.LogWarning("InjectAndAcceptAllQuests: RuntimeData not available yet");
                return;
            }

            var allQuests = Resources.FindObjectsOfTypeAll<FullQuestBase>();
            int injected = 0;
            int skipped = 0;

            foreach (var quest in allQuests)
            {
                var questName = quest.name;
                if (string.IsNullOrEmpty(questName))
                    continue;

                if (ExcludedQuests.Contains(questName))
                {
                    skipped++;
                    continue;
                }

                if (rt.Contains(questName))
                    continue;

                if (!QuestModPlugin.IsQuestDiscovered(questName))
                {
                    skipped++;
                    continue;
                }

                var template = GetAnyValue(rt);
                if (template == null) continue;
                var newData = QuestDataAccess.SetFields(template, seen: true, accepted: true, completed: false, wasEver: false);
                rt[questName] = newData;
                injected++;
            }

            QuestModPlugin.Log.LogInfo($"Injected {injected} new quests into RuntimeData (skipped {skipped} excluded, total ScriptableObjects: {allQuests.Length})");

            AcceptAllQuests();
        }

        public static void ForceAcceptAllQuests()
        {
            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;

            bool savedActState = PlayerData.instance.blackThreadWorld;

            var allQuests = Resources.FindObjectsOfTypeAll<FullQuestBase>();
            int injected = 0;

            foreach (var quest in allQuests)
            {
                var questName = quest.name;
                if (string.IsNullOrEmpty(questName)) continue;
                if (ExcludedQuests.Contains(questName)) continue;
                if (rt.Contains(questName)) continue;

                var template = GetAnyValue(rt);
                if (template == null) continue;
                var newData = QuestDataAccess.SetFields(template, seen: true, accepted: true, completed: false, wasEver: false);
                rt[questName] = newData;
                injected++;
            }

            AcceptAllQuests();

            PlayerData.instance.blackThreadWorld = savedActState;
            QuestModPlugin.Log.LogInfo($"Force accepted ALL quests (injected {injected} new, total ScriptableObjects: {allQuests.Length}), act state preserved");
        }

        public static void ForceCompleteAllQuests()
        {
            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;

            bool savedActState = PlayerData.instance.blackThreadWorld;

            var allQuests = Resources.FindObjectsOfTypeAll<FullQuestBase>();
            int injected = 0;

            foreach (var quest in allQuests)
            {
                var questName = quest.name;
                if (string.IsNullOrEmpty(questName)) continue;
                if (ExcludedQuests.Contains(questName)) continue;
                if (rt.Contains(questName)) continue;

                var template = GetAnyValue(rt);
                if (template == null) continue;
                var newData = QuestDataAccess.SetFields(template, seen: true, accepted: true, completed: true, wasEver: true);
                rt[questName] = newData;
                injected++;
            }

            CompleteAllQuests();

            PlayerData.instance.blackThreadWorld = savedActState;
            QuestModPlugin.Log.LogInfo($"Force completed ALL quests (injected {injected} new, total ScriptableObjects: {allQuests.Length}), act state preserved");
        }

        public static void AcceptAllQuests()
        {
            QuestModPlugin.Log.LogInfo("AcceptAllQuests called");

            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;

            int count = 0;
            var keys = new List<object>();
            foreach (var key in rt.Keys) keys.Add(key);

            foreach (string key in keys)
            {
                var qd = rt[key];
                qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: QuestDataAccess.IsCompleted(qd), wasEver: QuestDataAccess.IsCompleted(qd));
                rt[key] = qd;
                count++;
            }
            QuestModPlugin.Log.LogInfo($"Accepted {count} quests");
        }

        public static void ListQuests()
        {
            QuestModPlugin.Log.LogInfo("ListQuests called");

            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;

            QuestModPlugin.Log.LogInfo("=== Quest List ===");
            int count = 0;
            foreach (DictionaryEntry kvp in rt)
            {
                var qd = kvp.Value;
                QuestModPlugin.Log.LogInfo($"  {kvp.Key}: Seen={QuestDataAccess.HasBeenSeen(qd)}, Accepted={QuestDataAccess.IsAccepted(qd)}, Completed={QuestDataAccess.IsCompleted(qd)}");
                count++;
            }
            QuestModPlugin.Log.LogInfo($"Listed {count} quests");

            DiscoverQuestObjects();
        }

        private static void DiscoverQuestObjects()
        {
            QuestModPlugin.Log.LogInfo("=== Discovering FullQuestBase Objects ===");

            var allQuests = Resources.FindObjectsOfTypeAll<FullQuestBase>();
            QuestModPlugin.Log.LogInfo($"Found {allQuests.Length} FullQuestBase objects");

            foreach (var quest in allQuests)
            {
                var questName = quest.name ?? "?";
                var targets = quest.Targets;
                if (targets == null || targets.Count == 0)
                    continue;

                var sb = new System.Text.StringBuilder();
                sb.Append($"  {questName}: ");

                foreach (var target in targets)
                {
                    sb.Append($"[{target.ItemName}x{target.Count}] ");
                }

                QuestModPlugin.Log.LogInfo(sb.ToString());
            }
        }

        public static List<QuestInfo> GetQuestList()
        {
            var result = new List<QuestInfo>();
            if (PlayerData.instance == null) return result;
            var rt = QuestDataAccess.GetRuntimeData();

            var seen = new HashSet<string>();

            if (rt != null)
            {
                foreach (DictionaryEntry kvp in rt)
                {
                    var key = kvp.Key as string;
                    if (key == null) continue;
                    seen.Add(key);
                    result.Add(new QuestInfo
                    {
                        Name = key,
                        DisplayName = GetDisplayName(key),
                        IsAccepted = QuestDataAccess.IsAccepted(kvp.Value),
                        IsCompleted = QuestDataAccess.IsCompleted(kvp.Value)
                    });
                }
            }

            var allQuests = Resources.FindObjectsOfTypeAll<FullQuestBase>();
            foreach (var quest in allQuests)
            {
                if (string.IsNullOrEmpty(quest.name)) continue;
                if (seen.Contains(quest.name)) continue;

                result.Add(new QuestInfo
                {
                    Name = quest.name,
                    DisplayName = GetDisplayName(quest.name),
                    IsAccepted = false,
                    IsCompleted = false
                });
            }

            result.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public static void AcceptQuest(string name)
        {
            QuestModPlugin.Log.LogInfo($"AcceptQuest called for: {name}");
            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) { QuestModPlugin.Log.LogWarning("AcceptQuest: RuntimeData is null"); return; }
            object qd;
            if (!rt.Contains(name))
            {
                var template = GetAnyValue(rt);
                if (template == null) { QuestModPlugin.Log.LogWarning("AcceptQuest: RuntimeData empty"); return; }
                qd = QuestDataAccess.SetFields(template, seen: false, accepted: false, completed: false, wasEver: false);
                rt[name] = qd;
                QuestModPlugin.Log.LogInfo($"Injected into RuntimeData: {name}");
            }
            else
            {
                qd = rt[name];
            }
            qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: QuestDataAccess.IsCompleted(qd), wasEver: QuestDataAccess.IsCompleted(qd));
            rt[name] = qd;
            QuestModPlugin.Log.LogInfo($"Accepted: {name}");
        }

        public static void CompleteQuest(string name)
        {
            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;
            object qd;
            if (!rt.Contains(name))
            {
                var template = GetAnyValue(rt);
                if (template == null) return;
                qd = QuestDataAccess.SetFields(template, seen: false, accepted: false, completed: false, wasEver: false);
                rt[name] = qd;
                QuestModPlugin.Log.LogInfo($"Injected into RuntimeData: {name}");
            }
            else
            {
                qd = rt[name];
            }
            qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: true, wasEver: true);
            rt[name] = qd;
            QuestModPlugin.Log.LogInfo($"Completed: {name}");
        }

        public static void UnacceptQuest(string name)
        {
            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;
            if (rt.Contains(name))
            {
                var qd = rt[name];
                qd = QuestDataAccess.SetFields(qd, seen: true, accepted: false, completed: false, wasEver: QuestDataAccess.IsCompleted(qd));
                rt[name] = qd;
                QuestModPlugin.Log.LogInfo($"Unaccepted: {name}");
            }
        }

        public static void UncompleteQuest(string name)
        {
            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;
            if (rt.Contains(name))
            {
                var qd = rt[name];
                qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: false, wasEver: true);
                rt[name] = qd;
                QuestModPlugin.Log.LogInfo($"Uncompleted: {name}");
            }
        }

        public static void CompleteAllQuests()
        {
            if (PlayerData.instance == null) return;
            var rt = QuestDataAccess.GetRuntimeData();
            if (rt == null) return;
            int count = 0;
            var keys = new List<object>();
            foreach (var key in rt.Keys) keys.Add(key);

            foreach (string key in keys)
            {
                var qd = rt[key];
                qd = QuestDataAccess.SetFields(qd, seen: true, accepted: true, completed: true, wasEver: true);
                rt[key] = qd;
                count++;
            }
            QuestModPlugin.Log.LogInfo($"Completed {count} quests");
        }

        public static bool IsChainStep(string name) => ChainStepNames.Contains(name);

        public static List<ChainInfo> GetChainList()
        {
            var result = new List<ChainInfo>();
            var rt = QuestDataAccess.GetRuntimeData();

            foreach (var kvp in ChainRegistry)
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

                string display = ChainDisplayNames.TryGetValue(chainName, out var d) ? d : chainName;

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
            if (!ChainRegistry.TryGetValue(chainName, out var steps)) return;
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
                CompleteQuest(steps[current]);
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
            if (!ChainRegistry.TryGetValue(chainName, out var steps)) return;
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

        private static object GetAnyValue(IDictionary dict)
        {
            foreach (DictionaryEntry entry in dict)
                return entry.Value;
            return null;
        }
    }
}
