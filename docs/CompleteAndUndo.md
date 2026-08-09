# Complete and Undo

How the Quests-tab **Complete** button behaves, and how to reverse a mistake.

## Two Complete modes

Controlled by **Tools → Remote Complete (per-quest Complete)** (also `EnableFullRemoteComplete` per save / BepInEx config).

| Mode | Remote Complete | What Complete does |
|------|-----------------|-------------------|
| **Flag-only** (default) | OFF | Sets QuestData completed flags only. No item deduct, no reward grant, no minigame/world FSM progress. |
| **Remote Complete** | ON | Mirrors NPC turn-in when possible: deduct counters, grant rewards, cascade dependents, then flip flags. |

The Quests tab header shows **Flag-only Complete** or **Remote Complete ON**.

## When flag-only is refused

Some wishes need **in-world progress** (minigames, world state). Flag-only Complete would look “done” but leave the game unfinished.

Default list (internal names):

- `Flea Games` / `Flea Games Pre` — **Ecstasy of the End** (flea carnival)

On refuse, the GUI shows a toast pointing at in-world play and **Done → Undo Complete**.

Remote Complete is allowed to attempt turn-in for these, but **still may not drive minigame scores** — finish the carnival in the world when that is what you care about.

Forks can extend the list via `"worldStateComplete": [ ... ]` in the embedded capabilities JSON (merged into `QuestRegistry.WorldStateCompleteQuests`).

Mass **Complete Available** skips the same list while Remote Complete is off.

## Undo Complete

1. Open **Quests**.
2. Set filter to **Done** (or **All**).
3. Click the blue **Undo Complete** on the row.

This clears the completed flag (and QuestMod’s completed tracking). It does **not**:

- un-grant abilities or items  
- refund minigame / world state  
- reverse a full Remote Complete reward pipeline in every case  

If you flag-completed by mistake, Undo is the recovery path.

## Save safety

On legacy saves without the safety override, Complete / Remote Complete / mass ops that mutate QuestData are refused until you opt in under Tools (Save Safety).

## Related Tools toggles

- **Auto-Accept Available** — scene-load accept for naturally available wishes (not the same as Complete).
- **All Quests Accepted** — inject + accept everything each scene load (forces Adjusted).
- Mass **Accept Available** / **Complete Available** + **Undo Last Mass Operation**.
