# Wish Location Reassignment

Stretch goal. Scaffold only, no behavior wired up yet. Save-data fields and a ConfigEntry exist; everything else is planning.

Two related features sharing storage:

- **NPC to Wishboard.** Quests normally accepted by talking to a specific NPC become acceptable from any (or a specific) wishboard.
- **Wishboard to World.** Quests normally posted on wishboards become acceptable via world triggers (scene enter, prop interact).

Both redirect the *acceptance source*. Completion still flows through the same FullQuestBase runtime data everything else uses.

## How vanilla routes available to accepted

`FullQuestBase` ScriptableObjects are the canonical quest registry. Each carries `IsAvailable` plus target lists. The mod already patches `IsAvailable` (getter postfix) for AllWishesMode.

Two source layers in vanilla:

- NPC dialog FSMs. PlayMaker FSMs that, on a successful branch, write to runtime data with `accepted=true`.
- Wishboards (`QuestBoardInteractable` MonoBehaviour). Reads an offered-quest list at refresh time, presents the entries, player picks, board writes runtime data.

Both funnel into `QuestDataAccess.GetRuntimeData()`, the live IDictionary keyed by internal name. We can also write directly via `AcceptQuest(name)`. Reassigning is about hooking new sources to that funnel.

## NPC to Wishboard

Plan: Harmony postfix on `QuestBoardInteractable.RefreshQuestBoard`. Read our `WishLocationOverrides` map and append the redirected NPC quests. Single patch site, additive to a list that already rebuilds every scene load, no FSM editing.

Cloning the FullQuestBase under a new internal name would double runtime data entries and break chain prereqs (which key on internal name).

FSM-redirecting the NPC's dialog requires per-quest FSM rewriting, doesn't actually solve "from any wishboard" because the NPC still has to be reachable.

## Wishboard to World

Plan for v1: scene-enter hook. On `SceneManager.sceneLoaded`, if a quest is mapped to the loaded scene name in `WishLocationOverrides`, auto-accept with a one-shot save-data gate (`WishLocationTriggersFired`). Reuses the OnSceneLoaded hook, zero new GameObjects, can't softlock because acceptance is non-destructive.

Coarser than position-specific. Good enough.

Collider injection (spawning a `BoxCollider2D` at a configured position) works in theory but means injecting GameObjects at scene load, per-scene position config, Y-sort and parenting concerns. The prior Bonebottom wishboard reparenting work caused softlocks. Skip unless someone actually asks.

## Save data

```csharp
public Dictionary<string, string> WishLocationOverrides { get; set; } = new();
public HashSet<string> WishLocationTriggersFired { get; set; } = new();
```

Value semantics for `WishLocationOverrides`:

- `"questName" -> "wishboard:any"`, any wishboard
- `"questName" -> "wishboard:Bonetown"`, specific board
- `"questName" -> "scene:Crawl_01"`, scene enter
- `"questName" -> "trigger:<id>"`, reserved for collider trigger if it ever ships

Strings keep the JSON forward-compatible; a future enum or record can replace the scheme without a save migration.

## Compatibility notes

When AllWishesMode is on, every quest is already available everywhere, so reassignment is a no-op. Short-circuit if `WishesMode != Disabled` and disable the GUI controls.

Per-quest auto-accept wins over location triggers because it fires earlier in the scene-load cycle. Document precedence: auto-accept > location trigger > manual.

Reuse `QuestAcceptance.AcceptQuest` for the actual call so mutually-exclusive gating and `IsQuestDiscovered` are honored.

Chain quests are the one footgun: relocating step 1 to a scene the prereqs lock out creates a dead end. Validate at config time and warn.
