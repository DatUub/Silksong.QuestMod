# QuestMod - TODO

## General (applies to all quests)

### Per-Quest Feature Key
Each quest tracks which mod features apply to it:
- **State** = shows in Quests tab (accept/complete buttons)
- **Count** = shows in Completion tab (edit target count)
- **Available** = affected by All Quests Available toggle (per-save)
- **AutoAccept** = affected by All Quests Accepted toggle (per-save)

---

## Gather / Hunt Quests

### Flexile Spines (Brolly Get)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (number of brollies to collect?)
- [ ] Research: does lowering Count below current progress auto-complete?
- [ ] Research: what spawns brollies — is spawn rate moddable?
- [ ] Test count override
- [ ] Test reset restores original

### Broodfeast (Huntress Quest)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (kill count?)
- [ ] Research: does lowering Count auto-complete?
- [ ] Test count override
- [ ] Test reset

### Runtfeast (Huntress Quest Runt)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control?
- [ ] Research: does this quest chain from Broodfeast?
- [ ] Test count override
- [ ] Test reset

### Silver Bells (Shiny Bell Goomba)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (bells to collect?)
- [ ] Research: does lowering Count auto-complete?
- [ ] Research: what controls bell spawn rate — can we modify it?
- [ ] Test count override
- [ ] Test reset
- [ ] Silver bell spawn chance modifier

### Volatile Flintbeetles (Rock Rollers)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (rollers to kill?)
- [ ] Research: does lowering Count auto-complete?
- [ ] Test count override
- [ ] Test reset

### Garb of the Pilgrims (Pilgrim Rags)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (rags to collect?)
- [ ] Research: are rags world pickups or drops?
- [ ] Test count override
- [ ] Test reset

### Fine Pins (Fine Pins)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (pins to collect?)
- [ ] Research: are pins enemy drops — what controls drop rate?
- [ ] Test count override
- [ ] Test reset
- [ ] Drop RNG modifier

### Cloaks of the Choir (Song Pilgrim Cloaks)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control?
- [ ] Research: are cloaks world pickups or drops?
- [ ] Test count override
- [ ] Test reset

### Roach Guts (Roach Killing)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (roaches to kill?)
- [ ] Research: do roaches respawn or are they finite?
- [ ] Test count override
- [ ] Test reset

### Crawbug Clearing (Crow Feathers + Crow Feathers Pre)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (feathers to collect?)
- [ ] Research: are feathers drops or world pickups?
- [ ] Research: what does the Pre stage do? (initial acceptance phase?)
- [ ] Test count override
- [ ] Test reset

### Rite of the Pollip (Shell Flowers)
- State: ✅ | Count: ❌ not in questCategories | Available: ✅ | AutoAccept: ✅
- [ ] Add to questCategories dictionary as "Shell Flowers"
- [ ] Research: what does Count control?
- [ ] Research: are flowers world pickups — finite or respawning?
- [ ] Test count override
- [ ] Test reset

### Berry Picking (Mossberry Collection Pre + Mossberry Collection 1)
- State: ✅ | Count: ❌ not in questCategories | Available: ✅ | AutoAccept: ✅
- [ ] Add "Mossberry Collection 1" to questCategories dictionary
- [ ] Research: what does Count control?
- [ ] Research: what does the Pre stage do?
- [ ] Research: are mossberries world pickups — finite or respawning?
- [ ] Test count override
- [ ] Test reset

---

## Craftmetal Quests

### Bone Bottom Repairs (Building Materials)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (craftmetal amount?)
- [ ] Research: does the game check exact amount or >= ?
- [ ] Research: what happens if player already donated some?
- [ ] Test count override
- [ ] Test reset

### An Icon of Hope (Building Materials Statue)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: same questions as Bone Bottom Repairs
- [ ] Test count override
- [ ] Test reset

### A Lifesaving Bridge (Building Materials Bridge)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: same questions as Bone Bottom Repairs
- [ ] Test count override
- [ ] Test reset

### Restoration of Bellhart (Belltown House Start)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control?
- [ ] Research: does completing this unlock Bellhart's Glory?
- [ ] Test count override
- [ ] Test reset

### Bellhart's Glory (Belltown House Mid)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control?
- [ ] Research: is this chained from Restoration of Bellhart?
- [ ] Test count override
- [ ] Test reset

### Building Up Songclave (Songclave Donation 1)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (craftmetal to donate?)
- [ ] Research: does donation count track globally or per-quest?
- [ ] Test count override
- [ ] Test reset
- [ ] Donation-locked wish bypass

### Strengthening Songclave (Songclave Donation 2)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control?
- [ ] Research: is this chained from Building Up Songclave?
- [ ] Test count override
- [ ] Test reset
- [ ] Donation-locked wish bypass
- [ ] Broodmother prerequisite bypass (5 Songclave wishes)

---

## Progression Quests

### Grand Gate (Grand Gate Bellshrines)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (bellshrines to activate?)
- [ ] Research: does completion trigger a scripted event? (gate opening?)
- [ ] Research: will changing Count break story progression?
- [ ] Decide: should Count editing be blocked for this quest?

### Passing of the Age (Mr Mushroom)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (mushroom locations to visit?)
- [ ] Research: is this a sequential chain (must visit in order)?
- [ ] Decide: should Count editing be blocked for this quest?

### Great Taste of Pharloom (Great Gourmand)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control?
- [ ] Research: does completion trigger scripted events?
- [ ] Decide: should Count editing be blocked for this quest?

### Ecstasy of the End (Flea Games + Flea Games Pre)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (games to win?)
- [ ] Research: does completion trigger scripted events?
- [ ] Research: what does the Pre stage do?
- [ ] Decide: should Count editing be blocked for this quest?

### Silk and Soul (Soul Snare + Soul Snare Pre)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control?
- [ ] Research: does completion trigger scripted events?
- [ ] Research: what does the Pre stage do?
- [ ] Decide: should Count editing be blocked for this quest?

---

## Courier Quests (excluded from count overrides)

### Liquid Lacquer (Courier Delivery Mask Maker)
- State: ✅ | Count: ❌ excluded | Available: ✅ | AutoAccept: ❌ excluded
- [ ] Research: is Count delivery health or something else?
- [ ] Research: what happens when courier dies mid-delivery?
- [ ] Research: can courier be re-summoned?
- [ ] Courier delivery health modifier
- [ ] Should this be in the count editor?

### Fleatopia Supplies (Courier Delivery Fleatopia)
- State: ✅ | Count: ❌ excluded | Available: ✅ | AutoAccept: ❌ excluded
- [ ] Research: same questions as Liquid Lacquer
- [ ] Should this be in the count editor?

### Queen's Egg (Courier Delivery Dustpens Slave)
- State: ✅ | Count: ❌ excluded | Available: ✅ | AutoAccept: ❌ excluded
- [ ] Research: same questions as Liquid Lacquer
- [ ] Should this be in the count editor?

### Pilgrim's Rest Supplies (Courier Delivery Pilgrims Rest)
- State: ✅ | Count: ❌ excluded | Available: ✅ | AutoAccept: ❌ excluded
- [ ] Research: same questions as Liquid Lacquer
- [ ] Should this be in the count editor?

### Bone Bottom Supplies (Courier Delivery Bonebottom)
- State: ✅ | Count: ❌ excluded | Available: ✅ | AutoAccept: ❌ excluded
- [ ] Research: same questions as Liquid Lacquer
- [ ] Should this be in the count editor?

### Survivor's Camp Supplies (Courier Delivery Fixer)
- State: ✅ | Count: ❌ excluded | Available: ✅ | AutoAccept: ❌ excluded
- [ ] Research: same questions as Liquid Lacquer
- [ ] Should this be in the count editor?

### Songclave Supplies (Courier Delivery Songclave)
- State: ✅ | Count: ❌ excluded | Available: ✅ | AutoAccept: ❌ excluded
- [ ] Research: same questions as Liquid Lacquer
- [ ] Should this be in the count editor?

### My Missing Courier (Save Courier Short)
- State: ❌ not wired | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: is this a rescue quest or courier quest?
- [ ] Decide: which category and features apply

### My Missing Brother (Save Courier Tall)
- State: ❌ not wired | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: is this a rescue quest or courier quest?
- [ ] Decide: which category and features apply

---

## Misc Quests

### Advanced Alchemy (Extractor Blue Worms)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (worms to collect?)
- [ ] Research: are worms finite or respawning?
- [ ] Test count override
- [ ] Test reset

### Alchemist's Assistant (Extractor Blue)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control?
- [ ] Research: is this related to Advanced Alchemy?
- [ ] Test count override
- [ ] Test reset

### Bugs of Pharloom (Journal)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (journal entries?)
- [ ] Research: will changing Count break journal tracking?
- [ ] Decide: should Count editing be blocked for this quest?

### A Vassal Lost (Steel Sentinel + Steel Sentinel Pt2)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control?
- [ ] Research: how does Sentinel appearance work (Garmond)?
- [ ] Research: what does Pt2 do?
- [ ] Test count override
- [ ] Garmond / 2nd Sentinel appearance count bypass

### The Lost Fleas (Save the Fleas + Save the Fleas Pre)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (fleas to save?)
- [ ] Research: are fleas in specific locations or random?
- [ ] Research: what does the Pre stage do?
- [ ] Test count override
- [ ] Test reset

### Fastest in Pharloom (Sprintmaster Race + Sprintmaster Pre)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (races to win? time?)
- [ ] Research: will changing Count break the race mechanic?
- [ ] Research: what does the Pre stage do?
- [ ] Decide: should Count editing be blocked for this quest?

### Dark Hearts (Destroy Thread Cores)
- State: ✅ | Count: ✅ | Available: ✅ | AutoAccept: ✅
- [ ] Research: what does Count control? (cores to destroy?)
- [ ] Research: are cores in fixed locations?
- [ ] Test count override
- [ ] Test reset

### The Wandering Merchant (Save City Merchant)
- State: ❌ not wired | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: what is this quest?
- [ ] Decide: which category and features apply

### The Lost Merchant (Save City Merchant Bridge)
- State: ❌ not wired | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: is this chained from The Wandering Merchant?
- [ ] Decide: which category and features apply

### Balm for the Wounded (Save Sherma)
- State: ❌ not wired | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: what is this quest?
- [ ] Decide: which category and features apply

---

## Boss / Special Quests

### The Terrible Tyrant (Skull King)
- State: ✅ | Count: ❌ excluded | Available: ✅ | AutoAccept: ✅
- [ ] Research: Count is ×1 — does changing it matter?
- [ ] Should this be in the count editor?

### Savage Beastfly (Beastfly Hunt)
- State: ✅ | Count: ❌ excluded | Available: ✅ | AutoAccept: ✅
- [ ] Research: Count is ×1 — does changing it matter?
- [ ] Should this be in the count editor?

### The Hidden Hunter (Ant Trapper)
- State: ✅ | Count: ❌ excluded | Available: ✅ | AutoAccept: ✅
- [ ] Research: Count is ×1 — does changing it matter?
- [ ] Should this be in the count editor?

### The Wailing Mother (Broodmother Hunt)
- State: ✅ | Count: ❌ excluded | Available: ✅ | AutoAccept: ✅
- [ ] Research: Count is ×1 — does changing it matter?
- [ ] Research: locked behind 5 Songclave wishes — bypass?

### Infestation Operation (Doctor Curse Cure)
- State: ✅ | Count: ❌ excluded | Available: ✅ | AutoAccept: ✅
- [ ] Research: Count is ×1 — does changing it matter?
- [ ] Should this be in the count editor?

### Pinmaster's Oil (A Pinsmiths Tools)
- State: ✅ | Count: ❌ excluded | Available: ✅ | AutoAccept: ✅
- [ ] Research: Count is ×1 — does changing it matter?
- [ ] Should this be in the count editor?

### Fatal Resolve (Pinstress Battle + Pinstress Battle Pre)
- [ ] Research: what triggers Pinstress fight?
- [ ] Research: what does the Pre stage do?
- [ ] Pinstress before needle strike handling
- [ ] Research: is this act-locked?
- [ ] Act-locked wish override

### Beast in the Bells (Bellbeast Rescue)
- State: ❌ not wired | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: what is this quest? (boss fight or rescue?)
- [ ] Decide: which features apply

### Rite of Rebirth (Wood Witch Curse)
- State: ❌ not wired | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: what is this quest?
- [ ] Decide: which features apply

### Final Audience (Song Knight)
- State: ❌ not wired | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: what is this quest?
- [ ] Decide: which features apply

### Trail's End (Shakra Final Quest)
- State: ❌ not wired | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: what is this quest?
- [ ] Decide: which features apply

### Hero's Call (Garmond Black Threaded)
- State: ❌ not wired | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: what is this quest?
- [ ] Decide: which features apply

### Pain (Anguish and Misery)
- State: ❌ not wired | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: what is this quest?
- [ ] Decide: which features apply

### SB2 (Soulbound 2)
- [ ] Research: arena conflict with 4C
- [ ] Research: is this act-locked?
- [ ] Act-locked wish override

### Trobbio
- [ ] Research: what causes double Trobbio?
- [ ] Research: is this a quest or NPC encounter?
- [ ] Double Trobbio handling

---

## Main Story Chain (research needed)

### The Great Citadel (Citadel Seeker)
- State: ❌ | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: should this be moddable?

### The Threadspun Town (The Threadspun Town)
- State: ❌ | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: should this be moddable?

### Silent Halls (Citadel Investigate)
- State: ❌ | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: should this be moddable?

### Pharloom's Crown (Citadel Ascent + Citadel Ascent Melodies + Citadel Ascent Lift)
- State: ❌ | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: 3 internal entries for one quest — how do they interact?
- [ ] Research: should this be moddable?

### Pale Monarch (Citadel Ascent Silk Defeat)
- State: ❌ | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: should this be moddable?

### After the Fall (Black Thread Pt0)
- State: ❌ | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: should this be moddable?

### Awaiting the End (Black Thread Pt1 Shamans)
- State: ❌ | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: should this be moddable?

### The Dark Below (Black Thread Pt2 Abyss + Diving Bell Pt1-3)
- State: ❌ | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: 4 internal entries — how do they interact?
- [ ] Research: should this be moddable?

### Return to Pharloom (Black Thread Pt3 Escape)
- State: ❌ | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: should this be moddable?

### Spell Seeker (Black Thread Pt4 Return)
- State: ❌ | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: should this be moddable?

### The Old Hearts (Black Thread Pt5 Heart)
- State: ❌ | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: should this be moddable?

### Last Dive (Black Thread Pt6 Flower)
- State: ❌ | Count: ❌ | Available: ❌ | AutoAccept: ❌
- [ ] Research: should this be moddable?

---

## 🔴 After per-quest research (before v0.1.0 release)
- [x] CompleteTotalGroup runtime dump to JSON (Dump Groups button in Tools tab)
- [x] Fixed CompleteTotalGroup type lookup (actual type is `QuestCompleteTotalGroup`)
- [x] Tools tab with mass operations, data dumps, and quick config toggles
- [x] Force Accept ALL / Force Complete ALL buttons (bypass OnlyDiscoveredQuests)
- [x] Force ops preserve Act state (`blackThreadWorld` saved/restored)
- [x] Act 3 (Black Thread World) manual toggle in Tools tab
- [x] Undo button for mass operations (snapshot/restore)
- [x] Configurable GUI keybind (`GuiToggleKey`, default F9)
- [x] ShowQuestDisplayNames config option
- [x] Actually wire ShowQuestDisplayNames to the GUI quest list
- [x] Chain quest grouping in Quests tab (10 chains with ◀/▶ controls)
- [x] Per-save All Quests mode (runtime-only, resets on title screen)
- [x] ShowAllCompletionIcons synced with All Quests mode
- [x] Guaranteed Silver Bells config option (SilverBellPatch)
- [ ] Analyze CompleteTotalGroups.json for quest chain/prerequisite data
- [ ] Per-item count adjusters (individual +/- for each quest target, organized by category)
- [ ] Wire up `questCategories` dictionary to GUI for category grouping
- [ ] Block/disable unsafe count edits in the GUI
- [x] Un-accept / un-complete quests (reverse buttons in GUI)
- [ ] Quest progress tracker — show current vs target (e.g. 3/5 Silver Bells)
- [ ] Remote complete wishes (complete from anywhere, with/without item)
- [ ] Better GUI (styling, layout)
- [ ] Completion tab: show read-only counts when EnableCompletionOverrides is off (instead of blocking the whole tab)
- [x] Display names in GUI (show "Flexile Spines" instead of "Brolly Get")
- [ ] Agree on QoL default config for v0.1.0
- [ ] Remove/gate debug logs for release
- [ ] TheMythical playtest feedback
- [ ] Config manager (BepInEx ConfigManager) compatibility check
- [ ] Version number display in GUI
- [x] Reorganize CS source files into folders (Core/, GUI/, Patches/)

---

## After v0.1.0
- [x] Quest chain grouping (implemented with chain registry)
- [ ] "Only unlockable quests" mode
- [ ] Silk & Soul point threshold override
- [ ] Silk & Soul per-quest point value override
- [ ] Toggle individual Silk & Soul requirements
- [ ] Option for "pure" vs "adjusted" All Wishes Mode
- [ ] Custom wish board population
- [ ] Per-board wish list customization
- [ ] Add custom requirements to wishes
- [ ] Boss ordering (defeat certain bosses before others appear)
- [ ] Thunderstore packaging (.toml, CI)
- [x] Configurable keybind (F9 default, rebindable)
- [ ] Per-target toggle for multi-target quests
- [ ] Changelog
