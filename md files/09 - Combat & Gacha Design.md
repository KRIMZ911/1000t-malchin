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

## Gacha system spec — DECIDED 2026-06-24

The full ruleset for Stage 2. Modeled on Arknights (closest shipped reference).
All numbers live in **data** (ScriptableObjects), so they are tunable later — these
are the launch defaults, not hard-coded constants.

### Currency model — two-tier
- **Special Horses** — the *earned* premium currency. Comes from battle rewards and
  idle/offline generation (per the economy pillar). This is what most players spend.
- **Sky-Blue Khadag** — the *paid* currency on top (name is cosmetic, tweakable). Bought
  with real money (Phase 6); converts into summon currency. Kept separate from Special
  Horses so the F2P economy and the payer economy can be tuned independently.
- Both ultimately resolve to a **summon** action; the split only governs the source.

### Rarity tiers — 6 tiers (1★–6★), all pullable
| Tier | Name | Base pull rate |
|---|---|---|
| 6★ | Legendary | 2% |
| 5★ | Epic | 8% |
| 4★ | Rare | 30% |
| 3★ | Common | 30% |
| 2★ | — | 20% |
| 1★ | — | 10% |
Rates sum to 100%. 1★/2★ are intentionally in the pool (player chose full 6-tier RNG).

### Pity — soft + hard
- **Soft pity:** 6★ rate holds at 2% through pull 50, then **+2% per pull** until a 6★
  is obtained. Average 6★ lands ~pull 35 effective.
- **Hard pity:** 6★ guaranteed by ~pull 99.
- **Reset:** the pity counter resets to 0 on any 6★.
- **10-pull guarantee:** every 10-pull yields **at least one 5★ or higher**.
- Pity counter is **persisted in the save** (survives app close).

### Duplicates — Potential system (copies = power)
- Each character has a **Potential** level **1→6** (i.e. up to **5 duplicates** absorbed).
- Milestones alternate between **stat boosts** (HP / ATK) and **deploy-cost reduction**:
  - Pot 2: +HP   · Pot 3: −1 deploy cost   · Pot 4: +ATK
  - Pot 5: −1 deploy cost   · Pot 6: +HP & +ATK
  (exact values tuned in data per rarity)
- **Overflow:** a duplicate of an already-maxed (Pot 6) character converts to a
  **generic upgrade token** (feeds the Hero Upgrade Ger).

### Pull format
- **Single pull** and **10-pull** (10-pull carries the 5★+ guarantee).
- **Stage 2:** one **standard pool** containing the launch roster.
- **Phase 6:** rate-up / limited **event banners** + foreign fighters (see `01`).

### Free-to-play generosity (recommended, tweakable)
- **First-launch guaranteed 10-pull** as the hook (uses the 5★+ guarantee).
- Steady **idle + battle drip** of Special Horses so an early player can afford roughly
  one 10-pull every few days. Tune once the economy numbers are live.

### Drop-rate disclosure
- Show the **real per-tier %** on the banner/summon screen. Legally required in several
  markets (CN/KR/JP) and good-faith everywhere. Build it into the summon UI from day one.

### Data-model implications for Stage 2 (guides the code)
- **`CharacterDefinition`** (ScriptableObject): id, displayName, rarity (1–6),
  reference to a `CombatUnitDefinition` (its combat stat block), base deploy cost,
  and a **Potential boost table** (per-milestone HP/ATK/cost deltas).
- **`GachaPool` / banner** (ScriptableObject): list of characters per tier + the rate
  table + pity config (soft start, step, hard cap, 10-pull guarantee tier).
- **Save data:** owned roster = map of `characterId → { count, potential }`; current
  `pityCounter`; the two currency balances. Bump the save version + migrate.
- **`GachaService`:** rolls a tier (rate table + current pity), picks a character in
  that tier, applies pity reset / 10-pull guarantee, then either adds the character to
  the roster or raises its Potential (or grants a token on overflow).
