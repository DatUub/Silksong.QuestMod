using UnityEngine;

namespace QuestMod
{
    public partial class QuestGUI
    {
        private System.Collections.Generic.List<ChainInfo> cachedChainList;

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
                GUILayout.Label("Chains", QuestGUISkin.SectionHeader);
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

                    string displayName = QuestModPlugin.ShowQuestDisplayNames.Value
                        ? chain.DisplayName : chain.ChainName;
                    var chainContent = new GUIContent($"{statusIcon} {displayName}", chain.ChainName);
                    GUILayout.Label(chainContent, GUILayout.Width(200));
                    GUILayout.Label(stepLabel, GUILayout.Width(180));
                    GUILayout.FlexibleSpace();

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
                GUILayout.Space(6);
                GUILayout.Label("Quests", QuestGUISkin.SectionHeader);
                foreach (var quest in cachedQuestList)
                {
                    if (QuestAcceptance.IsChainStep(quest.Name)) continue;

                    string status = quest.IsCompleted ? "✓" : (quest.IsAccepted ? "◐" : "○");
                    GUILayout.BeginHorizontal();
                    string displayName = QuestModPlugin.ShowQuestDisplayNames.Value ? quest.DisplayName : quest.Name;
                    GUILayout.Label(new GUIContent($"{status} {displayName}", quest.Name), GUILayout.Width(260));
                    GUILayout.FlexibleSpace();

                    if (!quest.IsAccepted && GUILayout.Button("Accept", GUILayout.Width(55)))
                    { QuestAcceptance.AcceptQuest(quest.Name); questListDirty = true; }

                    if (quest.IsAccepted && !quest.IsCompleted && GUILayout.Button("Drop", GUILayout.Width(45)))
                    { QuestAcceptance.UnacceptQuest(quest.Name); questListDirty = true; }

                    if (quest.IsAccepted && !quest.IsCompleted && GUILayout.Button("Complete", GUILayout.Width(65)))
                    { QuestAcceptance.CompleteQuest(quest.Name); questListDirty = true; }

                    if (quest.IsCompleted && GUILayout.Button("Undo", GUILayout.Width(50)))
                    { QuestAcceptance.UncompleteQuest(quest.Name); questListDirty = true; }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
        }
    }
}
