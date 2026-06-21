# 07 — Build Plan & Placeholder Assets

> How to go from an empty Universal 2D project to a playable vertical slice, and exactly when to add which placeholder assets. Golden rule: **use the crudest placeholder that still lets you tell things apart. Never make/buy art before the system that uses it exists.** Boxes first, real art last.

## The placeholder philosophy
- A "placeholder" is throwaway art whose only job is to let you test a system. A colored square labeled "SHEEP" is a perfect placeholder.
- You upgrade placeholder → real art ONLY after the system works and feels fun.
- This saves money (no art for features you cut) and time (systems get tested immediately).
- Unity ships with built-in primitives (sprites/shapes) and the 2D package can make plain squares/circles. That's all you need for most of the build.

---

## STEP 0 — From the empty project: foundation (no assets at all)
Goal: prove the pipeline works before any gameplay.
- Confirm the project opens, Universal 2D / URP is active.
- Set platform to **Android** (File > Build Settings).
- Create folder structure under `Assets/`: `Scripts/`, `Art/`, `Prefabs/`, `Scenes/`, `Data/`, `UI/`.
- Make one empty scene called `Main`.
- Do a throwaway Android build of the empty scene to confirm it runs on a phone/emulator.
- Git init + Unity `.gitignore` + first commit.
**Placeholder assets: NONE.** This is pure plumbing.

---

## STEP 1 — Economy skeleton (placeholder: colored squares + text)
Goal: livestock exists as data and grows over time.
- Code the livestock data (sheep, cattle, special horses), a herd manager, a tick-based growth system, and local save/load.
- Build a simple on-screen UI showing counts.
**Placeholder assets:**
- Plain **colored squares** as livestock icons (white=sheep, brown=cattle, gold=special horse) — make these in any image editor or use Unity's default sprite tinted by color.
- Default Unity UI text for the numbers.
- That's it. No animations, no real art.

---

## STEP 2 — Base building (placeholder: colored boxes labeled with ger names)
Goal: place gers and upgrade them by spending livestock.
- Code the ger data model (type, level, upgrade cost, effect) and the upgrade logic.
- Put 3 gers in the scene: Main Ger, Herding Ger, Hero Calling Shrine.
**Placeholder assets:**
- Each ger = a **flat colored rectangle or circle** with a text label ("MAIN GER", "SHRINE").
- Different color per ger type so you can tell them apart.
- No ger illustrations yet — the shape just marks position and tap target.

---

## STEP 3 — Combat slice (placeholder: colored capsules/circles for units)
Goal: minimal Arknights-style lane battle.
- Code unit data, deployment, movement (some hold, some advance), enemy spawns, fighting, win/lose.
- One battlefield scene with a simple lane.
**Placeholder assets:**
- **Circles or capsules** as units: blue=your melee, green=your archer, red=enemy.
- A flat rectangle for the lane/ground.
- Health bars = simple colored bars (Unity UI). No character art, no attack animations — a unit "attacking" can just be a color flash or number popup.

---

## STEP 4 — Gacha slice (placeholder: colored cards with names)
Goal: summon a random hero by rarity rate at the Shrine.
- Code the hero roster as data (3–5 heroes, rarity tiers, pull rates), the summon action (spends special horses), and a summon result screen.
**Placeholder assets:**
- Each hero = a **rectangle "card"** colored by rarity (gray/blue/purple/gold) with the hero's name as text.
- The summon animation can be a simple card flip or fade — no fancy art.
- This is where you FIRST think about real character art, but you still don't need it yet — names on colored cards prove the system.

---

## STEP 5 — Connect the loop + FIRST real placeholder art pass
Goal: the full loop works end to end and starts to look like a game.
- Wire it together: herd grows → upgrade ger → summon hero → win battle → earn herd → repeat.
- NOW introduce your **first real placeholder art** (still not final/commissioned):
  - **AI-generated or asset-pack** character images for the heroes (replace the colored cards).
  - Simple **ger sprites** (asset pack or AI) replacing the colored boxes.
  - A **steppe background** image (parallax layers if you want the 2.5D feel).
  - Your **mascot** (kid + lamb) as a first sketch/placeholder.
- This is the playable vertical slice. If it's fun with rough art, the concept works.
**Placeholder assets: first real images, but cheap/temporary — AI or asset packs, NOT commissioned.**

---

## STEP 6+ — Production art (only after the slice is fun)
- Commission real character art for key heroes (your biggest spend — do it once the game is proven).
- Lock the mascot design, build the art style guide.
- Real ger art, battle effects, animations (Unity 2D Animation or Spine).
- Then: event banners/foreign fighters, backend, IAP, store submission.

---

## Quick reference: when does art quality step up?
| Step | What you build | Art level |
|------|----------------|-----------|
| 0 | Project/build/Git | None |
| 1 | Economy | Colored squares + text |
| 2 | Gers/upgrades | Colored boxes + labels |
| 3 | Combat | Colored circles/capsules |
| 4 | Gacha | Colored name cards |
| 5 | Connect loop | First real placeholders (AI/asset packs) |
| 6+ | Expand | Commissioned production art |

## What Claude Code does at each step
Claude Code builds ALL the code/systems in steps 0–6. It does **not** make any of the art in this table — you supply placeholders (boxes are free; AI/asset packs for step 5). Claude Code wires whatever art you drop in into the systems it builds.
