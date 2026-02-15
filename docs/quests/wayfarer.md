# Wayfarer

NPC rescue, fetch, and encounter quests. The largest category (20 quests).

---

### Save the Fleas Pre
| Field | Value |
|---|---|
| **Display Name** | The Lost Fleas |
| **Quest Type** | Wayfarer |
| **Act** | 1 |
| **Chain** | Save the Fleas (step 1 of 2) |
| **Next Step** | Save the Fleas |
| **Prerequisites** | pdTest: `CaravanTroupeLocation` (string check) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Save the Fleas
| Field | Value |
|---|---|
| **Display Name** | The Lost Fleas |
| **Quest Type** | Wayfarer |
| **Act** | 1 |
| **Chain** | Save the Fleas (step 2 of 2) |
| **Previous Step** | Save the Fleas Pre |
| **Prerequisites** | Save the Fleas Pre |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | FleasCollected | CollectableItemBasic | 5 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Targets

Rescue 5 of 30 fleas scattered across Pharloom. Special fleas (Huge Flea, Kratt, Vog)
count toward total and have unique icons. Reward: Flea Brew.

---

### A Pinsmiths Tools
| Field | Value |
|---|---|
| **Display Name** | Pinmaster's Oil |
| **Quest Type** | Wayfarer |
| **Act** | 1 |
| **Chain** | — (standalone) |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Pale Oil | CollectableItemBasic | 1 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ⚠️ Targets (single-count fetch)

3 Pale Oil sources: Choral Chambers pickup, Great Gourmand reward, Flea Caravan reward.
Reward: Needle upgrade. Bringing Pale Oil to Plinney before accepting skips the quest
(upgrade + point still granted).

---

### Save Courier Short
| Field | Value |
|---|---|
| **Display Name** | My Missing Courier |
| **Quest Type** | Wayfarer |
| **Act** | 1 |
| **Chain** | Save Courier (step 1 of 2) |
| **Next Step** | Save Courier Tall |
| **Prerequisites** | None |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Save Courier Tall
| Field | Value |
|---|---|
| **Display Name** | My Missing Brother |
| **Quest Type** | Wayfarer |
| **Act** | 2 |
| **Chain** | Save Courier (step 2 of 2) |
| **Previous Step** | Save Courier Short |
| **Prerequisites** | reqComplete: `Save Courier Short` + pdTest: `enclaveLevel > 0` OR `GourmandQuestAccepted` |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Save Sherma
| Field | Value |
|---|---|
| **Display Name** | Balm for the Wounded |
| **Quest Type** | Wayfarer |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | pdTest: `shermaQuestActive` |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Soul Snare Pre
| Field | Value |
|---|---|
| **Display Name** | Silk and Soul |
| **Quest Type** | Wayfarer |
| **Act** | 2 |
| **Chain** | Soul Snare (step 1 of 2) |
| **Next Step** | Soul Snare |
| **Prerequisites** | reqGroups: `Soul Snare` |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

Gated by the `Soul Snare` CompleteTotalGroup — threshold of **17 points**.

#### Required Quests (Value 0, must complete)

| Quest | Category |
|---|---|
| Brolly Get | Gather |
| Building Materials | Donate |
| Building Materials (Bridge) | Donate |
| Crow Feathers | Hunt |
| Songclave Donation 1 | Donate |
| Songclave Donation 2 | Donate |
| Belltown House Start | Donate |
| Belltown House Mid | Donate |
| Shakra Final Quest | Wayfarer |
| Save Sherma | Wayfarer |

#### Optional Quests (contribute points toward 17 threshold)

| Quest | Value | Category |
|---|---|---|
| Mossberry Collection 1 | 1 | Gather |
| Building Materials (Statue) | 1 | Donate |
| Shell Flowers | 1 | Gather |
| Rock Rollers | 1 | Hunt |
| A Pinsmiths Tools | 1 | Wayfarer |
| Skull King | 1 | Grand Hunt |
| Fine Pins | 1 | Hunt |
| Roach Killing | 1 | Hunt |
| Great Gourmand | 1 | Wayfarer |
| Extractor Blue | 1 | Gather |
| Journal | 1 | Unique |
| Shiny Bell Goomba | 1 | Gather |
| Beastfly Hunt | 1 | Grand Hunt |
| Huntress Quest | 1 | Hunt |
| Pilgrim Rags | 1 | Hunt |
| Save City Merchant | 1 | Wayfarer |
| Save City Merchant Bridge | 1 | Wayfarer |
| Song Knight | 1 | Wayfarer |
| Song Pilgrim Cloaks | 1 | Hunt |
| Broodmother Hunt | 1 | Grand Hunt |
| Save Courier Short | 1 | Wayfarer |
| Save Courier Tall | 1 | Wayfarer |
| Courier Delivery Bonebottom | 0.5 | Delivery |
| Courier Delivery Pilgrims Rest | 0.5 | Delivery |
| Courier Delivery Songclave | 0.5 | Delivery |
| Courier Delivery Fleatopia | 0.5 | Delivery |
| Courier Delivery Dustpens Slave | 0.5 | Delivery |
| Courier Delivery Mask Maker | 0.5 | Delivery |

Max possible: 22 (1-pt quests) + 3 (6×0.5-pt couriers) = **25 points total**.

Completing the Soul Snare triggers the Snared Silk ending and permanently locks out
the Weaver Queen and Twisted Child endings on that save.

---

### Soul Snare
| Field | Value |
|---|---|
| **Display Name** | Silk and Soul |
| **Quest Type** | Wayfarer |
| **Act** | 2 |
| **Chain** | Soul Snare (step 2 of 2) |
| **Previous Step** | Soul Snare Pre |
| **Prerequisites** | Soul Snare Pre |

| Target | Counter | Type | Count | Wiki Context |
|---|---|---|---|---|
| 0 | Snare Soul Churchkeeper | CollectableItemBasic | 1 | Maiden's Soul (Chapel Maid, Bone Bottom) |
| 1 | Snare Soul Bell Hermit | CollectableItemBasic | 1 | Hermit's Soul (Bell Hermit, Bellhart) |
| 2 | Snare Soul Swamp Bug | CollectableItemBasic | 1 | Seeker's Soul (Groal the Great corpse, Bilewater) |
| 3 | Silk Snare | ToolItemBasic | 1 | Snare Setter (hidden workshop, Weavenest Atla) |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Toggle (4 targets)

---

### Save City Merchant
| Field | Value |
|---|---|
| **Display Name** | The Wandering Merchant |
| **Quest Type** | Wayfarer |
| **Act** | 2 |
| **Chain** | Merchant Rescue (step 1 of 2) |
| **Next Step** | Save City Merchant Bridge |
| **Prerequisites** | None |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Save City Merchant Bridge
| Field | Value |
|---|---|
| **Display Name** | The Lost Merchant |
| **Quest Type** | Wayfarer |
| **Act** | 2 |
| **Chain** | Merchant Rescue (step 2 of 2) |
| **Previous Step** | Save City Merchant |
| **Prerequisites** | reqComplete: `Save City Merchant` + pdTest: `cityMerchantCanLeaveForBridge` |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Wood Witch Curse
| Field | Value |
|---|---|
| **Display Name** | Rite of Rebirth |
| **Quest Type** | Wayfarer |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | pdTest: `spinnerDefeated` |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Doctor Curse Cure
| Field | Value |
|---|---|
| **Display Name** | Infestation Operation |
| **Quest Type** | Wayfarer |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | None |

| Target | Counter | Type | Count |
|---|---|---|---|
| 0 | Extractor Machine Pins | CollectableItemBasic | 1 |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ⚠️ Targets (single-count fetch)

---

### Shakra Final Quest
| Field | Value |
|---|---|
| **Display Name** | Trail's End |
| **Quest Type** | Wayfarer |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | pdTest: `ShakraFinalQuestAppear` AND `hasDoubleJump` |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Song Knight
| Field | Value |
|---|---|
| **Display Name** | Final Audience |
| **Quest Type** | Wayfarer |
| **Act** | 2 |
| **Chain** | — (standalone) |
| **Prerequisites** | pdTest: `songChevalierQuestReady` |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Pinstress Battle Pre
| Field | Value |
|---|---|
| **Display Name** | Fatal Resolve |
| **Quest Type** | Wayfarer |
| **Act** | 3 |
| **Chain** | Pinstress Battle (step 1 of 2) |
| **Next Step** | Pinstress Battle |
| **Prerequisites** | reqComplete: `Black Thread Pt1 Shamans` + pdTest: `pinstressQuestReady` AND `hasSuperJump` |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Pinstress Battle
| Field | Value |
|---|---|
| **Display Name** | Fatal Resolve |
| **Quest Type** | Wayfarer |
| **Act** | 3 |
| **Chain** | Pinstress Battle (step 2 of 2) |
| **Previous Step** | Pinstress Battle Pre |
| **Prerequisites** | Pinstress Battle Pre |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Tormented Trobbio
| Field | Value |
|---|---|
| **Display Name** | Pain, Anguish and Misery |
| **Quest Type** | Wayfarer |
| **Act** | 3 |
| **Chain** | — (standalone) |
| **Prerequisites** | reqTools: `Dazzle Bind` + pdTest: `hasSuperJump` |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

Boss fight in the Whispering Vaults. Reward: Dark Mirror (→ Claw Mirrors upgrade).

---

### Garmond Black Threaded
| Field | Value |
|---|---|
| **Display Name** | Hero's Call |
| **Quest Type** | Wayfarer |
| **Act** | 3 |
| **Chain** | — (standalone) |
| **Prerequisites** | pdTest: `garmondFinalQuestReady` |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Flea Games Pre
| Field | Value |
|---|---|
| **Display Name** | Ecstasy of the End |
| **Quest Type** | Wayfarer |
| **Act** | 3 |
| **Chain** | Flea Games (step 1 of 2) |
| **Next Step** | Flea Games |
| **Prerequisites** | pdTest: `FleaGamesCanStart` AND `hasSuperJump` |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Flea Games
| Field | Value |
|---|---|
| **Display Name** | Ecstasy of the End |
| **Quest Type** | Wayfarer |
| **Act** | 3 |
| **Chain** | Flea Games (step 2 of 2) |
| **Previous Step** | Flea Games Pre |
| **Prerequisites** | Flea Games Pre |

| Target | Item Name |
|---|---|
| 0 | Juggle |
| 1 | Dodge |
| 2 | Bounce |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Toggle (3 targets)

Three flea circus challenges, each an AltTest-based toggle.
