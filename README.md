# QuestMod

BepInEx mod for Hollow Knight: Silksong. **F9** opens a panel for managing quests (wishes): accept, complete, undo, tweak target counts, bypass story gates, and apply custom rules.

**Current release: v2.2.0**

## What it does

- **All Wishes Mode** (Tools): Disabled (vanilla), **Adjusted** (recommended — opens wishes but keeps chain order, exclusions, and edge-case patches), Pure (raw, no checks; Advanced only). See [docs/AllWishesModes.md](docs/AllWishesModes.md).
- **Per-quest** Accept / Drop / Complete / **Undo Complete**, plus Available + AutoAccept toggles with bulk reset.
- **Remote Complete** (Tools, per-save): when ON, Complete mirrors NPC turn-in (deduct targets, grant rewards, cascade). When OFF (default), Complete is **flag-only** — only QuestData flags flip. See [docs/CompleteAndUndo.md](docs/CompleteAndUndo.md).
- **Auto-Accept Available** (Tools): each scene load accepts wishes that pass natural availability (chain-aware). Unlike Accept All, story-locked wishes wait for their gate.
- Chain quests collapse into one row with step navigation.
- Mass-accept / mass-complete available wishes with Undo Last Mass Operation.
- Granular prereq bypasses per save: Fleatopia, Mandatory Wishes, Faydown Cloak, Needolin, Bonebottom Quest Board, plus **Quest Boards Always Available** (all three wishwalls).
- Target count overrides (Targets tab): sliders, presets (Set to 1, Half, Default), category multiplier.
- Checklist UI for multi-target / sequential wishes (Mr Mushroom, etc.).
- Delivery tab: Gourmand timer freeze, quest item invincibility.
- Custom requirements presets from Tools; optional `BepInEx/config/QuestMod/QuestRequirements.user.json` overlay. See [docs/CustomRequirements.md](docs/CustomRequirements.md).
- Save state Copy/Paste via clipboard.

Destructive features are gated behind a one-way **safety override** per save. Legacy saves stay vanilla until you confirm.

### Removed in 2.2.0

- Silk & Soul tab / point overrides  
- Tags editor tab (rules still work via presets + user JSON)  
- Wish Location Reassignment scaffold (never implemented)  
- One-click Act 3 (`blackThreadWorld`) Tools toggle  

## Install

Use [Gale](https://github.com/Kesomannen/gale) or any Thunderstore mod manager. Dependencies pull automatically (BepInExPack_Silksong, DataManager, FsmUtil, UnityHelper).

Manual: drop `QuestMod.dll` into `BepInEx/plugins/QuestMod/` (or `DatUub-QuestMod/`).

## Keys

**F9** opens the Quest Manager. Rebindable in BepInEx config (`GuiToggleKey`).

## Config

`BepInEx/config/com.silkmod.questmod.cfg` or the **Tools** tab.

| Area | Examples |
|------|----------|
| General | EnableCompletionOverrides, OnlyDiscoveredQuests, QuestItemInvincible, ShowQuestDisplayNames, GuiToggleKey, GuiScale, GuaranteedSilverBells |
| Custom requirements | EnableCustomRequirements (per-save), ActivePreset (per-save) |
| Complete | EnableFullRemoteComplete (per-save; also Tools → **Remote Complete**) |
| Delivery | GourmandStopDecay, GourmandDecaySeconds |
| Advanced | DebugLogging, DevRemoveLimits, DevForceOperations, ShowPureWishesMode |

The cfg file has full descriptions for each entry.

## Tabs

| Tab | Purpose |
|-----|---------|
| **Quests** | Chains + wish list; **Active / Done / All** filter; Accept / Drop / Complete / **Undo Complete**; Flag-only vs Remote Complete status |
| **Targets** | Count overrides and presets |
| **Delivery** | Gourmand timer + quest item invincibility |
| **Checklist** | Multi-target / sequential stage ticks |
| **Tools** | Save safety, mass ops, mode, Remote Complete, Auto-Accept Available, prereq bypasses, rules presets, save Copy/Paste |

## Complete / Undo (support)

| Situation | What to do |
|-----------|------------|
| Normal inventory wishes | Flag-only Complete is fine; use **Done → Undo Complete** to reverse |
| Ecstasy of the End / flea carnival (and similar) | Flag-only Complete is **blocked** — finish in the world. Remote Complete may help inventory turn-ins but **won’t run minigames** |
| “I completed something and can’t find Undo” | Quests → **Done** → blue **Undo Complete** |
| Want NPC-style turn-in | Tools → enable **Remote Complete** |

Full detail: [docs/CompleteAndUndo.md](docs/CompleteAndUndo.md).

Default world-state list (flag-only refuse): `Flea Games`, `Flea Games Pre`. Extend via `worldStateComplete` in embedded `QuestCapabilities.json` if you maintain a fork.

## Known issues

- With **Quest Boards Always Available**, the Fixer NPC may briefly play his hammering animation before the FSM rewrite catches him (Addressables load; patch retries for ~30s). The kiosk stays interactable.
- Flag-only Complete does not advance minigame / world FSM wishes — see Complete / Undo above.
- Undo Complete clears completion flags only; it does not refund abilities or minigame progress.

## Docs

| Doc | Topic |
|-----|--------|
| [CHANGELOG.md](CHANGELOG.md) | Release history |
| [docs/AllWishesModes.md](docs/AllWishesModes.md) | Disabled / Adjusted / Pure |
| [docs/CompleteAndUndo.md](docs/CompleteAndUndo.md) | Flag-only vs Remote Complete, Undo |
| [docs/CustomRequirements.md](docs/CustomRequirements.md) | Tags, presets, per-quest rules |
| [docs/quests/](docs/quests/) | Per-quest capability notes |

## Building

```sh
git clone <repo>
# Optional: SilksongPath.props with your install path for post-build copy
dotnet build -c Release
```

Thunderstore package:

```sh
dotnet build -c Release -t:ThunderstorePack
```

### Dev harness (not in Thunderstore builds)

SelfTest + GuiShots compile only when explicitly enabled:

```sh
dotnet build -c Release -p:COMPILE_SELFTEST=true
```

| Env | Effect |
|-----|--------|
| `QUESTMOD_SELFTEST=1` | SelfTest IPC + GuiShots file trigger (no auto-quit) |
| `QUESTMOD_SELFTEST_FULL=1` | Also runs the auto ContinueGame / assertion suite (loads save, then quits) |

Do **not** ship SelfTest builds to Thunderstore. Local harness sources are gitignored.

## Contributing

Issues and PRs welcome. Target `main`. Test in-game; check `BepInEx/LogOutput.log` when something misbehaves.

## Thanks

TheMythical2046 for the original request, quest research, and testing.
