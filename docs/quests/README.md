# Quest Modding Capabilities

Detailed reference for all 82 quests in Hollow Knight: Silksong, organized by wish type.
Documents internal engine data, targets, prerequisites, and what QuestMod can modify.

## Legend

| Capability | Description |
|---|---|
| ✅ Accept | Force-accept the quest via `IsAvailable` + FSM bypass |
| ✅ Complete | Mark quest as completed in save data |
| ✅ Targets | Override target counts to satisfy completion |
| ✅ Toggle | Toggle individual checklist items (AltTest-based) |
| ⚠️ Limited | Partially moddable, see notes |
| ❌ None | No target manipulation possible (accept/complete only) |

## Categories

| Category | Quests | File |
|---|---|---|
| [Main Story](main-story.md) | 21 | Chain quests + standalone progression |
| [Gather](gather.md) | 7 | Collect items from the world |
| [Hunt](hunt.md) | 11 | Kill enemies for drops |
| [Grand Hunt](grand-hunt.md) | 4 | Boss hunts |
| [Donate](donate.md) | 7 | Currency donations |
| [Delivery](delivery.md) | 7 | Courier escort missions |
| [Wayfarer](wayfarer.md) | 20 | NPC rescue, fetch, and encounter quests |
| [Unique](unique.md) | 5 | Journal, races, Mr Mushroom, Steel Sentinel |

**Total: 82 quests.** 36 have prerequisites, 46 have none.
