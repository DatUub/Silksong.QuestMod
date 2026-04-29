# All Wishes Mode — Pure vs Adjusted

The "All Wishes" toggle lifts the game's normal wish-availability gating so the
player can attempt quests out of order. There are two flavours of this with
different trade-offs.

## Modes

```
AllWishesMode {
    Disabled,   // vanilla behaviour
    Pure,       // raw bypass, no safety nets
    Adjusted,   // smart bypass that preserves playability
}
```

### Pure All Wishes Mode

Every quest is technically *available* — no prerequisite checks at all,
including chain gating. The player can attempt anything from the moment the
mod is enabled. This is the chaotic / completionist option.

Trade-offs the player accepts:

- **Soft-locks are possible.** Quests that require items not yet collected
  (e.g. Silk Defeat Snare needs the Soul Snare unlocked) will appear as
  available but be uncompletable until the underlying capability exists.
- **Chain order is *not* enforced.** Mid-chain quests can be accepted before
  earlier ones, which can leave the chain stuck or duplicate progress flags.
- **Edge cases are not handled.** Shared-arena quests (SB2/4C),
  Pinstress/Needle Strike, and double Trobbio can collide.

### Adjusted All Wishes Mode

Smart bypasses that preserve playability. The default for new saves and the
recommended setting.

Behaviours:

- **Chain gating is respected.** A chain step is only available once every
  earlier step in the chain is completed (existing behaviour, see
  `QuestAcceptance.IsChainPrereqMet`). Silk Defeat Snare stays gated behind
  Soul Snare completion.
- **NPC spawn flags** for board NPCs (Mapper, Tipp, Sherma, Fixer, Caretaker,
  etc.) are auto-set so the wishboards are populated regardless of progression
  flags.
- **Edge cases** *(owned by Cluster M, not implemented here — listed for
  spec completeness)*:
  - SB2/4C shared-arena: only one of the conflicting wishes is exposed
    until the other is accepted/completed (handled via
    `QuestRegistry.MutuallyExclusiveQuests`).
  - Pinstress / Needle Strike: gated together so the player cannot orphan
    one half of the pair.
  - Double Trobbio: deduplicated so the second instance does not appear on
    the board until the first is resolved.
- **Granular prereqs** *(owned by Cluster E, not implemented here — listed
  for spec completeness)*: Faydown Cloak, Fleatopia unlock, mandatory wishes,
  etc. will plug into Adjusted as additional gates.

## Migration from `AllQuestsAvailable: bool`

The legacy save field `AllQuestsAvailable` (bool) is preserved for one
release for back-compat. On load:

| Legacy `AllQuestsAvailable` | New `AllWishesMode` |
|-----------------------------|---------------------|
| `false`                     | `Disabled`          |
| `true`                      | `Adjusted`          |

`Adjusted` is the safer default — it matches the *current* behaviour of
`AllQuestsAvailable=true` (chain gating already respected via
`IsChainPrereqMet`), so migrated saves retain identical semantics.

When writing save data we set both `AllQuestsAvailable` (derived: `Mode !=
Disabled`) and `AllWishesMode`. Older builds that don't know about the enum
field still see the bool and behave correctly.

## Coordination

- **Cluster M (edge cases):** owns SB2/4C, Pinstress/Needle Strike, double
  Trobbio. Their handlers should branch on `AllWishesMode == Adjusted` so
  Pure stays "raw".
- **Cluster E (granular prereqs):** any new prereq toggle (Fleatopia,
  Faydown Cloak, mandatory wishes) is an Adjusted-only gate. In Pure these
  are all ignored.

## GUI

The Tools tab exposes a 3-way radio (Disabled / Pure / Adjusted) instead of
the previous "All Quests Available" checkbox. "All Quests Accepted" still
implies `Adjusted` when toggled on.
