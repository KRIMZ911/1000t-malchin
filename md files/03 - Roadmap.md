# 03 — Roadmap

> Philosophy: **vertical slice first.** A tiny version of all three systems, connected into one playable loop, before deepening anything.

## Phase 0 — Setup (do this first)
Get the project, tools, and version control ready. No gameplay yet.
- Unity project created (Universal 2D, `6000.0.46f1`). ✅ (done by you)
- Obsidian vault in place. ✅
- Git initialized with Unity `.gitignore`.
- Claude Code pointed at the `claude game` root.
- Confirm an Android build runs on a device/emulator ("Hello World" build) so the pipeline works end to end early.

## Phase 1 — Core data & economy skeleton
The herd economy as data, no art yet.
- Define data for livestock types (sheep, cattle, special horses) — counts, growth rates, caps.
- A simple ticking system that grows the herd over time.
- A save/load system using local storage (no backend yet).
- A basic UI showing current livestock counts.

## Phase 2 — Base building (minimal)
- Data model for gers (type, level, upgrade cost in livestock, effect).
- Place 2–3 ger types in a scene (Main Ger, Herding Ger, Ovoo).
- Upgrade a ger by spending livestock; upgrade changes a stat (e.g. Herding Ger raises growth rate).

## Phase 3 — Combat slice (minimal Arknights-style)
- One battlefield scene, a simple lane.
- 1–2 deployable unit types: one that holds position (archer), one that advances (melee).
- Enemies spawn and move; units fight; win/lose condition.
- Battle rewards livestock → loops back to base.

## Phase 4 — Gacha slice (minimal)
- A roster of 3–5 heroes as data, with rarity tiers.
- A summon at the Ovoo that spends special horses and grants a random hero by rate.
- Pull rates defined in data; results shown in a simple summon screen.
- Local only — no real money yet.

## Phase 5 — Connect & polish the loop
- Verify the full loop: herd grows → upgrade gers → summon heroes → win battles → earn herd → repeat.
- First-pass art, basic juice (animation, sound). This is the playable vertical slice.

## Phase 6+ — Expand (only after the slice is fun)
- More gers, livestock types, heroes, and battle maps.
- **Event banner system** + foreign fighters.
- **Backend integration** (PlayFab/Firebase) for accounts, server-validated pulls, remote config.
- Real-money IAP + store compliance (drop-rate disclosure).
- Android store submission, then iOS via Mac.

## Milestone definition of "done" for the slice
A new player can: open the game, see their herd growing, upgrade one ger, summon one hero, win one battle, and see their herd increase — all in one sitting, on an Android phone.
