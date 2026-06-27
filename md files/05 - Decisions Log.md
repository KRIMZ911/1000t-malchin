# 05 — Decisions Log

> Why we chose what we chose. Add a dated entry whenever a real decision is made, so future-you (and Claude Code) understand the reasoning.

## Engine: Unity 6 LTS (`6000.0.46f1`)
Stable long-term-support release, huge ecosystem, proven cross-platform export to Android + iOS, best tooling for this genre. LTS = supported for years, right call for a project meant to ship.

## Render pipeline: Universal 2D (URP)
URP is the modern, mobile-optimized pipeline. Chosen over Built-in (legacy) for performance on a wide range of phones, and over HDRP (console/PC-grade, too heavy for mobile). 2D variant matches an Arknights-style game.

## Dimension: 2D / 2.5D, not 3D
Reference game (Arknights) is fundamentally 2D — sprites, illustrated characters, UI screens. The "2.5D" feel comes from layered sprites, parallax, and skeletal animation, not 3D models. Starting from the 2D template gives correct defaults.

## Genre: single-player gacha (not PvP like Clash of Clans)
Chosen to keep costs low early and avoid the heavy always-online server requirement of competitive multiplayer. Entertainment comes from collection + gacha + lane combat, like Arknights.

## Backend: deferred, managed service later (likely PlayFab)
Early offline vertical slice uses local save only — no hosting cost. A backend is required before real-money gacha ships, because purchases and pull rates must be server-validated to prevent local-file cheating. Managed services (PlayFab/Firebase) have free tiers that scale with revenue, matching the "reinvest once earning" plan.

## Dev OS: Windows
All dev + Android builds happen on Windows. A Mac (owned or cloud) is only needed for the final iOS/App Store step, which Apple mandates. iOS is deferred to after Android.

## Art: sourced from artists / AI image tools, NOT from code (three.js rejected)
three.js was considered for assets and rejected. It is a JavaScript web-3D rendering library — not an art generator, not part of Unity, not for 2D. It cannot produce Arknights-style character art; nothing in code can. Character art is the biggest cost/quality driver of a gacha game and comes from commissioned artists, asset packs, or AI image generators. Claude Code builds the systems that use the art but does not create art. Plan: placeholders now, commissioned art for key heroes once the game is proven fun.

## Process: vertical slice first
Three big systems (herding, building, combat) is ambitious. We build a minimal connected version of all three before deepening any one, to avoid building systems that don't fit together.

## Economy: timestamp-based growth with offline/idle progression (2026-06-22)
The herd grows on a real-time rate (`baseGrowthPerMinute` per livestock type), computed from elapsed wall-clock time — both continuously in-session and in one batch on load for the time the app was closed. Chosen over a simple in-session-only tick because idle generation / "come back → herd has grown" is the core return-to-game hook of the genre (see `01 - Game Design`). Growth is always clamped to each type's cap, so being away longer never exceeds the cap. Decided to build this in Phase 1 rather than defer to polish, to avoid reworking the save format after gers/heroes add more save data.

## Base building: fixed 20x20 grid with footprints + free movement (2026-06-24)
Gers are placed on a fixed 20x20 cell grid (cellSize 0.5 world units, centered on the origin). Each ger has a footprint in cells (Main 4x4, Herding/Ovoo 3x3). The player drags gers freely; they snap to cells and cannot overlap (invalid placement tints red and reverts). Saved as the bottom-left "origin cell" per ger (save v3), not raw world position, so layouts are grid-native and survive cellSize/camera changes. Chosen over free-form placement so the base reads as a deliberate, Clash-of-Clans-style camp and so footprints/space become a design lever later. Camera auto-frames the whole grid (no pan/zoom yet — deferred until the base outgrows one screen).

## Combat: grid battles + data-driven levels + characters-as-gacha (2026-06-24)
Combat moved from a single lane to a **grid** (deploy units on cells; enemies enter the
top and march to the base row at the bottom). Levels are **data**: a `LevelDefinition`
asset holds grid size + a spawn timeline (enemy/time/column) + rewards, authored via a
custom inspector (and `Malchin > Create Battle Level`). Every unit is an **individual
gacha character** the player collects from drops/cases; before a battle the player
**selects a squad** from their owned roster, each character usable **once per battle**.
Building in three stages: (1) grid combat + level editor with a fixed test squad
[done, unverified], (2) gacha + owned roster, (3) squad selection. Chosen for an
Arknights-style grid feel, fast/version-controlled level authoring, and to make the
gacha collection the spine of combat. The lane version was committed first as a safety
checkpoint. Full design in `09 - Combat & Gacha Design.md`.

## Gacha: Arknights-style, 6 tiers + potential + soft/hard pity (2026-06-24)
The Stage 2 gacha ruleset, decided with the user. **Two-tier currency:** *Special Horses*
are the earned premium (battles/idle); a separate **paid** currency (*Sky-Blue Khadag*,
name cosmetic) sits on top — split so F2P and payer economies tune independently, chosen
over a single shared currency for that flexibility. **6 rarity tiers (1★–6★), all in the
pull pool** (player wanted full RNG over an Arknights-style top-tiers-only pool), rates
6★ 2% / 5★ 8% / 4★ 30% / 3★ 30% / 2★ 20% / 1★ 10%. **Soft + hard pity** (6★ +2%/pull
after pull 50, hard cap ~99, resets on 6★; every 10-pull guarantees a 5★+) — chosen over
no-pity/hard-only because modern players expect it and it aids retention/fairness.
**Duplicates raise a character's Potential 1→6** (stat boosts + deploy-cost reductions;
overflow past max → upgrade token) — chosen over shards/refund to make collection a
power-chase that feeds the existing Hero Upgrade Ger. Single + 10-pull, one standard pool
for Stage 2 (event banners deferred to Phase 6). **Real drop rates shown on the summon
screen** (legal requirement in several markets). All values live in ScriptableObjects so
they stay tunable. Full spec in `09 - Combat & Gacha Design.md`.

## Characters: 12-unit starter roster + Talent/Skill ability model (2026-06-24)
Built the Stage 2 collectible roster as **data, not art**: 12 `CharacterDefinition`
assets across all six rarities, each with a `CombatUnitDefinition` stat block and **two
abilities** — a passive **Talent** and a charging **Skill** — drawn from a fixed
**effect vocabulary** (DamageBoost, Heal/HoT, Shield, AoeDamage, Slow, Stun, Taunt,
MultiShot, ArmorPierce, DamageReduction, AttackSpeedBoost) with magnitude/duration/
radius/target. Generated by a one-click tool (`Malchin > Create Starter Roster`,
`RosterBuilder.cs`) so it stays tunable + version-controlled, matching the project's
data-driven + editor-tool conventions. **Deliberately did NOT wire ability *execution*
into the live combat** (`CombatUnit`/`BattleController`) this pass — Stage 1 combat is
still unverified, so running effects blind risks destabilizing it; runtime is the next,
PC-testable step. Full roster + effect table in `10 - Characters & Abilities.md`.

## Combat: runtime ability runner + status-effect system, manual & auto skills (2026-06-24)
Built the runtime that makes character abilities actually fire in battle. Each unit has a
passive **Talent** and a charging **Skill** (visible charge bar). **Activation is mixed,
per the user:** some skills are **Manual** (player taps the unit → a HUD "Use skill" button
appears, enabled only when charged — for ultimates), others **Auto** with a condition
(`WhenCharged`, `EnemyInRange` = "enemy in front", or `AllyWounded`). Effects resolve
through `BattleController.ResolveAbility` against a small **status-effect system** on
`CombatUnit` (damage buffs, attack-speed, damage-reduction, shields with an absorb pool,
stun, slow, heal-over-time, taunt aggro, multishot); effects refresh rather than stack to
keep it bounded. Abilities support an optional **secondary effect** (so e.g. AoE+stun,
shield+taunt work). **Additive + guarded:** units spawned from a bare `CombatUnitDefinition`
(enemies, the old test squad) carry no abilities and behave exactly as before, so the
unverified Stage 1 combat isn't changed for them. `Setup Combat Scene` now deploys roster
characters (Khulan/Sukhbaatar) as the test squad when the roster exists, to demo abilities.
Still needs a play-test on the PC. Detail in `10 - Characters & Abilities.md`.

## Combat areas: grid for placement, continuous world-space shapes for hits (2026-06-24)
Formalized the split the user asked for: **placement uses the grid** (units snap to cells,
Arknights-style — purely to make deployment clean), while **attacks and areas of effect are
resolved as continuous world-space shapes** (circle / cone / line), because enemies move
smoothly and snapping hit-detection to tiles would feel wrong. **Phase 1 (foundation)
built:** an `AreaShape` type (`AoeShape` Circle/Cone/Line + `ShapeAnchor` Caster/TargetEnemy
+ `ShapeDirection` Forward/TowardNearestEnemy + radius/coneAngle/lineWidth) in `Ability.cs`,
and a single resolver `BattleController.GatherUnitsInShape` doing the circle/cone/line math
(replacing the old circle-only `EnemiesAroundPoint`). "Forward" = toward the foe (players
face up the grid, enemies down). It is **behavior-preserving**: abilities without a custom
shape fall back to a circle of their old `radius`, so existing skills resolve identically;
new shapes are now possible. Decisions taken (defaults, since the structured answers didn't
arrive): shapes = Circle+Cone+Line (no grid-box); aim = Forward by default; attack reach to
become per-unit shapes (Phase 2); enemies stay in-lane for now (pathing later). Remaining
phases: 2 attack-reach shapes, 3 per-character shapes, 4 visual telegraphs, 5 verify.
Full system in `10 - Characters & Abilities.md`.

## Save format: versioned JSON, local-first (2026-06-22)
Save is a single JSON file in `Application.persistentDataPath` carrying a `version` field and a `lastSavedUnixSeconds` timestamp. The version field exists so saves can be migrated when the schema grows and when we move local → backend (Phase 6). Partially resolves the "save data format and migration strategy" open question.
