# Explore The Wonderland
## M0 Execution Packet v1.0

- Date: March 27, 2026
- Purpose: Convert M0 from a planning idea into an executable production baseline
- Source Docs:
  - `Docs/ExploreTheWonderland_PRD_TDD_v1.3.2.md`
  - `Docs/ExploreTheWonderland_30_Day_Production_Milestone_Plan_v1.0.md`

## 1. Locked Day 30 Vertical Slice

This 30-day cycle is not building the full park. It is building one polished vertical slice.

Mandatory route order:

1. `Arrival Plaza / Human Entry`
2. `Flower Field`
3. `Lotus Lagoon`
4. `Cat Route`
5. `Fireworks Plaza`

Mandatory Day 30 features:

- headset-stable XR player rig
- comfort-safe locomotion
- blink-based scale shift
- one weather system with readable visual impact
- one growth-based traversal unlock
- one particle gather and shaping interaction
- one guided cat ride
- one lotus musical attraction
- one fireworks finale
- coherent NPR visual identity, sound, VFX, and onboarding

Anything outside that scope is backlog unless it directly supports stability, readability, comfort, or presentation quality.

## 2. Locked Technical Baseline

### Engine And Runtime

- Unity editor version: `6000.3.11f1`
- XR stack: OpenXR on tethered PCVR
- Render pipeline: URP
- Input path: Unity Input System
- Current reference content: Unity VR template and XR sample assets may be referenced, but new production implementation must live in `Assets/_Project`

### Target Performance

- framerate target: `90 FPS`
- frame-time target: `< 11.1 ms`
- comfort rule: no feature is allowed to ship into the Day 30 slice if it creates avoidable nausea, disorientation, or major readability loss

### Production Authoring Rules

- all new production code, prefabs, scenes, ScriptableObjects, and art integration live under `Assets/_Project`
- `Assets/Samples`, `Assets/VRTemplateAssets`, `Assets/XR`, and `Assets/XRI` are treated as reference or dependency content, not the main production workspace
- shared logic goes in `Assets/_Project/Core`
- feature-specific logic stays inside its feature folder until there is a strong reason to promote it to `Core`
- world scenes should place and connect prefabs, not own the gameplay logic directly
- scene logic should prefer prefabs and ScriptableObjects over scene-only serialized state

### Scene Strategy

Planned production scene approach for M1:

- one master scene: `Assets/_Project/World/Persistent/World_WonderlandPark.unity`
- all park parts live inside that one scene as organized roots and prefabs
- for the current prototype slice, do not build separate playable scenes for each land
- region folders under `Assets/_Project/World/Regions/` store blockout prefabs, landmark prefabs, and future zone content, not separate playable scenes for M1

Current state note:

- the repo still builds `Assets/Scenes/SampleScene.unity`
- moving to the production master scene is an M1 task, not an M0 blocker

## 3. Branching And Merge Rules

### Branch Naming

Use one of these branch prefixes:

- `feature/<owner>-<area>-<short-name>`
- `fix/<owner>-<area>-<short-name>`
- `art/<owner>-<area>-<short-name>`
- `prototype/<owner>-<area>-<short-name>`
- `docs/<owner>-<area>-<short-name>`

Examples:

- `feature/yu-fu-core-xr-rig`
- `feature/xuanyuan-scale-weather`
- `feature/haobo-world-human-entry`
- `art/wenao-fireworks-lighting`
- `docs/tongyan-onboarding-playtest`

### Merge Rules

- no direct commits to `main`
- one pull request per focused task or tightly related task set
- minimum one reviewer before merge
- if a PR touches the master park scene, the active scene owner must review
- if a PR touches shared XR/core systems, the technical lead must review
- branches should sync with `main` frequently to reduce Unity merge conflicts
- if two people need the master park scene, pause and assign one active scene owner before continuing

### Scene Ownership Rules

- `World_WonderlandPark.unity` active scene owner during M1 foundation: Yu Fu
- `HumanEntry` zone owner: Haobo Xu
- `FlowerField` zone owner: Haobo Xu
- `LotusPond` zone staging owner: Haobo Xu
- `CatRoute` zone owner: Haobo Xu
- `FireworksClearing` zone staging owner: Haobo Xu
- `UI/Onboarding` objects inside the master scene: Tongyan Sun

If the active scene owner changes for a work period, the change must be recorded in the task board before edits begin.

## 4. Build Validation Checklist

Every milestone build should be tested against this list.

### Boot And XR

- app launches without missing-scene or missing-reference failure
- headset tracking is stable
- controllers or input bindings are detected correctly
- the player spawns in the intended start location

### Comfort And Navigation

- locomotion works with no major stutter
- snap turn or chosen turn mode works as intended
- onboarding explains the next step clearly
- the next attraction or destination is visually readable

### Signature Features

- scale shift completes without camera instability
- weather change visibly affects the environment
- growth interaction changes traversal state
- particle gathering and shaping complete successfully
- lotus attraction triggers notes and ripple feedback correctly
- cat ride starts, moves, and ends safely
- fireworks finale triggers and remains legible

### Stability

- no critical null-reference or hard-stop progression bug during the slice route
- no scene load dead-end
- no feature requires restarting the app to recover from a normal use case

## 5. Visual Direction Priorities

This vertical slice should feel like a magical amusement park, not a generic garden demo.

### Visual Pillars

- storybook NPR rendering with clean shape readability
- themed land silhouettes that read instantly from a distance
- theatrical attraction staging rather than random environmental clutter
- controlled, readable VFX instead of noisy full-screen particles
- strong entry points, vista moments, and finale payoff framing

### Priority Visual Beats

1. first scale-shift reveal
2. Flower Field vista and micro-scale awe
3. Lotus Lagoon performance-space feel
4. Cat Route scenic ride moment
5. Fireworks Plaza finale

### Shader And Material Priorities

First-pass production shader priorities:

- toon master surface
- stylized foliage wind motion
- stylized water for lotus lagoon
- emissive attraction accent material
- fireworks and particle additive materials

### Landmark Priorities

Each land needs at least one strong landmark cue:

- Human Entry: arrival arch / plaza anchor
- Flower Field: oversized flower cluster or tall grass silhouette
- Lotus Lagoon: glowing lagoon center / musical pad cluster
- Cat Route: elevated route reveal and moving cat silhouette
- Fireworks Plaza: clear open-sky reveal and launch focal point

## 6. Onboarding, Comfort, And Playtest Criteria

### Onboarding Rules

The first 3 minutes must explain:

- how to move
- what the first interaction is
- where the first destination is
- that the world is attraction-driven and explorable

Preferred onboarding tools:

- diegetic signs
- world-space panels
- visual attraction cues
- simple one-step prompts

Avoid:

- large HUD overlays
- long paragraphs in-headset
- multiple simultaneous instructions

### Comfort Defaults

Default comfort settings for first-time users:

- blink scale transition enabled
- snap turn enabled unless a smooth-turn option is explicitly selected
- comfort vignette enabled for locomotion and mount motion where needed
- guided cat ride mode only for the first 30-day slice

### Playtest Questions

Every playtest should answer:

- did the player know where to go next?
- was the first scale shift understandable and comfortable?
- did the lotus attraction feel playful and readable?
- did the cat ride feel memorable and comfortable?
- did the fireworks finale feel like a payoff?
- where did the player hesitate, get lost, or feel discomfort?

## 7. M0 Completion Standard

Repo-side M0 is complete when all of the following exist:

- milestone plan
- execution packet
- issue board seed with owners and starter tickets
- M0 closeout checklist
- docs are versionable in git
- first implementation tickets are ready for M1

Human signoff M0 is complete when all of the following are confirmed by the team:

- owner map accepted
- branch and merge rules accepted
- Unity version accepted
- Day 30 route accepted
