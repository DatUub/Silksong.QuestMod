# Main Story

Main Story quests use the `MainQuest` base type. Most have no targets — they are progression-gated and
completed by reaching specific game events.

---

### Grand Gate Bellshrines
| Field | Value |
|---|---|
| **Display Name** | Grand Gate |
| **Quest Type** | Open |
| **Act** | 1 |
| **Chain** | None (standalone) |
| **Prerequisites** | None |
| **Targets** | 5 toggle targets (AltTest-based, count 1 each) |

| Target | Item Name |
|---|---|
| 0 | The Marrow |
| 1 | Far Fields |
| 2 | Greymoor |
| 3 | Bellhart |
| 4 | Shellwood |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Toggle

Each bellshrine target uses an `AltTest` (PlayerDataTest) to check if Hornet has visited
that region. This is the only Main Story quest with manipulable targets.

---

### The Threadspun Town
| Field | Value |
|---|---|
| **Display Name** | The Threadspun Town |
| **Quest Type** | Save |
| **Act** | 1 (spans multiple acts) |
| **Chain** | None (standalone) |
| **Prerequisites** | None |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Bellbeast Rescue
| Field | Value |
|---|---|
| **Display Name** | Beast in the Bells |
| **Quest Type** | Defeat |
| **Act** | 3 |
| **Chain** | None (standalone) |
| **Prerequisites** | None |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Citadel Seeker
| Field | Value |
|---|---|
| **Display Name** | The Great Citadel |
| **Quest Type** | Seek |
| **Act** | 2 |
| **Chain** | Citadel (step 1 of 6) |
| **Next Step** | Citadel Investigate |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Citadel Investigate
| Field | Value |
|---|---|
| **Display Name** | Silent Halls |
| **Quest Type** | Search |
| **Act** | 2 |
| **Chain** | Citadel (step 2 of 6) |
| **Previous Step** | Citadel Seeker |
| **Next Step** | Citadel Ascent |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Citadel Ascent
| Field | Value |
|---|---|
| **Display Name** | Pharloom's Crown |
| **Quest Type** | Ascend |
| **Act** | 2 |
| **Chain** | Citadel (step 3 of 6) |
| **Previous Step** | Citadel Investigate |
| **Next Step** | Citadel Ascent Melodies |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Citadel Ascent Melodies
| Field | Value |
|---|---|
| **Display Name** | Pharloom's Crown |
| **Quest Type** | Ascend |
| **Act** | 2 |
| **Chain** | Citadel (step 4 of 6) |
| **Previous Step** | Citadel Ascent |
| **Next Step** | Citadel Ascent Lift |
| **Prerequisites** | None (chain-gated) |
| **Targets** | 3 melodies (PD bools + collectable) |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Custom (click-to-give)

Requires ItemChanger integration — melodies are tracked as 2 PD bools + 1 collectable item.

| Sub-objective | PD Field | Location |
|---|---|---|
| Conductor's Melody | `HasMelodyConductor` | High Halls |
| Architect's Melody | `HasMelodyArchitect` | Cogwork Core |
| Vaultkeeper's Melody | `HasMelodyLibrarian` + collectable | Whispering Vaults |

---

### Citadel Ascent Lift
| Field | Value |
|---|---|
| **Display Name** | Pharloom's Crown |
| **Quest Type** | Ascend |
| **Act** | 2 |
| **Chain** | Citadel (step 5 of 6) |
| **Previous Step** | Citadel Ascent Melodies |
| **Next Step** | Citadel Ascent Silk Defeat |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Citadel Ascent Silk Defeat
| Field | Value |
|---|---|
| **Display Name** | Pale Monarch |
| **Quest Type** | Defeat |
| **Act** | 2 |
| **Chain** | Citadel (step 6 of 6) |
| **Previous Step** | Citadel Ascent Lift |
| **Prerequisites** | None (chain-gated) |
| **hideIfComplete** | Silk Defeat Snare |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

Completing this quest hides the "Silk Defeat Snare" quest.

---

### Silk Defeat Snare
| Field | Value |
|---|---|
| **Display Name** | Soul Snare |
| **Quest Type** | Destroy |
| **Act** | 2 |
| **Chain** | None (standalone, linked via hideIfComplete) |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

Hidden when Citadel Ascent Silk Defeat is completed.

---

### Black Thread Pt0
| Field | Value |
|---|---|
| **Display Name** | After the Fall |
| **Quest Type** | Seek |
| **Act** | 3 |
| **Chain** | Main Story (step 1 of 10) |
| **Next Step** | Black Thread Pt1 Shamans |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Black Thread Pt1 Shamans
| Field | Value |
|---|---|
| **Display Name** | Awaiting the End |
| **Quest Type** | Seek |
| **Act** | 3 |
| **Chain** | Main Story (step 2 of 10) |
| **Previous Step** | Black Thread Pt0 |
| **Next Step** | Diving Bell Pt1 Inspect |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Diving Bell Pt1 Inspect
| Field | Value |
|---|---|
| **Display Name** | The Dark Below |
| **Quest Type** | Seek |
| **Act** | 3 |
| **Chain** | Main Story (step 3 of 10) |
| **Previous Step** | Black Thread Pt1 Shamans |
| **Next Step** | Diving Bell Pt2 Ballow |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Diving Bell Pt2 Ballow
| Field | Value |
|---|---|
| **Display Name** | The Dark Below |
| **Quest Type** | Seek |
| **Act** | 3 |
| **Chain** | Main Story (step 4 of 10) |
| **Previous Step** | Diving Bell Pt1 Inspect |
| **Next Step** | Diving Bell Pt3 Descend |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Diving Bell Pt3 Descend
| Field | Value |
|---|---|
| **Display Name** | The Dark Below |
| **Quest Type** | Descend |
| **Act** | 3 |
| **Chain** | Main Story (step 5 of 10) |
| **Previous Step** | Diving Bell Pt2 Ballow |
| **Next Step** | Black Thread Pt2 Abyss |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Black Thread Pt2 Abyss
| Field | Value |
|---|---|
| **Display Name** | The Dark Below |
| **Quest Type** | Seek |
| **Act** | 3 |
| **Chain** | Main Story (step 6 of 10) |
| **Previous Step** | Diving Bell Pt3 Descend |
| **Next Step** | Black Thread Pt3 Escape |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Black Thread Pt3 Escape
| Field | Value |
|---|---|
| **Display Name** | Return to Pharloom |
| **Quest Type** | Ascend |
| **Act** | 3 |
| **Chain** | Main Story (step 7 of 10) |
| **Previous Step** | Black Thread Pt2 Abyss |
| **Next Step** | Black Thread Pt4 Return |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Black Thread Pt4 Return
| Field | Value |
|---|---|
| **Display Name** | Spell Seeker |
| **Quest Type** | Seek |
| **Act** | 3 |
| **Chain** | Main Story (step 8 of 10) |
| **Previous Step** | Black Thread Pt3 Escape |
| **Next Step** | Black Thread Pt5 Heart |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

---

### Black Thread Pt5 Heart
| Field | Value |
|---|---|
| **Display Name** | The Old Hearts |
| **Quest Type** | Seek |
| **Act** | 3 |
| **Chain** | Main Story (step 9 of 10) |
| **Previous Step** | Black Thread Pt4 Return |
| **Next Step** | Black Thread Pt6 Flower |
| **Prerequisites** | None (chain-gated) |
| **Targets** | 4 hearts (collect 3 of 4, requires ItemChanger) |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ✅ Custom (pick 3 of 4)

| Heart | Registry Name | PD Field | Boss |
|---|---|---|---|
| Encrusted Heart | `Clover Heart` | `CollectedHeartClover` | Crust King Khann |
| Pollen Heart | `Flower Heart` | `CollectedHeartFlower` | Nyleth |
| Hunter's Heart | `Hunter Heart` | `CollectedHeartHunter` | Skarrsinger Karmelita |
| Conjoined Heart | `Coral Heart` | `CollectedHeartCoral` | Unknown |

---

### Black Thread Pt6 Flower
| Field | Value |
|---|---|
| **Display Name** | Last Dive |
| **Quest Type** | Descend |
| **Act** | 3 |
| **Chain** | Main Story (step 10 of 10) |
| **Previous Step** | Black Thread Pt5 Heart |
| **Prerequisites** | None (chain-gated) |
| **Targets** | None |

**Mod Capabilities:** ✅ Accept · ✅ Complete · ❌ Targets

Final main story quest — end of the Main Story chain.
