# START HERE — Project Overview

> This vault is the brain of the project. If you are Claude Code: read this file first, then `01 - Game Design.md`, then `02 - Tech Stack.md`, then work from `03 - Roadmap.md` and `04 - TODO.md`.

## What this game is (one paragraph)
A mobile, single-player **gacha strategy game** set on the **Mongolian steppe**. The player is a clan leader who builds a camp of traditional **gers** (yurts), raises **livestock as the resource economy** (sheep, cattle, special horses), collects **fighters via a gacha system**, and fights **Arknights-style lane battles** where some units hold position and some advance. The hook is a fresh cultural setting + the cozy herding economy fused with collection-gacha combat.

## Core platform decisions (already made)
- **Engine:** Unity 6 LTS (editor `6000.0.46f1`)
- **Template:** Universal 2D (URP, 2D)
- **Target:** Mobile — Android first (Google Play + Samsung Galaxy Store), iOS later
- **Genre:** Single-player gacha + base-building + lane combat
- **Monetization:** Gacha summons (premium currency tied to "special horses")
- **Art direction:** 2D / 2.5D (layered sprites, parallax, skeletal animation) — NOT 3D models

## The golden rule for this project
**Build a vertical slice first.** Do not build all three systems (herding / building / combat) fully before they connect. Get a tiny version of each, wired together into one playable loop, THEN expand. See `03 - Roadmap.md`.

## Vault map
- `00 - START HERE.md` — this file
- `01 - Game Design.md` — the full game concept and systems
- `02 - Tech Stack.md` — engine, backend, tools, folder structure
- `03 - Roadmap.md` — phases from setup → vertical slice → expansion
- `04 - TODO.md` — the live task list (work from here)
- `05 - Decisions Log.md` — why we chose what we chose
- `06 - Open Questions.md` — things still undecided
- `07 - Build Plan & Placeholder Assets.md` — step-by-step from empty project + when to add which placeholder art
