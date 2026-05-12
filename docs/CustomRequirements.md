# Custom Requirements

JSON rules engine for overriding quest counts + completion gates without touching mod source.

Two layers stacked on top of each other:

- presets that sweep many quests at once via tags
- per-quest overrides for fine-grained tweaks

## Files

Embedded baseline ships with the mod: `QuestMod.Data.QuestRequirements.json` (read-only). User overlay lives at `BepInEx/config/QuestMod/QuestRequirements.user.json` (created on first run, edit freely). The overlay merges over the baseline, arrays union, objects replace by key. Edits land on the next save reload.

## Schema (with examples)

```jsonc
{
  "version": 1,

  // questId -> tags. Used for preset matching.
  "tags": {
    "Brolly Get":     ["farmable", "garb"],
    "Pilgrim Rags":   ["farmable", "garb"],
    "Mossberry Collection 1": ["unique", "world-limited"],
    "Beastfly Hunt":  ["boss", "unique"]
  },

  // presets. ActivePreset (per save) picks one.
  "presets": {
    "farmable-only": {
      "description": "halve farmables, leave uniques alone",
      "rules": [
        { "match": { "anyTag": ["farmable"] }, "scale": 0.5, "round": "ceil", "min": 1 }
      ]
    },
    "quick": {
      "description": "set every count to 1 unless world-limited",
      "rules": [
        { "match": { "not": { "anyTag": ["world-limited"] } }, "set": 1 }
      ]
    },
    "vanilla": { "description": "no changes", "rules": [] }
  },

  // per-quest overrides applied after the preset.
  "perQuest": {
    "Brolly Get": {
      "targets": { "0": { "count": 10 } },
      "extraConditions": [
        { "kind": "playerData", "field": "silkSkillsAcquired", "op": ">=", "value": 5 }
      ]
    }
  }
}
```

## Match expression

```jsonc
"match": {
  "questId":  ["Brolly Get"],         // exact ids
  "category": ["Gather", "Hunt"],     // categories from QuestCapabilities
  "anyTag":   ["farmable"],           // OR
  "allTag":   ["farmable", "garb"],   // AND
  "not":      { "<match-expr>" }      // negation
}
```

Empty / missing match = matches everything.

## Rule operations (in order)

1. `set`, replace count with a literal int.
2. `scale`, multiply `OriginalCount` (not current!) by a float, then `round` (ceil/floor/nearest).
3. `min`, clamp result up.
4. `max`, clamp result down (unless `DevRemoveLimits` is on).

## extraConditions, gates Complete

Conditions on a quest. All must pass (AND) before the Complete button does anything. Failed condition = refusal message in the GUI.

```jsonc
{ "kind": "playerData",    "field": "<field>", "op": "==|!=|>=|>|<=|<", "value": <literal> }
{ "kind": "questCompleted", "quest": "<questId>" }
{ "kind": "tagAccepted",    "anyTag": ["..."], "count": 5 }
```

## availableConditions, gates IsAvailable

Same schema as extraConditions but gates wish visibility/availability under Adjusted mode. Used for boss arena-reuse gates and similar.

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
  }
}
```

Per mode: Disabled doesn't run the gate (vanilla rules). Pure ignores it (raw chaos). Adjusted enforces it.

## Merge order (highest wins)

1. Vanilla `QuestTarget.Count` baseline.
2. Per-target overrides from `QuestModSaveData.QuestTargetOverrides` (slider state).
3. Preset rules (when `ActivePreset != "vanilla"`).
4. Per-quest overrides from `perQuest`.

Slider edits win over presets. Once you touch the slider on a quest, that target is user-overridden and the preset stops touching it.

## Tag taxonomy

| Tag | Definition | Examples |
|---|---|---|
| `farmable` | drops from respawning enemies / regenerating sources | Brolly Get, Pilgrim Rags, Crow Feathers |
| `unique` | fixed world locations, no respawn | Mossberries, Shell Flowers, Plasmium |
| `world-limited` | hard upper bound on count, going past breaks things | Mossberry Collection 1, Shell Flowers |
| `boss` | single boss kill | Skull King, Beastfly Hunt, Broodmother Hunt |
| `garb` | wearable garbs/cloaks | Brolly Get, Pilgrim Rags, Song Pilgrim Cloaks |
| `pin` | pin items | Fine Pins, A Pinsmith's Tools |
| `donate` | shard/material donation quests | Building Materials, Songclave Donation |
| `delivery` | courier deliveries (excluded from mass-ops) | Courier Delivery * |
| `toggle` | sub-targets are checklists not counts | Bellshrines, Soul Snare, Flea Games |
| `act_only` | no countable target | Citadel Seeker, Wood Witch Curse |

`farmable` vs `unique` is the load-bearing distinction for the farmable presets.

## playerData whitelist

The `playerData` condition kind can only reference field names in the `playerDataWhitelist` array. This is a safety boundary, stops a malicious or buggy overlay from probing arbitrary save state.

Baseline groups (the actual JSON is one flat array, these groupings are just for orientation):

Generic state: `silkRegenMax`, `blackThreadWorld`

Bosses defeated: `defeatedSongGolem`, `defeatedTrobbio`, `defeatedTormentedTrobbio`, `defeatedBellBeast`, `defeatedBroodMother`, `defeatedDockForemen`, `defeatedLastJudge`, `defeatedSplinterQueen`, `defeatedAntQueen`, `defeatedAntQueenAfterRedMemory`, `defeatedCoralKing`, `defeatedFlowerQueen`, `defeatedMossMother`, `defeatedFirstWeaver`, `defeatedLace1`, `defeatedLaceTower`, `defeatedSeth`, `defeatedCrowCourt`

Bosses encountered: `encounteredSongGolem`, `encounteredBellBeast`, `encounteredDockForemen`, `encounteredLastJudge`, `encounteredSplinterQueen`, `encounteredTrobbio`, `encounteredTormentedTrobbio`, `encounteredLace1`, `encounteredLaceTower`, `encounteredLaceBlastedBridge`

NPCs met: `metMapper`, `metAntQueenNPC`, `metGarmondAct3`, `metGrindleAct3`, `metLearnedPilgrimAct3`, `metGrubFarmerAct3`

Areas visited: `visitedFleatopia`, `visitedSlab`

Abilities + milestones: `hasNeedolin`, `hasDoubleJump`, `hasSuperJump`, `bonebottomQuestBoardFixed`, `pinstressQuestReady`, `MushroomQuestFound1`

To gate on a field not in the list, append it to `playerDataWhitelist` in your user overlay (array union with baseline, you don't have to copy the whole list).
