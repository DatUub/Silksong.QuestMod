# QuestMod — TODO

### Release
- [ ] Write changelog for v0.1.0
- [ ] Final version bump to 0.1.0
- [ ] Create GitHub repo and push
- [ ] Set up issue labels (bug, enhancement, all-wishes-mode, quest-customization, etc.)
- [ ] Create GitHub issues for known bugs:
    * Plasmium quests uncompletable without Needle Phial in All Wishes Mode
    * Shakra not appearing with Trail's End in All Wishes Mode
    * Junilana missing NPC behavior in All Wishes Mode
    * Mr Mushroom not spawning at later locations in All Wishes Mode
- [ ] Add `THUNDERSTORE_API_KEY` secret to GitHub repo
- [ ] Flip `allow-release` to `'true'` in `build-publish.yml`
- [ ] Tag v0.1.0 and push tag to trigger release
- [ ] Announce in the modding Discord server

---

## 🔮 Post-Release

### Testing & Validation
- [ ] TheMythical playtest — collect feedback
- [ ] Test fresh save + late-game save compatibility
- [ ] Test with ModMenu installed — verify all bool configs appear
- [ ] Verify title screen reset clears all runtime state

### Delivery System
- [ ] Separate **Delivery tab** — deliveries + Great Gourmand
- [ ] Gourmand custom implementation (8 segments, 47s decay)

### Quest Customization
- [ ] Remote complete wishes (complete from anywhere)
- [ ] Per-quest availability and auto-accept controls
- [ ] Custom wish board population and per-board editing
- [ ] Custom requirements on wishes
- [ ] Granular prerequisite toggles (Fleatopia, mandatory wishes, Faydown Cloak, etc.)
- [ ] Add custom requirements to wishes (e.g. collect 5 silk skills)
- [ ] Other RNG drop manipulation (Fine Pins, etc.)

### All Wishes Mode
- [ ] "Pure" vs "adjusted" All Wishes Mode
    * Make sure chain gating is respected. 
- [ ] Boss ordering (defeat prerequisites)
- [ ] Edge cases: SB2/4C shared arena, Pinstress/Needle Strike, double Trobbio

### ItemChanger Integration
- [ ] Melodies + Hearts GUI (requires ItemChanger for collectable items)
