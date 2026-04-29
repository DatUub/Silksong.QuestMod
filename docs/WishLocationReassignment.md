# Wish Location Reassignment — Design Doc

**Status:** Stretch goal (TODO §7). Scaffold-only; no behavior wired up.
**Cluster:** J. Owners: future implementer.
**Scope:** Two related features sharing one storage/feature-flag substrate.

  1. **NPC → Wishboard** — quests normally accepted by talking to a specific NPC become acceptable from any (or any whitelisted) wishboard.
  2. **Wishboard → World** — quests normally posted on wishboards become acceptable via arbitrary world triggers (scene enter, custom prop interact, NPC pop-up, etc.).

Both features redirect the *quest acceptance source* without altering completion logic. Completion still flows through the same `FullQuestBase` runtime data the rest of QuestMod manipulates.

---

## 1. Domain Model — How Silksong Routes "Available" → "Accepted" Today

QuestMod's existing code (`Core/QuestAcceptance.cs`, `Patches/QuestStateHooks.cs`, `Data/QuestDataAccess.cs`) reveals the three-layer model:

1. **Quest definition layer.** `FullQuestBase` ScriptableObjects are the canonical quest registry. Each quest carries `IsAvailable` (gating Boolean) and target lists. The mod patches `FullQuestBase.IsAvailable` (getter Postfix) to override availability globally.
2. **Source/offer layer.** Two concrete sources exist in vanilla:
   - **NPC dialog FSMs** — PlayMaker FSMs on NPC GameObjects that, on a successful dialog branch, call into runtime data to add a quest entry with `accepted=true`. The whitelist in `QuestStateHooks.WhitelistedObjects` (`Courier`, `Tipp`, `Mapper`, `Leader`, `Sherma`, `Fixer`, `Caretaker`, etc.) captures the known NPCs.
   - **Wishboards** — `QuestBoardInteractable` MonoBehaviour. The board reads an offered-quest list at refresh time (`RefreshQuestBoard()` is invoked after scene load) and presents the entries; the player selects, and the board calls into runtime data the same way.
3. **Runtime data layer.** `QuestDataAccess.GetRuntimeData()` exposes the live `IDictionary` keyed by quest internal name with `seen/accepted/completed/wasEver` fields. Both sources funnel here. QuestMod writes here directly via `AcceptQuest(name)`.

The acceptance "source" in vanilla is therefore **the FSM action / `QuestBoardInteractable` callback that writes to runtime data**. The actual writing is a single line — gating it is what the source layer does.

### FSM names to inspect
We could not inspect Silksong's decompiled assemblies in this worktree (they live on Apollo at `C:\Program Files (x86)\Steam\...\Managed\`). What an implementer should grep for in `Assembly-CSharp.dll` via dnSpy/ILSpy:

- `QuestBoardInteractable` — methods `RefreshQuestBoard`, `OnInteract`, and any field named `quests`, `availableQuests`, `boardQuests`, or `offeredQuests` (the offer pool).
- `AcceptQuest` / `OfferQuest` PlayMaker actions in `QuestPlaymakerActions` namespace (sibling of `CheckQuestStateV2` already used here).
- The NPC dialog FSM convention: states named `Offer`, `Accept`, `Got Quest`, `Quest Accepted`. These map 1:1 with the existing whitelisted NPCs.
- Any `QuestPickup` / `QuestProp` MonoBehaviour for environmental triggers (e.g. shrines, journal entries) — we know these exist for Mr Mushroom and Steel Sentinel "Unique" quests.

Until that inspection happens, the implementation candidates below are written to be agnostic: each one identifies which fields it depends on.

---

## 2. Implementation Candidates

### Feature A: NPC → Wishboard

| Option | Mechanism | Pros | Cons | Risk |
|---|---|---|---|---|
| **A1. Patch the wishboard offer pool** (recommended) | Harmony Postfix on `QuestBoardInteractable.RefreshQuestBoard` (or whichever method materializes the offer list). Append the redirected NPC quests by reading our `WishLocationOverrides` map. | Single patch site. Reuses existing `RefreshQuestBoard()` invocations already wired in `QuestStateHooks.OnSceneLoaded`. No FSM editing — stays inside the whitelist. | Requires confirmed field name (e.g. `quests` array). May need to dedupe per board (don't show the same redirected quest on every board unless user opts in). | **Low** — read-only inspection of vanilla pool followed by additive patch. |
| **A2. Duplicate the quest definition into the wishboard pool** | Clone the `FullQuestBase` ScriptableObject under a new internal name and inject into the board's pool. Original NPC source untouched. | Cleanly preserves vanilla NPC path (player can still accept from NPC). | Doubles entries in runtime data. Breaks chain prereq logic (`IsChainPrereqMet` keys on internal name). Confuses `QuestRegistry.json`. | **High** — bifurcates the quest identity; conflicts with Cluster D auto-accept and Cluster H All Wishes. Reject. |
| **A3. FSM redirect on NPC dialog** | Patch the NPC's dialog FSM to fire its "Accept" event when the wishboard fires *its* event (cross-FSM signal). | Stays purely in the FSM whitelist patch system already used. | Requires dynamic per-quest FSM rewriting, complicates the patch. NPC must still be reachable (story-gated NPCs can't be triggered from a board pre-spawn). | **Medium-high** — fragile and does not actually solve "from any wishboard." Reject. |

**Recommendation: A1.** Mirrors how `QuestAvailabilityPatch` already overrides one method on the canonical type. The implementer's first task is to dnSpy `QuestBoardInteractable` and confirm the pool field.

### Feature B: Wishboard → World

| Option | Mechanism | Pros | Cons | Risk |
|---|---|---|---|---|
| **B1. Attach to existing scene-enter events** (recommended for v1) | On `SceneManager.sceneLoaded`, if a quest is mapped to the loaded scene name in `WishLocationOverrides`, auto-accept it (with a one-shot "have I already triggered this?" gate stored in save data). | Zero new GameObjects. Zero collider math. Reuses the hook already in `QuestStateHooks.OnSceneLoaded`. Easy to feature-flag. | Coarse: scene-entry, not position-specific. May fire too early (before player loads). | **Low** — pure save-data gate + accept call. Cannot softlock because acceptance is non-destructive. |
| **B2. Inject a Unity collider trigger zone** | Spawn a `BoxCollider2D` GameObject at a configured world position; on `OnTriggerEnter2D` with the player layer, call `AcceptQuest(...)`. | Position-specific, immersive. | Requires collider injection at scene load (`Instantiate` lifecycle), per-scene position config (where do these positions come from?), Y-sort/parenting concerns. The previous `bonebottomQuestBoardFixed` reparenting work caused softlocks/duplicate NPCs — reparenting in Silksong scenes is fraught. | **High.** Defer to v2 once B1 is shipped. |
| **B3. Menu/GUI command** | Add a "Locations" tab in the GUI that lists redirected quests and accepts on button click. | Trivial implementation; ships day one. | Breaks the immersive intent of "accept from world locations" — reduces to a manual button. Already overlaps with the existing Quests tab. | **Trivial / not aligned with feature intent.** Use only as a debug fallback. |

**Recommendation: B1.** It's the only candidate that delivers immersive worldward acceptance without GameObject lifecycle hazards. B2 stays on the roadmap for after B1 proves the save-data + config path works.

---

## 3. Save-Data Implications

When a player accepts a relocated quest mid-playthrough, three pieces of state must survive a reload:

1. **The mapping itself** — `Dictionary<string, WishLocation>` from quest internal name to the override location. Must persist via `ISaveDataMod` because override choices are per-save (a player may use Cluster J on one save and not another).
2. **Trigger-fired set** — `HashSet<string>` of quests whose world trigger has already fired. Without this, a B1 scene-enter would re-fire on every reload of that scene. Mirrors the existing `InjectedQuests`/`CompletedQuests` HashSets.
3. **Pool-injected set** (A1 only) — not strictly needed at runtime since `RefreshQuestBoard` re-runs every scene load, but useful for diagnostics and undo.

Vanilla `runtimeQuests` already persists the `accepted=true` flag once we write it, so we do **not** need to re-accept on reload. We only need to suppress re-firing of triggers and re-injecting into pools that vanilla state has already advanced past.

### Field stub (added to `Data/QuestModSaveData.cs`)
```csharp
// Cluster J scaffold — see docs/WishLocationReassignment.md
public Dictionary<string, string> WishLocationOverrides { get; set; } = new();
public HashSet<string> WishLocationTriggersFired { get; set; } = new();
```
Value semantics for `WishLocationOverrides`:
- `"questName" -> "wishboard:any"` — accept from any wishboard (Feature A)
- `"questName" -> "wishboard:Bonetown"` — accept from a specific board
- `"questName" -> "scene:Crawl_01"` — accept on scene enter (Feature B1)
- `"questName" -> "trigger:<id>"` — reserved for B2 collider trigger IDs

A future enum/discriminated record can replace the string scheme; strings here keep the JSON forward-compatible while no enforcement code exists.

---

## 4. Compatibility

| Feature | Interaction | Resolution |
|---|---|---|
| **Cluster H — All Wishes Mode** | When AWM is on, every quest is already available everywhere. Wish reassignment is a no-op while AWM is on. | Short-circuit Cluster J's hooks if `AllQuestsAvailable` is true. Document the override visually in the GUI ("disabled while All Wishes Mode is on"). |
| **Cluster D — Per-quest auto-accept** | If a quest is set to auto-accept *and* has a wish location override, auto-accept wins (it triggers earlier in the scene-load cycle). | No code conflict; document precedence: auto-accept > location trigger > manual. |
| **Chain quests** | Chain steps depend on prior steps being completed (`IsChainPrereqMet`). Relocating step N of a chain to a wishboard is fine, but relocating step 1 to a scene that's only reachable post-step-3 creates a dead end. | Validate at GUI configuration time: warn if `WishLocationOverrides[questName]` references a scene that the quest's prereqs lock the player out of. The validation table can come from a future `QuestSceneRequirements.json`. |
| **Mutually exclusive quests** | `MutuallyExclusiveQuests` already gates acceptance — call `GetExclusionConflict` before any redirect-driven `AcceptQuest`. | Already covered by reusing `QuestAcceptance.AcceptQuest`. |
| **`OnlyDiscoveredQuests`** | A relocated quest the player has never seen would skip the existing discovery gate. | `IsQuestDiscovered` should be honored before any redirect-driven acceptance, identical to `IsAvailable` patch behavior. |

---

## 5. Recommendation — What to Build First

Build **Feature A1 (wishboard offer-pool patch)** first, then **B1 (scene-enter triggers)**, then revisit B2 only if user feedback demands position-specific triggers.

Reasoning:
- A1 has the smallest surface (one Harmony postfix on `QuestBoardInteractable`) and the lowest blast radius (additive to a list that is already rebuilt every scene).
- A1 validates the save-data + feature-flag substrate end-to-end without any new Unity object lifecycle concerns.
- B1 reuses the same substrate (the `WishLocationOverrides` dictionary) plus the already-existing `OnSceneLoaded` hook — it's effectively free once A1 ships.
- B2 (collider injection) is the only candidate that the prior `bonebottomQuestBoardFixed` reparenting attempt is a cautionary tale for. Defer it.

### Phase plan
| Phase | Deliverable |
|---|---|
| 0 (this doc) | Save-data field stub, feature-flag config (default OFF), this design doc. |
| 1 | dnSpy inspection of `QuestBoardInteractable`. Confirm pool field name. Document in this file. |
| 2 | A1: Harmony patch on offer pool. GUI tab to add/remove `WishLocationOverrides` entries (NPC → any/specific board). |
| 3 | B1: Scene-enter trigger acceptance. Reuse `OnSceneLoaded`. Persist `WishLocationTriggersFired`. |
| 4 (optional) | B2: collider injection. Requires per-scene position config schema. |

---

## 6. Open Questions for the User

1. **Per-board specificity** — Does "accept NPC wishes from wishboards" mean *all* boards globally, or should the player choose a specific board per quest? (A1 supports both via the `wishboard:any` vs `wishboard:<name>` value scheme; the GUI affordance differs.)
2. **Inverse direction** — When a quest is reassigned, should the original source still work (dual sources), or be suppressed?
3. **Discovery semantics** — Does relocating a quest also "discover" it for `OnlyDiscoveredQuests` purposes, or must the player still visit the original NPC at least once? (Recommend: relocation implies discovery.)

---

## 7. What to Inspect on Apollo

This worktree did not have access to the decompiled Silksong assemblies. Before Phase 1, run on Apollo:

```
dnSpy "C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight Silksong\Hollow Knight Silksong_Data\Managed\Assembly-CSharp.dll"
```

Capture and paste here:
- `QuestBoardInteractable` full class — fields, methods, especially the offered-quest pool.
- The `OfferQuest` / `AcceptQuest` PlayMaker actions in `QuestPlaymakerActions`.
- Any `QuestPickup` / `QuestProp` / `QuestTriggerVolume` types — these may already exist and could replace B2 collider injection with a vanilla pattern.
