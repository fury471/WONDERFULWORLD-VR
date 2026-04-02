# Explore The Wonderland
## M2 Execution Packet v1.0

- Date: April 2, 2026
- Purpose: Convert M2 from a milestone idea into an executable feature-implementation baseline
- Source Docs:
  - `Docs/ExploreTheWonderland_PRD_TDD_v1.3.2.md`
  - `Docs/ExploreTheWonderland_30_Day_Production_Milestone_Plan_v1.0.md`
  - `Docs/M0/ExploreTheWonderland_M0_Execution_Packet_v1.0.md`
  - `Docs/M2/Start_Here_M2_Kickoff_Guide.md`

## 1. Locked M2 Scope

M2 is the Signature Systems milestone.

Mandatory M2 systems:

1. blink-based scale shift
2. weather manager with at least 4 presets
3. one growth-based traversal route
4. particle gather plus 3 preset shapes
5. lotus interaction with note, ripple, and cooldown
6. guided cat ride route v1
7. fireworks controller with at least 3 patterns

M2 is not the milestone for full park integration polish.

The priority is:

- isolated reliability
- tunable data
- safe feature handoff
- no hard-crash or play-flow breakage

## 2. Locked Technical Rules

### Production Authoring Rules

- all new M2 production logic lives under `Assets/_Project/Features`
- feature code goes in `Runtime`
- reusable feature objects go in `Prefabs`
- tuning data goes in `ScriptableObjects`
- isolated validation lives in `Tests` or personal sandbox scenes
- no feature should require hidden scene-only serialized state to work

### Isolation-First Rule

Each feature must work in isolation before it touches `World_WonderlandPark.unity`.

Preferred implementation style:

- authored states over high-risk procedural systems
- ScriptableObjects over hard-coded tuning constants
- feature prefabs over scene-only setup
- sandbox validation before main-scene integration

## 3. Owner Map

- Xuanyuan Qin owns ScaleShift, Weather, ParticleVitality, and Fireworks gameplay logic
- Tongyan Sun owns Growth and LotusPond gameplay logic
- Haobo Xu owns Mounts and CatRoute interaction flow
- Wenao Li owns VFX presentation hooks and visual support for M2 systems
- Yu Fu owns XR/player-rig integration support and later scene-safe handoff review

## 4. Branching And Merge Rules

### Branch Naming

Use one branch per feature.

Examples:

- `feature/xuanyuan-qin-scale-shift-v1`
- `feature/xuanyuan-qin-weather-v1`
- `feature/tongyan-sun-growth-v1`
- `feature/xuanyuan-qin-particle-vitality-v1`
- `feature/tongyan-sun-lotus-pond-v1`
- `feature/haobo-xu-mount-route-v1`
- `feature/xuanyuan-qin-fireworks-v1`

### Merge Rules

- no direct commits to `main`
- one focused PR per system whenever possible
- systems should merge only after isolated demo readiness
- if a PR adds shared XR/core hooks, Yu Fu reviews it
- if a PR adds feature handoff requirements for M3 integration, the scene owner reviews it

## 5. Validation Checklist

Every M2 feature handoff should answer these questions:

- does the target runtime script exist in the correct feature folder?
- does the feature expose tuning through ScriptableObjects or prefabs?
- can the feature be demonstrated in isolation?
- does the feature avoid hard-crashing play mode?
- is the feature reliable enough for handoff even if presentation is still placeholder quality?

## 6. Expected Repo Targets

Required core M2 targets:

- `Assets/_Project/Features/ScaleShift/Runtime/ScaleManager.cs`
- `Assets/_Project/Features/Weather/Runtime/WeatherManager.cs`
- `Assets/_Project/Features/Growth/Runtime/GrowthController.cs`
- `Assets/_Project/Features/ParticleVitality/Runtime/ParticleShapeSystem.cs`
- `Assets/_Project/Features/Fireworks/Runtime/FireworkController.cs`
- `Assets/_Project/Features/Mounts/Runtime/MountController.cs`
- `Assets/_Project/Features/LotusPond/Runtime/LotusNoteTrigger.cs`
- first-pass ScriptableObject assets for settings and presets

Useful supporting targets from the locked structure:

- `ScaleState.cs`
- `ScaleTransitionController.cs`
- `RegionWeatherResponder.cs`
- `GrowthStageDriver.cs`
- `ParticleCollector.cs`
- `ParticlePreviewAnchor.cs`
- `FireworkLaunchPad.cs`
- `CatRideRouteController.cs`
- `LotusRippleController.cs`

## 7. Handoff To M3

M3 should begin only after each M2 system works in isolation and the team can demonstrate stable first-pass behavior without relying on hidden one-off scene hacks.
