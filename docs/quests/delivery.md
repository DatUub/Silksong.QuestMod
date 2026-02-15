# Delivery

Courier escort quests. The target count represents courier hitpoints, not items collected.
Two base classes: `DeliveryQuestItem` (all 7 couriers) and `DeliveryQuestItemStandalone`
(Gourmand's Rasher, time-based with 8 segments × 47s = 376s max).

---

## Standard Deliveries (5 HP)

### Courier Delivery Bonebottom
| Field | Value |
|---|---|
| **Display Name** | Bone Bottom Supplies |
| **Quest Type** | Delivery |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | pdTest: `blackThreadWorld=False` (Act 1-2 only) |

| Target | Counter | Type | HP |
|---|---|---|---|
| 0 | Courier Supplies | CollectableItemBasic | 5 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ⚠️ Targets (HP-based)

---

### Courier Delivery Pilgrims Rest
| Field | Value |
|---|---|
| **Display Name** | Pilgrim's Rest Supplies |
| **Quest Type** | Delivery |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | pdTest: `blackThreadWorld=False` (Act 1-2 only) |

| Target | Counter | Type | HP |
|---|---|---|---|
| 0 | Courier Supplies | CollectableItemBasic | 5 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ⚠️ Targets (HP-based)

---

### Courier Delivery Fleatopia
| Field | Value |
|---|---|
| **Display Name** | Fleatopia Supplies |
| **Quest Type** | Delivery |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | pdTest: `CaravanTroupeLocation` (string check) |

| Target | Counter | Type | HP |
|---|---|---|---|
| 0 | Courier Supplies | CollectableItemBasic | 5 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ⚠️ Targets (HP-based)

---

### Courier Delivery Songclave
| Field | Value |
|---|---|
| **Display Name** | Songclave Supplies |
| **Quest Type** | Delivery |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | pdTest: (`enclaveLevel > 0` AND `soulSnareReady=False`) OR `blackThreadWorld` |

| Target | Counter | Type | HP |
|---|---|---|---|
| 0 | Courier Supplies | CollectableItemBasic | 5 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ⚠️ Targets (HP-based)

---

### Courier Delivery Fixer
| Field | Value |
|---|---|
| **Display Name** | Survivor's Camp Supplies |
| **Quest Type** | Delivery |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | pdTest: `blackThreadWorld=True` (Act 3 only) |

| Target | Counter | Type | HP |
|---|---|---|---|
| 0 | Courier Supplies | CollectableItemBasic | 5 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ⚠️ Targets (HP-based)

---

## Special Deliveries (lower HP)

### Courier Delivery Dustpens Slave
| Field | Value |
|---|---|
| **Display Name** | Queen's Egg |
| **Quest Type** | Delivery |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | pdTest: `farmer_grewFirstGrub` |

| Target | Counter | Type | HP |
|---|---|---|---|
| 0 | Courier Supplies | CollectableItemBasic | 4 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ⚠️ Targets (HP-based)

---

### Courier Delivery Mask Maker
| Field | Value |
|---|---|
| **Display Name** | Liquid Lacquer |
| **Quest Type** | Delivery |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | pdTest: `hasDoubleJump` AND `UnlockedMelodyLift` AND `SlaveDeliveryQuestCompleted` |

| Target | Counter | Type | HP |
|---|---|---|---|
| 0 | Courier Supplies | CollectableItemBasic | 3 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ⚠️ Targets (HP-based)

Heaviest prereqs of any delivery: Double Jump + Melody Lift + Dustpens Slave delivery completed.
