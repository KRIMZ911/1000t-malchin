# 06 — Open Questions

> Things still undecided. Resolve these as the project moves; move answers into the Decisions Log.

## Design
- [ ] What is the win/progression fantasy long-term — endless growth, or chapters/campaign with an ending?
- [ ] How does combat difficulty scale so gacha heroes feel worth pulling without becoming pure pay-to-win?
- [ ] Do livestock get consumed permanently on upgrades, or staked/invested and recoverable?
- [ ] How many ger types at launch vs added later?
- [ ] What's the signature hero/unit that represents the game in marketing (horse archer?)?

## Economy / monetization
- [x] Exact premium currency design — **two-tier**: Special Horses (earned) + Sky-Blue Khadag (paid) on top. (Decisions Log 2026-06-24)
- [~] Free-to-play generosity — direction set (first-launch guaranteed 10-pull + idle/battle drip ≈ one 10-pull every few days); exact numbers tune once economy values are live. (`09`)
- [x] Pity system? — **yes, soft + hard** (6★ +2%/pull after 50, hard ~99, resets on 6★; 10-pull guarantees 5★+). (Decisions Log 2026-06-24)
- [x] Drop-rate disclosure — **show real per-tier % on the summon screen**, built in from day one. (Decisions Log 2026-06-24)

## Tech
- [ ] Which backend exactly (PlayFab vs Firebase) — decide before Phase 6.
- [ ] Skeletal animation tool — Unity 2D Animation package vs Spine (Spine costs money but is industry standard).
- [~] Save data format and migration strategy when local → backend. (Partial: local JSON with `version` field + timestamp chosen — see Decisions Log 2026-06-22. Backend migration path still open for Phase 6.)

## Cultural
- [ ] Source for authentic Mongolian names, ger architecture, clothing references — consider consulting someone knowledgeable to avoid caricature.
- [ ] Which "foreign fighters" rosters are respectful and fun vs risky stereotypes.

## Art sourcing (biggest cost decision)
- [ ] Placeholder strategy for the vertical slice: AI-generated vs free asset packs?
- [ ] Production art: which heroes get commissioned art first, and what budget per character?
- [ ] If using AI image generation: which tool, and confirm its commercial-use/licensing terms.
- [ ] Define the game's own art style guide so "Arknights-looking" stays inspired-by, never a copy of their IP.
- [ ] Lock the signature mascot design (kid + lamb) early — it anchors the whole art direction.

## Business
- [ ] Target launch regions first (Mongolia? Broader Asia? Global?).
- [ ] Art production: solo, asset packs, or commissioned artists — biggest cost/quality lever for a gacha game.
