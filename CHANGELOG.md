# Changelog

## v2.1.2

New toggle: **Auto-Accept Available** (Quests tab header). When on, every scene load accepts any wish where `IsActuallyAvailable()` passes -- respects Adjusted's full chain prereq + exclusion + availableConditions gating. Unlike Accept All, story-locked wishes only accept once their gate naturally unlocks, so chain ordering stays coherent. Toggling on auto-flips Disabled -> Adjusted (same coupling as Accept All). Skipped on legacy saves without the safety override.

Quieted DumpNpcDiagnostics: `FindObjectsInactive.Exclude` + `activeInHierarchy` filter. Walking inactive FSMs with `state.Actions` was forcing PlayMaker lazy init on uninitialized ActionData and producing thousands of `state.Fsm == null` log lines per scene when DebugLogging was on. Dump now only covers live NPCs.

## v2.1.1

Hotfix for the FSM cascade flood introduced in 2.1.0.

- Scene-load hooks now bail on `Pre_Menu_Loader` and `Quit_To_Menu` in addition to `Menu_Title`. The 2.1.0 sweeps fired on `Pre_Menu_Loader` and ran `FindObjectsByType<PlayMakerFSM>(FindObjectsInactive.Include)` plus `ForceActivateRecursive` on dormant FSM components, which lazy-init'd `ActionData` while `state.Fsm` was still null and produced thousands of `state.Fsm == null` + NullReferenceException errors per session, hanging save loads.
- `IsTransientMenuScene` helper centralizes the blocklist so a future scene-name surprise only needs one edit.
- Defense-in-depth `fsm.Fsm == null` guards added before any `fsm.FsmStates` access in `QuestStateHooks.PatchAllQuestFSMs`, `QuestStateHooks.DumpNpcDiagnostics`, and `WishwallFsmPatch.SweepOnce` so a partially-initialized FSM is skipped instead of triggering the lazy-init NPE.

## v2.1.0

Big bug pass. No new gameplay features, every change is making something that already exists actually work like it's supposed to.

What got fixed:

- AllQuestsAccepted no longer hands you Faydown Cloak as a side effect. The spawn-flag set had hasDoubleJump in it plus 5 other things that weren't actual NPC spawn bools (Mushroom stage markers 2-7, cityMerchantSaved, cityMerchantCanLeaveForBridge, visitedBellhartSaved, fixerQuestBoardConvo). All removed.
- Mr Mushroom's wish stays hidden under AllQuests until you've actually met him.
- FSM patches are now per-action idempotent so flipping WishesMode mid-scene doesn't double-rewrite the same actions.
- Every scene-load coroutine bails if you've moved on by the time it fires. A stale 0.5s sweep can't patch the wrong scene's FSMs anymore.
- Returning to title clears every static (mode, AllQuests, target-count caches, SilkSoul cache) so save A doesn't leak into save B.
- Plugin init wrapped in try/catch per step. One bad Initialize won't blank the F9 panel.
- Corrupt save data backs up to `BepInEx/config/QuestMod/corrupt-saves/` and toasts a warning instead of silently resetting.
- EdgeBugDoubleTrobbio rewritten. Distinguishes wandering NPC from boss prefab structurally so the boss can never be suppressed. Tiebreak is scene root index, not InstanceID.

Save-safety gate now covers every single-quest path (AcceptQuest, CompleteQuest, UnacceptQuest, UncompleteQuest, AdvanceChain, RewindChain). Legacy saves don't mutate from GUI/IPC clicks until you flip the override. Refused clicks set a refusal message so the GUI can surface it.

Cascade and bulk-op fixes:

- RemoteComplete `markCompleted` cascade flips flags only, instead of running the full reward+deduct pipeline on every dependent. Was giving 4x rewards on chains.
- RemoteComplete safety gate runs before adding to the visited set, so refused calls don't poison retries after you flip the override.
- Complete All no longer re-evaluates availableConditions on already-accepted wishes, so it actually completes them instead of skipping half.
- ToggleChecklistTarget on sequential quests handles both directions: ticking step N zeroes 0..N-1, unticking step N resets later steps too.
- Mass-op Undo also snapshots the full save data JSON so injected/completed sets revert.

Bypass GUI cleanup:

- Granular bypass toggles relabeled "(one-way)" since they ratchet PlayerData false to true and toggling off doesn't undo the write.
- "Bypass Faydown Cloak" renamed to "Grant Faydown Cloak" since it grants the whole ability, not just the quest gate.
- "Reset all per-save toggles" tooltip clarifies it clears flags but the PlayerData side effects stick.
- SetSafetyOverride refuses false. Was supposed to be one-way, now actually is.
- SetAllQuestsAccepted toasts when it auto-flips WishesMode to Adjusted instead of doing it silently.

Save data + JSON:

- Schema version field on QuestModSaveData with migration dispatch.
- SaveData setter normalizes every collection (Newtonsoft can write explicit nulls through the property initializer).
- HasAnyQuestModState reflection-walks GranularPrereqs so a new bypass field doesn't silently break grandfathering.
- EvalPlayerData on bool fields rejects non-equality ops instead of silently coercing them to `==`.
- IsInventoryDeductibleCounter dropped the "Item/Collectable in the type name" fallback. Only explicit CollectableItem inheritance counts now. Boss-kill trackers with "Item" in their name no longer get drained.

Save/discard pipeline:

- MarkSaveExplicit re-baselines snapshots properly. Reload rules refreshes the baseline. HasUnsavedChanges lazy-captures when the panel was opened pre-save. Bulk reset confirms bump the baseline so the wipe isn't reverted on close.
- Dirty detection now compares JSON content, not just collection counts. Editing an existing override's value no longer slips past.

FSM patch hardening:

- `_fsmNamePatterns` narrowed from `build/hammer/construct` to `builder/hammering/construction` so blacksmiths and hammer enemies don't false-match.
- `ForceActivateRecursive` walks children even when the parent name is on the exclude list, so healthy children under post-progress parents aren't stranded.
- Reflection name strings migrated to `nameof()` everywhere so a Silksong field rename fails the build instead of silently no-opping.

SilkSoul:

- TryResolveEntries prefers first float over last int, so a future game patch adding sortOrder-style int fields can't hijack the value field.
- SetPointValue clamps + rounds int casts safely.
- Reset restores struct values before nulling the cache so save A's overrides can't bleed into save B.
- SetThreshold clamped to [0, 100]. Threshold +/- buttons display the clamped value.
- SetActivePreset auto-applies and validates; unknown name falls back to vanilla with a warning.

Polish:

- DumpNpcDiagnostics gated behind DebugLogging so it doesn't flood the log per scene.
- GourmandSegmentCount slider removed. Was a placebo with no quest-data backing.
- Targets multiplier slider snaps to 0.1, text shows two decimals, only resyncs when the slider actually moves. Apply parses culture-invariantly and clamps.
- Tabs rebuild only when EnableSilkSoulTab changes, not every IMGUI event.
- Tags-version dirty signal so HasUnsavedChanges doesn't re-serialize the full tag dict every OnGUI pass.

## v2.0.3

Hover tooltips no longer get stuck. Unity doesn't clear `GUI.tooltip` between frames, so when the cursor moved off a control the old text rendered as a stuck tooltip on whatever was now under the mouse. Cleared at Layout start so hovered controls re-assert it on Repaint, otherwise nothing draws.

## v2.0.2

Small GUI styling pass on the F9 panel.

- Tools tab gets a scroll view so entries below the fold are reachable.
- Gear glyph in the tab label that rendered as a tofu box on the default IMGUI font is gone.
- Quest name header in the Checklist tab uses the same section style as the Tags tab.
- Category strips in Targets and Tags match the outer tab strip styling (gray normal, blue active) instead of looking like accent buttons.

## v2.0.1

Dep bumps. No code changes from 2.0.0.

- Silksong.GameLibs 1.0.29315 -> 1.0.30000
- DataManager 1.2.1 -> 1.2.2
- FsmUtil 0.3.13 -> 0.3.16
- UnityHelper 1.1.1 -> 1.2.0
- action-gh-release 2 -> 3

## v2.0.0

Wishes overhaul. Rewrites the All Wishes Mode plumbing, ships full quest customization (rules engine + presets + tags + per-quest policies), opens all three wishwalls, lets you complete wishes from anywhere, fixes the four open NPC bugs, and adds a save-safety gate so legacy saves stay vanilla.

Mode + safety:

- All Wishes Mode is now a three-way selector (Disabled / Pure / Adjusted). Adjusted is the recommended one. It bypasses act and NPC prereqs but keeps chain ordering, mutually-exclusive twin gating, and edge-case fixes. Pure exposes every wish raw with no protection (hidden behind an Advanced toggle). Disabled is vanilla.
- Save safety gate. Destructive features (force accept, NPC spawn flag writes, granular bypasses, Remote Complete) only fire on saves QuestMod created. Legacy saves stay vanilla unless you click the override-with-confirm in the Tools tab.

Quest control:

- Per-quest Available and AutoAccept policies with a bulk reset.
- Granular prereq bypasses per save: Fleatopia, Mandatory Wishes, Faydown Cloak, Needolin, Bonebottom Quest Board.
- Quest Boards Always Available per save. Non-destructive blanket toggle. Opens all three wishwalls (Bone Bottom, Bellhart, Songclave) without flipping any story PD bools. Sweeps the scene on load, force-activates the wishwall + every descendant the game shipped inactive, skips post-game broken variants like the skull king destroyed wishwall.
- Fixer FSM rewrite (companion to that toggle). The Fixer NPC's Init state used to send NO WISHWALL when `defeatedBellBeast` was false. Now the action is disabled on player approach so the gate never fires. PlayerData stays untouched. Re-sweeps every 2s for 30s after each scene load to catch the FSM whenever Addressables actually spawns the NPC.

Customization engine:

- Four built-in presets (vanilla, farmable-only, farmable-quarter, quick) plus per-quest `extraConditions` (gates CompleteQuest) and `availableConditions` (gates IsAvailable under Adjusted). User overlay at `BepInEx/config/QuestMod/QuestRequirements.user.json` merges over the embedded baseline.
- In-GUI tag editor in the Tags tab. Per-quest tag pills, one-click adds, custom input, writes back to the user overlay.
- Per-save destructive toggles. `EnableCustomRequirements` and `EnableFullRemoteComplete` moved from global ConfigEntry to per-save fields. The ConfigEntry is just the default for new saves now.
- Per-save active preset. Whichever requirements preset you picked persists with the save.

Quest completion:

- Remote Complete wishes per save. When on, the Complete button walks `FullQuestBase.targets[].Counter.Consume` to deduct items, calls `rewardItem.Get` to grant the reward, cascades through `markCompleted[]` with cycle protection, fires the toast, flips QuestData flags last. Gated by save safety.
- Mr Mushroom per-stage encounter gating. Under Adjusted, the Checklist disables a stage toggle until you've actually encountered that stage.
- Boss defeat gating for arena-reuse wishes. Under Adjusted, Beastfly Hunt waits on `defeatedSongGolem`, Tormented Trobbio waits on `defeatedTrobbio`, Mr Mushroom (first encounter) waits on `MushroomQuestFound1`. Stops the wish from showing up before the arena exists.

QoL:

- Save state Copy/Paste via clipboard in the Tools tab. Four-second confirm on paste so you can't overwrite your state by accident.
- Quest tab search/filter box. Case-insensitive, matches display or internal name.
- Reset all per-save toggles button with confirm. Clears every BypassX, BypassAllWishwalls, AllQuestsAccepted, and per-quest policy in one click. Doesn't touch Override Safety (one-way ratchet).
- Toast notifications for mode changes, auto-accept counts, completion refusals, force-op blocks, mass-op results, save state copy, safety override flip, per-save toggle reset.
- Per-tag pill colors. Each tag hashes to a fixed hue so the same tag always renders the same color.

Fixes:

- Hover tooltip used to get stuck on screen after moving off the control (running the render block on every OnGUI pass with stale GUI.tooltip from the previous frame). Gated on Repaint events now.
- The four open NPC bugs. `CheckQuestStateV2.NotTrackedEvent` was redirecting to `CompletedEvent`, which made every whitelisted quest-giver enter their thank-you dialogue the moment you walked up. Now redirects to `IncompleteEvent` with `CompletedEvent` as fallback. V1 had a parallel bug writing to a nonexistent field. Closes #1 (Plasmium), #2 (Shakra), #4 (Mr Mushroom), #7 (Junilana).

## v1.1.1

- Reverted HarmonyX to 2.9.0 (matches game runtime) to fix `MonoMod.Backports` startup errors.
- Removed MonoDetour dependency (not needed).
- Pinned dependabot off HarmonyX/MonoMod.
- Fixed GUI rendering garbage on Linux/Wine when Segoe UI is missing.

## v1.1.0 (broken)

Bumped HarmonyX to 2.16.0 which required MonoMod.Backports that wasn't shipped. Don't use.

## v1.0.0

Initial release.

- F9 panel: accept, complete, unaccept, uncomplete any quest.
- Chain quest grouping (10 chains across 82 quests) with ◀/▶ step navigation.
- Per-target count overrides with safety clamping, QoL presets (Set to 1, Half, Default), batch multiplier slider.
- Checklist tab for sub-target toggles (bellshrines, flea games, soul snare components) with sequential ordering.
- Silk & Soul tab: threshold editor, per-quest point values, completion tracker.
- All Wishes Mode (single bool at this point) with whitelist-based FSM patching.
- Guaranteed Silver Bells, Quest Item Invincibility, Gourmand Rasher timer freeze, Act 3 toggle.
- Data-driven registry from embedded `QuestCapabilities.json`.
- Custom dark IMGUI skin.

Known issues at the time: Shakra didn't appear when Trail's End was active in All Wishes Mode. Junilana had missing NPC behavior in All Wishes Mode. Both fixed in 2.0.0.
