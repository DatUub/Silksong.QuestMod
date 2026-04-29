using System.Collections.Generic;
using UnityEngine;

namespace QuestMod
{
    public partial class QuestGUI
    {
        private List<QuestInfo> undoSnapshot;

        // CLUSTER P: 2-step override-with-confirm state. Click "Override" to
        // arm a 4-second confirmation window; second click commits, otherwise
        // it auto-disarms.
        private float _safetyOverrideArmedAt = -1f;
        private const float SafetyOverrideArmWindow = 4f;

        // Export/Import state for paste confirmation.
        private float _importArmedAt = -1f;
        private const float ImportArmWindow = 4f;
        private string _lastIoMessage = "";

        private void DrawSafetyGate()
        {
            if (QuestModPlugin.AreDestructiveFeaturesAllowed)
            {
                if (QuestModPlugin.IsSafetyOverridden && !QuestModPlugin.IsQuestModSave)
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUI.color = new Color(1f, 0.85f, 0.4f);
                    GUILayout.Label("Safety override active on this legacy save.");
                    GUI.color = Color.white;
                    GUILayout.EndVertical();
                    GUILayout.Space(6);
                }
                return;
            }

            // Locked: legacy save, no override.
            GUILayout.BeginVertical(GUI.skin.box);
            GUI.color = new Color(1f, 0.6f, 0.6f);
            GUILayout.Label("Save Safety -- destructive features locked", QuestGUISkin.SectionHeader);
            GUI.color = Color.white;
            GUILayout.Label("This save was created without QuestMod, or has no QuestMod state set. Destructive features (NPC spawn flags, granular bypasses, force-accept/complete) are disabled to protect existing progress.\n\nStart a New Game with QuestMod active to enable them automatically -- or override below if you accept the risk.");

            bool armed = _safetyOverrideArmedAt > 0f
                && (Time.realtimeSinceStartup - _safetyOverrideArmedAt) < SafetyOverrideArmWindow;

            if (!armed)
            {
                if (GUILayout.Button("Override Safety on this save..."))
                {
                    _safetyOverrideArmedAt = Time.realtimeSinceStartup;
                }
            }
            else
            {
                var remaining = SafetyOverrideArmWindow - (Time.realtimeSinceStartup - _safetyOverrideArmedAt);
                GUI.color = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button(string.Format("Confirm: enable destructive features (PERMANENT)  [{0:0.0}s]", remaining)))
                {
                    QuestModPlugin.SetSafetyOverride(true);
                    _safetyOverrideArmedAt = -1f;
                    QuestModToast.Show("Safety override active for this save. No off button.",
                        new Color(1f, 0.45f, 0.4f), 5f);
                }
                GUI.color = Color.white;
                if (GUILayout.Button("Cancel"))
                {
                    _safetyOverrideArmedAt = -1f;
                }
            }
            GUILayout.EndVertical();
            GUILayout.Space(6);
        }

        // Two-step confirm pattern (mirrors the safety override flow). Lets
        // the user wipe every per-save toggle in one click after the confirm
        // window. Useful as a panic button when the save's state has drifted.
        private float _resetAllArmedAt = -1f;
        private const float ResetAllArmWindow = 4f;

        private void DrawResetAllPerSaveTogglesButton()
        {
            var sd = QuestModPlugin.Instance?.SaveData;
            if (sd == null) return;
            GUILayout.Label("Per-save toggles", QuestGUISkin.SectionHeader);
            if (_resetAllArmedAt < 0f
                || (Time.realtimeSinceStartup - _resetAllArmedAt) > ResetAllArmWindow)
            {
                if (GUILayout.Button(new GUIContent("Reset all per-save toggles",
                    "Clears every BypassX, BypassAllWishwalls, AllQuestsAccepted, and per-quest policy. Does NOT touch the Override Safety flag (one-way ratchet).")))
                {
                    _resetAllArmedAt = Time.realtimeSinceStartup;
                }
            }
            else
            {
                var remaining = ResetAllArmWindow - (Time.realtimeSinceStartup - _resetAllArmedAt);
                GUI.color = new Color(1f, 0.6f, 0.4f);
                if (GUILayout.Button(string.Format("Confirm: clear all per-save toggles  [{0:0.0}s]", remaining)))
                {
                    int cleared = 0;
                    if (sd.Prereqs != null)
                    {
                        if (sd.Prereqs.BypassFleatopia) { sd.Prereqs.BypassFleatopia = false; cleared++; }
                        if (sd.Prereqs.BypassMandatoryWishes) { sd.Prereqs.BypassMandatoryWishes = false; cleared++; }
                        if (sd.Prereqs.BypassFaydownCloak) { sd.Prereqs.BypassFaydownCloak = false; cleared++; }
                        if (sd.Prereqs.BypassNeedolin) { sd.Prereqs.BypassNeedolin = false; cleared++; }
                        if (sd.Prereqs.BypassBonebottomQuestBoard) { sd.Prereqs.BypassBonebottomQuestBoard = false; cleared++; }
                        if (sd.Prereqs.BypassAllWishwalls) { sd.Prereqs.BypassAllWishwalls = false; cleared++; }
                    }
                    if (sd.AllQuestsAccepted) { sd.AllQuestsAccepted = false; cleared++; }
                    int policyCount = sd.QuestPolicies?.Count ?? 0;
                    sd.QuestPolicies?.Clear();
                    cleared += policyCount;
                    QuestModPlugin.SetWishesMode(AllWishesMode.Disabled);
                    questListDirty = true;
                    _resetAllArmedAt = -1f;
                    QuestModToast.Show($"Reset {cleared} per-save toggle(s)", new Color(0.9f, 0.7f, 0.4f), 3f);
                    QuestModPlugin.Log.LogInfo($"Reset all per-save toggles: cleared {cleared} field(s)");
                }
                GUI.color = Color.white;
                if (GUILayout.Button("Cancel")) _resetAllArmedAt = -1f;
            }
        }

        private void SnapshotBeforeMassOp()
        {
            undoSnapshot = QuestAcceptance.GetQuestList();
        }

        // Count how many quests transitioned to accepted (or completed) as a
        // result of running the mass-op delegate. Used to drive the toast
        // ("Accepted 5 quests"). Snapshots state before and after.
        private int MassOpCount(System.Action op, bool accepted)
        {
            int before = 0;
            foreach (var q in QuestAcceptance.GetQuestList())
                if (accepted ? q.IsAccepted : q.IsCompleted) before++;
            op();
            int after = 0;
            foreach (var q in QuestAcceptance.GetQuestList())
                if (accepted ? q.IsAccepted : q.IsCompleted) after++;
            return System.Math.Max(0, after - before);
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
            // Save / Discard pattern banner. Closing the F9 panel without
            // clicking Save reverts SaveData and Tags to their state at open.
            DrawSaveDiscardBanner();

            // CLUSTER P: render the save-safety gate at the top of the Tools tab.
            DrawSafetyGate();

            GUILayout.Label("Mass Operations", QuestGUISkin.SectionHeader);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Accept Available")) { SnapshotBeforeMassOp(); int n = MassOpCount(() => QuestAcceptance.AcceptAllQuests(), accepted: true); questListDirty = true; QuestModToast.Show($"Accepted {n} quest" + (n == 1 ? "" : "s")); }
            if (GUILayout.Button("Complete Available")) { SnapshotBeforeMassOp(); int n = MassOpCount(() => QuestAcceptance.CompleteAllQuests(), accepted: false); questListDirty = true; QuestModToast.Show($"Completed {n} quest" + (n == 1 ? "" : "s")); }
            GUILayout.EndHorizontal();

            if (QuestModPlugin.DevForceOperations.Value)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Accept ALL (force)"))
                {
                    if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
                        QuestModToast.Show("Force op blocked: legacy save (toggle Override Safety first)", new Color(1f, 0.6f, 0.4f), 4f);
                    else { SnapshotBeforeMassOp(); QuestAcceptance.ForceAcceptAllQuests(); questListDirty = true; QuestModToast.Show("Force-accepted all quests", new Color(0.9f, 0.7f, 0.4f), 3f); }
                }
                if (GUILayout.Button("Complete ALL (force)"))
                {
                    if (!QuestModPlugin.AreDestructiveFeaturesAllowed)
                        QuestModToast.Show("Force op blocked: legacy save (toggle Override Safety first)", new Color(1f, 0.6f, 0.4f), 4f);
                    else { SnapshotBeforeMassOp(); QuestAcceptance.ForceCompleteAllQuests(); questListDirty = true; QuestModToast.Show("Force-completed all quests", new Color(0.9f, 0.7f, 0.4f), 3f); }
                }
                GUILayout.EndHorizontal();
            }

            GUI.enabled = undoSnapshot != null;
            if (GUILayout.Button("Undo Last Mass Operation")) { UndoMassOp(); }
            GUI.enabled = true;

            GUILayout.Space(10);
            DrawResetAllPerSaveTogglesButton();
            GUILayout.Space(10);
            DrawExportImportSection();
            GUILayout.Space(10);
            GUILayout.Label("Mode (this save)", QuestGUISkin.SectionHeader);

            bool allAccepted = QuestModPlugin.AllQuestsAccepted;
            bool newAllAccepted = GUILayout.Toggle(allAccepted,
                new GUIContent("All Quests Accepted", "Auto-inject and accept every quest each scene load. Forces All Wishes Mode → Adjusted."));
            if (newAllAccepted != allAccepted) QuestModPlugin.SetAllQuestsAccepted(newAllAccepted);

            // CLUSTER H: 3-way All Wishes Mode selector (Disabled / Pure / Adjusted).
            GUI.enabled = !allAccepted;

            GUILayout.Label(new GUIContent("  All Wishes Mode",
                "Disabled = vanilla.\n" +
                "Pure = every wish available, no prereq checks (soft-locks possible).\n" +
                "Adjusted = bypass act/NPC prereqs but respect chain gating (recommended)."));

            var current = QuestModPlugin.WishesMode;
            // CLUSTER P: Pure is hidden by default. Show only when
            // Advanced.ShowPureWishesMode is true OR Pure is already the
            // active mode (so users who set it via save data dont lose
            // visibility / cant un-set it).
            bool showPure = QuestModPlugin.ShowPureWishesMode.Value || current == AllWishesMode.Pure;
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(current == AllWishesMode.Disabled, "Disabled", "Button") && current != AllWishesMode.Disabled)
                QuestModPlugin.SetWishesMode(AllWishesMode.Disabled);
            if (GUILayout.Toggle(current == AllWishesMode.Adjusted, new GUIContent("Adjusted", "Bypass NPC/act prereqs but keep chain gating + edge-case fixes. Safe for normal play (recommended)."), "Button") && current != AllWishesMode.Adjusted)
                QuestModPlugin.SetWishesMode(AllWishesMode.Adjusted);
            if (showPure && GUILayout.Toggle(current == AllWishesMode.Pure, new GUIContent("Pure", "Every quest available -- no chain gating, no edge-case handling. Soft-locks possible. Enable Advanced > ShowPureWishesMode to see this option."), "Button") && current != AllWishesMode.Pure)
                QuestModPlugin.SetWishesMode(AllWishesMode.Pure);
            GUILayout.EndHorizontal();

            // I-5: transient hint after a mid-playthrough mode change.
            // Shown for ~12s after the change so the user knows the change
            // does not retroactively undo state (FSM patches, accepted
            // quests, NPC spawn flags persist on the current scene).
            if (QuestModPlugin.LastWishesModeChangeRealtime > 0f
                && (Time.realtimeSinceStartup - QuestModPlugin.LastWishesModeChangeRealtime) < 12f)
            {
                GUI.color = new Color(1f, 0.85f, 0.4f);
                GUILayout.Label("Mode changed mid-run. Already-applied state stays; future scene loads use the new mode.");
                GUI.color = Color.white;
            }

            GUI.enabled = true;

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

            bool bypassWish = QuestModPlugin.BypassWishboardLock.Value;
            bool newBypassWish = GUILayout.Toggle(bypassWish,
                new GUIContent("Bypass Wishboard Lock",
                    "Use the Bonetown wishboard without defeating the Bell Beast. " +
                    "Runtime-only: does NOT touch save data — turn it off and the vanilla lock returns. " +
                    "Re-enter the room (or reload the scene) for the change to apply."));
            if (newBypassWish != bypassWish) QuestModPlugin.BypassWishboardLock.Value = newBypassWish;
            if (newBypassWish)
            {
                GUILayout.Label(
                    "Note: bypass is purely runtime. Save data is untouched, so this will NOT persist " +
                    "across reloads on its own — the toggle stays in mod config and re-applies each scene load.");
            }

            GUILayout.Space(8);
            if (PlayerData.instance != null)
            {
                bool act3 = PlayerData.instance.blackThreadWorld;
                bool newAct3 = GUILayout.Toggle(act3, "Act 3 (Black Thread World)");
                if (newAct3 != act3) PlayerData.instance.blackThreadWorld = newAct3;
            }

            DrawGranularPrereqsSection();
            DrawCustomRequirementsSection();
        }

        // CLUSTER E: granular prerequisite bypass toggles. Each checkbox sets
        // a small cluster of PlayerData bools on every scene load, independent
        // of the AllQuestsAvailable global. See QuestStateHooks.ApplyGranularBypasses.
        private void DrawGranularPrereqsSection()
        {
            var prereqs = QuestModPlugin.Prereqs;
            if (prereqs == null)
                return;

            GUILayout.Space(10);
            GUILayout.Label("Granular Prerequisite Bypasses", QuestGUISkin.SectionHeader);
            GUILayout.Label(
                "Each toggle independently unlocks a single prerequisite chain. " +
                "Applies on scene load. Save the file after toggling.",
                GUI.skin.label);

            bool flea = GUILayout.Toggle(prereqs.BypassFleatopia,
                new GUIContent("Bypass Fleatopia",
                    "Marks Fleatopia as visited and the troupe leader spoken. Unlocks quests gated behind the caravan-to-Fleatopia chain."));
            if (flea != prereqs.BypassFleatopia) prereqs.BypassFleatopia = flea;

            bool mand = GUILayout.Toggle(prereqs.BypassMandatoryWishes,
                new GUIContent("Bypass Mandatory Wishes",
                    "Sets promisedFirstWish so the early forced wishboard wish does not gate progression."));
            if (mand != prereqs.BypassMandatoryWishes) prereqs.BypassMandatoryWishes = mand;

            bool fay = GUILayout.Toggle(prereqs.BypassFaydownCloak,
                new GUIContent("Bypass Faydown Cloak",
                    "Grants the Faydown Cloak (double jump) so quests gated on this movement ability become reachable."));
            if (fay != prereqs.BypassFaydownCloak) prereqs.BypassFaydownCloak = fay;

            bool needolin = GUILayout.Toggle(prereqs.BypassNeedolin,
                new GUIContent("Bypass Needolin",
                    "Grants the Needolin so Needolin-gated quests become reachable without finishing its acquisition chain."));
            if (needolin != prereqs.BypassNeedolin) prereqs.BypassNeedolin = needolin;

            bool bbBoard = GUILayout.Toggle(prereqs.BypassBonebottomQuestBoard,
                new GUIContent("Bypass Bone Bottom Quest Board",
                    "Sets bonebottomQuestBoardFixed so the first wishboard is usable without defeating Bell Beast."));
            if (bbBoard != prereqs.BypassBonebottomQuestBoard) prereqs.BypassBonebottomQuestBoard = bbBoard;

            bool allWalls = GUILayout.Toggle(prereqs.BypassAllWishwalls,
                new GUIContent("Quest boards always available",
                    "Force-activates all 3 wishwall kiosks (Bone Bottom, Bellhart, Songclave) at runtime without touching PlayerData. NPC build animations (e.g. Flick) may still play visually, but the kiosks are interactable. Avoids the cascade of flipping defeatedBellBeast / visitedBellhartSaved / metCaretaker, which would falsely advance story milestones."));
            if (allWalls != prereqs.BypassAllWishwalls) prereqs.BypassAllWishwalls = allWalls;
        }

        private int presetSelected = -1;

        private void DrawCustomRequirementsSection()
        {
            GUILayout.Space(10);
            GUILayout.Label("Custom Requirements", QuestGUISkin.SectionHeader);

            bool enabled = QuestModPlugin.IsCustomRequirementsEnabled;
            bool newEnabled = GUILayout.Toggle(enabled,
                new GUIContent("Enable custom requirements",
                    "Apply preset + per-quest overrides from QuestRequirements.user.json on save load. " +
                    "Also evaluates extraConditions before quest turn-in."));
            if (newEnabled != enabled) QuestModPlugin.IsCustomRequirementsEnabled = newEnabled;

            if (!QuestRequirements.IsLoaded)
            {
                GUILayout.Label("(rules not loaded)");
                return;
            }

            var names = QuestRequirements.GetPresetNames();
            if (presetSelected < 0)
            {
                for (int i = 0; i < names.Length; i++)
                    if (string.Equals(names[i], QuestRequirements.ActivePresetName, System.StringComparison.OrdinalIgnoreCase))
                    { presetSelected = i; break; }
                if (presetSelected < 0) presetSelected = 0;
            }

            GUI.enabled = newEnabled;

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Active preset:",
                "Applied on save load. 'vanilla' = no preset (slider/per-quest values only)."),
                GUILayout.Width(96));
            int newSel = GUILayout.SelectionGrid(presetSelected, names, System.Math.Min(names.Length, 4));
            if (newSel != presetSelected)
            {
                presetSelected = newSel;
                string picked = names[newSel];
                QuestModPlugin.ActivePreset.Value = picked;
                QuestRequirements.SetActivePreset(picked);
            }
            GUILayout.EndHorizontal();

            if (presetSelected >= 0 && presetSelected < names.Length
                && QuestRequirements.Presets.TryGetValue(names[presetSelected], out var p)
                && !string.IsNullOrEmpty(p.Description))
            {
                GUILayout.Label("  " + p.Description);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Apply now",
                "Re-applies the active preset to all quests. Slider-touched targets are skipped.")))
            {
                QuestRequirements.ApplyActivePreset();
                RefreshOverrides();
            }
            if (GUILayout.Button(new GUIContent("Reload rules",
                "Re-reads QuestRequirements.user.json from BepInEx/config/QuestMod/.")))
            {
                ReloadRequirements();
            }
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(QuestAcceptance.LastCompletionRefusal))
            {
                var prev = GUI.color;
                GUI.color = new Color(1f, 0.6f, 0.4f);
                GUILayout.Label("Last refusal: " + QuestAcceptance.LastCompletionRefusal);
                GUI.color = prev;
            }

            GUI.enabled = true;
        }

        private static void ReloadRequirements()
        {
            QuestRequirements.Reload();
            QuestRequirements.SetActivePreset(QuestModPlugin.ActivePreset.Value);
            QuestModPlugin.Log.LogInfo("QuestRequirements reloaded");
        }
        // Compact single row: small label, two buttons, paste arms a confirm.
        // Helper text moves to tooltips so the section stays out of the way.
        private void DrawExportImportSection()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Save state I/O:",
                "Copy / paste your QuestMod save state via clipboard. Useful for sharing challenge run setups or backing up before destructive ops. Vanilla save state is not affected."),
                GUILayout.Width(110));
            if (GUILayout.Button(new GUIContent("Copy", "Serialize this save's QuestMod state to your clipboard."),
                GUILayout.Width(60)))
            {
                var json = QuestModPlugin.ExportSaveDataToJson();
                if (json == null)
                {
                    _lastIoMessage = "No save loaded";
                    QuestModToast.Show("Copy failed: no save loaded", new Color(1f, 0.6f, 0.4f), 3f);
                }
                else
                {
                    GUIUtility.systemCopyBuffer = json;
                    _lastIoMessage = $"Copied {json.Length} chars";
                    QuestModToast.Show($"Save state copied ({json.Length} chars)", new Color(0.5f, 0.9f, 0.5f), 2.5f);
                }
            }
            bool importArmed = _importArmedAt > 0f
                && (Time.realtimeSinceStartup - _importArmedAt) < ImportArmWindow;
            if (!importArmed)
            {
                if (GUILayout.Button(new GUIContent("Paste...",
                    "Replace this save's QuestMod state with the JSON in your clipboard. Two click confirm."),
                    GUILayout.Width(70)))
                    _importArmedAt = Time.realtimeSinceStartup;
            }
            else
            {
                var remaining = ImportArmWindow - (Time.realtimeSinceStartup - _importArmedAt);
                GUI.color = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button(string.Format("Confirm REPLACE [{0:0.0}s]", remaining), GUILayout.Width(160)))
                {
                    try
                    {
                        var clip = GUIUtility.systemCopyBuffer ?? "";
                        QuestModPlugin.ImportSaveDataFromJson(clip);
                        _lastIoMessage = $"Imported {clip.Length} chars";
                        questListDirty = true;
                    }
                    catch (System.Exception ex)
                    {
                        _lastIoMessage = "Import failed: " + ex.Message;
                    }
                    _importArmedAt = -1f;
                }
                GUI.color = Color.white;
                if (GUILayout.Button("X", GUILayout.Width(24))) _importArmedAt = -1f;
            }
            if (!string.IsNullOrEmpty(_lastIoMessage))
            {
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                GUILayout.Label(_lastIoMessage);
                GUI.color = Color.white;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // Save / Discard banner: shown at top of Tools tab. Save Changes
        // marks the current state as the new baseline (so closing the F9
        // panel does not revert). Discard rolls back to the open snapshot.
        private void DrawSaveDiscardBanner()
        {
            bool dirty = HasUnsavedChanges();
            GUILayout.BeginHorizontal();
            if (dirty)
            {
                GUI.color = new Color(1f, 0.85f, 0.4f);
                GUILayout.Label(new GUIContent("Unsaved changes",
                    "You changed save data or tags since opening this panel. Click Save Changes to commit, or close the panel without saving to revert."),
                    QuestGUISkin.SectionHeader);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.7f, 0.9f, 0.7f);
                GUILayout.Label("All changes saved", QuestGUISkin.SectionHeader);
                GUI.color = Color.white;
            }
            GUILayout.FlexibleSpace();
            GUI.enabled = dirty;
            if (GUILayout.Button(new GUIContent("Save Changes",
                "Commit the current state as the saved baseline. Closing the panel after this point will not revert."),
                GUILayout.Width(110)))
            {
                MarkSaveExplicit();
            }
            if (GUILayout.Button(new GUIContent("Discard",
                "Roll back save data and tags to the state at panel open."),
                GUILayout.Width(80)))
            {
                if (_uiOpenSnapshotSaveData != null)
                    QuestModPlugin.ImportSaveDataFromJson(_uiOpenSnapshotSaveData);
                if (_uiOpenSnapshotTags != null)
                    QuestRequirements.ImportTagsJson(_uiOpenSnapshotTags);
                questListDirty = true;
                QuestModToast.Show("Discarded changes", new Color(0.9f, 0.7f, 0.4f), 2.5f);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }
    }
}
