using System.Collections.Generic;
using UnityEngine;

namespace QuestMod
{
    public partial class QuestGUI
    {
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
            QuestModPlugin.LogDebugInfo("Undid last mass operation");
        }

        private void DrawToolsTab()
        {
            GUILayout.Label("Mass Operations", QuestGUISkin.SectionHeader);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Accept Available")) { SnapshotBeforeMassOp(); QuestAcceptance.AcceptAllQuests(); questListDirty = true; }
            if (GUILayout.Button("Complete Available")) { SnapshotBeforeMassOp(); QuestAcceptance.CompleteAllQuests(); questListDirty = true; }
            GUILayout.EndHorizontal();

            if (QuestModPlugin.DevForceOperations.Value)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Accept ALL (force)")) { SnapshotBeforeMassOp(); QuestAcceptance.ForceAcceptAllQuests(); questListDirty = true; }
                if (GUILayout.Button("Complete ALL (force)")) { SnapshotBeforeMassOp(); QuestAcceptance.ForceCompleteAllQuests(); questListDirty = true; }
                GUILayout.EndHorizontal();
            }

            GUI.enabled = undoSnapshot != null;
            if (GUILayout.Button("Undo Last Mass Operation")) { UndoMassOp(); }
            GUI.enabled = true;

            GUILayout.Space(10);
            GUILayout.Label("Mode (this save)", QuestGUISkin.SectionHeader);

            bool allAvail = QuestModPlugin.AllQuestsAvailable;
            bool newAllAvail = GUILayout.Toggle(allAvail,
                new GUIContent("All Quests Available", "Bypasses act/chain prerequisites for this save. Mutually exclusive with All Quests Accepted."));
            if (newAllAvail != allAvail) QuestModPlugin.SetAllQuestsAvailable(newAllAvail);

            bool allAccepted = QuestModPlugin.AllQuestsAccepted;
            bool newAllAccepted = GUILayout.Toggle(allAccepted,
                new GUIContent("All Quests Accepted", "Auto-inject and accept every quest each scene load. Mutually exclusive with All Quests Available."));
            if (newAllAccepted != allAccepted) QuestModPlugin.SetAllQuestsAccepted(newAllAccepted);

            GUILayout.Space(10);
            GUILayout.Label("Quick Config", QuestGUISkin.SectionHeader);

            bool onlyDisc = QuestModPlugin.OnlyDiscoveredQuests.Value;
            bool newOnlyDisc = GUILayout.Toggle(onlyDisc,
                new GUIContent("Only Discovered Quests", "Only show quests the player has encountered"));
            if (newOnlyDisc != onlyDisc) QuestModPlugin.OnlyDiscoveredQuests.Value = newOnlyDisc;

            bool itemInv = QuestModPlugin.QuestItemInvincible.Value;
            bool newItemInv = GUILayout.Toggle(itemInv, "Quest Item Invincible");
            if (newItemInv != itemInv) QuestModPlugin.QuestItemInvincible.Value = newItemInv;

            bool displayNames = QuestModPlugin.ShowQuestDisplayNames.Value;
            bool newDisplayNames = GUILayout.Toggle(displayNames, "Show Display Names");
            if (newDisplayNames != displayNames) QuestModPlugin.ShowQuestDisplayNames.Value = newDisplayNames;

            GUILayout.Space(8);
            if (PlayerData.instance != null)
            {
                bool act3 = PlayerData.instance.blackThreadWorld;
                bool newAct3 = GUILayout.Toggle(act3, "Act 3 (Black Thread World)");
                if (newAct3 != act3) PlayerData.instance.blackThreadWorld = newAct3;
            }
        }
    }
}
