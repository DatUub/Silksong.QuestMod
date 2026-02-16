# QuestMod - TODO

## ✅ Completed

- [x] CompleteTotalGroup runtime dump to JSON (Dump Groups button in Tools tab)
- [x] Fixed CompleteTotalGroup type lookup (actual type is `QuestCompleteTotalGroup`)
- [x] Tools tab with mass operations, data dumps, and quick config toggles
- [x] Force Accept ALL / Force Complete ALL buttons (bypass OnlyDiscoveredQuests)
- [x] Force ops preserve Act state (`blackThreadWorld` saved/restored)
- [x] Act 3 (Black Thread World) manual toggle in Tools tab
- [x] Undo button for mass operations (snapshot/restore)
- [x] Configurable GUI keybind (`GuiToggleKey`, default F9)
- [x] ShowQuestDisplayNames config option
- [x] Display names in GUI (show "Flexile Spines" instead of "Brolly Get")
- [x] Chain quest grouping in Quests tab (10 chains with ◀/▶ controls)
- [x] Per-save All Quests mode (runtime-only, resets on title screen)
- [x] ShowAllCompletionIcons synced with All Quests mode
- [x] Guaranteed Silver Bells config option (SilverBellPatch)
- [x] Un-accept / un-complete quests (reverse buttons in GUI)
- [x] Reorganize CS source files into folders (Core/, GUI/, Patches/)
- [x] Quest chain grouping (implemented with chain registry)
- [x] Scrape game data for per-quest prerequisites — 36/82 quests have prereqs
- [x] Research Melody PD fields and Heart PD fields
- [x] Per-item +/- count adjusters, blocked unsafe edits (min 1, MaxCaps)
- [x] Bone Bottom donation caps (800 Shell Shards)
- [x] Read-only counts when `EnableCompletionOverrides` is off
- [x] Version number display in GUI title
- [x] QoL preset buttons (Set to 1, QoL Half, Default)
- [x] Broodfeast/Runtfeast mutual exclusion + shared target syncing
- [x] Gate debug logs behind `DebugLogging` config
- [x] Quest progress tracker — show current vs target (green when met)
- [x] Chain-aware availability & auto-accept — `IsChainPrereqMet()`
- [x] Journal set-count cap — 236 entries
- [x] DevDebug mode config (drops all hard limits)
- [x] Per-target toggle for multi-target quests
- [x] Silk & Soul tab — threshold display, override, quest point checklist
- [x] Per-quest point value override (runtime field discovery)
- [x] S&S ConfigManager entry (`SilkSoulThreshold`, runtime SettingChanged)
- [x] Shared `ReflectionCache` helper — cached type/field/property lookups, null-safe Read/Write, WriteToArray

---

## 🚀 Pre-Release (v0.1.0)

### Code Quality
- [x] Shared reflection helper (`ReflectionCache.cs`) — consolidated scattered AccessTools calls
- [x] Gated dump/debug tools behind `DebugLogging` config (dump buttons, `LogFsmActionTypes`)
- [x] Removed dead code (`ListQuests`, `DiscoverQuestObjects`, unused `using System.Linq`)
- [x] Clean up file structure (extracted QuestDumper.cs from QuestCompletionOverrides)
- [x] Audit config entries — regrouped sections, added Order, improved descriptions
- [x] Review all reflection access for null safety and error handling
- [x] SRP, DRY & Functional Decomposition (extracted EnsureQuestEntry, ForceAllQuestsOp, ModifyAllQuests)
### Config & Modularity
- [x] Removed QuestDumper.cs entirely — dumping not part of this mod
- [x] Review default config values for v0.1.0 (what should be on/off out of the box) and remove bad and redundant options
- [x] Make features independently toggleable (e.g. disable Silk & Soul tab, completion overrides)
- [x] All config entries visible and grouped properly in ModMenu (bools only, removed int/keyboard entries)
- [x] Opt-in destructive features (force-complete, mass operations default off)
- [] Rstructure prject
- [ ] Verify all features in [requested.md](requested.md) are fulfilled
### GUI Polish
- [ ] Improve GUI layout and spacing (consistent widths, better grouping)
- [ ] Review tab naming and ordering
- [ ] Add tooltips or inline help for non-obvious features

#


### Packaging & Release
https://github.com/silksong-modding/Silksong.Modding.Templates
- [ ] Thunderstore packaging (.toml, icon, CI)
- [ ] Write changelog for v0.1.0
- [ ] README with feature list and install instructions
- [ ] Set version number to 0.1.0

---

## 🔮 Post-Release

## Testing & Validation
- [ ] TheMythical playtest — collect feedback
- [ ] Test fresh save + late-game save compatibility
- [ ] Test with ConfigManager installed
- [ ] Verify title screen reset clears all state properly
### Delivery System
- [ ] Separate **Delivery tab** — deliveries + Great Gourmand
- [ ] Gourmand Rasher timer stop (`HeroController.TickDeliveryItems` patch)
- [ ] Gourmand custom implementation (8 segments, 47s decay)

### Quest Customization
- [ ] Remote complete wishes (complete from anywhere)
- [ ] Improve Available and AutoAccept on a quest-by-quest basis

### All Wishes Mode
- [ ] "Pure" vs "adjusted" All Wishes Mode
- [ ] Boss ordering (defeat prerequisites)

### Wish Board Customization
- [ ] Custom wish board population
- [ ] Per-board wish list editing
- [ ] Custom requirements on wishes

### ItemChanger Integration
- [ ] Melodies + Hearts GUI (requires ItemChanger for collectable items)

### UI/UX
- [ ] Better GUI styling (custom skin, colors, icons)