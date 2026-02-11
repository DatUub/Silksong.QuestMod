# QuestMod

A BepInEx mod for Hollow Knight: Silksong that gives you full control over the quest system — unlock, accept, complete, rewind, and tweak quest targets from an in-game GUI.

> **This is a development/debug tool.** Debug logs are currently enabled and will be cleaned up in a future release.

## Features

- **Quest State Control** — Accept, complete, un-accept, or un-complete any quest
- **Chain Quest Grouping** — Multi-step quest chains (Main Story, Citadel, etc.) are collapsed into single rows with ◀/▶ step navigation
- **Per-Save "All Quests" Mode** — Unlock all quests on boards and NPCs for the current save only (resets on title screen, won't affect other saves)
- **Target Count Overrides** — Edit quest completion targets (e.g. lower Silver Bells from 8 → 3) with per-target sliders
- **Checklist Toggles** — Toggle individual sub-targets for multi-target quests (Grand Gate bellshrines, Flea Games, etc.)
- **Mass Operations** — Accept All / Complete All / Force All buttons with undo support
- **Guaranteed Silver Bells** — Force every bell drop to be a Silver Bell (overrides the 75/25 split)
- **Quest Item Protection** — Prevent delivery quest items from being destroyed by enemy attacks
- **Act 3 Toggle** — Manually toggle Black Thread World state
- **Data Dumps** — Export quest availability, targets, and completion groups to JSON

## Installation

Put `QuestMod.dll` into your `BepInEx/plugins/QuestMod/` folder.

### Dependencies

Install these from Thunderstore first:

- **BepInExPack_Silksong**
- **MMHOOK**
- **FsmUtil**
- **DataManager**
- **UnityHelper**

## Keybinds

| Key | Action |
|-----|--------|
| F9 (default, rebindable) | Toggle the Quest Manager GUI |

## Config Options

Found in `BepInEx/config/com.silkmod.questmod.cfg`, through a config manager, or via the **Tools** tab in the F9 GUI.

| Setting | Default | Description |
|---------|---------|-------------|
| EnableCompletionOverrides | `true` | Restore custom quest target counts when loading a save |
| OnlyDiscoveredQuests | `true` | Only affect quests already discovered in the current save |
| QuestItemInvincible | `false` | Prevent delivery quest items from being destroyed |
| ShowQuestDisplayNames | `true` | Show in-game display names instead of internal names |
| GuiToggleKey | `F9` | Key to open/close the Quest Manager GUI |
| GuaranteedSilverBells | `false` | Force every bell drop to be a Silver Bell |

**Runtime-only toggles** (per-save, reset on title screen):

| Toggle | Description |
|--------|-------------|
| All Quests Available | Show all quests on boards and NPCs, bypass story gates |
| All Quests Accepted | Auto-accept every available quest on scene load |

## GUI (F9)

Four tabs:

- **Quests** — Chain quests grouped with ◀/▶ step controls; standalone quests with accept/complete buttons
- **Targets** — Per-target count overrides with a multiplier slider, organized by category
- **Checklist** — Per-target toggle UI for multi-target quests (Grand Gate, Flea Games, Great Gourmand, Mr Mushroom, Soul Snare)
- **Tools** — Mass operations, data dumps, Act 3 toggle, and quick config toggles

## Project Structure

```
QuestMod/
├── Core/                    # Core logic
│   ├── QuestModPlugin.cs    # Plugin entry point & config
│   ├── QuestAcceptance.cs   # Quest state + chain registry
│   ├── QuestDataAccess.cs   # Reflection helpers for RuntimeData
│   ├── QuestCompletionOverrides.cs
│   └── QuestModSaveData.cs
├── GUI/
│   └── QuestGUI.cs          # IMGUI-based quest manager
├── Patches/
│   ├── QuestStateHooks.cs   # FSM patching for quest availability
│   ├── CheatManagerToggle.cs
│   ├── SilverBellPatch.cs
│   └── SilverBellDiagnostic.cs
├── QuestCapabilities.json   # Quest definitions (embedded resource)
├── QuestMod.csproj
├── README.md
└── TODO.md
```

## Building

Requires the `Silksong.GameLibs` NuGet package. Set your game path in `SilksongPath.props`:

```xml
<Project>
  <PropertyGroup>
    <SilksongPath>C:\path\to\Hollow Knight Silksong</SilksongPath>
  </PropertyGroup>
</Project>
```

Then `dotnet build`.
