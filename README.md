# QuestMod

A BepInEx mod for Hollow Knight: Silksong that gives you full control over the quest (wish) system — unlock, accept, complete, rewind, and tweak quest targets from an in-game GUI.
## Features

### Core
- **All Wishes Mode** -- three way Disabled / Adjusted / Pure selector. Adjusted bypasses act and NPC prereqs but keeps chain ordering, mutually exclusive twin gating, and edge case fixes (recommended). Pure exposes every wish raw with no protection (Advanced gated). Disabled is vanilla.
- **Save safety gate** -- destructive features only fire on saves QuestMod created or saves the user explicitly opts in via override-with-confirm. Legacy saves stay vanilla.
- **Quest state control** -- accept, complete, un-accept, un-complete any quest
- **Remote complete wishes** -- per save toggle. When ON, the per quest Complete button mirrors the NPC dialogue: deducts required items, grants the reward, cascades dependent quests, and shows the toast.
- **Chain quest grouping** -- multi step chains (Main Story, Citadel, etc.) collapse into rows with step navigation
- **Mass operations** -- Accept Available / Complete Available with undo (force variants gated behind a dev flag)

### Customization
- **Custom requirements** -- four built in presets (vanilla, farmable-only, farmable-quarter, quick) plus per quest extra and available conditions. User overlay at `BepInEx/config/QuestMod/QuestRequirements.user.json`.
- **In GUI tag editor** -- author the cluster I tag taxonomy via per quest pills, one click adds, custom input
- **Per quest Available + AutoAccept** policies with bulk reset
- **Granular prereq bypasses** (per save) -- Fleatopia, Mandatory Wishes, Faydown Cloak, Needolin, Bonebottom Quest Board, plus a blanket Quest Boards Always Available toggle that opens all three wishwalls (Bone Bottom, Bellhart, Songclave) without flipping any story PD bools
- **Target count overrides** -- per target sliders, QoL presets, multiplier slider
- **Checklist toggles** -- per target toggle UI for multi target quests; sequential quests respect per stage encounter gating under Adjusted

### Quality of life
- **Silk & Soul tab** -- threshold editor, quest point values, completion tracker
- **Delivery tab** -- separate panel for courier quests with Gourmand timer freeze and quest item invincibility toggles
- **Save state import / export** -- Copy / Paste your QuestMod save state via clipboard for sharing or backup
- **Toast notifications** for mode changes, auto accept counts, etc.
- **Guaranteed Silver Bells** -- force every bell drop to a Silver Bell
- **Act 3 toggle** -- manually toggle Black Thread World state
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
| **Quests** | Chain quests grouped with step controls; standalone quests with accept / complete / drop / undo buttons; per quest Available + AutoAccept toggles |
| **Targets** | Per target count overrides with multiplier slider and QoL presets (Set to 1, Half, Default) |
| **Delivery** | Courier quests + Great Gourmand controls (timer freeze, item invincibility) |
| **Checklist** | Per target toggle UI for multi target quests; sequential quests respect per stage encounter gating under Adjusted |
| **Silk & Soul** | Threshold editor, quest point values, completion tracker |
| **Tags** | In GUI editor for the cluster I tag taxonomy (writes to user overlay) |
| **Tools** | All Wishes Mode selector, save safety gate, mass operations, save state I/O, granular prereq bypasses, custom requirements toggle |

## Known Issues

- Quest Boards Always Available may show the Fixer NPC briefly hammering until the FSM rewrite sweep catches him on player approach (Silksong loads the NPC FSM via Addressables, so the sweep retries every two seconds for thirty seconds after each scene load); the kiosk itself is interactable regardless

## Changelog


### v1.1.1
- Reverts HarmonyX to 2.9.0 (matches game runtime) to fix `MonoMod.Backports` startup errors
- Removes MonoDetour dependency (not needed)
- Fixes dependabot auto-bumping HarmonyX/MonoMod
- Fixes GUI rendering on Linux/Wine when Segoe UI font is not installed

### v1.1.0 (broken)
- (BROKEN) Bumped HarmonyX to 2.16.0 which required MonoMod.Backports not present at runtime

### v1.0.8 (obsolete)
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
