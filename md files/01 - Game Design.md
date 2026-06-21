# 01 — Game Design

## Premise
You are a young clan leader on the Mongolian steppe. Build your camp of gers, grow your herd, collect warriors, and defend and expand your tribe. Your wealth is your livestock — just like real steppe life.

## Pillar 1 — Economy (livestock = resources)
Livestock replaces the usual gold/wood/ore. Herds grow passively over time (idle generation) and through player action, which drives the core return-to-game loop.

| Livestock | Role | Equivalent to | Notes |
|-----------|------|---------------|-------|
| Sheep | Basic resource | Gold / food | Common, breeds fast |
| Cattle (cows) | Mid resource | Premium build material | Slower, higher value |
| Special horses | Rare / premium | Gacha currency | Legendary bloodlines, ties to summoning |
| Goats / Yaks / Camels | Later additions | Variety resources | Yaks = cold-region upgrades, camels = trade/range |

Loop: come back → herd has grown → spend herd on ger upgrades & summons → fight battles → earn more.

## Pillar 2 — Base building (the gers)
Each ger has a job and a multi-level upgrade path. Upgrades cost livestock.

- **Main Ger** — the "town hall"; gates overall progression level.
- **Herding Gers** — raise livestock capacity and breeding rate.
- **Warrior Ger / Training Grounds** — unlock and level fighters.
- **Storage Gers** — hold livestock-resources.
- **Ovoo (Shrine)** — sacred cairn; the gacha/summoning + event hub.

## Pillar 3 — Combat (Arknights-style lane battles)
- Deploy units onto a battlefield. Some **hold position** (defenders/archers), some **advance and fight** (our twist on Arknights, which is mostly stationary).
- Unit archetypes: **horse archers** (iconic, signature unit), melee horsemen, spearmen/defenders, support.
- Battles cost and reward livestock, feeding back into the base loop.

## Pillar 4 — Gacha (monetization core)
- Summon fighters at the **Ovoo** using special horses / summoning currency.
- Heroes are named, have abilities and rarity tiers, and are collectible.
- **Event banners** introduce **foreign fighters** as limited-time rares — a Chinese general, Persian cavalryman, Tibetan monk-fighter, Russian Cossack, etc. Historically justified by the Mongol empire absorbing fighters from everywhere. This gives a lore-friendly, endless reason to release new banners → sustains revenue.

## Campaign & story framing
- A main **campaign with a "take over the world" arc**, but framed so the player character is the **sympathetic hero**: each enemy is defeated because they did something wrong — mistreating their people, acting unfairly, cruelty, etc. The player is forced into righteous conquest, not villainy. This keeps the protagonist in a good light while still allowing expansion gameplay.
- **Difficulty ramps as the campaign progresses.** Late-game and **end-boss fights are the hardest** and effectively require a **maxed-out roster** — giving long-term progression and gacha purpose.

## Economy rules (decided)
- **Consumed livestock/resources are gone forever** (permanent sink).
- Players regenerate resources via: **base/ger passive generation**, **drops from clearing story missions**, and **random chests**.
- This creates healthy demand for the resource loop without being recoverable on a whim.

## Full ger / building list
Confirmed by player:
- **Main Ger** — town hall; gates overall progression.
- **Armoury Ger** — gear/equipment for heroes.
- **Hero Upgrade Ger** — level and enhance heroes.
- **Hero Calling Shrine (Ovoo)** — the gacha summon hub.
- **Blessing-Granting Areas** — buffs/blessings (event + progression hub).

Suggested additions (from similar games — not locked, pick later):
- **Storage Ger** — raises resource caps.
- **Market / Trade Ger** — convert between livestock types or trade for currency.
- **Tavern / Gathering Ger** — daily quests, idle hero assignments.
- **Wall / Watchtower** — base-defense fantasy, ties to combat.

## Signature unit (mascot)
A **little kid in traditional Mongolian clothes holding a lamb.** Cozy, memorable, on-theme — the face of the game for marketing and player attachment.

## What makes it distinct
Cozy herding sim + collection gacha + lane combat, in an underused, beautiful real-world setting. Most gacha is anime-fantasy; this is grounded in Mongolian culture. That is both the identity and the marketing angle.

## Known design tension (watch this)
Three big systems is ambitious. Mitigation: vertical slice first (see roadmap). Don't deepen any one system until all three connect in a minimal playable loop.

## Tone & art notes
- 2.5D achieved via layered 2D sprites, parallax steppe backgrounds, skeletal (Spine/Unity 2D) animation for characters.
- Mood: open skies, warm felt textures of gers, big horizons. Cozy-but-epic.
- Respectful, researched cultural representation — names, clothing, architecture should be authentic, not caricatured.
