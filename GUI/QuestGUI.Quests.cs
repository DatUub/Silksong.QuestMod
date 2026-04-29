using UnityEngine;

namespace QuestMod
{
    public partial class QuestGUI
    {
        private System.Collections.Generic.List<ChainInfo> cachedChainList;

        // I-3: 2-step confirm window for the bulk policy reset, mirrored
        // from the cluster P safety override pattern.
        private float _bulkResetArmedAt = -1f;
        private const float BulkResetArmWindow = 4f;

        // Quest tab search filter. Empty string = no filter. Matched case
        // insensitively against display name and internal name.
        private string _questFilter = "";

        private bool QuestFilterMatches(string name, string display)
        {
            if (string.IsNullOrEmpty(_questFilter)) return true;
            return (name?.IndexOf(_questFilter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                || (display?.IndexOf(_questFilter, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void DrawQuestsTab()
        {
            if (questListDirty)
            {
                cachedQuestList = QuestAcceptance.GetQuestList();
                cachedChainList = QuestAcceptance.GetChainList();
                questListDirty = false;
            }

            // Search bar lives outside the scroll view so it stays anchored.
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Filter:", "Type to filter chains and quests by name (case insensitive)"),
                GUILayout.Width(45));
            _questFilter = GUILayout.TextField(_questFilter ?? "", GUILayout.MinWidth(160));
            if (!string.IsNullOrEmpty(_questFilter) && GUILayout.Button("×", GUILayout.Width(25)))
                _questFilter = "";
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            questScroll = GUILayout.BeginScrollView(questScroll);

            // I-3: bulk reset of per-quest policies (cluster D). Lives at
            // the top of the Quests tab so users who created a mess of
            // per-quest toggles can wipe them in one click (with confirm).
            DrawBulkPolicyReset();

            if (cachedChainList != null)
            {
                GUILayout.Label("Chains", QuestGUISkin.SectionHeader);
                foreach (var chain in cachedChainList)
                {
                    if (!QuestFilterMatches(chain.ChainName, chain.DisplayName)) continue;
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

                // Header row for the per-quest policy columns.
                GUILayout.BeginHorizontal();
                GUILayout.Label("", GUILayout.Width(180));
                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent("Avail",
                    "Make this quest available regardless of chain/act prerequisites."),
                    GUILayout.Width(45));
                GUILayout.Label(new GUIContent("Auto",
                    "Auto-accept this quest on first scene load each session."),
                    GUILayout.Width(45));
                GUILayout.Space(180);
                GUILayout.EndHorizontal();

                foreach (var quest in cachedQuestList)
                {
                    if (QuestAcceptance.IsChainStep(quest.Name)) continue;
                    if (!QuestFilterMatches(quest.Name, quest.DisplayName)) continue;

                    string status = quest.IsCompleted ? "✓" : (quest.IsAccepted ? "◐" : "○");
                    GUILayout.BeginHorizontal();
                    string displayName = QuestModPlugin.ShowQuestDisplayNames.Value ? quest.DisplayName : quest.Name;
                    GUILayout.Label(new GUIContent($"{status} {displayName}", quest.Name), GUILayout.Width(180));
                    GUILayout.FlexibleSpace();

                    // Per-quest Available / AutoAccept checkboxes. The legacy
                    // global AllQuestsAvailable toggle (Tools tab) overrides
                    // these when set; we still let the user edit the underlying
                    // per-quest values so they take effect when the global is off.
                    var pol = QuestPolicyStore.Get(quest.Name);
                    bool curAvail = pol != null && pol.Available;
                    bool curAuto  = pol != null && pol.AutoAccept;

                    bool newAvail = GUILayout.Toggle(curAvail, GUIContent.none, GUILayout.Width(45));
                    if (newAvail != curAvail) QuestPolicyStore.SetAvailable(quest.Name, newAvail);

                    GUI.enabled = newAvail; // auto-accept requires availability
                    bool newAuto = GUILayout.Toggle(curAuto, GUIContent.none, GUILayout.Width(45));
                    if (newAuto != curAuto) QuestPolicyStore.SetAutoAccept(quest.Name, newAuto);
                    GUI.enabled = true;

                    if (!quest.IsAccepted && GUILayout.Button("Accept", GUILayout.Width(55)))
                    { QuestAcceptance.AcceptQuest(quest.Name); questListDirty = true; }

                    if (quest.IsAccepted && !quest.IsCompleted && GUILayout.Button("Drop", GUILayout.Width(45)))
                    { QuestAcceptance.UnacceptQuest(quest.Name); questListDirty = true; }

                    if (quest.IsAccepted && !quest.IsCompleted && GUILayout.Button("Complete", GUILayout.Width(65)))
                    {
                        // Dispatch to RemoteComplete when the user has opted
                        // in (cluster Y). RemoteComplete tries the vanilla
                        // QuestManager.CompleteQuest path so item deductions
                        // and rewards land; otherwise we fall back to the
                        // legacy flag flip behaviour.
                        QuestAcceptance.LastCompletionRefusal = null;
                        bool ok;
                        if (QuestModPlugin.IsFullRemoteCompleteEnabled)
                            ok = QuestAcceptance.RemoteComplete(quest.Name);
                        else { QuestAcceptance.CompleteQuest(quest.Name); ok = string.IsNullOrEmpty(QuestAcceptance.LastCompletionRefusal); }
                        if (!ok && !string.IsNullOrEmpty(QuestAcceptance.LastCompletionRefusal))
                            QuestModToast.Show("Refused: " + QuestAcceptance.LastCompletionRefusal, new Color(1f, 0.6f, 0.4f), 4f);
                        questListDirty = true;
                    }

                    if (quest.IsCompleted && GUILayout.Button("Undo", GUILayout.Width(50)))
                    { QuestAcceptance.UncompleteQuest(quest.Name); questListDirty = true; }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawBulkPolicyReset()
        {
            var map = QuestPolicyStore.Map;
            int policyCount = map?.Count ?? 0;
            if (policyCount == 0 && _bulkResetArmedAt < 0f) return; // nothing to clear, hide

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Per-quest policies", QuestGUISkin.SectionHeader);
            GUILayout.Label("Cluster D Available + AutoAccept toggles. " + policyCount + " entries on this save.");

            bool armed = _bulkResetArmedAt > 0f
                && (Time.realtimeSinceStartup - _bulkResetArmedAt) < BulkResetArmWindow;

            if (!armed)
            {
                if (policyCount > 0 && GUILayout.Button("Clear all per-quest policies..."))
                    _bulkResetArmedAt = Time.realtimeSinceStartup;
            }
            else
            {
                var remaining = BulkResetArmWindow - (Time.realtimeSinceStartup - _bulkResetArmedAt);
                GUI.color = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button(string.Format("Confirm: clear {0} policies  [{1:0.0}s]", policyCount, remaining)))
                {
                    if (map != null) map.Clear();
                    _bulkResetArmedAt = -1f;
                    questListDirty = true;
                    QuestModPlugin.Log.LogInfo("Cleared all per-quest policies (" + policyCount + ")");
                }
                GUI.color = Color.white;
                if (GUILayout.Button("Cancel")) _bulkResetArmedAt = -1f;
            }
            GUILayout.EndVertical();
            GUILayout.Space(6);
        }
    }
}
