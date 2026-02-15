using UnityEngine;

namespace QuestMod
{
    public partial class QuestGUI
    {
        private void DrawChecklistTab()
        {
            if (!QuestCompletionOverrides.IsInitialized)
            {
                GUILayout.Label("Not initialized - load a save first");
                return;
            }

            checklistScroll = GUILayout.BeginScrollView(checklistScroll);

            foreach (var kvp in QuestRegistry.ChecklistQuests)
            {
                string questName = kvp.Key;
                string[] labels = kvp.Value;
                bool isSequential = QuestRegistry.SequentialQuests.Contains(questName);
                bool[] status = QuestCompletionOverrides.GetChecklistStatus(questName);

                int doneCount = 0;
                for (int i = 0; i < status.Length; i++)
                    if (status[i]) doneCount++;

                GUILayout.BeginVertical(GUI.skin.box);
                string seqTag = isSequential ? " (sequential)" : "";
                GUILayout.Label($"{questName}{seqTag}  ({doneCount}/{labels.Length})", GUI.skin.label);

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
                    GUILayout.Space(12);

                    string icon = done ? "■" : "□";
                    string label = $"{icon} {labels[i]}";

                    if (canToggle)
                    {
                        if (GUILayout.Button(label, GUILayout.ExpandWidth(true)))
                        {
                            int idx = i;
                            QuestCompletionOverrides.ToggleChecklistTarget(questName, idx, !done);
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        GUILayout.Button(label, GUILayout.ExpandWidth(true));
                        GUI.enabled = true;
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
                GUILayout.Space(3);
            }

            GUILayout.EndScrollView();
        }
    }
}
