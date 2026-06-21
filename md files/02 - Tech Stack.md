# 02 — Tech Stack

## Engine
- **Unity 6 LTS** — editor version `6000.0.46f1`
- **Render pipeline:** Universal Render Pipeline (URP), 2D — chosen for mobile performance.
- **Template used at creation:** Universal 2D
- **Language:** C#

## Platforms
- **Android first** — Google Play + Samsung Galaxy Store. Fully buildable from Windows.
- **iOS later** — requires a Mac (owned or cloud, e.g. MacinCloud / Codemagic / GitHub Actions macOS runner) for the final Xcode build + App Store submission. Apple requires this, not Unity. Apple Developer account = $99/year.

## Dev machine
- Windows PC for all development and Android builds. No Mac needed until iOS shipping.

## Backend (for the gacha — decide before real-money launch)
A single-player gacha still needs a light backend because real-money purchases and pull rates must be validated server-side (or players can hack local files for free pulls). Options, all with free/low tiers that scale with revenue:
- **PlayFab (Microsoft)** — built for live games; economy, inventory, purchase validation out of the box. Strong fit.
- **Firebase (Google)** — generous free tier; accounts, data, remote config.
- **Supabase / Nakama** — open-source-friendly, more control.

> Decision deferred: backend is NOT needed for the early offline vertical slice. Build the slice with local data first, add backend before any real-money feature ships. See `05 - Decisions Log.md`.

## Tools
- **Obsidian** — design + planning notes (this vault).
- **Claude Code** — reads this vault, writes/edits C# in the Unity project. Run it pointed at the `claude game` root so it sees both the notes and the Unity project.
- **Version control:** Git recommended early. Add a Unity `.gitignore` so `Library/`, `Temp/`, etc. are not committed.

## Art pipeline (IMPORTANT — read before planning assets)
**Claude Code writes code, not art.** It cannot draw or generate character illustrations. No code library (including three.js) produces Arknights-style character art — that art comes from artists or AI *image* generators, never from Claude Code.

**three.js is NOT used in this project.** It is a JavaScript library for rendering 3D graphics in web browsers. It is unrelated to Unity (C#), unrelated to 2D, and is not an art-generation tool. It was considered and rejected — see Decisions Log.

How we actually get Arknights-style 2D character art (the biggest cost/quality driver of a gacha game):
- **Commissioned human artists** — highest quality, industry standard, paid per character. The real production path.
- **2D asset packs** (Unity Asset Store, itch.io) — cheap/fast placeholders, but generic and not theme-specific.
- **AI image generators** (Midjourney, Stable Diffusion, etc.) — anime-style art from prompts/references; good for placeholders and possibly production. Caveats: hard to keep characters consistent, commercial-use/copyright terms vary, and must NOT copy Arknights' actual character designs (their IP). General anime style = fine; their specific characters = not.

**Recommended approach for now:** AI-generated or asset-pack **placeholders** to build and test, budget for **commissioned art** for key heroes once the game is proven fun. Claude Code builds all the systems that *use* the art; the art itself is sourced separately.

## Animation tooling
- 2.5D character motion via **Unity 2D Animation package** (free, skeletal/bone rigging of sprites) or **Spine** (paid, industry standard). Decide later — see Open Questions.

## Folder structure (the `claude game` root)
```
claude game/
├── obsidian/              <- this vault (notes Claude Code reads)
├── [UnityProjectName]/    <- the Unity project
│   ├── Assets/
│   │   ├── Scripts/       <- C# lives here
│   │   ├── Art/
│   │   ├── Prefabs/
│   │   ├── Scenes/
│   │   └── ...
│   ├── ProjectSettings/
│   └── ...
└── .gitignore
```

## What Claude Code can and cannot do in Unity
- **Can:** write/edit C# scripts, refactor, create data structures, manage files, set up systems in code, explain editor steps.
- **Cannot:** click around inside the Unity Editor GUI (placing objects in a scene, dragging components, wiring the inspector). Those steps the human does — Claude Code will give instructions for them.
