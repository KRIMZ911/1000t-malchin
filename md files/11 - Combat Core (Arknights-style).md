# 11 — Combat Core (Arknights-style)

> The target design for the combat core, decided 2026-06-24: **full Arknights** —
> blocking, a DP deployment economy, designed enemy paths, high/low ground, and a
> **data-driven terrain system** where tile types carry effects. This supersedes the
> "straight-down lane" model of Stage 1; the gacha, ability, and AoE-shape systems we
> already built **plug into** this core unchanged.
>
> **Status: design only.** Nothing here is built yet. Build it in the phases at the
> bottom — and **verify the existing combat on the PC first**.

## The pillars (what makes it feel like Arknights)

### 1. The map is a grid of tiles, each with a terrain type
Placement is still grid-based (clean + readable). But each cell now has a **terrain**
that governs what can stand there, whether enemies can walk it, and what happens to units
on it. Tiles have an **elevation**: **Low ground** (melee) or **High ground** (ranged).

### 2. Deployment: high vs low ground + facing
- **Melee units** deploy only on **Low ground**. They **block** enemies.
- **Ranged units** deploy only on **High ground**. They don't block and can't normally be
  reached by ground enemies — they attack into their **range shape**.
- On deploy, the player **chooses a facing direction**; this orients the unit's range
  shape (cone/line/circle from the shape system). Reuses our `AreaShape` work.

### 3. Blocking (the core tactic)
- Each unit has a **`blockCount`** (0 = ranged/non-blocker; 1–3 = melee).
- An enemy walking the path gets **blocked** when it reaches a melee unit with a free block
  slot: it **stops and fights** instead of advancing. The unit holds up to `blockCount`
  enemies at once.
- Enemies have **`blockCost`** (usually 1; heavy enemies take 2 slots; some are
  **unblockable** and walk past everything). Kill the blocker → blocked enemies resume.

### 4. DP — the deployment economy (tempo)
- The battle has **Deployment Points**: `startDP`, `+dpPerSecond` over time, up to `maxDP`.
- Deploying a unit costs its **`deployCost`** (already on `CharacterDefinition`). Can't
  afford it → can't place it yet. Stronger units cost more → you sequence cheap blockers
  first, expensive carries later. This is the puzzle.
- **Retreat** a deployed unit → frees its tile + refunds part of the DP; **redeploy** after
  a cooldown. (Potential field: per-character redeploy time later.)

### 5. Designed paths + life points (objective)
- Enemies **spawn at red tiles** and follow a **designed path** (a list of waypoints) to a
  **blue goal tile** (the camp/herd). No more "straight down a column."
- Each enemy that reaches the goal **leaks**, costing **life points** (most cost 1; bosses
  more). **Life points hit 0 → defeat.** Replaces the old single "base HP".
- A map can have **multiple spawn points and paths**.

### 6. Skills + AoE shapes (already built — they slot in)
- Skills charge and fire (auto on condition / manual tap) — done.
- Attacks and skill areas resolve as **world-space shapes** (circle/cone/line) — done.
- On high/low ground with facing, ranged range shapes finally have a real tactical home.

## The terrain system (data-driven, expandable)

A **`TerrainDefinition`** (ScriptableObject) per tile type, so we can add terrains forever
as assets. Each carries:
- `id`, `displayName`, placeholder `color`
- **`elevation`**: Low / High
- **`deploy`**: None / MeleeOnly / RangedOnly / Both
- **`enemiesCanWalk`** (is it part of walkable path space?)
- **`moveMultiplier`** (speed for anything crossing it; <1 = slows)
- **`onTileEffect`**: an `EffectType` + magnitude + who it hits (Allies / Enemies / Both)
  — **reuses the status-effect system** we already built, so terrain "just works."

### Starter terrain palette (initial set — all expandable)
| Terrain | Elevation | Deploy | Walkable | Effect |
|---|---|---|---|---|
| **Steppe Grass** | Low | Melee | yes | none (default ground) |
| **Rocky Hill** (high ground) | High | Ranged | no | (optional) +range/atk to occupant |
| **River / Ravine** | — | None | no | impassable wall; shapes the path |
| **Marsh / Mud** | Low | Melee | yes | **Slow** enemies crossing |
| **Tall Reeds** | Low | Melee | yes | **conceal** — units on it can't be targeted at range until adjacent |
| **Ovoo Shrine** | Low | None | yes | **buff** allies on/near it (Heal or +ATK) — thematic |
| **Scorched Ground / Brazier** | Low | None | yes | **hazard** — damages whatever stands on it |
| **Sand Dune / Snow** | Low | Melee | yes | **Slow everyone** crossing (terrain drag) |

This gives immediate tactical texture: funnel enemies through mud to buy time, perch
archers on hills, deny tiles with hazards, hold a shrine for the buff, hide a blocker in
reeds. New terrains = new assets, no code.

## What this changes in the existing code
- **`LevelDefinition`** grows a **tile map** (terrain per cell), **paths** (waypoints),
  **spawn points**, **goal tiles**, **DP settings**, and **life points**. The current
  `spawns` timeline stays but spawns reference a spawn point.
- **`BattleGrid`** holds the tile map, renders terrain colors, and answers tile queries
  (terrain at cell, walkable, deployable-for, elevation).
- **`CombatUnit`** gains `blockCount` / block state, path-following for enemies (instead of
  straight-down), facing, and "on-tile effect" application.
- **`CombatUnitDefinition`** gains `blockCount`, `blockCost` (enemies), and a deployment
  layer (melee/ranged) — much of which maps from `CharacterRole`.
- **`BattleController`** gains the **DP economy**, **life points**, **blocking resolution**,
  **retreat/redeploy**, and path-based spawning. Base-HP logic is replaced by life points.
- **Existing ability + shape systems are reused as-is.**

## Build phases (incremental, each committable)
> Do **0** first. Then roughly in order; each phase is a separate commit for review.

0. **Verify current combat on the PC** (run the roster + combat setup, fix any errors).
   Don't start the refactor on unverified code.
1. ✅ **Terrain data model (DONE 2026-06-24)** — `TerrainDefinition` SO + a one-click
   palette generator (`Malchin > Create Terrain Palette`). Each terrain has: a **tile
   texture slot** (`tileSprite`, placeholder color until assigned), elevation, deploy rule,
   walkable flag, move multiplier, conceal flag, and a **selectable on-tile effect**
   (`onTileEffect` from the existing `EffectType` vocabulary + magnitude + who it affects)
   — so terrain recycles the ability/status-effect system. `LevelDefinition` also gained a
   **whole-map `background` sprite** and a `tiles[]` array (terrain per cell) ready for the
   Phase 2 map editor. Built the 8 starter terrains as the **map pool**.
2. ✅ **Tile map + map editor (DONE 2026-06-24)** — `BattleGrid` now renders a **whole-map
   background**, a **per-cell terrain layer** (tile textures, or placeholder colors), and
   grid lines, and answers terrain queries (`TerrainAt`, `CanWalk`, `CanDeploy`). A
   **`Malchin > Map Editor`** window lets you pick a level, choose a terrain brush from the
   palette, and **paint cells** to build the map (row 0 / base edge at the bottom), plus set
   the background and resize the grid. Deploy/path *enforcement* of terrain comes in later
   phases (P5/P6); for now terrain is rendered + queryable. `BattleController` configures the
   grid from the full level (`grid.Configure(level)`).
3. **Paths + life points** — waypoints, enemies follow the path, goal tiles, life-point
   loss/defeat. (Replaces straight-down + base HP.)
4. **DP economy** — DP tick, deploy costs DP, deploy validity (terrain + elevation + DP),
   retreat/redeploy. (Replaces fixed unit counts.)
5. **Blocking** — `blockCount`/`blockCost`, block resolution, enemies stop at blockers.
6. **High/low ground + facing on deploy** — restrict by terrain elevation; choose facing →
   orient the range shape.
7. **Terrain effects live** — apply `onTileEffect` to units standing on tiles (slow/hazard/
   buff/conceal), via the status-effect system.
8. **Level editor + telegraphs + verify** — author maps (tiles, paths, spawns) via a custom
   inspector/tool; draw range/AoE/path placeholders; play-test on PC.

## Open questions to settle as we build
- **Map authoring:** paint tiles in a custom editor window, or hand-edit a compact text grid
  in the inspector? (Lean: text-grid first, visual painter later.)
- **Block count by role:** defenders 3, horsemen 1–2, archers 0? (Lean: yes.)
- **Conceal/stealth rules** exact behavior (reeds) — full untargetable vs reduced range.
- **Retreat refund %** and redeploy cooldown values.
- **Do we want flying/unblockable enemies** in the first pass, or add later? (Lean: later.)
