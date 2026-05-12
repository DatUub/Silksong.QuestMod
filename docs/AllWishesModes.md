# All Wishes Mode

Three modes for the All Wishes toggle, set in the Tools tab.

**Disabled** is vanilla, nothing changes.

**Pure** opens every quest with no checks. Chain order isn't enforced, so you can accept step 3 before step 1 and brick the chain. Shared-arena wishes (Beastfly Hunt vs Fourth Chorus) can fight each other. Edge cases aren't handled, soft-locks possible. Hidden behind an Advanced flag because mostly here for completionists who know what they're signing up for.

**Adjusted** is the recommended one. Opens every wish but keeps:

- chain ordering (step 2 still needs step 1 done)
- mutually-exclusive twin gating (only one of the twin pair active at a time)
- the edge-case patches (Pinstress narrowing, double-Trobbio dedup)
- boss arena-reuse gates: Beastfly Hunt waits on `defeatedSongGolem`, Tormented Trobbio waits on `defeatedTrobbio`, Mr Mushroom (first encounter) waits on `MushroomQuestFound1`. Stops the rematch wish from appearing before you've done the original fight.
- Mr Mushroom per-stage encounter lock (stages you haven't met can't be ticked)

NPC spawn flags get auto-set on scene load so quest-givers actually show up (Mapper, Sherma, Shakra, Mr Mushroom first encounter, City Merchant locations).

Flipping AllQuestsAccepted on while in Disabled auto-promotes to Adjusted with a toast. They need each other to do anything useful.

Pre-2.0 saves stored `AllQuestsAvailable: bool`. On load it migrates: `false` becomes Disabled, `true` becomes Adjusted. Matches the old `true` behaviour (chain gating was already respected) so migrated saves keep identical semantics.
