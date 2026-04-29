# Custom Requirements — Spec

Status: **draft** (v1, schema version `1`).
Implementation: partial — preset application + extra-condition gating ship in this iteration.
Per-board editing UI is out-of-scope for this round (tracked separately as
"Custom wish board population and per-board editing").

## Goal

Let users override quest completion requirements declaratively, without editing
mod source. Two layers:

1. **Presets** — sweep many quests at once by tag (`farmable-only`, `quick`, ...).
2. **Per-quest overrides** — fine-grained count edits and *extra conditions* that
   gate turn-in (e.g. "complete only if `silkSkillsAcquired >= 5`").

## File location

* Embedded baseline (read-only): `QuestMod.Data.QuestRequirements.json`
  (resource — defines tags + built-in presets).
* User override (writable): `BepInEx/config/QuestMod/QuestRequirements.user.json`.
  Created on first run if missing. Edits take effect on save reload.

The user file *merges* over the baseline (see "Merge order" below).

## Schema

```jsonc
{
  "version": 1,

  // Tag annotations: questId or targetCounter -> tag list.
  // Tags drive preset selection; ship a sensible default set, user can extend.
  "tags": {
    "Brolly Get":           ["farmable", "garb"],
    "Pilgrim Rags":         ["farmable", "garb"],
    "Crow Feathers":        ["farmable"],
    "Fine Pins":            ["farmable", "pin"],
    "Roach Killing":        ["farmable"],
    "Song Pilgrim Cloaks":  ["farmable", "garb"],
    "Destroy Thread Cores": ["farmable"],
    "Mossberry Collection 1": ["unique", "world-limited"],
    "Shell Flowers":          ["unique", "world-limited"],
    "Rock Rollers":           ["unique", "world-limited"],
    "Extractor Blue":         ["unique", "world-limited"],
    "Shiny Bell Goomba":      ["unique", "world-limited"],
    "Skull King":             ["boss", "unique"],
    "Beastfly Hunt":          ["boss", "unique"],
    "Broodmother Hunt":       ["boss", "unique"]
    // ... see baseline file for full taxonomy
  },

  // Named, switchable preset bundles. Active preset is chosen via config.
  "presets": {
    "farmable-only": {
      "description": "Halve farmable quest requirements; leave unique items alone.",
      "rules": [
        {
          "match":  { "anyTag": ["farmable"] },
          "scale":  0.5,         // multiplier on OriginalCount
          "round":  "ceil",      // ceil | floor | nearest
          "min":    1            // floor (clamped to maxCap if any)
        }
      ]
    },
    "quick": {
      "description": "Set every count to 1 unless explicitly excluded.",
      "rules": [
        { "match": { "not": { "anyTag": ["world-limited"] } }, "set": 1 }
      ]
    },
    "vanilla": { "description": "No changes — falls through to slider values.", "rules": [] }
  },

  // Per-quest overrides applied AFTER the preset.
  // null/missing = leave alone.
  "perQuest": {
    "Brolly Get": {
      "targets": { "0": { "count": 10 } },        // direct count by index
      "scale":   null,
      "extraConditions": [
        // ALL must pass for turn-in (AND).
        { "kind": "playerData", "field": "silkSkillsAcquired", "op": ">=", "value": 5 }
      ]
    }
  }
}
```

### Match expression

```jsonc
"match": {
  "questId":  ["Brolly Get", "..."],   // exact ids
  "category": ["Gather", "Hunt"],      // QuestCapabilities categories
  "anyTag":   ["farmable"],            // OR over tags
  "allTag":   ["farmable", "garb"],    // AND over tags
  "not":      { "<match-expr>" }       // negation
}
```

Empty / missing match ⇒ matches everything.

### Rule operations (applied in this order per matched quest target)

1. `set` — replace count with literal int.
2. `scale` — multiply `OriginalCount` (not current!) by float, then `round`.
3. `min` — clamp result up.
4. `max` — clamp result down (unless `DevRemoveLimits`).

### Extra conditions (`extraConditions`) -- gates `CompleteQuest`

Each condition is one of:

```jsonc
{ "kind": "playerData",  "field": "<bool/int/float field>", "op": "==|!=|>=|>|<=|<", "value": <literal> }
{ "kind": "questCompleted", "quest": "<questId>" }
{ "kind": "tagAccepted",    "anyTag": ["..."], "count": 5 }   // "5 quests with tag X must be accepted"
```

Evaluated by `QuestAcceptance.EvaluateExtraConditions(questName)` before
`CompleteQuest` succeeds. If any condition fails, completion is refused with a
`LogDebugInfo` reason; the GUI surfaces a tooltip on the quest row.

### Available conditions (`availableConditions`) -- gates `IsAvailable` (cluster R)

Identical schema to `extraConditions` but evaluated by
`QuestAcceptance.EvaluateAvailableConditions(questName)` from the
`IsAvailable` getter postfix and the mass-accept paths
(`AcceptAllQuests`, `InjectAndAcceptAllQuests`, `AutoAcceptFlaggedQuests`)
under cluster S.

```jsonc
"perQuest": {
  "Beastfly Hunt": {
    "availableConditions": [
      { "kind": "playerData", "field": "defeatedSongGolem", "op": "==", "value": true }
    ]
  },
  "Tormented Trobbio": {
    "availableConditions": [
      { "kind": "playerData", "field": "defeatedTrobbio", "op": "==", "value": true }
    ]
  },
  "Mr Mushroom": {
    "availableConditions": [
      { "kind": "playerData", "field": "MushroomQuestFound1", "op": "==", "value": true }
    ]
  }
}
```

Behaviour by mode:

* `Disabled` -- `IsAvailable` postfix doesn't fire; vanilla rules apply.
* `Pure` -- ignored; raw chaos as designed.
* `Adjusted` -- enforced. Failing conditions block availability and are
  logged via `LogDebugInfo`.

Reflection-based PlayerData reads use `QuestDataAccess` helpers (no FSM
patching, read-only on PlayerData). Whitelist of safe fields lives in the
baseline JSON under `playerDataWhitelist` (next section).

## Merge order (highest wins)

1. Game default `QuestTarget.Count` (from `QuestCompletionOverrides.originalCounts`).
2. Slider/per-target overrides from `QuestModSaveData.QuestTargetOverrides`.
3. **Preset rules** (when `ActivePreset != "vanilla"`).
4. **Per-quest overrides** from `perQuest`.

Per-save slider edits *win* over the preset by design — once the user pokes the
slider for a quest, that target is treated as "user-overridden" and the preset
no longer touches it. The flag lives in `QuestModSaveData.SliderTouched` (new).

## Tag taxonomy (proposed defaults)

| Tag             | Definition                                                       | Examples                                                  |
| --------------- | ---------------------------------------------------------------- | --------------------------------------------------------- |
| `farmable`      | Drops from respawning enemies / regenerating sources             | Brolly Get, Pilgrim Rags, Crow Feathers, Fine Pins        |
| `unique`        | Each item exists in fixed world locations; not respawning        | Mossberries, Shell Flowers, Plasmium                      |
| `world-limited` | World count is the upper bound — overrides past cap are unsafe   | Mossberry Collection 1 (3), Shell Flowers (6), Journal    |
| `boss`          | Single boss kill                                                 | Skull King, Beastfly Hunt, Broodmother Hunt               |
| `garb`          | Wearable garbs/cloaks (sub-tag of farmable)                      | Brolly Get, Pilgrim Rags, Song Pilgrim Cloaks             |
| `pin`           | Pin items                                                        | Fine Pins, A Pinsmiths Tools                              |
| `donate`        | Shard/material donation quests                                   | Building Materials family, Songclave Donation             |
| `delivery`      | Courier deliveries (currently `excluded`)                        | Courier Delivery *                                        |
| `toggle`        | `type: toggle` quests (sub-targets are checklists, not counts)   | Grand Gate Bellshrines, Soul Snare, Flea Games            |
| `act_only`      | `type: accept_only` — no countable target                        | Citadel Seeker, Wood Witch Curse                          |

`farmable` vs `unique` is the load-bearing distinction for the requested
"Farmable-only" preset. The `farmableExclude` array already in
`QuestCapabilities.json` is the authoritative source for "do not treat as
farmable" — it maps cleanly to `unique + world-limited`.

## Migration of existing per-quest overrides

* `QuestModSaveData.QuestTargetOverrides` (slider state) is **kept verbatim**.
* On save load:
  1. `QuestCompletionOverrides.ApplySavedOverrides()` runs first (existing).
  2. `QuestRequirements.ApplyActivePreset()` runs second, but **skips any
     `(questName, targetIndex)` already in `QuestTargetOverrides`**.
  3. `QuestRequirements.ApplyPerQuest()` runs third (always wins for
     explicitly-listed quests).
* Result: existing users see no behavior change until they pick a preset.

## Open design questions

1. **Preset persistence scope**: BepInEx config (global, all saves) or per-save
   (`QuestModSaveData`)? Current impl: global config — simpler, matches other
   QoL toggles. Per-save would need a save-data version bump.
2. **Extra-condition UX**: when a turn-in is gated and refused, do we (a)
   silently log, (b) show a temporary toast, or (c) refuse to enter the
   completed state and surface the reason in the GUI? Current impl: (a) + (c)
   in GUI tooltip, no in-game toast.
3. **Slider vs preset precedence**: should the "Default" preset button in the
   Targets tab also clear `SliderTouched` so the preset re-applies? Currently:
   yes (Reset clears the flag).
4. **Tag editor**: should the Tools tab grow a tag-editor sub-panel, or do tags
   stay JSON-only? Recommend JSON-only for v1 — full editor lands with the
   per-board editing item.
5. **`questCompleted`/`tagAccepted` conditions** — these can create cycles
   ("complete A only after B; B requires A"). Detect at load time and
   warn-and-disable, or trust the user?

## Stub: per-board editing

The "Custom wish board population and per-board editing" TODO is *not*
addressed here. The schema is forward-compatible — a future `boards` block
mapping `boardId -> [questId]` slots in cleanly. When implemented, the
GUI will get a "Boards" tab beside Targets.

## Reference: `playerDataWhitelist` (current entries)

The condition kinds `playerData` (in both `extraConditions` and
`availableConditions`) can only reference PlayerData field names that
appear in `playerDataWhitelist`. This is a **security boundary**: it stops
a malicious or buggy user file from probing arbitrary save state. Static
verification (`.agent/scripts/static-verify.ps1`) checks every entry
exists on the live `PlayerData` type at build time.

The shipped baseline groups entries by purpose (the JSON has them in
this order; comments are not legal JSON keys so the JSON itself is just
a flat array):

**Generic state**
```
silkRegenMax        blackThreadWorld
```

**Boss defeat** (cluster R arena reuse gating; pattern: `defeated*`)
```
defeatedSongGolem               (Fourth Chorus, internal name Song Golem)
defeatedTrobbio                 (regular Trobbio fight)
defeatedTormentedTrobbio        (post-fight variant)
defeatedBellBeast               (Bellbeast Rescue target)
defeatedBroodMother             (Broodmother Hunt target)
defeatedDockForemen             (Forebrothers Signis & Gron, internal name DockForemen)
defeatedLastJudge               (Last Judge)
defeatedSplinterQueen           (Sister Splinter)
defeatedAntQueen                (pre-Red Memory variant)
defeatedAntQueenAfterRedMemory  (post-Red Memory variant)
defeatedCoralKing
defeatedFlowerQueen
defeatedMossMother
defeatedFirstWeaver
defeatedLace1                   (Lace stage 1)
defeatedLaceTower               (Lace final)
defeatedSeth
defeatedCrowCourt
```

**Boss encounter** (gate on first sight; pattern: `encountered*`)
```
encounteredSongGolem        encounteredBellBeast       encounteredDockForemen
encounteredLastJudge        encounteredSplinterQueen   encounteredTrobbio
encounteredTormentedTrobbio encounteredLace1           encounteredLaceTower
encounteredLaceBlastedBridge
```

**NPC introduction** (gate on first meet; pattern: `met*`)
```
metMapper             metAntQueenNPC       metGarmondAct3
metGrindleAct3        metLearnedPilgrimAct3 metGrubFarmerAct3
```

**Area visit** (pattern: `visited*`)
```
visitedFleatopia    visitedSlab
```

**Items, abilities, milestones**
```
hasNeedolin    hasDoubleJump    hasSuperJump
bonebottomQuestBoardFixed    pinstressQuestReady
MushroomQuestFound1          (sequential gate for Mr Mushroom)
```

### Adding a new whitelist entry

To gate on a PlayerData field not in the list:

1. Confirm the field exists on `PlayerData` (live game data). The
   simplest check is to add it to `playerDataWhitelist` and run
   `.agent/scripts/static-verify.ps1` -- it'll FAIL the build if the
   name is wrong, or PASS if the field is real.
2. Append the field name to the `playerDataWhitelist` array in either
   the embedded baseline (mod-shipped) or the user overlay
   (`BepInEx/config/QuestMod/QuestRequirements.user.json`). User
   overlay wins on conflict but ADDS to the whitelist (does not replace
   the baseline list).
3. Reference it from a `playerData` condition in `extraConditions` or
   `availableConditions`.

The user overlay path is the right place for personal additions; only
edit the embedded baseline if the change should ship with the mod.

### How the merge works

On load (`QuestRequirements.Load`):
1. Read the embedded baseline JSON (mod resource).
2. Read the optional user overlay file at
   `BepInEx/config/QuestMod/QuestRequirements.user.json`.
3. Deep-merge the user overlay over the baseline. For arrays
   (`playerDataWhitelist`, `tags`), entries are unioned. For objects
   (`presets`, `perQuest`), user keys replace baseline keys at the same
   path.

Result: a baseline-extended-by-user config. To completely replace a
preset, give it the same name in the user file. To add a new preset,
give it a new name. The mod re-loads on first save reload after the
file changes.
