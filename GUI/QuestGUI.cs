using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace QuestMod
{
    public class QuestGUI : MonoBehaviour
    {
        private bool show = false;
        private Vector2 questScroll;
        private Vector2 overrideScroll;
        private Rect rect = new Rect(20, 20, 560, 550);
        private int tab = 0;
        private string[] tabs = { "Quests", "Targets", "Checklist", "Tools" };
        private int categoryTab = 0;
        private Dictionary<string, string> countInputs = new Dictionary<string, string>();
        private Dictionary<string, List<QuestOverrideInfo>> cachedByCategory = new Dictionary<string, List<QuestOverrideInfo>>();
        private float multiplier = 1f;
        private string multiplierText = "1.0";
        private Vector2 checklistScroll;
        private List<QuestInfo> cachedQuestList;
        private bool questListDirty = true;

        private void Update()
        {
            if (QuestModPlugin.GuiToggleKey.Value.IsDown())
            {
                show = !show;
                if (show)
                {
                    questListDirty = true;
                    if (tab == 1)
                        RefreshOverrides();
                }
            }
        }

        private void OnGUI()
        {
            if (!show) return;
            var keyName = QuestModPlugin.GuiToggleKey.Value.MainKey.ToString();
            rect = GUI.Window(12345, rect, Draw, $"Quest Manager ({keyName})");
        }

        private void RefreshOverrides()
        {
            cachedByCategory.Clear();
            foreach (var cat in QuestCompletionOverrides.Categories)
            {
                cachedByCategory[cat] = QuestCompletionOverrides.GetQuestsByCategory(cat);
            }
        }

        private void Draw(int id)
        {
            var oldTab = tab;
            tab = GUILayout.Toolbar(tab, tabs);
            if (tab != oldTab && tab == 1)
                RefreshOverrides();

            GUILayout.Space(5);

            if (tab == 0)
                DrawQuestsTab();
            else if (tab == 1)
                DrawCompletionTab();
            else if (tab == 2)
                DrawChecklistTab();
            else
                DrawToolsTab();

            GUI.DragWindow();
        }

        private List<ChainInfo> cachedChainList;

        private void DrawQuestsTab()
        {
            if (questListDirty)
            {
                cachedQuestList = QuestAcceptance.GetQuestList();
                cachedChainList = QuestAcceptance.GetChainList();
                questListDirty = false;
            }

            questScroll = GUILayout.BeginScrollView(questScroll);

            if (cachedChainList != null)
            {
                GUILayout.Label("── Chains ──", GUI.skin.box);
                foreach (var chain in cachedChainList)
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.BeginHorizontal();

                    string stepLabel;
                    string statusIcon;
                    if (chain.IsFullyComplete)
                    {
                        stepLabel = "Complete";
                        statusIcon = "✓";
                    }
                    else if (chain.CurrentStep < 0)
                    {
                        stepLabel = "Not started";
                        statusIcon = "○";
                    }
                    else
                    {
                        string stepName = chain.Steps[chain.CurrentStep];
                        string stepDisplay = QuestAcceptance.GetDisplayName(stepName);
                        stepLabel = $"Step {chain.CurrentStep + 1}/{chain.TotalSteps}: {stepDisplay}";
                        statusIcon = "◐";
                    }

                    var chainLabel = QuestModPlugin.ShowQuestDisplayNames.Value
                        ? $"{statusIcon} {chain.DisplayName}"
                        : $"{statusIcon} {chain.ChainName}";
                    GUILayout.Label(chainLabel, GUILayout.Width(180));
                    GUILayout.Label(stepLabel, GUILayout.Width(180));

                    GUI.enabled = chain.CurrentStep >= 0;
                    if (GUILayout.Button("◀", GUILayout.Width(25))) { QuestAcceptance.RewindChain(chain.ChainName); questListDirty = true; }
                    GUI.enabled = !chain.IsFullyComplete;
                    if (GUILayout.Button("▶", GUILayout.Width(25))) { QuestAcceptance.AdvanceChain(chain.ChainName); questListDirty = true; }
                    GUI.enabled = true;

                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }
            }

            if (cachedQuestList != null)
            {
                GUILayout.Space(5);
                GUILayout.Label("── Quests ──", GUI.skin.box);
                foreach (var quest in cachedQuestList)
                {
                    if (QuestAcceptance.IsChainStep(quest.Name)) continue;

                    string status = quest.IsCompleted ? "✓" : (quest.IsAccepted ? "◐" : "○");
                    GUILayout.BeginHorizontal();
                    var label = QuestModPlugin.ShowQuestDisplayNames.Value ? $"{status} {quest.DisplayName}" : $"{status} {quest.Name}";
                    GUILayout.Label(new GUIContent(label, quest.Name), GUILayout.Width(250));
                    if (!quest.IsAccepted && GUILayout.Button("Acc", GUILayout.Width(50))) { QuestAcceptance.AcceptQuest(quest.Name); questListDirty = true; }
                    if (quest.IsAccepted && !quest.IsCompleted && GUILayout.Button("Unacc", GUILayout.Width(55))) { QuestAcceptance.UnacceptQuest(quest.Name); questListDirty = true; }
                    if (quest.IsAccepted && !quest.IsCompleted && GUILayout.Button("Done", GUILayout.Width(50))) { QuestAcceptance.CompleteQuest(quest.Name); questListDirty = true; }
                    if (quest.IsCompleted && GUILayout.Button("Undo", GUILayout.Width(50))) { QuestAcceptance.UncompleteQuest(quest.Name); questListDirty = true; }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawCompletionTab()
        {
            if (!QuestModPlugin.EnableCompletionOverrides.Value)
            {
                GUILayout.Label("Completion overrides are disabled in config");
                return;
            }

            if (!QuestCompletionOverrides.IsInitialized)
            {
                GUILayout.Label("Not initialized - load a save first");
                return;
            }

            var oldCatTab = categoryTab;
            categoryTab = GUILayout.Toolbar(categoryTab, QuestCompletionOverrides.Categories);
            if (categoryTab != oldCatTab)
                multiplierText = multiplier.ToString("0.0");

            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Multiplier:", GUILayout.Width(65));
            multiplier = GUILayout.HorizontalSlider(multiplier, 0f, 1f, GUILayout.Width(150));
            multiplier = Mathf.Round(multiplier * 20f) / 20f;

            string newMultText = multiplier.ToString("0.0");
            if (newMultText != multiplierText)
            {
                multiplierText = newMultText;
            }
            multiplierText = GUILayout.TextField(multiplierText, GUILayout.Width(35));

            if (GUILayout.Button("Apply", GUILayout.Width(45)))
            {
                if (float.TryParse(multiplierText, out float parsed))
                {
                    multiplier = parsed;
                    ApplyMultiplier(parsed);
                }
            }

            if (GUILayout.Button("1x", GUILayout.Width(25)))
            {
                multiplier = 1f;
                multiplierText = "1.0";
                ApplyMultiplier(1f);
            }

            if (GUILayout.Button("R", GUILayout.Width(22)))
            {
                ResetCategory();
            }

            if (GUILayout.Button("Dump", GUILayout.Width(40)))
            {
                var path = QuestCompletionOverrides.DumpAllTargets();
                if (path != null)
                    QuestModPlugin.Log.LogInfo($"Targets dumped to: {path}");
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(3);

            string currentCategory = QuestCompletionOverrides.Categories[categoryTab];
            if (!cachedByCategory.ContainsKey(currentCategory) || cachedByCategory[currentCategory].Count == 0)
            {
                GUILayout.Label("No quests found in this category.");
                return;
            }

            overrideScroll = GUILayout.BeginScrollView(overrideScroll);

            bool needsRefresh = false;

            foreach (var quest in cachedByCategory[currentCategory])
            {
                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.BeginHorizontal();
                GUILayout.Label(quest.QuestName, GUILayout.Width(300));
                if (GUILayout.Button("Reset", GUILayout.Width(45)))
                {
                    QuestCompletionOverrides.ResetToOriginal(quest.QuestName);
                    foreach (var target in quest.Targets)
                    {
                        string key = $"{quest.QuestName}_{target.TargetIndex}";
                        countInputs.Remove(key);
                    }
                    needsRefresh = true;
                }
                GUILayout.EndHorizontal();

                foreach (var target in quest.Targets)
                {
                    string key = $"{quest.QuestName}_{target.TargetIndex}";
                    if (!countInputs.ContainsKey(key))
                        countInputs[key] = target.CurrentCount.ToString();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(15);

                    bool isModified = target.CurrentCount != target.OriginalCount;
                    string label = isModified
                        ? $"  {target.DisplayName} ({target.OriginalCount}→{target.CurrentCount})"
                        : $"  {target.DisplayName} ({target.OriginalCount})";

                    GUILayout.Label(label, GUILayout.Width(220));
                    countInputs[key] = GUILayout.TextField(countInputs[key], GUILayout.Width(50));

                    if (GUILayout.Button("Set", GUILayout.Width(35)))
                    {
                        if (int.TryParse(countInputs[key], out int newCount))
                        {
                            QuestCompletionOverrides.SetTargetCount(quest.QuestName, target.TargetIndex, newCount);
                            needsRefresh = true;
                        }
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
                GUILayout.Space(2);
            }

            GUILayout.EndScrollView();

            if (needsRefresh)
                RefreshOverrides();
        }

        private void ApplyMultiplier(float mult)
        {
            string currentCategory = QuestCompletionOverrides.Categories[categoryTab];
            if (!cachedByCategory.ContainsKey(currentCategory)) return;

            foreach (var quest in cachedByCategory[currentCategory])
            {
                foreach (var target in quest.Targets)
                {
                    int raw = Mathf.Max(1, Mathf.RoundToInt(target.OriginalCount * mult));
                    int scaled = raw <= 1 ? 1 : (raw % 2 == 0 ? raw : raw + 1);
                    string key = $"{quest.QuestName}_{target.TargetIndex}";
                    countInputs[key] = scaled.ToString();
                    QuestCompletionOverrides.SetTargetCount(quest.QuestName, target.TargetIndex, scaled);
                }
            }

            RefreshOverrides();
        }

        private void ResetCategory()
        {
            string currentCategory = QuestCompletionOverrides.Categories[categoryTab];
            if (!cachedByCategory.ContainsKey(currentCategory)) return;

            foreach (var quest in cachedByCategory[currentCategory])
            {
                QuestCompletionOverrides.ResetToOriginal(quest.QuestName);
                foreach (var target in quest.Targets)
                {
                    string key = $"{quest.QuestName}_{target.TargetIndex}";
                    countInputs.Remove(key);
                }
            }

            multiplier = 1f;
            multiplierText = "1.0";
            RefreshOverrides();
        }
        private void DrawChecklistTab()
        {
            if (!QuestCompletionOverrides.IsInitialized)
            {
                GUILayout.Label("Not initialized - load a save first");
                return;
            }

            checklistScroll = GUILayout.BeginScrollView(checklistScroll);

            foreach (var kvp in QuestCompletionOverrides.ChecklistQuests)
            {
                string questName = kvp.Key;
                string[] labels = kvp.Value;
                bool isSequential = QuestCompletionOverrides.SequentialQuests.Contains(questName);
                bool[] status = QuestCompletionOverrides.GetChecklistStatus(questName);

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(questName + (isSequential ? " (sequential)" : ""), GUI.skin.label);

                for (int i = 0; i < labels.Length && i < status.Length; i++)
                {
                    bool done = status[i];
                    bool canToggle = true;

                    if (isSequential && !done)
                    {
                        int nextUndone = -1;
                        for (int j = 0; j < status.Length; j++)
                        {
                            if (!status[j]) { nextUndone = j; break; }
                        }
                        canToggle = (i == nextUndone);
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(15);

                    string icon = done ? "■" : "□";
                    string label = $"{icon} {labels[i]}";

                    if (canToggle)
                    {
                        if (GUILayout.Button(label, GUILayout.Width(300)))
                        {
                            int idx = i;
                            QuestCompletionOverrides.ToggleChecklistTarget(questName, idx, !done);
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        GUILayout.Button(label, GUILayout.Width(300));
                        GUI.enabled = true;
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
                GUILayout.Space(3);
            }

            GUILayout.EndScrollView();
        }

        private List<QuestInfo> undoSnapshot;

        private void SnapshotBeforeMassOp()
        {
            undoSnapshot = QuestAcceptance.GetQuestList();
        }

        private void UndoMassOp()
        {
            if (undoSnapshot == null) return;
            foreach (var snap in undoSnapshot)
            {
                if (snap.IsCompleted)
                    QuestAcceptance.CompleteQuest(snap.Name);
                else
                    QuestAcceptance.UncompleteQuest(snap.Name);

                if (snap.IsAccepted)
                    QuestAcceptance.AcceptQuest(snap.Name);
                else
                    QuestAcceptance.UnacceptQuest(snap.Name);
            }
            undoSnapshot = null;
            questListDirty = true;
            QuestModPlugin.Log.LogInfo("Undid last mass operation");
        }

        private void DrawToolsTab()
        {
            GUILayout.Label("── Mass Operations ──", GUI.skin.box);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Accept Discovered")) { SnapshotBeforeMassOp(); QuestAcceptance.AcceptAllQuests(); questListDirty = true; }
            if (GUILayout.Button("Complete Discovered")) { SnapshotBeforeMassOp(); QuestAcceptance.CompleteAllQuests(); questListDirty = true; }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Accept ALL (force)")) { SnapshotBeforeMassOp(); QuestAcceptance.ForceAcceptAllQuests(); questListDirty = true; }
            if (GUILayout.Button("Complete ALL (force)")) { SnapshotBeforeMassOp(); QuestAcceptance.ForceCompleteAllQuests(); questListDirty = true; }
            GUILayout.EndHorizontal();

            GUI.enabled = undoSnapshot != null;
            if (GUILayout.Button("Undo Last Mass Operation")) { UndoMassOp(); }
            GUI.enabled = true;

            GUILayout.Space(10);
            GUILayout.Label("── Data Dumps ──", GUI.skin.box);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dump Targets")) { QuestCompletionOverrides.DumpAllTargets(); }
            if (GUILayout.Button("Dump Groups")) { QuestCompletionOverrides.DumpCompleteTotalGroups(); }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Dump Quest Availability")) { QuestCompletionOverrides.DumpQuestAvailability(); }

            GUILayout.Space(10);
            GUILayout.Label("── Quick Config ──", GUI.skin.box);

            bool allAvail = QuestModPlugin.AllQuestsAvailable;
            bool newAllAvail = GUILayout.Toggle(allAvail, "All Quests Available (this save only)");
            if (newAllAvail != allAvail) { QuestModPlugin.AllQuestsAvailable = newAllAvail; CheatManagerToggle.SyncFlags(); }

            bool onlyDisc = QuestModPlugin.OnlyDiscoveredQuests.Value;
            bool newOnlyDisc = GUILayout.Toggle(onlyDisc, "Only Discovered Quests");
            if (newOnlyDisc != onlyDisc) QuestModPlugin.OnlyDiscoveredQuests.Value = newOnlyDisc;

            bool itemInv = QuestModPlugin.QuestItemInvincible.Value;
            bool newItemInv = GUILayout.Toggle(itemInv, "Quest Item Invincible");
            if (newItemInv != itemInv) QuestModPlugin.QuestItemInvincible.Value = newItemInv;

            bool displayNames = QuestModPlugin.ShowQuestDisplayNames.Value;
            bool newDisplayNames = GUILayout.Toggle(displayNames, "Show Display Names");
            if (newDisplayNames != displayNames) QuestModPlugin.ShowQuestDisplayNames.Value = newDisplayNames;

            GUILayout.Space(5);
            if (PlayerData.instance != null)
            {
                bool act3 = PlayerData.instance.blackThreadWorld;
                bool newAct3 = GUILayout.Toggle(act3, "Act 3 (Black Thread World)");
                if (newAct3 != act3) PlayerData.instance.blackThreadWorld = newAct3;
            }
        }
    }
}
