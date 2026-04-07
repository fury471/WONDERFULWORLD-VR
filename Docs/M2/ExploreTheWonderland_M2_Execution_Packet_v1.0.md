# Explore The Wonderland
## M2 Execution Packet v1.0

- Date: April 7, 2026
- Purpose: Convert M2 from a milestone idea into an executable feature-implementation baseline with a current repo status snapshot
- Source Docs:
  - `Docs/ExploreTheWonderland_PRD_TDD_v1.3.2.md`
  - `Docs/ExploreTheWonderland_30_Day_Production_Milestone_Plan_v1.0.md`
  - `Docs/M0/ExploreTheWonderland_M0_Execution_Packet_v1.0.md`
  - `Docs/M2/Start_Here_M2_Kickoff_Guide.md`

## 1. Current Repo Snapshot (April 7, 2026)

M2 is in active progress, not at kickoff anymore.

Current repo-side picture:

- ScaleShift runtime and first-pass tuning asset are in the repo
- Weather runtime and 4 preset assets are in the repo
- Growth runtime and first-pass tuning asset are in the repo
- ParticleVitality runtime and first-pass tuning asset are in the repo
- Fireworks runtime and first-pass pattern library are in the repo
- Lotus runtime is in the repo, but the planned `LotusScale_SO.asset` is still missing
- Mounts runtime has not landed yet
- Wenao's slice lighting and VFX hooks have not landed yet
- feature prefab folders are still mostly empty
- formal automated test coverage is still thin

This means the team is in mid-M2: most core logic tracks have landed, but the milestone is not yet complete.

## 2. Locked M2 Scope

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

## 3. Locked Technical Rules

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

## 4. Owner Map

- Xuanyuan Qin owns ScaleShift, Weather, ParticleVitality, and Fireworks gameplay logic
- Tongyan Sun owns Growth and LotusPond gameplay logic
- Haobo Xu owns Mounts and CatRoute interaction flow
- Wenao Li owns VFX presentation hooks and visual support for M2 systems
- Yu Fu owns XR/player-rig integration support and later scene-safe handoff review

## 5. Branching And Merge Rules

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

## 6. Current Required Remaining Repo Targets

Still missing or incomplete compared with the locked M2 target structure:

- `Assets/_Project/Features/Mounts/Runtime/MountController.cs`
- `Assets/_Project/Features/Mounts/Runtime/CatRideRouteController.cs`
- `Assets/_Project/Features/Mounts/ScriptableObjects/MountSettings_SO.asset`
- `Assets/_Project/Features/LotusPond/ScriptableObjects/LotusScale_SO.asset`
- first-pass reusable M2 prefabs in the feature `Prefabs/` folders
- Wenao's lighting and VFX support hooks under `Assets/_Project/World/Shared` and `Assets/_Project/Art`
- stronger test or sandbox proof for isolated system demos

## 7. Handoff To M3

M3 should begin only after:

- all required M2 runtime scripts exist
- first-pass tuning assets exist for every active system
- each system can be demonstrated in isolation
- the team can describe the handoff risk for integrating each system into the park scene
