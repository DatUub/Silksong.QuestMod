using UnityEngine;

namespace QuestMod
{
    public partial class QuestGUI
    {
        private System.Collections.Generic.List<ChainInfo> cachedChainList;

        // 2-step confirm. Mirrors the override pattern.
        private float _bulkResetArmedAt = -1f;
        private const float BulkResetArmWindow = QuestModConstants.ConfirmArmWindow;

        // Empty = no filter. Case-insensitive over display + internal name.
        private string _questFilter = "";
        // Status scope: makes Undo on completed rows easy to find (Discord feedback).
        private int _questStatusScope; // 0=Active, 1=Completed, 2=All

        private bool QuestFilterMatches(string name, string display)
        {
            if (string.IsNullOrEmpty(_questFilter)) return true;
            return (name?.IndexOf(_questFilter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                || (display?.IndexOf(_questFilter, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private bool QuestStatusMatches(bool isCompleted)
        {
            if (_questStatusScope == 0) return !isCompleted; // Active
            if (_questStatusScope == 1) return isCompleted;  // Completed (Undo here)
            return true; // All
        }

        private void DrawQuestsTab()
        {
            if (questListDirty)
            {
                cachedQuestList = QuestAcceptance.GetQuestList();
                cachedChainList = QuestAcceptance.GetChainList();
                questListDirty = false;
            }

            // Search bar outside scroll so it stays anchored.
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Filter:", "Type to filter chains and quests by name (case insensitive)"),
                GUILayout.Width(45));
            _questFilter = GUILayout.TextField(_questFilter ?? "", GUILayout.MinWidth(120));
            if (!string.IsNullOrEmpty(_questFilter) && GUILayout.Button("×", GUILayout.Width(25)))
                _questFilter = "";
            GUILayout.Space(6);
            _questStatusScope = GUILayout.Toolbar(_questStatusScope,
                new[]
                {
                    new GUIContent("Active", "Accepted / not completed (default)"),
                    new GUIContent("Done", "Completed wishes — use Undo Complete to reverse a flag-only complete"),
                    new GUIContent("All", "Every quest in the list"),
                },
                GUILayout.MinWidth(180));
            GUILayout.FlexibleSpace();
            GUI.color = QuestModPlugin.IsFullRemoteCompleteEnabled
                ? new Color(0.55f, 0.9f, 0.55f)
                : new Color(0.9f, 0.85f, 0.45f);
            GUILayout.Label(new GUIContent(
                QuestModPlugin.IsFullRemoteCompleteEnabled ? "Remote Complete ON" : "Flag-only Complete",
                "Flag-only only flips QuestData flags (no minigame/world progress).\n" +
                "Ecstasy of the End / flea carnival: play in the world (or Tools → Remote Complete).\n" +
                "Already flag-completed by mistake? Switch filter to Done → Undo Complete."),
                GUILayout.Width(130));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            questScroll = GUILayout.BeginScrollView(questScroll);

            // Top of tab so users can wipe a messy policy set easily.
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

                // Header row.
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
                    if (!QuestStatusMatches(quest.IsCompleted)) continue;

                    string status = quest.IsCompleted ? "✓" : (quest.IsAccepted ? "◐" : "○");
                    GUILayout.BeginHorizontal();
                    string displayName = QuestModPlugin.ShowQuestDisplayNames.Value ? quest.DisplayName : quest.Name;
                    string nameTip = quest.Name;
                    if (QuestAcceptance.BlocksFlagOnlyComplete(quest.Name))
                        nameTip += "\nNeeds in-world progress (minigame/world). Flag-only Complete is blocked.";
                    GUILayout.Label(new GUIContent($"{status} {displayName}", nameTip), GUILayout.Width(180));
                    GUILayout.FlexibleSpace();

                    // Legacy AllQuestsAvailable overrides these when set, but
                    // we still let the user edit so they take effect when off.
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

                    if (quest.IsAccepted && !quest.IsCompleted)
                    {
                        bool worldLocked = QuestAcceptance.BlocksFlagOnlyComplete(quest.Name);
                        string completeTip = QuestModPlugin.IsFullRemoteCompleteEnabled
                            ? "Mirrors NPC turn-in: deduct targets, grant rewards, cascade dependents."
                            : worldLocked
                                ? "Blocked: this wish needs in-world minigame/world progress (e.g. flea carnival). Play it in the world, or enable Tools → Remote Complete."
                                : "Flag-only: marks the wish complete in QuestData. Minigames / world state are NOT advanced. Use Done → Undo Complete to reverse. Enable Remote Complete in Tools for full turn-in.";
                        string completeLabel = worldLocked ? "Complete…" : "Complete";
                        if (worldLocked) GUI.color = new Color(1f, 0.75f, 0.45f);
                        bool hit = GUILayout.Button(new GUIContent(completeLabel, completeTip), GUILayout.Width(70));
                        GUI.color = Color.white;
                        if (hit)
                        {
                            QuestAcceptance.LastCompletionRefusal = null;
                            string label = displayName;
                            bool ok;
                            if (QuestModPlugin.IsFullRemoteCompleteEnabled)
                                ok = QuestAcceptance.RemoteComplete(quest.Name);
                            else
                            {
                                QuestAcceptance.CompleteQuest(quest.Name);
                                ok = string.IsNullOrEmpty(QuestAcceptance.LastCompletionRefusal);
                            }
                            if (!ok && !string.IsNullOrEmpty(QuestAcceptance.LastCompletionRefusal))
                                QuestModToast.Show(QuestAcceptance.LastCompletionRefusal, new Color(1f, 0.6f, 0.4f), 5.5f);
                            else if (ok)
                            {
                                if (QuestModPlugin.IsFullRemoteCompleteEnabled)
                                    QuestModToast.Show($"Completed {label}", new Color(0.5f, 0.9f, 0.5f), 2.5f);
                                else
                                    QuestModToast.Show($"Flag-completed {label} — Done filter → Undo Complete", new Color(0.85f, 0.85f, 0.5f), 4f);
                            }
                            questListDirty = true;
                        }
                    }

                    if (quest.IsCompleted)
                    {
                        // High-visibility Undo (support: "I completed Dark Below and can't find Undo").
                        GUI.color = new Color(0.55f, 0.8f, 1f);
                        if (GUILayout.Button(new GUIContent("Undo Complete",
                                "Clears the completed flag for this wish. Does not un-grant abilities or refund minigame state. "
                                + "Tip: use the Done filter to list only completed wishes."),
                            GUILayout.Width(100)))
                        {
                            string label = displayName;
                            QuestAcceptance.UncompleteQuest(quest.Name);
                            QuestModToast.Show($"Undid complete: {label}", new Color(0.7f, 0.85f, 1f), 2.5f);
                            questListDirty = true;
                        }
                        GUI.color = Color.white;
                    }

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
            GUILayout.Label("Per-quest Available + AutoAccept toggles. " + policyCount + " entries on this save.");

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
                    // Re-baseline so close doesn't undo it.
                    ReplaceOpenSnapshot();
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
