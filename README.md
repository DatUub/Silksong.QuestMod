# QuestMod

BepInEx mod for Hollow Knight: Silksong. F9 opens a panel for managing quests (wishes). Accept, complete, undo, tweak target counts, bypass story gates, write custom rules.

## What it does

- Three-way All Wishes Mode: Disabled (vanilla), Adjusted (recommended, opens everything but keeps chain order + edge-case patches + boss arena-reuse gates), Pure (raw, no checks, hidden behind Advanced).
- Per-quest Accept / Drop / Complete / Undo buttons, plus per-quest Available + AutoAccept toggles with bulk reset.
- Remote Complete (opt-in per save): the Complete button mirrors NPC turn-in, deducts items + grants rewards + cascades dependents.
- Chain quests collapse into one row with step navigation.
- Mass-accept / mass-complete available wishes with Undo.
- Granular prereq bypasses per save: Fleatopia, Mandatory Wishes, Faydown Cloak (grants the ability), Needolin (grants it), Bonebottom Quest Board, plus a blanket Quest Boards Always Available.
- Target count overrides: per-target sliders, presets (Set to 1, Half, Default), category multiplier.
- Checklist UI for multi-target quests. Sequential ones (Mr Mushroom etc.) gate per-stage so you can't tick stages you haven't met.
- Silk & Soul tab: threshold + per-quest point values.
- Delivery tab: Gourmand timer freeze, quest item invincibility.
- Tag editor for the custom-requirements DSL (writes to your user overlay).
- Save state Copy/Paste via clipboard so you can share or back up your config.

Destructive features are gated behind a one-way safety override per save. Legacy saves stay vanilla until you confirm.

## Install

Use [Gale](https://github.com/Kesomannen/gale) or any Thunderstore mod manager. Deps pull automatically (BepInExPack_Silksong, DataManager, FsmUtil, UnityHelper).

Manual: drop `QuestMod.dll` into `BepInEx/plugins/QuestMod/`.

## Keys

F9. Rebindable in BepInEx config.

## Config

In `BepInEx/config/com.silkmod.questmod.cfg` or the Tools tab.

General toggles: EnableCompletionOverrides, OnlyDiscoveredQuests, QuestItemInvincible, ShowQuestDisplayNames, GuiToggleKey, GuiScale, GuaranteedSilverBells, EnableSilkSoulTab.

Custom requirements: EnableCustomRequirements (per-save), ActivePreset (per-save), EnableFullRemoteComplete (per-save).

Delivery: GourmandStopDecay, GourmandDecaySeconds.

Advanced (hidden by default): DebugLogging, DevRemoveLimits, DevForceOperations, ShowPureWishesMode.

The cfg file has full descriptions for each.

## Tabs

Quests, Targets, Delivery, Checklist, Silk & Soul, Tags, Tools.

## Known issues

When Quest Boards Always Available is on, the Fixer NPC may briefly play his hammering animation before the FSM rewrite catches him (Silksong loads his FSM via Addressables, so the patch retries every 2s for 30s after a scene load). The kiosk itself is interactable regardless.

## Docs

[CHANGELOG.md](CHANGELOG.md) for release notes. [docs/](docs/) for per-quest data + design notes on the rules engine.

## Building

```sh
git clone <repo>
# Set SilksongPath.props to your install path
dotnet build
```

Thunderstore package: `dotnet build -c Release -t:ThunderstorePack`.

## Contributing

Issues + PRs welcome. Target main. Test in-game. Check `BepInEx/LogOutput.log` if something acts weird.

## Thanks

TheMythical2046 for the original request, quest research, and testing.
