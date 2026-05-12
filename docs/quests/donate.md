# Donate

Donation quests involve spending currency. Three chains: Bone Bottom (Shell Shards),
Bellhart (Rosaries), and Songclave (Rosaries).

---

### Building Materials
| Field | Value |
|---|---|
| **Display Name** | Bone Bottom Repairs |
| **Quest Type** | Donate |
| **Act** | 1 |
| **Chain** | Bone Bottom Donate (step 1 of 3) |
| **Next Step** | Building Materials (Bridge) |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Shard | CollectableItemBasic | 200 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

Shell Shard capacity: base 400, max 800 (Tool Pouch upgrade).

---

### Building Materials (Bridge)
| Field | Value |
|---|---|
| **Display Name** | A Lifesaving Bridge |
| **Quest Type** | Donate |
| **Act** | 1 |
| **Chain** | Bone Bottom Donate (step 2 of 3) |
| **Previous Step** | Building Materials |
| **Next Step** | Building Materials (Statue) |
| **Prerequisites** | reqComplete: `Building Materials` + pdTest: `spinnerDefeated` OR `visitedGreymoor` |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Shard | CollectableItemBasic | 300 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

---

### Building Materials (Statue)
| Field | Value |
|---|---|
| **Display Name** | An Icon of Hope |
| **Quest Type** | Donate |
| **Act** | 2 |
| **Chain** | Bone Bottom Donate (step 3 of 3) |
| **Previous Step** | Building Materials (Bridge) |
| **Prerequisites** | reqComplete: `Building Materials (Bridge)` + pdTest: `citadelWoken` AND `fixerBridgeConstructed` |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Shard | CollectableItemBasic | 440 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

Total Bone Bottom chain: 940 Shell Shards (exceeds max capacity of 800).

---

### Belltown House Start
| Field | Value |
|---|---|
| **Display Name** | Restoration of Bellhart |
| **Quest Type** | Donate |
| **Act** | 1 |
| **Chain** | Bellhart Donate (step 1 of 2) |
| **Next Step** | Belltown House Mid |
| **Prerequisites** | pdTest: `nailUpgrades > 0` AND `BelltownRelicDealerGaveRelic` AND `BelltownGreeterMetTimePassed` |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Rosary | CollectableItemBasic | 250 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

---

### Belltown House Mid
| Field | Value |
|---|---|
| **Display Name** | Bellhart's Glory |
| **Quest Type** | Donate |
| **Act** | 2 |
| **Chain** | Bellhart Donate (step 2 of 2) |
| **Previous Step** | Belltown House Start |
| **Prerequisites** | reqComplete: `Belltown House Start` + pdTest: `citadelHalfwayComplete` |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Rosary | CollectableItemBasic | 400 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

Total Bellhart chain: 650 Rosaries.

---

### Songclave Donation 1
| Field | Value |
|---|---|
| **Display Name** | Building Up Songclave |
| **Quest Type** | Donate |
| **Act** | 2 |
| **Chain** | Songclave Donate (step 1 of 2) |
| **Next Step** | Songclave Donation 2 |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Rosary | CollectableItemBasic | 300 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

---

### Songclave Donation 2
| Field | Value |
|---|---|
| **Display Name** | Strengthening Songclave |
| **Quest Type** | Donate |
| **Act** | 2 |
| **Chain** | Songclave Donate (step 2 of 2) |
| **Previous Step** | Songclave Donation 1 |
| **Prerequisites** | reqComplete: `Songclave Donation 1` + pdTest: `enclaveLevel > 1` AND `enclaveDonation2_Available` |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Rosary | CollectableItemBasic | 500 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

Total Songclave chain: 800 Rosaries.
