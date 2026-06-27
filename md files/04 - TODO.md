# 04 — TODO

> Live task list. Work top-down. `- [ ]` = not done, `- [x]` = done. Claude Code: implement the next unchecked task in the current phase, then check it off.

## NOW — Phase 0: Setup
- [x] Install Unity Hub + Unity 6 (`6000.0.46f1`)
- [x] Create Unity project (Universal 2D)
- [x] Install Obsidian + create vault
- [x] Put this vault inside `claude game/md files/` (vault is in `md files/`, readable by Claude Code)
- [x] Initialize Git in the `claude game` root
- [x] Add a Unity-specific `.gitignore` (ignore `Library/`, `Temp/`, `Obj/`, `Build/`, `Logs/`, `.vs/`)
- [x] Make first commit
- [x] Point Claude Code at the `claude game` root and confirm it can read this vault
- [x] Do a throwaway Android build to confirm the build pipeline works on a device/emulator

## DONE — Phase 1: Economy skeleton
- [x] Create `Scripts/Economy/` folder
- [x] Define `LivestockType` (sheep, cattle, special horses) data
- [x] Create a herd manager that stores current counts
- [x] Implement passive growth over time (timestamp-based, incl. offline growth)
- [x] Implement local save/load (versioned JSON to disk)
- [x] Build a simple on-screen UI showing live counts

## DONE — Phase 2: Base building (gers)
- [x] `GerDefinition` data model: footprint, economy effect, multi-level upgrade path
- [x] 3 gers placed: Main Ger (4x4), Herding Ger (3x3), Ovoo/Shrine (3x3)
- [x] Upgrade a ger by spending livestock; effect changes (Herding=growth, Main=cap)
- [x] 20x20 build grid: snap-to-grid drag, footprints, overlap prevention, grid visual
- [x] Building levels + grid positions persist in save (v3)
- [x] One-click `Malchin > Setup Building Scene` editor tool

## DONE — Phase 3 (checkpoint): lane combat slice
- [x] `CombatUnitDefinition` data: HP, damage, attack rate, range, hold-vs-advance
- [x] Deploy 2 unit types: archer (holds position), horseman (advances)
- [x] Enemies spawn and move; units fight; win/lose condition
- [x] Battle reward grants livestock → loops back to the economy
- [x] One-click `Malchin > Setup Combat Scene` editor tool
- [x] (Committed as a safety checkpoint before the grid refactor)

## NOW — Phase 3 Stage 1: grid combat + level editor  ⏳ NEEDS VERIFYING
- [x] Refactor battle onto a grid (`BattleGrid`: cell↔world, deploy collider, visual)
- [x] `LevelDefinition` data: grid size + spawn timeline (enemy/time/column) + rewards
- [x] `BattleController` loads a level, runs the spawn schedule, deploys onto cells
- [x] Custom inspector to author levels (`LevelDefinitionEditor`) + `Create Battle Level`
- [ ] **VERIFY in editor:** run `Malchin > Setup Combat Scene`, play the grid battle,
      confirm deploy/win/lose + the Level_01 custom inspector work (fix any errors)

## NEXT — Phase 3 Stage 2: gacha + owned roster
> Ruleset locked — see "Gacha system spec — DECIDED 2026-06-24" in `09`.
- [x] Resolve gacha decisions (currency model, rarity tiers, pity, dupes, rates) — done 2026-06-24
- [x] Ability/effect data model (`Ability.cs`: Talent/Skill, 12 effect types, targets) — 2026-06-24
- [x] `CharacterDefinition` data: id, displayName, rarity (1–6), `CombatUnitDefinition` ref, base deploy cost, abilities, Potential boost table — 2026-06-24
- [x] Starter roster of 12 characters + abilities, one-click `Malchin > Create Starter Roster` (`RosterBuilder.cs`) — 2026-06-24 (see `10`)
- [x] **Runtime: abilities fire in battle** — ability-runner on `CombatUnit` (charge bar, auto/manual fire) + status-effect system (buff/shield/stun/slow/HoT/taunt/multishot) + `ResolveAbility` — 2026-06-24
- [x] Manual vs auto activation: manual = tap unit → HUD "Use skill" button; auto = on charge / enemy-in-range / ally-wounded — 2026-06-24
- [ ] **VERIFY on PC:** run `Create Starter Roster` → `Setup Combat Scene`, play; confirm charge bars, auto-fire, and manual skill button work

### Area-shape system (grid = placement; attacks/AoE = world-space shapes)
- [x] Phase 1: `AreaShape` model (circle/cone/line) + `GatherUnitsInShape` resolver (behavior-preserving) — 2026-06-24
- [x] Phase 3: per-character skill shapes (cones/lines/circles) in `RosterBuilder` — 2026-06-24
- [ ] Phase 2: per-unit **attack-reach** shapes (`rangeShape` on `CombatUnitDefinition` + targeting uses it)
- [ ] Phase 4: visual telegraphs (draw range + AoE shapes as placeholders)
- [ ] Phase 5: verify shapes on PC
- [ ] `GachaPool` data: characters per tier + rate table (6★2/5★8/4★30/3★30/2★20/1★10) + pity config
- [ ] Two-tier currency: Special Horses (earned) + Sky-Blue Khadag (paid); balances in save
- [ ] Player roster persisted in save: `characterId → { count, potential }` + `pityCounter` (bump save version + migrate)
- [ ] `GachaService`: roll tier w/ soft+hard pity (reset on 6★, 10-pull guarantees 5★+); add to roster OR raise Potential; overflow → upgrade token
- [ ] Single + 10-pull actions; summon UI shows real drop rates (disclosure)
- [ ] First-launch guaranteed 10-pull hook

## THEN — Phase 3 Stage 3: pre-battle squad selection
- [ ] Squad screen: pick owned characters to bring (limited slots)
- [ ] Each character deployable once per battle; feeds the grid battle (replaces test squad)

## LATER — captured so we don't forget
- [ ] Wire the full loop together (Phase 5)
- [ ] Event banners + foreign fighters (Phase 6)
- [ ] Backend (PlayFab/Firebase) before any real money (Phase 6)
- [ ] IAP + drop-rate disclosure compliance (Phase 6)
- [ ] Android store submission (Phase 6)
- [ ] iOS build via Mac/cloud (Phase 6)

## Parking lot (ideas, not scheduled)
- [ ] Goats / yaks / camels as extra livestock
- [ ] Seasons/weather affecting herd growth
- [ ] Clan/social features (would require more backend — keep single-player for now)
