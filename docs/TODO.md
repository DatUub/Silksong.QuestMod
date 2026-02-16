# QuestMod — TODO

### Release

- [ ] Flip `allow-release` to `'true'` in `build-publish.yml`
- [ ] Announce in the modding Discord server

---

## 🔮 Post-Release *(sorted easiest → hardest)*

### 1. Testing & Validation
- [ ] TheMythical playtest — collect feedback

### 2. Quest Customization
- [ ] Custom requirements on wishes
    
    * **Preset: Farmable-only** — reduce requirements for farmable items (garbs, pins, etc.) but leave unique items (mossberries, pollips, etc.) unchanged
    
- [ ] Granular prerequisite toggles (Fleatopia, mandatory wishes, Faydown Cloak, etc.)
- [ ] Remote complete wishes (complete from anywhere)
- [ ] Per-quest availability and auto-accept controls
- [ ] Add custom requirements to wishes (e.g. collect 5 silk skills)
- [ ] Custom wish board population and per-board editing

### 3. Wishboard Access
- [ ] Bypass wishboard unlock requirement (use boards without defeating Bell Beast)
    * Bell Beast completion is hard-coded — need to intercept the check and fake the flag

### 4. Delivery System
- [ ] Separate **Delivery tab** — deliveries + Great Gourmand
- [ ] Gourmand custom implementation (8 segments, 47s decay)

### 5. All Wishes Mode
- [ ] "Pure" vs "adjusted" All Wishes Mode
    * Make sure chain gating is respected. 
- [ ] Boss ordering (defeat prerequisites)
- [ ] Edge cases: SB2/4C shared arena, Pinstress/Needle Strike, double Trobbio

### 6. ItemChanger Integration
- [ ] Melodies + Hearts GUI (requires ItemChanger for collectable items)
- [ ] Mossberry cap → 7 (matches world count of 7)
    * Currently blocked: raising cap past default conflicts with Druid's Eyes reward
    * Fix: use ItemChanger to grant Druid's Eyes alongside quest completion

### 7. Wish Location Reassignment *(stretch goal)*
- [ ] Accept NPC wishes from wishboards
- [ ] Get wishboard wishes from random world locations
    * Low priority — no clear implementation path yet
