# 09 — Combat & Gacha Design

> The expanded combat + collection design. `01 - Game Design.md` holds the original
> concept; this file is the detailed system design we are actually building, decided
> with the user on **2026-06-24**.

## The vision (what the user asked for)
1. **Battles happen on a grid** (like the base-building grid, but for combat).
2. **Levels are data + a level creator.** Each level defines the **grid size** and a
   **spawn schedule** (which enemies appear, when, and where). Editable by both the
   user (Unity Inspector) and Claude (code). Industry-standard, data-driven.
3. **Every unit is an individual gacha character.** Characters are **collectibles**
   obtained from **drops / opening cases** (a normal gacha). The player owns a roster.
4. **Pre-battle squad selection.** Before a fight, the player **chooses which owned
   characters to bring**. Each character can be used **only once per battle**.

## How combat works (grid)
- The battlefield is a grid (e.g. 6 wide × 8 tall). **Row 0 (bottom) is the player's
  base edge**; enemies enter above the **top** row and march **down** toward the base.
- The player **deploys units onto empty cells** (one unit per cell).
- Unit behaviors (from `CombatUnitDefinition`):
  - **Hold** (archer) — stays on its cell, attacks foes within range (ranged).
  - **Advance** (horseman) — walks up its column toward enemies, melee.
- Enemies **advance** down toward the base; if one reaches the base edge it damages
  Base HP and despawns.
- **Win:** all scheduled enemies defeated. **Lose:** Base HP hits 0.
- **Win reward:** livestock (sheep + cattle + a **special horse**, which is the gacha
  currency) — this is the loop closing back into the economy.

## Levels as data (the level creator)
- `LevelDefinition` (ScriptableObject) holds: `gridWidth`, `gridHeight`, `cellSize`,
  `baseMaxHP`, a **spawn timeline** (`EnemySpawn { enemy, time, column }`), and the
  win reward.
- Authoring: a **custom inspector** (`LevelDefinitionEditor`) with an editable spawn
  list (add / remove / sort by time) and a preview. New levels via
  `Malchin > Create Battle Level`.
- This keeps level design fast, version-controlled, and editable by both of us.
- **Future option (not built):** a fully visual Level Editor window (paint grid,
  drag spawns on a timeline). We chose the asset + custom-inspector route first.

## Characters as gacha collectibles
- A **`CharacterDefinition`** (Stage 2) describes a collectible unit: identity,
  **rarity**, portrait/placeholder, and its combat stat block (`CombatUnitDefinition`).
- A **player roster** (saved) lists which characters the player **owns**.
- A **gacha / case-opening** grants a random character by rarity (drop rates in data).
- This is the monetization core: special horses (premium currency) → summons.

## Pre-battle squad selection
- Before a battle, a **squad screen** lets the player pick characters from their
  owned roster (limited slots).
- During the battle, each chosen character can be **deployed once** (consumed for
  that battle), then it returns to the roster afterward.

## The staged build plan
- **Stage 1 — grid combat + level editor.** ✅ coded (unverified). Battles on a grid,
  `LevelDefinition` assets + custom inspector, **fixed archer/horseman test squad**.
- **Stage 2 — gacha + owned roster.** Characters become collectibles; summon/open
  cases; roster persisted in the save.
- **Stage 3 — squad selection.** Pick owned characters pre-battle; one-use each;
  feed them into the grid battle in place of the test squad.

## Current placeholder units (Stage 1)
| Unit | Side | Behavior | Notes |
|---|---|---|---|
| Archer | player | Hold (ranged) | place mid-grid, covers cells in range |
| Horseman | player | Advance (melee) | tanky frontline, charges up |
| Raider | enemy | Advance | marches down to the base |

All are placeholder colored circles with health bars — **no real art yet** (per the
placeholder philosophy in `07 - Build Plan & Placeholder Assets.md`).

## Decisions still owed (before Stage 2 ships) — see `06 - Open Questions.md`
- Are **special horses** the paid currency, or a layer above them?
- **Pity system?** (guaranteed rare after N pulls — standard in modern gacha.)
- Free-to-play generosity (how many free early summons), and per-region drop-rate
  disclosure (legally required in several markets).
