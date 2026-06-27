# Mobile Work — session log

> A running log of work done from **mobile Claude Code sessions** on the branch
> **`claude/mobile-claude-work-r7pby5`** (kept off `master` for review). Phone/web
> sessions can read the vault, plan, and write/edit C# — but **cannot run Unity**, so
> everything here still needs a play-test on the PC before it's considered verified.
>
> Started: **2026-06-24**.

## Branch & git
- All work is committed to **`claude/mobile-claude-work-r7pby5`** and pushed to GitHub.
- **`master` is untouched.**
- Commits so far (newest first):
  - `b6d0f43` — runtime ability runner + status-effect system (manual & auto skills)
  - `3074a46` — Stage 2 starter roster: 12 characters with abilities + effects
  - `e03421a` — lock Stage 2 gacha design (currency, rarity, pity, potential, rates)

---

## 1. Locked the Stage 2 gacha design
Worked through the open gacha questions and recorded the decisions in the vault.
- **Currency — two-tier:** *Special Horses* (earned via battles/idle) + a paid currency
  on top (*Sky-Blue Khadag*, name tweakable), tuned independently.
- **Rarity — 6 tiers (1★–6★), all pullable.** Rates: 6★ 2% / 5★ 8% / 4★ 30% / 3★ 30% /
  2★ 20% / 1★ 10%.
- **Pity — soft + hard:** 6★ rate climbs +2%/pull after pull 50, hard cap ~99, resets on
  6★; every 10-pull guarantees a 5★+.
- **Duplicates — Potential system (copies = power):** Pot 1→6, milestones alternate
  +HP / −deploy-cost / +ATK; overflow past max → upgrade token.
- **Format:** single + 10-pull, one standard pool for Stage 2; event banners later.
- **Disclosure:** real per-tier % shown on the summon screen.
- Written up in `09 - Combat & Gacha Design.md`; rationale in `05 - Decisions Log.md`;
  resolved items checked off in `06 - Open Questions.md`.

## 2. Built the starter roster (12 characters + abilities)
Created the collectible characters as **data** (no art — placeholder colored circles).
- **Data model:** `Ability.cs` (Talent/Skill + a 12-entry effect vocabulary + targets)
  and `CharacterDefinition.cs` (identity, rarity, role, combat stat block, abilities,
  Potential table).
- **One-click generator:** `RosterBuilder.cs` → menu **`Malchin > Create Starter Roster`**
  builds all 12 characters + their stat blocks + abilities + Potential tables as assets
  under `Assets/Data/Characters/`.
- **The roster:** 6★ Naranbaatar, 6★ Sarangerel · 5★ Khulan, 5★ Ganbaatar · 4★ Tamir,
  Sukhbaatar, Oyun · 3★ Chuluun, Bataar, Naran · 2★ Erdene · 1★ Otgon (mascot homage).
- **Effect vocabulary:** DamageBoost, AttackSpeedBoost, Heal, HealOverTime, Shield,
  DamageReduction, AoeDamage, Slow, Stun, ArmorPierce, Taunt, MultiShot.
- Full roster + per-character abilities in `10 - Characters & Abilities.md`.

## 3. Built the runtime ability runner + status-effect system
Made the abilities actually work in battle.
- **Status-effect system** on `CombatUnit`: buffs (damage/attack-speed), damage-reduction,
  **shields** (absorb pool), **stun**, **slow**, **heal-over-time**, **taunt** (pulls
  enemy aggro), **multishot**. Effects refresh rather than stack (bounded).
- **Ability runner:** each skill **charges over time** with a visible charge bar
  (blue → **gold when ready**), resolved via `BattleController.ResolveAbility` (gathers
  targets, applies primary + optional secondary effect).
- **Manual vs auto activation (per the design ask):**
  - **Manual** — tap the unit → HUD **"Use: {skill}"** button, enabled only when charged.
    (Naranbaatar, Sarangerel, Ganbaatar.)
  - **Auto** — fires itself when charged, gated by a condition: `WhenCharged`,
    `EnemyInRange` ("enemy in front"), or `AllyWounded`.
- **Manual UI** wired in `BattleHUD.cs` + `CombatSceneSetup.cs`.
- **Guarded/additive:** units spawned from a bare `CombatUnitDefinition` (enemies, the old
  test squad) have no abilities and behave exactly as before, so the unverified Stage 1
  combat isn't changed for them. `Setup Combat Scene` deploys Khulan + Sukhbaatar as the
  test squad **if the roster exists**, to demo abilities.
- Files: `Ability.cs`, `CombatUnit.cs`, `BattleController.cs`, `BattleHUD.cs`,
  `CombatSceneSetup.cs`, `RosterBuilder.cs`.

## 4. Area-shape system — Phase 1 (foundation)
Started formalizing combat areas: **grid = placement only; attacks/AoE = continuous
world-space shapes** (circle / cone / line), since enemies move smoothly.
- Added `AreaShape` + `AoeShape` (Circle/Cone/Line), `ShapeAnchor`, `ShapeDirection` to
  `Ability.cs`.
- Added one resolver, `BattleController.GatherUnitsInShape` (circle/cone/line math),
  replacing the old circle-only `EnemiesAroundPoint`.
- **Behavior-preserving:** abilities without a custom shape fall back to a circle of their
  old radius, so nothing changes yet; cones/lines are now possible.
- Decisions (defaults, structured answers didn't arrive): Circle+Cone+Line; aim Forward by
  default; per-unit attack-reach shapes coming in Phase 2; enemies stay in-lane for now.
- **Next phases:** 2 attack-reach shapes · 3 per-character shapes · 4 visual telegraphs ·
  5 verify on PC.

## 5. Area-shape system — Phase 3 (per-character skill shapes)
Assigned each skill a real world-space shape using the Phase 1 system (`RosterBuilder.cs`,
via a new `WithShape` helper):
- **Cones (forward):** Sunlit Charge (Naranbaatar, 90°), Cleave (Sukhbaatar, 120° sweep).
- **Line (up the lane):** Piercing Shot (Tamir) — **changed from single-target to a piercing
  line** that hits every enemy in its path (first real Line showcase).
- **Circles:** Arrow Storm (on the enemy cluster), Tengri's Blessing + Shieldwall (around the
  caster).
- Healing Brew stays single-target; talent auras stay circles.
- **Why these:** each shape matches the ability's fantasy (a charge/sweep = cone, a piercing
  arrow = line, rained arrows / protective auras = circle). All shapes are data on the
  ability, tunable later. Table in `10 - Characters & Abilities.md`.
- **Note:** this is the skill-AoE half of Phase 3. Per-unit **attack-reach** shapes need
  Phase 2 (a `rangeShape` field + a targeting change) — **not done yet**, flagged for next.

## 6. Combat core direction — full Arknights (design doc)
After a review/pause, the user chose to make combat a **faithful Arknights tower-defense**:
blocking, a DP deployment economy, designed enemy paths, high/low ground, and a
**data-driven terrain system** (each tile type has an effect — mud slows, hills boost,
shrine buffs, hazards damage, reeds conceal, rivers block). Wrote the full design (no code
yet) to **`11 - Combat Core (Arknights-style).md`**, including how the existing gacha /
ability / AoE-shape systems plug in unchanged, what existing code it changes, an 8-phase
build plan, and open questions. **Key call:** verify the current (unverified) combat on the
PC **before** starting this rebuild. Why design-first: this replaces the heart of combat
(lanes→paths, base-HP→life points, fixed-counts→DP), so a reviewed plan beats blind code.

## 7. Combat core — Phase 1: terrain palette (map pool)
Built the reusable terrain building blocks the user asked for:
- `TerrainDefinition` SO with a **tile texture slot** (`tileSprite`, color fallback), rules
  (elevation, deploy, walkable, move multiplier, conceal), and a **selectable on-tile
  effect** (`onTileEffect` from the existing `EffectType` list + magnitude + who it affects)
  — so terrain reuses the ability/status-effect system.
- `Malchin > Create Terrain Palette` (`TerrainPaletteBuilder.cs`) generates 8 starter
  terrains (grass, hill/high-ground, river, mud, reeds, ovoo shrine, scorched, sand/snow).
- `LevelDefinition` gained a whole-map **`background`** sprite + a **`tiles[]`** terrain-per-
  cell array (+ `TileAt`) ready for the Phase 2 map editor — so future maps are "drop tiles
  on a grid + set a background."
- **Why:** matches the request — easy map-building from tiles, per-tile textures, a big
  background, and freely-chosen terrain effects that recycle what we built.

## 8. Combat core — Phase 2: tile map render + map editor
Made maps real and easy to author:
- **`BattleGrid`** now renders a **whole-map background**, a **per-cell terrain layer** (uses
  each terrain's tile texture, or its placeholder color), and grid lines. Added terrain
  queries (`TerrainAt`, `CanWalk`, `CanDeploy`) for later phases. `BattleController` now
  configures the grid from the whole level (`grid.Configure(level)`).
- **`Malchin > Map Editor`** (`MapEditorWindow.cs`): pick a level, pick a terrain brush from
  the palette, **paint cells** to build the map (base row at the bottom), set the background,
  resize the grid, fill-all, save. This is the "easily create maps by adding tiles" tool.
- **Not yet enforced:** terrain deploy/path rules (which unit can stand where, walls blocking
  enemies) — that's phases P5/P6. For now terrain is rendered + queryable, so the demo still
  works as before.

---

## What still needs the PC (not done from mobile)
- **Compile + play-test.** The C# is written carefully but **not compile-tested** (no Unity
  on mobile). If the Console shows errors, paste them and they can be fixed quickly.
- **Generate the assets:** run `Malchin > Create Starter Roster`, then
  `Malchin > Setup Combat Scene`, press Play, and confirm: charge bars fill, auto-skills
  fire, and tapping a manual unit shows the "Use skill" button.
- **`.meta` files** for the new scripts/assets are generated by Unity on import (can't be
  created from mobile) — commit them on the PC alongside the assets.

## Suggested next steps
- Verify the above on the PC.
- **Stage 2 gacha implementation:** `GachaPool` + summon logic (rates + pity) that grants/
  levels these characters, two-currency balances, owned roster persisted in the save.
- Then **Stage 3:** pre-battle squad selection feeding the grid battle.
