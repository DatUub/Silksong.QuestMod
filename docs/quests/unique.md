# Unique

Miscellaneous quests with unique mechanics.

---

### Journal
| Field | Value |
|---|---|
| **Display Name** | Bugs of Pharloom |
| **Quest Type** | Unique |
| **Act** | 1 |
| **Chain** | — (standalone) |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Journal Seen | CollectableItemBasic | 100 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

230 journal entries exist in the game.

---

### Sprintmaster Pre
| Field | Value |
|---|---|
| **Display Name** | Fastest in Pharloom |
| **Quest Type** | Unique |
| **Act** | 3 |
| **Chain** | Sprintmaster (step 1 of 2) |
| **Next Step** | Sprintmaster Race |
| **Prerequisites** | pdTest: `hasSuperJump` |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Sprintmaster Race
| Field | Value |
|---|---|
| **Display Name** | Fastest in Pharloom |
| **Quest Type** | Unique |
| **Act** | 3 |
| **Chain** | Sprintmaster (step 2 of 2) |
| **Previous Step** | Sprintmaster Pre |
| **Prerequisites** | Sprintmaster Pre |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Sprintmaster Race Target | CollectableItemBasic | 3 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

Exactly 3 races exist.

---

### Mr Mushroom
| Field | Value |
|---|---|
| **Display Name** | Passing of the Age |
| **Quest Type** | Unique |
| **Act** | 3 |
| **Chain** | — (standalone) |
| **Prerequisites** | None |

| Target | Item Name |
|---|---|
| 0 | Chapel (Monarch) |
| 1 | Camp (Black Thread) |
| 2 | Scorched Field |
| 3 | Towers (Surgeon) |
| 4 | Cage (Silken Lie) |
| 5 | Heart of Frost |
| 6 | Cradle's Peak |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Toggle Sequential (7 targets)

Targets must be completed in order (`toggle_sequential` type). Each location is
an AltTest-based toggle.

---

### Steel Sentinel
| Field | Value |
|---|---|
| **Display Name** | A Vassal Lost |
| **Quest Type** | Unique |
| **Act** | 3 |
| **Chain** | Steel Sentinel (step 1 of 2) |
| **Next Step** | Steel Sentinel Pt2 |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Steel Sentinal Target | CollectableItemBasic | 3 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

Visit 3 randomly-chosen resting sites (from 6 locations), then fight Summoned Saviour.
Steel Soul Mode only — Steel Seer Zi does not appear in normal playthroughs.

---

### Steel Sentinel Pt2
| Field | Value |
|---|---|
| **Display Name** | A Vassal Lost |
| **Quest Type** | Unique |
| **Act** | 3 |
| **Chain** | Steel Sentinel (step 2 of 2) |
| **Previous Step** | Steel Sentinel |
| **Prerequisites** | Steel Sentinel |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

Defeat Summoned Saviour and report back.
