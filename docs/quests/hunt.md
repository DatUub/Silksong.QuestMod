# Hunt

Hunt quests involve killing enemies and collecting drops.

---

### Pilgrim Rags
| Field | Value |
|---|---|
| **Display Name** | Garb of the Pilgrims |
| **Quest Type** | Hunt |
| **Act** | 1 |
| **Chain** | — (standalone) |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Pilgrim Rag | CollectableItemBasic | 12 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

---

### Brolly Get
| Field | Value |
|---|---|
| **Display Name** | Flexile Spines |
| **Quest Type** | Hunt |
| **Act** | 1 |
| **Chain** | — (standalone) |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Common Spine | CollectableItemBasic | 25 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

---

### Rock Rollers
| Field | Value |
|---|---|
| **Display Name** | Volatile Flintbeetles |
| **Quest Type** | Hunt |
| **Act** | 1 |
| **Chain** | — (standalone) |
| **Prerequisites** | pdTest: `visitedGreymoor` OR `visitedShellwood` |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Rock Roller | CollectableItemBasic | 3 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

Only 3 Rock Rollers exist in the world (fixed encounters, no respawn).

---

### Crow Feathers Pre
| Field | Value |
|---|---|
| **Display Name** | Crawbug Clearing |
| **Quest Type** | Hunt |
| **Act** | 1 |
| **Chain** | Crow Feathers (step 1 of 2) |
| **Next Step** | Crow Feathers |
| **Prerequisites** | pdTest: `spinnerDefeated` |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Crow Feathers
| Field | Value |
|---|---|
| **Display Name** | Crawbug Clearing |
| **Quest Type** | Hunt |
| **Act** | 1 |
| **Chain** | Crow Feathers (step 2 of 2) |
| **Previous Step** | Crow Feathers Pre |
| **Prerequisites** | Crow Feathers Pre + pdTest: `spinnerDefeated` |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Crow Feather | CollectableItemBasic | 25 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

Dropped by Craws (1×), Tallcraws (5×), and Squatcraws (4×). Enemies respawn.

---

### Roach Killing
| Field | Value |
|---|---|
| **Display Name** | Roach Guts |
| **Quest Type** | Hunt |
| **Act** | 1 |
| **Chain** | — (standalone) |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Roach Corpse | CollectableItemBasic | 10 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

Enemies respawn.

---

### Fine Pins
| Field | Value |
|---|---|
| **Display Name** | Fine Pins |
| **Quest Type** | Hunt |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Fine Pin | CollectableItemBasic | 12 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

Dropped by Choristors and Reeds in the Choral Chambers. Enemies respawn.

---

### Song Pilgrim Cloaks
| Field | Value |
|---|---|
| **Display Name** | Cloaks of the Choir |
| **Quest Type** | Hunt |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | reqComplete: `Fine Pins` + pdTest: `enclaveLevel > 1` |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Song Pilgrim Cloak | CollectableItemBasic | 16 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

Dropped by Choir Pouncers, Choir Hornheads, Choir Flyers, and Choir Elders. Enemies respawn.

---

### Huntress Quest
| Field | Value |
|---|---|
| **Display Name** | Broodfeast |
| **Quest Type** | Hunt |
| **Act** | 2 |
| **Chain** | — (standalone, mutually exclusive with Runtfeast) |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Seared | CollectableItemBasic | 15 |
| 1 | Shredded | CollectableItemBasic | 35 |
| 2 | Speared | CollectableItemBasic | 10 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets (3 targets)

Multi-target hunt with 3 organ types (60 total). Seared (fire/electric), Shredded
(jagged weapons), Skewered (stabbing). Enemies respawn.
If not completed before Act 3, Huntress is devoured and replaced by Runtfeast.

---

### Huntress Quest Runt
| Field | Value |
|---|---|
| **Display Name** | Runtfeast |
| **Quest Type** | Hunt |
| **Act** | 3 |
| **Chain** | — (standalone, mutually exclusive with Broodfeast) |
| **Prerequisites** | cancelIfInc: `Huntress Quest` |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Seared | CollectableItemBasic | 15 |
| 1 | Shredded | CollectableItemBasic | 35 |
| 2 | Speared | CollectableItemBasic | 10 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets (3 targets)

Identical targets to Broodfeast. Given by Runt if Broodfeast was not completed
before Act 3. Progress from Broodfeast carries forward.

---

### Destroy Thread Cores
| Field | Value |
|---|---|
| **Display Name** | Dark Hearts |
| **Quest Type** | Hunt |
| **Act** | 3 |
| **Chain** | — (standalone) |
| **Prerequisites** | reqComplete: `Black Thread Pt1 Shamans` |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Black Thread Core | CollectableItemBasic | 12 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

Destroy 12 of 47 Void Masses scattered across Pharloom. Fixed world objects, do not
respawn. Void Masses destroyed before accepting the quest still count.
