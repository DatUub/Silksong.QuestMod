# Changelog


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
- Plasmium quests cannot be completed without the Needle Phial in All Wishes Mode
- Shakra does not appear when Trail's End is active in All Wishes Mode
- Junilana has missing NPC behavior in All Wishes Mode
- Mr Mushroom may not spawn at later locations in All Wishes Mode
