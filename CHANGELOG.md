# Changelog

## v2.0.0: Wishes Overhaul

Big one. Rewrites the entire All Wishes Mode plumbing, ships full quest customization (rules engine with presets, tags, and per quest policies), opens up all three wishwalls, lets you complete wishes from anywhere, fixes the four open NPC bugs, and adds a save safety gate so legacy saves never get touched without explicit opt in.

### Features

- All Wishes Mode is now a three way selector (Disabled, Pure, Adjusted) instead of a single boolean. Adjusted is the recommended one. It bypasses act and NPC prereqs but keeps chain ordering, mutually exclusive twin gating, and the edge case fixes. Pure exposes every wish raw with no protection (hidden behind an Advanced toggle). Disabled is vanilla.
- Save safety gate. Destructive stuff (force accept, NPC spawn flag writes, granular bypasses, remote complete) only fires on saves QuestMod created. Legacy saves stay vanilla unless you click the override with confirm in the Tools tab.
- Per quest Available and AutoAccept policies with a bulk reset button.
- Granular prereq bypasses (per save). Fleatopia, Mandatory Wishes, Faydown Cloak, Needolin, Bonebottom Quest Board.
- Quest Boards Always Available (per save). Non destructive blanket toggle that opens all three wishwalls (Bone Bottom, Bellhart, Songclave) without flipping a single story PD bool. Sweeps the scene on load, force activates the wishwall and every descendant the game shipped inactive (`Quest_Board`, `Quest Board Pivot`, the Quests UI, Backboards), and skips post game broken variants like the skull king destroyed wishwall so they stay dormant.
- Fixer FSM rewrite (companion to the toggle above). The Fixer NPC's Init state used to send a `NO WISHWALL` event when `defeatedBellBeast` was false. Now we disable that action on player approach so the gate never fires. PlayerData never gets touched, so you don't accidentally tick off "Bell Beast defeated" or any other story milestone. Re sweeps every two seconds for thirty seconds after each scene load to catch the FSM whenever Addressables actually spawns the NPC.
- Custom requirements rules engine with four built in presets (`vanilla`, `farmable-only`, `farmable-quarter`, `quick`), plus per quest `extraConditions` (gates `CompleteQuest`) and `availableConditions` (gates `IsAvailable` under Adjusted). User overlay at `BepInEx/config/QuestMod/QuestRequirements.user.json` merges over the embedded baseline.
- In GUI tag editor (Tags tab). Per quest tag pills, one click adds, custom input, writes back to the user overlay.
- Remote complete wishes. Per save toggle. When on, the Complete button in the Quests tab walks `FullQuestBase.targets[].Counter.Consume` to deduct items, calls `rewardItem.Get` to grant the reward, cascades through `markCompleted[]` with cycle protection, fires the toast, and flips `QuestData` flags last. Gated by the save safety gate.
- Mr Mushroom per stage encounter gating. Under Adjusted, the Checklist tab disables a stage toggle until you've actually encountered that stage (`MushroomQuestFound{N}` is true).
- Boss defeat gating for arena reuse wishes (cluster R). Under Adjusted, Beastfly Hunt waits on `defeatedSongGolem`, Tormented Trobbio waits on `defeatedTrobbio`, Mr Mushroom (first encounter) waits on `MushroomQuestFound1`. Stops the wish from showing up before the arena exists.
- Save state import / export. Copy to clipboard and Paste from clipboard buttons in the Tools tab. Four second confirm on paste so you can't paste over your own state by accident.
- Per save destructive toggles. `EnableCustomRequirements` and `EnableFullRemoteComplete` moved from global ConfigEntry to per save fields. The ConfigEntry value is the default for new saves.
- Per save active preset. Whichever requirements preset you picked persists with the save instead of being a global setting.
- Toast notifications for mode changes, auto accept counts, completion refusals, force op blocks, mass op results, save state copy, safety override flip, and per-save-toggle reset.
- Quest tab search/filter box. Type to filter chains and standalone quests by display or internal name (case insensitive).
- "Reset all per-save toggles" button in the Tools tab with a four second confirm. Clears every BypassX, BypassAllWishwalls, AllQuestsAccepted, and per-quest policy in one click. Does NOT touch Override Safety (one way ratchet).
- Per tag pill colors. Each tag hashes to a fixed hue so the same tag always renders the same color across rows in the Tags tab. Helps visually group quests with shared tags.

### Fixes

- Hover tooltip used to get stuck on screen after you moved off the control. Was running the render block on every OnGUI pass including Layout, so it drew with stale `GUI.tooltip` from the previous frame. Gated on Repaint events now.
- The four open NPC bugs. `CheckQuestStateV2.NotTrackedEvent` was redirecting to `CompletedEvent`, which made every whitelisted quest giver enter their thank you dialogue the moment you walked up. Now redirects to `IncompleteEvent` with `CompletedEvent` as fallback. V1 had a parallel bug writing to a nonexistent field. Closes #1 (Plasmium), #2 (Shakra), #4 (Mr Mushroom), #7 (Junilana).

## v1.1.1 — Dependency Fix

### Fixes
- Revert HarmonyX to 2.9.0 (matches game runtime) to fix `MonoMod.Backports` startup errors
- Remove MonoDetour dependency (not needed)
- Fix dependabot auto-bumping HarmonyX/MonoMod
- Fix GUI rendering garbage on Linux/Wine when Segoe UI font is not installed

## v1.1.0 — BROKEN

### Notes
- Bumped HarmonyX to 2.16.0 which required MonoMod.Backports not present at runtime

## v1.0.0 — Initial Release

### Features
- **Quest Management GUI** — Accept, complete, unaccept, and uncomplete any quest from an in-game overlay (default: F9)
- **Chain Quest Grouping** — 10 quest chains with ◀/▶ navigation across all 82 quests
- **Target Count Overrides** — Per-target count adjusters with safety clamping, QoL presets (Set to 1, Half, Default), and batch multiplier slider
- **Checklist Tab** — Toggle individual sub-targets (bellshrines, flea games, soul snare components) with sequential ordering support
- **Silk & Soul Tab** — Edit the Soul Snare threshold, adjust quest point values, and track completion progress
- **All Wishes Mode** — Per-save toggle that makes all quests available regardless of prerequisites (whitelist-based FSM patching)
- **Guaranteed Silver Bells** — Config option to guarantee silver bell drops from bell enemies
- **Quest Item Invincibility** — Protect delivery items from destruction and hit damage
- **Gourmand Rasher Timer Stop** — Prevents the Courier's Rasher from decaying while carried
- **Act 3 Toggle** — Manually toggle Black Thread World state
- **Data-Driven Registry** — All quest definitions loaded from embedded `QuestCapabilities.json`
- **Custom GUI Skin** — Dark theme with styled controls

### Configuration
All boolean configs are compatible with ModMenu. Advanced options (`DevRemoveLimits`, `DevForceOperations`) are hidden by default.

### Known Issues
- Shakra does not appear when Trail's End is active in All Wishes Mode
- Junilana has missing NPC behavior in All Wishes Mode
- Mr Mushroom may not spawn at later locations in All Wishes Mode
