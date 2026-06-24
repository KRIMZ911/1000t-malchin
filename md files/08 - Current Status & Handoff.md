# 08 — Current Status & Handoff

> **Read this first if you are picking the project back up.** It captures where we
> are, what the immediate task is, and the conventions a new Claude session must
> follow. Last updated: **2026-06-24**.

## TL;DR — where we are right now
- **Phases 0, 1, 2 are DONE and verified** in the Unity editor.
- **Phase 3 (combat) — lane version is DONE and verified** (a safety checkpoint).
- **Phase 3 — grid version (Stage 1) is CODED and pushed to GitHub, but NOT yet
  verified in the editor.** This is the live edge of the project.
- **Immediate next task:** the user runs `Malchin > Setup Combat Scene` in Unity,
  plays the grid battle, and confirms it works (or pastes Console errors to fix).
  Then we move to **Stage 2 (gacha + roster)**.
- **Stage 2 gacha ruleset is now fully DECIDED (2026-06-24)** — currency, rarity tiers,
  rates, pity, dupes/Potential all locked. See the spec in `09` before writing code.

## The goal (one paragraph)
A mobile, single-player **gacha strategy game** on the **Mongolian steppe**: build a
camp of **gers**, grow a **livestock economy** (sheep/cattle/special horses) that
generates idle/offline, **collect fighters via gacha**, and fight **grid-based,
Arknights-style battles** that reward livestock — closing one loop:
*herd grows → upgrade gers → summon heroes → win battles → earn herd → repeat.*
Build a **vertical slice first**; deepen later. See `01 - Game Design.md`.

## Phase status
| Phase | What | Status |
|---|---|---|
| 0 — Setup | Unity, Git/GitHub, Android build | ✅ done |
| 1 — Economy | livestock data, offline growth, save/load, UI | ✅ done |
| 2 — Base building | gers on a 20×20 grid, upgrade paths | ✅ done |
| 3 — Combat | **grid battle + data-driven levels** | 🚧 Stage 1 coded, **unverified** |
| 4 — Gacha | summon/collect characters | ⬜ next (now merged into combat plan) |
| 5 — Connect loop + first art | wire everything; rough art | ⬜ |
| 6+ — Expand | banners, backend, IAP, store | ⬜ |

## The current task in detail
The user expanded combat into a bigger vision (see `09 - Combat & Gacha Design.md`).
We are building it in **three stages**:
- **Stage 1 — grid combat + level editor.** ✅ coded, ⏳ needs verifying.
  Battles run on a grid; levels are `LevelDefinition` assets (grid size + a spawn
  timeline) edited via a custom inspector. Uses a fixed archer/horseman test squad.
- **Stage 2 — gacha + owned-character roster.** ⬜ next. Each unit becomes an
  individual collectible obtained from drops/cases; persisted in the save.
- **Stage 3 — pre-battle squad selection.** ⬜ Pick which owned characters to bring;
  each usable **once per battle**.

**Right now:** verify Stage 1, then build Stage 2.

## Continuing from a phone vs. the PC
- **Anything that needs Unity (running setup tools, pressing Play, testing) requires
  the Windows PC + Unity editor.** A phone session can read the vault, plan, and
  write/edit C# code, but cannot run the project.
- So from a phone you can: review the design, refine the plan, and write the Stage 2
  code. Actually testing it happens back on the PC.

## Hard constraints (do not violate)
1. **Claude writes code, not art.** The user supplies all art/placeholders.
2. **Claude cannot click in the Unity Editor GUI.** For anything that needs the
   editor (placing objects, wiring the inspector), either write a **one-click editor
   tool** (preferred) or give the user clear step-by-step instructions.

## Key technical conventions (how this project is built)
- **Data-driven via ScriptableObjects.** Every system has a `...Definition` asset:
  `LivestockDefinition`, `GerDefinition`, `CombatUnitDefinition`, `LevelDefinition`.
  Tunable in the Inspector; created/edited in code too.
- **One-click editor setup tools.** Because Claude can't wire scenes by hand, each
  system has an editor script under `Assets/Editor/` that builds its scene objects +
  UI from a menu. Current menu items (Unity top bar → **Malchin**):
  - `Malchin > Setup Economy Scene`
  - `Malchin > Setup Building Scene`
  - `Malchin > Setup Combat Scene`
  - `Malchin > Create Battle Level`
  **Gotcha:** a compile error in ANY script makes the whole **Malchin** menu vanish
  (menus only register after a clean compile). If the menu is missing, check the
  Console for red errors first.
- **Save system:** one versioned JSON file in `Application.persistentDataPath`
  (`SaveSystem.cs`), currently **v3**. Holds livestock counts + a timestamp (for
  offline growth) + building levels/grid cells. Bump the version + handle migration
  when the shape changes.
- **Folder layout:**
  - Unity project: `1000t malchin/` — code in `Assets/Scripts/{Economy,Building,Combat}/`,
    editor tools in `Assets/Editor/`, data assets in `Assets/Data/`.
  - This vault: `md files/`.
- **Always commit Unity `.meta` files alongside their assets** (they carry the GUIDs
  that link references). Have the user **save the scene (Ctrl+S)** before committing
  scene/asset changes.

## Git / GitHub
- Remote (SSH, no token needed): `git@github.com:KRIMZ911/1000t-malchin.git`
- Branch: `master`. Workflow: commit each piece once verified; push.
- The Stage 1 commit is flagged "unverified checkpoint" — if testing reveals bugs,
  fix them in a follow-up commit.

## Where to look next
- `09 - Combat & Gacha Design.md` — the expanded combat + gacha design and the staged plan.
- `04 - TODO.md` — the live task list (combat stages are there).
- `05 - Decisions Log.md` — why things are the way they are.
- `06 - Open Questions.md` — gacha decisions still owed (currency model, pity) before Stage 2 finishes.
