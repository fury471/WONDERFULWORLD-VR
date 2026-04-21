# Explore The Wonderland
## Start Here: M2 Kickoff Guide

- Date: March 31, 2026
- Audience: Group 5 M2 feature team
- Goal: Start M2 cleanly from the current repo without guessing where feature work belongs or who should touch the master scene

Current demonstration note:

- M2 systems should be built as discoverable attractions and environmental affordances for an exploration-first magical park, not as steps in a rigid mission chain

## Current Progress Snapshot (April 7, 2026)

- ScaleShift, Weather, Growth, ParticleVitality, Fireworks, and most Lotus runtime work have landed in the repo.
- Mounts has not landed yet.
- Lotus is still missing its planned LotusScale_SO.asset.
- Wenao's M2 lighting and VFX support hooks have not landed yet.
- The team is past M2 kickoff and is now in active mid-milestone implementation.

## 1. What This Repo Is Right Now

The repo is ready for M2 feature kickoff planning, but M2 should begin as isolated feature implementation, not as direct park-scene hacking.

Current confirmed baseline:

- Unity version is locked to `6000.3.11f1`
- Render pipeline is `URP`
- XR stack is `OpenXR` on tethered `PCVR`
- Input path is the Unity `Input System`
- production content must live under `Assets/_Project`
- feature folders already exist under `Assets/_Project/Features`
- the master park scene exists, but M2 should not begin by stuffing new mechanic logic directly into that scene

Current M2 mindset:

- M1 created the route shell
- M2 creates the signature systems
- every system should work first in isolation
- reliability matters more than polish in this milestone
- the team can prepare and build M2 systems now, but final M1 headset signoff should still happen as soon as hardware is available

This means the correct way to start is:

1. confirm M2 ownership and branch plan
2. create or verify each owner's sandbox test setup
3. create runtime scripts, prefabs, and ScriptableObject data in the correct feature folders
4. make each system work in isolation before touching the park scene
5. hand systems to Yu Fu for controlled scene integration later

## 2. First Team Sync

Do this before feature coding starts:

1. Confirm that Yu Fu remains the active owner of the master scene during early M2.
2. Confirm that feature owners prototype inside sandbox scenes or isolated feature tests first.
3. Confirm that each M2 system gets:
   - runtime code in `Runtime`
   - at least one prefab or interaction anchor in `Prefabs`
   - tuning data in `ScriptableObjects`
   - a quick validation path in `Tests` or a sandbox scene
4. Confirm that nobody hard-codes production tuning only inside `World_WonderlandPark.unity`.
5. Confirm that M2 work will not be called complete until each system works in isolation without hard-crashing play flow.
6. Confirm that M3 integration work will wait until the M2 acceptance check passes.

## 3. Machine Setup

Each teammate should do this on their own machine:

1. Pull the latest `main`.
2. Open the project in Unity `6000.3.11f1`.
3. Let Unity finish compile and import fully.
4. Open `Assets/_Project/Features/README.md`.
5. Open `Assets/_Project/Sandbox/README.md`.
6. Verify your own sandbox folder exists under `Assets/_Project/Sandbox`.
7. If your sandbox scene does not exist yet, create it now before feature work starts.
8. Do not prototype new gameplay systems directly in `World_WonderlandPark.unity`.

## 4. Branch Setup

Before doing any work, create one focused branch per feature.

Use one of these patterns:

- `feature/<owner>-<feature>-v1`
- `fix/<owner>-<feature>-bugfix`
- `art/<owner>-<feature>-lookdev`
- `prototype/<owner>-<feature>-sandbox`

Good M2 examples:

- `feature/xuanyuan-qin-scale-shift-v1`
- `feature/xuanyuan-qin-weather-v1`
- `feature/tongyan-sun-growth-v1`
- `feature/xuanyuan-qin-particle-vitality-v1`
- `feature/tongyan-sun-lotus-pond-v1`
- `feature/haobo-xu-mount-route-v1`
- `feature/xuanyuan-qin-fireworks-v1`
- `art/wenao-li-fireworks-vfx-hooks`
- `feature/yu-fu-core-m2-handoff-hooks`

Avoid this mistake:

- do not mix multiple unrelated systems into one branch just because they are all part of M2

## 5. Feature Folder Map

Use the existing folders exactly as they are laid out now:

- `Assets/_Project/Features/ScaleShift/Runtime`
- `Assets/_Project/Features/ScaleShift/Prefabs`
- `Assets/_Project/Features/ScaleShift/ScriptableObjects`
- `Assets/_Project/Features/ScaleShift/Tests`

- `Assets/_Project/Features/Weather/Runtime`
- `Assets/_Project/Features/Weather/Prefabs`
- `Assets/_Project/Features/Weather/ScriptableObjects`
- `Assets/_Project/Features/Weather/Tests`

- `Assets/_Project/Features/Growth/Runtime`
- `Assets/_Project/Features/Growth/Prefabs`
- `Assets/_Project/Features/Growth/ScriptableObjects`
- `Assets/_Project/Features/Growth/Tests`

- `Assets/_Project/Features/ParticleVitality/Runtime`
- `Assets/_Project/Features/ParticleVitality/Prefabs`
- `Assets/_Project/Features/ParticleVitality/ScriptableObjects`
- `Assets/_Project/Features/ParticleVitality/Tests`

- `Assets/_Project/Features/Fireworks/Runtime`
- `Assets/_Project/Features/Fireworks/Prefabs`
- `Assets/_Project/Features/Fireworks/ScriptableObjects`
- `Assets/_Project/Features/Fireworks/Tests`

- `Assets/_Project/Features/Mounts/Runtime`
- `Assets/_Project/Features/Mounts/Prefabs`
- `Assets/_Project/Features/Mounts/ScriptableObjects`
- `Assets/_Project/Features/Mounts/Tests`

- `Assets/_Project/Features/LotusPond/Runtime`
- `Assets/_Project/Features/LotusPond/Prefabs`
- `Assets/_Project/Features/LotusPond/ScriptableObjects`
- `Assets/_Project/Features/LotusPond/Tests`

## 6. Day 1 Start Order

Start in this order so the team does not block itself.

### Step 1: Create the minimum runtime and data scaffolding

Owners should first create or verify the minimum v1 files for their systems.

Xuanyuan Qin:

1. `Assets/_Project/Features/ScaleShift/Runtime/ScaleManager.cs`
2. `Assets/_Project/Features/ScaleShift/Runtime/ScaleState.cs`
3. `Assets/_Project/Features/ScaleShift/Runtime/ScaleTransitionController.cs`
4. `Assets/_Project/Features/ScaleShift/ScriptableObjects/ScaleSettings_SO.asset`
5. `Assets/_Project/Features/Weather/Runtime/WeatherManager.cs`
6. `Assets/_Project/Features/Weather/Runtime/RegionWeatherResponder.cs`
7. `Assets/_Project/Features/Weather/ScriptableObjects/WeatherPreset_Clear.asset`
8. `Assets/_Project/Features/Weather/ScriptableObjects/WeatherPreset_Overcast.asset`
9. `Assets/_Project/Features/Weather/ScriptableObjects/WeatherPreset_Rain.asset`
10. `Assets/_Project/Features/Weather/ScriptableObjects/WeatherPreset_Windy.asset`
11. `Assets/_Project/Features/ParticleVitality/Runtime/ParticleCollector.cs`
12. `Assets/_Project/Features/ParticleVitality/Runtime/ParticleShapeSystem.cs`
13. `Assets/_Project/Features/ParticleVitality/Runtime/ParticlePreviewAnchor.cs`
14. `Assets/_Project/Features/ParticleVitality/ScriptableObjects/ParticleShapeLibrary_SO.asset`
15. `Assets/_Project/Features/Fireworks/Runtime/FireworkController.cs`
16. `Assets/_Project/Features/Fireworks/Runtime/FireworkLaunchPad.cs`
17. `Assets/_Project/Features/Fireworks/ScriptableObjects/FireworkPatternLibrary_SO.asset`

Tongyan Sun:

1. `Assets/_Project/Features/Growth/Runtime/GrowthController.cs`
2. `Assets/_Project/Features/Growth/Runtime/GrowthStageDriver.cs`
3. `Assets/_Project/Features/Growth/ScriptableObjects/GrowthProfile_SO.asset`
4. `Assets/_Project/Features/LotusPond/Runtime/LotusNoteTrigger.cs`
5. `Assets/_Project/Features/LotusPond/Runtime/LotusRippleController.cs`
6. `Assets/_Project/Features/LotusPond/ScriptableObjects/LotusScale_SO.asset`

Haobo Xu:

1. `Assets/_Project/Features/Mounts/Runtime/MountController.cs`
2. `Assets/_Project/Features/Mounts/Runtime/CatRideRouteController.cs`
3. `Assets/_Project/Features/Mounts/ScriptableObjects/MountSettings_SO.asset`

Wenao Li:

1. prepare VFX presentation hooks for Fireworks and Weather
2. prepare placeholder materials or VFX-prefab hookup points only where they unblock M2 logic
3. keep art-side hooks separate from feature-state logic

Yu Fu:

1. verify the XR rig exposes safe integration points for future feature hooks
2. review whether any system needs player-rig references, input hooks, or scene-safe bootstrap support
3. avoid deep integration until feature owners can demo isolated v1 behavior

### Step 2: Make each system work in isolation

Every owner should validate their mechanic first in a sandbox scene or a simple isolated test setup.

Use this rule:

- if a feature does not work in isolation, it is not ready for the master park scene

Target isolation behavior for each feature:

- ScaleShift: blink transition changes scale state safely and preserves comfort-first behavior
- Weather: 4 authored presets switch world mood within 2 to 3 seconds
- Growth: one plant or route changes traversal state and collider state
- ParticleVitality: gather, hold, and 3 preset shapes work without obvious instability
- LotusPond: note, ripple, and cooldown work with either hand
- Mounts: guided cat route starts, traverses, and ends safely
- Fireworks: at least 3 patterns trigger without obvious failure

### Step 3: Prefer authored v1 behavior over high-risk simulation

For M2, make safe, authored versions first:

- ScaleShift should default to blink transition
- Weather should use authored presets, not complex simulation
- Growth should use authored stages, not fully procedural biology
- ParticleVitality should use restricted target shapes and point budgets
- Fireworks should use authored pattern libraries first
- Mounts should use guided route logic, not free manual ride control
- LotusPond should use simple readable triggers and cooldowns first

### Step 4: Put tuning into ScriptableObjects, not scene-only values

For every system, the first question should be:

- what values will designers or QA need to tune without rewriting code?

Those values should live in `ScriptableObjects` or feature prefabs, not only inside one scene instance.

### Step 5: Keep master-scene contact controlled

During early M2:

- feature owners build in their own sandboxes
- Yu Fu reviews handoff readiness
- only controlled, small integration passes should touch the master park scene

## 7. Recommended Feature Start Order

If the team wants one shared order, use this:

1. ScaleShift v1
2. Weather v1
3. Growth v1
4. ParticleVitality v1
5. LotusPond v1
6. Mounts v1
7. Fireworks v1
8. Lighting and VFX support hooks

Why this order:

- ScaleShift and Weather establish systemic foundation
- Growth and Lotus create readable interaction beats
- ParticleVitality and Fireworks build spectacle logic
- Mounts depends on stable route logic and comfort-safe movement decisions

## 8. First Acceptance Check

Do not move on to M3 systems until all of this is true:

1. `ScaleManager.cs` exists and scale shift works in isolation.
2. `WeatherManager.cs` exists and 4 presets work in isolation.
3. `GrowthController.cs` exists and one route-changing growth interaction works.
4. `ParticleShapeSystem.cs` exists and 3 preset particle shapes work.
5. `LotusNoteTrigger.cs` exists and note + ripple + cooldown works.
6. `MountController.cs` exists and guided cat ride flow works safely.
7. `FireworkController.cs` exists and 3 patterns trigger correctly.
8. first-pass ScriptableObject assets exist for the systems that need tuning data.
9. no system hard-crashes play mode or blocks the rest of the experience from loading.
10. each system can be demoed in isolation without requiring hidden scene-only setup.

## 9. What Not To Do

Avoid these mistakes at the start of M2:

- do not put core mechanic logic directly into `World_WonderlandPark.unity`
- do not skip ScriptableObjects and hard-code all tuning in one MonoBehaviour
- do not treat sandbox scenes as production scenes
- do not let one branch own multiple unrelated systems without a strong reason
- do not chase final art quality before prototype reliability is proven
- do not make high-motion VR comfort risks the default behavior in v1
- do not start M3-style full-park integration before the systems pass in isolation

## 10. Immediate Next Tickets After M2 Foundation

When the isolated M2 systems are working, the next milestone should focus on M3 integration:

1. connect the systems into the master park loop
2. create `Assets/_Project/Core/Runtime/GameFlowManager.cs`
3. create `Assets/_Project/Core/Runtime/ParkAttractionState.cs`
4. create `Assets/_Project/UI/Prefabs/AttractionMarker.prefab`
5. integrate onboarding clarity and attraction pacing
6. test the whole slice as one authored experience instead of feature islands

## 11. Reference Docs

Use these as the source of truth:

- `Docs/ExploreTheWonderland_PRD_TDD_v1.3.2.md`
- `Docs/ExploreTheWonderland_30_Day_Production_Milestone_Plan_v1.0.md`
- `Docs/ExploreTheWonderland_M0_Execution_Packet_v1.0.md`
- `Docs/ExploreTheWonderland_M0_Issue_Board_Seed_v1.0.md`
- `Docs/Start_Here_M1_Kickoff_Guide.md`

If the team follows this order, M2 starts with the least scene-merge risk and the clearest path to prototype-quality signature systems.

