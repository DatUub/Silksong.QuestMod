# QuestMod

A BepInEx mod for Hollow Knight: Silksong that gives you full control over the quest (wish) system — unlock, accept, complete, rewind, and tweak quest targets from an in-game GUI.

## Features

- **Quest State Control** — Accept, complete, un-accept, or un-complete any quest
- **Chain Quest Grouping** — Multi-step quest chains (Main Story, Citadel, etc.) collapsed into single rows with ◀/▶ step navigation
- **Per-Save "All Quests" Mode** — Unlock all quests on boards and NPCs for the current save (resets on title screen)
- **Target Count Overrides** — Edit quest completion targets (e.g. lower Silver Bells from 8 → 3) with per-target sliders, QoL presets, and a multiplier slider
- **Checklist Toggles** — Toggle individual sub-targets for multi-target quests (Grand Gate bellshrines, Flea Games, etc.)
- **Silk & Soul Tab** — Edit threshold requirements, quest point values, and track completion progress
- **Mass Operations** — Accept All / Complete All with undo support (gated behind config)
- **Guaranteed Silver Bells** — Force every bell drop to be a Silver Bell (overrides the 75/25 split)
- **Quest Item Protection** — Prevent delivery quest items from being destroyed by enemy attacks
- **Act 3 Toggle** — Manually toggle Black Thread World state
## Installation

### Via Thunderstore (recommended)

Install **QuestMod** from Thunderstore using a mod manager like [Gale](https://github.com/Kesomannen/gale). Dependencies are installed automatically.

### Manual

Put `QuestMod.dll` into `BepInEx/plugins/QuestMod/`.

#### Dependencies

Install these from Thunderstore first:

- **BepInExPack_Silksong**
- **DataManager**
- **FsmUtil**
- **UnityHelper**
- **MonoDetour BepInEx 5**
## Keybinds

| Key | Action |
|-----|--------|
| F9 (default, rebindable) | Toggle the Quest Manager GUI |

## Config Options

Found in `BepInEx/config/com.silkmod.questmod.cfg`, or via the **Tools** tab in the GUI.

| Setting | Default | Description |
|---------|---------|-------------|
| EnableCompletionOverrides | `true` | Restore custom quest target counts when loading a save |
| OnlyDiscoveredQuests | `true` | Only affect quests already discovered in the current save |
| QuestItemInvincible | `false` | Prevent delivery quest items from being destroyed |
| ShowQuestDisplayNames | `true` | Show in-game display names instead of internal names |
| GuiToggleKey | `F9` | Key to open/close the Quest Manager GUI |
| GuaranteedSilverBells | `false` | Force every bell drop to be a Silver Bell |
| EnableSilkSoulTab | `true` | Show the Silk & Soul tab in the GUI |

**Advanced** (hidden by default):

| Setting | Default | Description |
|---------|---------|-------------|
| DebugLogging | `false` | Enable verbose debug logging |
| DevRemoveLimits | `false` | Remove target count slider limits |
| DevForceOperations | `false` | Show Force Accept/Complete ALL buttons |

**Per-save toggles** (set in the Tools tab, reset on title screen):

| Toggle | Description |
|--------|-------------|
| All Quests Available | Show all quests on boards and NPCs, bypass story gates |
| All Quests Accepted | Auto-accept every available quest on scene load (forces Available on) |

## GUI Tabs

| Tab | Description |
|-----|-------------|
| **Quests** | Chain quests grouped with ◀/▶ step controls; standalone quests with accept/complete buttons |
| **Targets** | Per-target count overrides with multiplier slider and QoL presets (Set to 1, Half, Default) |
| **Checklist** | Per-target toggle UI for multi-target quests |
| **Silk & Soul** | Threshold editor, quest point values, and completion tracker |
| **Tools** | Mode toggles, mass operations, Act 3 toggle, and data dumps |

## Known Issues

- Plasmium quests cannot be completed without the Needle Phial in All Wishes Mode
- Shakra does not appear when Trail's End is active in All Wishes Mode
- Junilana has missing NPC behavior in All Wishes Mode
- Mr Mushroom may not spawn at later locations in All Wishes Mode

## Changelog


### v1.0.9
- No longer bundles `MonoMod` runtime DLLs. Now relies on the MonoDetour_BepInEx_5 Thunderstore dependency for runtime patching libraries.
- Fixes startup/load errors caused by missing `MonoMod.Backports` on some installs.

### v1.0.8
- (OBSOLETE) Previously included `MonoMod` runtime DLLs in packaged output to fix startup errors caused by missing `MonoMod.Backports` on some player installs

### v1.0.7
- Fix save data crash when loading saves from older mod versions
- "All Quests Accepted" now automatically enables "All Quests Available"

## Project Structure

```
QuestMod/
├── Core/
│   ├── QuestAcceptance.cs         # Quest state changes + chain registry
│   ├── QuestCompletionOverrides.cs # Persisted target count overrides
│   └── SilkSoulOverrides.cs       # Silk & Soul threshold/point overrides
├── Data/
│   ├── QuestDataAccess.cs         # Reflection helpers for RuntimeData
│   ├── QuestModSaveData.cs        # Per-save data via DataManager
│   └── QuestRegistry.cs           # Loads QuestCapabilities.json
├── GUI/
│   ├── QuestGUI.cs                # Main GUI window + tab routing
│   ├── QuestGUI.Quests.cs         # Quests tab (chain + standalone)
│   ├── QuestGUI.Targets.cs        # Targets tab (count overrides)
│   ├── QuestGUI.Checklist.cs      # Checklist tab (sub-target toggles)
│   ├── QuestGUI.SilkSoul.cs       # Silk & Soul tab
│   ├── QuestGUI.Tools.cs          # Tools tab (toggles, mass ops)
│   └── QuestGUISkin.cs            # Custom IMGUI skin
├── Patches/
│   ├── QuestStateHooks.cs         # FSM patching for quest availability
│   ├── QuestItemProtection.cs     # Delivery item invincibility
│   └── SilverBellPatch.cs         # Guaranteed Silver Bell drops
├── Util/
│   └── ReflectionCache.cs         # Shared reflection helpers
├── QuestCapabilities.json         # Quest definitions (embedded resource)
├── QuestModPlugin.cs              # Plugin entry point & config
└── QuestMod.csproj
```

## Building

1. Clone the repo
2. Set your game path in `SilksongPath.props`:
```xml
<Project>
  <PropertyGroup>
    <SilksongFolder>C:\path\to\Hollow Knight Silksong</SilksongFolder>
  </PropertyGroup>
</Project>
```
3. `dotnet build`

To create a Thunderstore package: `dotnet build -c Release -t:ThunderstorePack`

## Documentation

- [Quest Modding Capabilities](docs/quests/) — detailed reference for all 82 quests, split by category: internal data, targets, prerequisites, and what QuestMod can modify

## Contributing

Bug reports and feature requests welcome via GitHub Issues.

Pull requests are welcome! Please:
- Target the `main` branch
- Keep PRs focused on a single feature or fix
- Test in-game before submitting
- Check `BepInEx/LogOutput.log` for errors

## SPECIAL THANKS

- **TheMythical2046** - For the original mod request, quest research and testing, and feedback :3
