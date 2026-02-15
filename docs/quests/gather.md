# Gather

Gather quests involve exploring and finding specific items to turn in. These quests have
count-based targets — the mod can override target counts.

---

### Shiny Bell Goomba
| Field | Value |
|---|---|
| **Display Name** | Silver Bells |
| **Quest Type** | Gather |
| **Act** | 1 |
| **Chain** | — (standalone) |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Silver Bellclapper | CollectableItemBasic | 8 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

The Silver Bell RNG patch can guarantee silver bell drops from bell enemies.

---

### Mossberry Collection Pre
| Field | Value |
|---|---|
| **Display Name** | Berry Picking |
| **Quest Type** | Gather |
| **Act** | 1 |
| **Chain** | Mossberry (step 1 of 2) |
| **Next Step** | Mossberry Collection 1 |
| **Prerequisites** | None |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

Pre-quest accept step for Berry Picking.

---

### Mossberry Collection 1
| Field | Value |
|---|---|
| **Display Name** | Berry Picking |
| **Quest Type** | Gather |
| **Act** | 1 |
| **Chain** | Mossberry (step 2 of 2) |
| **Previous Step** | Mossberry Collection Pre |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Mossberry | CollectableItemBasic | 3 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

---

### Shell Flowers
| Field | Value |
|---|---|
| **Display Name** | Rite of the Pollip |
| **Quest Type** | Gather |
| **Act** | 1 |
| **Chain** | None |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Shell Flower | CollectableItemBasic | 6 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

6 fixed world pickups in Shellwood/Greyroot.

---

### Extractor Blue
| Field | Value |
|---|---|
| **Display Name** | Alchemist's Assistant |
| **Quest Type** | Gather |
| **Act** | 1 |
| **Chain** | Alchemy (step 1 of 2) |
| **Next Step** | Extractor Blue Worms |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Plasmium | CollectableItemBasic | 3 |
| 1 | Extractor | ToolItemBasic | 1 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets (2 targets)

Target 1 (Extractor) is a tool gate — checks whether Hornet has the Needle Phial.

---

### Great Gourmand
| Field | Value |
|---|---|
| **Display Name** | Great Taste of Pharloom |
| **Quest Type** | Gather |
| **Act** | 2 |
| **Chain** | None |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Mossberry Stew | CollectableItemBasic | 1 |
| 1 | Vintage Nectar | CollectableItemBasic | 1 |
| 2 | Courier Supplies Gourmand | DeliveryQuestItemStandalone | 1 |
| 3 | Coral Ingredient | CollectableItemBasic | 1 |
| 4 | Pickled Roach Egg | CollectableItemBasic | 1 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Toggle (5 items)

Toggle-style quest with 5 unique food items. Target 2 (Courier's Rasher) uses
`DeliveryQuestItemStandalone` — has 8 durability segments that decay every 47 seconds.

---

### Extractor Blue Worms
| Field | Value |
|---|---|
| **Display Name** | Advanced Alchemy |
| **Quest Type** | Gather |
| **Act** | 3 |
| **Chain** | Alchemy (step 2 of 2) |
| **Previous Step** | Extractor Blue |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Plasmium Blood | CollectableItemBasic | 10 |
| 1 | Extractor | ToolItemBasic | 1 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets (2 targets)

Plasmium Blood spawns dynamically (not fixed world locations). Target 1 (Extractor)
is a tool gate — checks for Needle Phial.
