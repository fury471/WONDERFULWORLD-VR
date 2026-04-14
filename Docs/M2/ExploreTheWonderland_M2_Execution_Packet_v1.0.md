# Explore The Wonderland
## M2 Execution Packet v1.0

- Date: April 14, 2026
- Purpose: Convert M2 from a milestone idea into an executable feature-implementation baseline with a current repo status snapshot
- Source Docs:
  - `Docs/ExploreTheWonderland_PRD_TDD_v1.3.2.md`
  - `Docs/ExploreTheWonderland_30_Day_Production_Milestone_Plan_v1.0.md`
  - `Docs/M0/ExploreTheWonderland_M0_Execution_Packet_v1.0.md`
  - `Docs/M2/Start_Here_M2_Kickoff_Guide.md`
  - `Docs/Current_Demonstration_Direction_v1.0.md`

Current demonstration note:

- M2 systems are being handed off as modular attractions inside an exploration-first park slice, not as required sequence gates

## 1. Current Repo Snapshot (April 14, 2026)

M2 is in active progress, not at kickoff anymore.

Current repo-side picture:

- ScaleShift runtime and first-pass tuning asset are in the repo
- Weather runtime and 4 preset assets are in the repo
- Growth runtime and first-pass tuning asset are in the repo
- ParticleVitality runtime and first-pass tuning asset are in the repo
- Fireworks runtime and first-pass pattern library are in the repo
- Lotus runtime and `LotusScale_SO.asset` are in the repo
- Mounts runtime, mount settings asset, and first-pass prefabs are in the repo
- Wenao's first-pass VFX hook code and shader support are in the repo
- feature prefab folders are still mostly empty
- formal automated test coverage is still thin
- simulator validation has now been completed for the major M2 systems
- two caveats remain from simulator validation: Growth visual readability and Lotus ray interaction

This means the team is no longer in mid-M2 implementation. The code-and-data side of M2 is substantially complete, and the project can move into M3 integration with documented caveats.

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

## 6. Current Remaining M2 Work

What remains before full M2 signoff:

- record the simulator validation results in the closeout checklist
- review Wenao's VFX hook work for actual M3 integration readiness
- document known caveats:
  - Growth trigger path passed, but visual readability needs follow-up
  - Lotus core logic passed with `Z`, but simulator ray interaction still needs follow-up
- complete Yu Fu's handoff note for safe M3 integration
- get team agreement that M3 can begin without rewriting M2 scope

## 7. Handoff To M3

M3 should begin only after:

- all required M2 runtime scripts exist
- first-pass tuning assets exist for every active system
- each system can be demonstrated in isolation
- the team can describe the handoff risk for integrating each system into the park scene
