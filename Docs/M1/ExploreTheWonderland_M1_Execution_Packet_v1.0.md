# Explore The Wonderland
## M1 Execution Packet v1.0

- Date: April 2, 2026
- Purpose: Convert M1 from kickoff scope into an executable repo-side integration baseline
- Source Docs:
  - `Docs/ExploreTheWonderland_PRD_TDD_v1.3.2.md`
  - `Docs/ExploreTheWonderland_30_Day_Production_Milestone_Plan_v1.0.md`
  - `Docs/M0/ExploreTheWonderland_M0_Execution_Packet_v1.0.md`
  - `Docs/Current_Demonstration_Direction_v1.0.md`

Current demonstration note:

- the route shell in M1 should be treated as a readability spine for exploration-first park play, not as a hard mission sequence

## 1. Locked M1 Goal

M1 is the Core Foundation milestone.

Mandatory M1 outcomes:

1. `World_WonderlandPark.unity` exists as the master production park scene.
2. `WonderlandXROrigin.prefab` exists under `_Project`.
3. The project boots into the master park scene instead of template sample content.
4. A readable orientation spine exists in one big scene:
   `Human Entry -> Flower Field -> Lotus Pond -> Cat Route -> Fireworks Clearing`
5. Settings and onboarding stubs exist and are reachable.
6. One smoke-pass-ready greybox park shell exists before M2 systems begin.

Anything outside route assembly, XR boot safety, UI stub reachability, and first-pass comfort validation is not M1 scope.

## 2. Locked Technical Baseline

### Engine And Runtime

- Unity editor version: `6000.3.11f1`
- XR stack: OpenXR on tethered PCVR
- Render pipeline: URP
- Input path: Unity Input System
- Production content lives under `Assets/_Project`

### Scene Strategy

- one master playable scene: `Assets/_Project/World/Persistent/World_WonderlandPark.unity`
- region folders feed prefabs into the master scene rather than separate playable scenes
- scene logic should place and connect prefabs rather than own gameplay logic directly
- UI panels are world-space panels staged inside the route rather than desktop-only menus

### M1 Comfort Rule

- locomotion must be stable enough for first-pass team traversal
- onboarding and settings must be readable and reachable
- any unresolved comfort issue must be logged before claiming human-complete M1

## 3. Branching And Merge Rules

### Branch Naming

Use one of these branch prefixes:

- `feature/<owner>-<area>-<short-name>`
- `fix/<owner>-<area>-<short-name>`
- `docs/<owner>-<area>-<short-name>`

Examples used in M1:

- `feature/yu-fu-world-master-scene`
- `feature/haobo-xu-world-zone-blockouts`
- `feature/tongyan-sun-ui-onboarding-stub`
- `feature/yu-fu-world-m1-integration`

### Merge Rules

- no direct commits to `main`
- one focused PR per task or tightly related task set
- if a PR touches `World_WonderlandPark.unity`, the active scene owner reviews it
- if a PR touches shared XR/core systems, Yu Fu reviews it
- only one active scene owner edits the master scene at a time

## 4. Scene Ownership Rules

- `World_WonderlandPark.unity` active owner during M1: Yu Fu
- route blockout and world staging owner: Haobo Xu
- onboarding and settings scene UI owner: Tongyan Sun
- scene-side XR and startup-scene integration owner: Yu Fu

## 5. Repo Validation Checklist

Every repo-side M1 handoff should pass this list:

- `Assets/_Project/World/Persistent/World_WonderlandPark.unity` exists
- `Assets/_Project/Core/XR/WonderlandXROrigin.prefab` exists
- build settings point to `World_WonderlandPark.unity`
- all five blockout prefabs exist under `Assets/_Project/World/Regions`
- the master scene references all five blockout prefabs
- `SettingsPanel.prefab` and `OnboardingPanel.prefab` exist under `Assets/_Project/UI/Panels`
- the scene contains an `EventSystem`
- onboarding and settings are wired into the route shell

## 6. Human Validation Checklist

Every human M1 signoff should answer these questions:

- can the player start in Human Entry correctly?
- can the player traverse the route without a show-stopping blocker?
- are the route landmarks readable enough for a first-time tester?
- are onboarding and settings reachable in practice?
- is the current locomotion comfort baseline acceptable for M1?
- if teleport or vignette are missing or incomplete, are they accepted as non-blocking for M1?

## 7. Handoff To M2

M2 may begin once repo-side M1 is complete and the team agrees that any remaining M1 issues are human-signoff items rather than missing implementation artifacts.

The first M2 target files are:

- `Assets/_Project/Features/ScaleShift/Runtime/ScaleManager.cs`
- `Assets/_Project/Features/Weather/Runtime/WeatherManager.cs`
- `Assets/_Project/Features/Growth/Runtime/GrowthController.cs`
- `Assets/_Project/Features/ParticleVitality/Runtime/ParticleShapeSystem.cs`
- `Assets/_Project/Features/LotusPond/Runtime/LotusNoteTrigger.cs`
- `Assets/_Project/Features/Mounts/Runtime/MountController.cs`
- `Assets/_Project/Features/Fireworks/Runtime/FireworkController.cs`
