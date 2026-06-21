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

## NEXT — Phase 1: Economy skeleton
- [x] Create `Scripts/Economy/` folder
- [x] Define `LivestockType` (sheep, cattle, special horses) data
- [x] Create a herd manager that stores current counts
- [x] Implement passive growth over time (a tick system)
- [x] Implement local save/load (e.g. JSON to disk)
- [x] Build a simple on-screen UI showing live counts

## LATER — captured so we don't forget
- [ ] Ger data model + upgrade system (Phase 2)
- [ ] Minimal lane combat (Phase 3)
- [ ] Minimal gacha summon (Phase 4)
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
