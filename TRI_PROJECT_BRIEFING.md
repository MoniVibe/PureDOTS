# Unity Tri-Project Briefing

**Last Updated**: 2025-02-15  
**Purpose**: Quick orientation so every agent knows where each repo lives and how they interact. This document is intentionally lean—see each repo’s `Docs/` folder for feature-level specs.

---

## 1. Repo Layout & Ownership

| Repo | Location | Owns |
|------|----------|------|
| **PureDOTS** | `C:\Users\Moni\Documents\claudeprojects\unity\PureDOTS` | Shared DOTS framework (`Packages/com.moni.puredots`). Only reusable engine/runtime systems live here. |
| **Space4x** | `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` | Carrier-first 4X game. All game-specific sim/presentation/scripts belong under `Assets/Scripts/Space4x`. |
| **Godgame** | `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` | Divine intervention game. All villager/miracle/presentation code belongs under `Assets/Scripts/Godgame`. |

**Never copy one repo inside another.** If you see a `Godgame/` folder under the Space4x repo (or vice versa), treat it as an artifact and delete it instead of editing it. Likewise, game folders never belong under `PureDOTS`. Keep the three repos cleanly separated to avoid “false directory” edits.

---

## 2. Interaction Rules (High-Level)

1. **Engine vs Game**  
   - Engine/generic systems live under `PureDOTS/Packages/com.moni.puredots/Runtime/<Module>/`.  
   - Space4x- or Godgame-specific logic lives under their respective game repos. No forks or copies of shared structs; consume the PureDOTS packages directly.

2. **Package Reference**  
   Each game references PureDOTS via a local package path in `Packages/manifest.json`:  
   ```json
   {
     "dependencies": {
       "com.moni.puredots": "file:../../PureDOTS/Packages/com.moni.puredots"
     }
   }
   ```
   Never edit PureDOTS types from a game repo—make changes inside the PureDOTS repo and pull them in through the package.

3. **Body / Mind / Aggregate**  
   These are architectural slices inside PureDOTS. They do **not** correspond to extra repos or packages. Keep the naming consistent but remember everything still compiles inside Unity.Entities 1.4.

4. **Presentation Boundaries**  
   Cameras, HUDs, input bridges, and scene authoring stay inside each game repo. PureDOTS only carries reusable data structures, baker pipelines, and deterministic system logic.

---

## 3. Coordination Expectations

- **Docs Live With Their Repo**  
  - PureDOTS specs: `PureDOTS/Docs/*`  
  - Space4x plans: `Space4x/Docs/*`, `space.plan.md`  
  - Godgame plans: `Godgame/Docs/*`, `god.plan.md`
- **Briefing Sync**  
  This file exists in each repo root. When it changes, copy it to the other two repos to keep wording identical. (Yes, there are four copies if you count the shared root—update all of them.)
- **No Cross-Repro Testing Assets**  
  Authoring components, prefabs, telemetry scenes, etc. ship only with their owning game. If another game needs the same data, promote the shared pieces into PureDOTS first.
- **Error Ownership**  
  - PureDOTS errors → fix in PureDOTS.  
  - Space4x compile/runtime issues → fix in Space4x.  
  - Godgame compile/runtime issues → fix in Godgame.  
  Don’t patch Godgame problems from inside Space4x just because a copy of the file exists there.

---

## 4. Quick Checklist Before You Start Working

1. Confirm you are inside the correct repo (check the path prompt).  
2. Run `git status` to ensure the workspace is clean and no accidental mirrored folders are present.  
3. If you need a shared component or system, add it to PureDOTS first, then integrate it from each game.  
4. When wiring game presentation or telemetry, create/edit files only under that game’s repo.  
5. If you see stray folders from another project, stop and clean them up before continuing—otherwise agents will repeat the same mistake.

That’s it. Use this briefing only to stay grounded in the overall structure; everything else (feature specs, cadence gates, telemetry plans) belongs in the specific repo you’re modifying.
