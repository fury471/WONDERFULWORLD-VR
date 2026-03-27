# Explore The Wonderland
## 30-Day Production Milestone Plan v1.0

- Date: March 27, 2026
- Project: Open-World PCVR Fantasy Amusement Park Experience
- Source Vision: `Docs/ExploreTheWonderland_PRD_TDD_v1.3.2.md`
- Planning Horizon: 30 calendar days
- Target Outcome: polished vertical slice, not full production completion
- Status: Proposed execution plan mapped to the current repo structure

## 1. Executive Goal

At the end of 30 days, the team should have a stable, headset-ready vertical slice that demonstrates the project's identity as a magical open-world wonderland park with strong visual quality, professional presentation, and comfort-first immersion.

The Day 30 build must support one connected park loop:

1. Arrival Plaza / Human Entry
2. Flower Field land
3. Lotus Lagoon attraction
4. Cat Route ride / traversal segment
5. Fireworks Plaza finale

The build must include the following shippable features:

- comfort-safe XR locomotion and settings
- reliable scale shift with blink transition as default
- one weather state system with readable world impact
- one growth-based traversal unlock
- one particle gathering and shaping interaction
- one guided cat ride route
- one playable lotus music attraction
- one polished fireworks reveal
- one coherent visual identity with lighting, shader, VFX, audio, and UI support

## 2. Assumptions And Planning Rules

This plan assumes:

- the current team is Group 5: Xuanyuan Qin, Tongyan Sun, Haobo Xu, Wenao Li, Yu Fu
- Unity version remains the current project version already in the repo
- target platform is tethered PCVR through OpenXR
- the first 30 days are focused on one polished vertical slice, not the full park
- all new production work is created under `Assets/_Project`
- Unity template/sample content may be referenced, but not modified directly unless there is no cleaner production alternative

Planning rules:

- comfort, clarity, and spectacle take priority over feature count
- every major system must be testable in isolation before world integration
- every milestone ends with a headset build and a short written check-in
- no new feature enters production after Day 24 unless it replaces a broken feature
- if performance and polish conflict, cut scope before lowering comfort or readability

## 3. Proposed Owners

These owner assignments are proposed so the team can begin immediately. Adjust only if the team's actual strengths differ substantially.

| Team Member | Primary Role | Primary Ownership | Secondary Support |
| --- | --- | --- | --- |
| Yu Fu | Technical Director / XR Lead | `Assets/_Project/Core`, XR rig, locomotion, scene loading, performance, final integration | supports scale shift, particle vitality, mount system and build stabilization |
| Xuanyuan Qin | Systems Gameplay Lead | `Features/ScaleShift`, `Features/Weather`, `Features/ParticleVitality`, core gameplay state flow | supports growth and fireworks logic |
| Haobo Xu | World Design And Mounts Lead | `World/Persistent`, `World/Regions`, cat route, traversal flow, attraction layout | supports onboarding and navigation |
| Wenao Li | Art Direction / Tech Art Lead | `Art`, `World/Shared/Lighting`, `World/Shared/Materials`, VFX look, shaders, lighting, fireworks presentation | supports world polish and visual integration |
| Tongyan Sun | Interaction, UX, Audio, QA Lead | `Features/Growth`, `Features/LotusPond`, `UI`, `Audio`, usability testing, onboarding clarity | supports playtests, comfort tuning, bug triage |

Ownership rules:

- one owner is accountable for each file area
- support members may contribute, but they do not bypass the area owner without coordination
- the master park scene must always have one active scene owner for any work period
- prefab and ScriptableObject work should be preferred over direct scene-only logic

## 4. Repo-Mapped Target File Structure

The following paths are the production target structure for the first 30 days. Some folders already exist; the listed files are the expected deliverables to populate them.

```text
Assets/_Project/
  Core/
    Bootstrap/
      AppBootstrap.prefab
      StartupSceneConfig.asset
    Input/
      WonderlandInputActions.inputactions
      InputActionRouter.cs
    Runtime/
      ServiceRegistry.cs
      WorldStateManager.cs
      GameFlowManager.cs
      ParkAttractionState.cs
    SceneManagement/
      PlayerStartAnchor.cs
      ParkZoneMarker.cs
    ScriptableObjects/
      ParkSettings_SO.asset
      ComfortSettings_SO.asset
      AttractionDescriptor_SO.asset
    Settings/
      GraphicsQualityProfile_SO.asset
      VRComfortProfile_SO.asset
    XR/
      WonderlandXROrigin.prefab
      ComfortVignetteController.cs
      XRPlayerRigController.cs
    Tests/
      PlayMode/
        XRPlayerSmokeTests.cs
        SceneLoadSmokeTests.cs
      Editor/
        ScriptableObjectValidationTests.cs

  Features/
    ScaleShift/
      Runtime/
        ScaleManager.cs
        ScaleState.cs
        ScaleTransitionController.cs
      Prefabs/
        ScaleShiftShrine.prefab
      ScriptableObjects/
        ScaleSettings_SO.asset
      Tests/
        ScaleTransitionTests.cs

    Weather/
      Runtime/
        WeatherManager.cs
        RegionWeatherResponder.cs
      Prefabs/
        WeatherShrine.prefab
      ScriptableObjects/
        WeatherPreset_Clear.asset
        WeatherPreset_Overcast.asset
        WeatherPreset_Rain.asset
        WeatherPreset_Windy.asset
      Tests/
        WeatherPresetTests.cs

    Growth/
      Runtime/
        GrowthController.cs
        GrowthStageDriver.cs
      Prefabs/
        GrowthFlowerRoute.prefab
        GrowthVineBridge.prefab
      ScriptableObjects/
        GrowthProfile_SO.asset
      Tests/
        GrowthStageTests.cs

    ParticleVitality/
      Runtime/
        ParticleCollector.cs
        ParticleShapeSystem.cs
        ParticlePreviewAnchor.cs
      Prefabs/
        ParticleSource_Flower.prefab
        ParticleShapePreview.prefab
      ScriptableObjects/
        ParticleShapeLibrary_SO.asset
      Tests/
        ParticleBudgetTests.cs

    Fireworks/
      Runtime/
        FireworkController.cs
        FireworkLaunchPad.cs
      Prefabs/
        FireworksLauncher.prefab
      ScriptableObjects/
        FireworkPatternLibrary_SO.asset
      Tests/
        FireworkPatternValidationTests.cs

    Mounts/
      Runtime/
        MountController.cs
        CatRideRouteController.cs
      Prefabs/
        CatMount.prefab
        CatRouteNode.prefab
      ScriptableObjects/
        MountSettings_SO.asset
      Tests/
        MountStateTests.cs

    LotusPond/
      Runtime/
        LotusNoteTrigger.cs
        LotusRippleController.cs
      Prefabs/
        LotusPad.prefab
        LotusMusicCluster.prefab
      ScriptableObjects/
        LotusScale_SO.asset
      Tests/
        LotusTriggerTests.cs

  World/
    Persistent/
      World_WonderlandPark.unity
      GlobalLightingProfile.asset
      ReflectionProbeProfile.asset
    Regions/
      HumanEntry/
        HumanEntry_Blockout.prefab
        HumanEntry_Landmarks.prefab
      FlowerField/
        FlowerField_Blockout.prefab
        FlowerField_Landmarks.prefab
      MushroomGrove/
        MushroomGrove_Blockout.prefab
      LotusPond/
        LotusPond_Blockout.prefab
        LotusPond_MusicStage.prefab
      CatRoute/
        CatRoute_Blockout.prefab
        CatRoute_RideAnchors.prefab
      FireworksClearing/
        FireworksClearing_Blockout.prefab
        FireworksClearing_FinaleStage.prefab
    Shared/
      Lighting/
        PP_Wonderland_Global.asset
        LightRig_Directional.prefab
      Materials/
        M_Wonderland_Toon_Master.mat
        M_Wonderland_Water.mat
        M_Wonderland_Foliage.mat
      Prefabs/
        ParkLandmarkArch.prefab
        ScenicVistaMarker.prefab
      Audio/
        RegionAudioZone.prefab

  Art/
    Shaders/
      SG_ToonMaster.shadergraph
      SG_FoliageWind.shadergraph
      SG_WaterStylized.shadergraph
    Materials/
    Models/
    Textures/
    Placeholders/

  Audio/
    Ambience/
    Music/
    SFX/
    Mixers/
      Wonderland_Main.mixer

  UI/
    Panels/
      SettingsPanel.prefab
      OnboardingPanel.prefab
    Prefabs/
      DiegeticSignpost.prefab
      AttractionMarker.prefab
      WristMenu.prefab
    Fonts/
```

## 5. Milestones Overview

| Milestone | Days | Outcome | Primary Owner | Supporting Owners |
| --- | --- | --- | --- | --- |
| M0: Production Setup | 1-3 | repo, workflow, scope, and target slice locked | Yu Fu | all |
| M1: Core Foundation | 4-8 | XR rig, scene structure, player comfort baseline, blockout route | Yu Fu | Haobo, Tongyan |
| M2: Signature Systems | 9-14 | scale, weather, growth, particles, lotus, mounts, fireworks prototype complete | Xuanyuan | Tongyan, Haobo, Wenao |
| M3: Integrated Park Slice | 15-19 | connected playable park loop in greybox with all core interactions | Haobo | all |
| M4: Visual And Immersion Pass | 20-24 | professional look, lighting, VFX, audio, onboarding, attraction identity | Wenao | Tongyan, Haobo, Xuanyuan |
| M5: Optimization And Final Release Candidate | 25-30 | stable, polished, performant build ready for showcase | Yu Fu | all |

## 6. Milestone Details

### M0: Production Setup
### Days 1-3

Objective:

- convert the team from concept mode into production mode
- lock the vertical slice scope and owner map
- define the real file structure and source-of-truth workflow

Deliverables:

- milestone plan approved by the team
- exact Day 30 vertical slice route approved
- branch naming and merge rule approved
- issue board created with owner tags
- Unity version lock confirmed for all team members
- current project hygiene issues logged and triaged

Repo outputs:

- `Docs/ExploreTheWonderland_30_Day_Production_Milestone_Plan_v1.0.md`
- `Docs/ExploreTheWonderland_M0_Execution_Packet_v1.0.md`
- `Docs/ExploreTheWonderland_M0_Issue_Board_Seed_v1.0.md`
- `Docs/ExploreTheWonderland_M0_Closeout_Checklist_v1.0.md`
- issue board seed linked from project notes and ready for external tracker creation
- target file list created for `_Project` production content

Owner actions:

- Yu Fu: lock technical baseline, merge rules, version policy, build validation checklist
- Xuanyuan: split systems backlog into implementation tickets
- Haobo: define the route and attraction order for the vertical slice
- Wenao: define the visual direction board and shader/material priorities
- Tongyan: define onboarding, comfort, UX, and playtest criteria

Exit criteria:

- no ambiguity remains about what the Day 30 build contains
- all team members know their primary ownership zones
- first implementation tickets are ready to start on Day 4

### M1: Core Foundation
### Days 4-8

Objective:

- establish the technical shell and master park scene skeleton
- make the game boot into a stable production XR setup
- make the vertical-slice world navigable inside one big scene even before features are polished
- explicitly use one big scene containing all park parts for the current slice instead of separate playable scenes

Deliverables:

- master production park scene created
- zone root structure created inside the master scene
- the team is working from one big park scene instead of separate playable scenes for M1
- production XR rig prefab created under `_Project`
- locomotion baseline implemented and tested in headset
- comfort settings panel stub exists and is reachable
- first greybox route from Human Entry to Fireworks Clearing exists inside the master scene

Required repo outputs:

- `Assets/_Project/Core/XR/WonderlandXROrigin.prefab`
- `Assets/_Project/World/Persistent/World_WonderlandPark.unity`
- `Assets/_Project/World/Regions/HumanEntry/HumanEntry_Blockout.prefab`
- `Assets/_Project/World/Regions/FlowerField/FlowerField_Blockout.prefab`
- `Assets/_Project/World/Regions/LotusPond/LotusPond_Blockout.prefab`
- `Assets/_Project/World/Regions/CatRoute/CatRoute_Blockout.prefab`
- `Assets/_Project/World/Regions/FireworksClearing/FireworksClearing_Blockout.prefab`
- `Assets/_Project/UI/Panels/SettingsPanel.prefab`

Owner breakdown:

- Yu Fu owns XR rig, locomotion, master scene boot setup, and smoke tests
- Haobo owns the route blockout, zone ownership, and landmark placement
- Tongyan owns comfort menu stub, onboarding placeholder, and readability pass
- Wenao supports landmark silhouettes and temporary materials for visual hierarchy
- Xuanyuan supports future M2 feature hooks and anchor planning

Exit criteria:

- player can enter headset and move through the greybox route inside the master scene without critical failure
- visual landmarks are readable enough to orient first-time testers
- the single-scene hierarchy and prefab structure are organized clearly enough for manual team work

### M2: Signature Systems
### Days 9-14

Objective:

- get all signature systems working at prototype quality inside `_Project`
- prioritize reliability over polish at this stage

Deliverables:

- scale shift implemented with blink transition default
- weather manager implemented with at least 4 presets
- one growth route implemented
- particle gather and 3 preset shapes implemented
- fireworks system implemented with at least 3 patterns
- cat ride implemented with guided route only for v1
- lotus interaction implemented with note, ripple, and cooldown

Required repo outputs:

- `Assets/_Project/Features/ScaleShift/Runtime/ScaleManager.cs`
- `Assets/_Project/Features/Weather/Runtime/WeatherManager.cs`
- `Assets/_Project/Features/Growth/Runtime/GrowthController.cs`
- `Assets/_Project/Features/ParticleVitality/Runtime/ParticleShapeSystem.cs`
- `Assets/_Project/Features/Fireworks/Runtime/FireworkController.cs`
- `Assets/_Project/Features/Mounts/Runtime/MountController.cs`
- `Assets/_Project/Features/LotusPond/Runtime/LotusNoteTrigger.cs`
- all first-pass ScriptableObject assets for settings and presets

Owner breakdown:

- Xuanyuan owns ScaleShift, Weather, ParticleVitality, and Fireworks gameplay logic
- Tongyan owns Growth and Lotus interaction implementation
- Haobo owns Mounts/CatRoute interaction flow and route logic
- Wenao owns fireworks presentation support and VFX look-dev hooks
- Yu Fu integrates systems into the player rig and validates scene interactions

Exit criteria:

- each system works in isolation in a sandbox or feature test setup
- no signature system hard-crashes the build or blocks play flow
- all systems expose tunable data through ScriptableObjects or prefabs, not hard-coded scene-only values

### M3: Integrated Park Slice
### Days 15-19

Objective:

- connect the systems into one coherent park experience
- prove that the slice functions as a single authored attraction loop

Deliverables:

- player start in Human Entry with onboarding guidance
- scale shift gate in Flower Field
- weather response visible in at least one land
- growth route opens one new traversal path
- particle interaction leads into a show-like payoff moment
- lotus attraction is reachable and readable
- cat ride connects park spaces in a memorable scenic beat
- fireworks finale is triggered at the end of the route

Required repo outputs:

- scene integration across all vertical-slice zones inside the master park scene
- `Assets/_Project/Core/Runtime/GameFlowManager.cs`
- `Assets/_Project/Core/Runtime/ParkAttractionState.cs`
- `Assets/_Project/UI/Prefabs/AttractionMarker.prefab`
- `Assets/_Project/UI/Panels/OnboardingPanel.prefab`

Owner breakdown:

- Haobo owns attraction layout, route pacing, landmark spacing, and region integration
- Yu Fu owns scene boot order, load safety, and integration bug fixing
- Xuanyuan owns feature handoff wiring and state transitions
- Tongyan owns clarity, interaction prompts, and first-time-user usability
- Wenao owns visual continuity between lands and spectacle staging support

Exit criteria:

- a tester can complete the whole slice with minimal explanation
- the game reads as one wonderland park, not disconnected feature rooms
- no major transition breaks immersion or causes confusion about where to go next

### M4: Visual And Immersion Pass
### Days 20-24

Objective:

- raise the slice from functional prototype to professional-looking presentation quality
- make the park feel intentional, theatrical, and immersive

Deliverables:

- global lighting direction locked
- final or near-final toon material language established
- key attraction entrances and skyline silhouettes upgraded
- scale shift VFX, pollen VFX, weather VFX, ripple VFX, ride motion VFX, and fireworks VFX upgraded
- ambience, music cues, haptics, and attraction sound design added
- onboarding and settings UI upgraded from placeholders to presentable diegetic panels

Required repo outputs:

- `Assets/_Project/Art/Shaders/SG_ToonMaster.shadergraph`
- `Assets/_Project/Art/Shaders/SG_FoliageWind.shadergraph`
- `Assets/_Project/Art/Shaders/SG_WaterStylized.shadergraph`
- `Assets/_Project/World/Shared/Lighting/PP_Wonderland_Global.asset`
- `Assets/_Project/World/Shared/Materials/M_Wonderland_Toon_Master.mat`
- `Assets/_Project/Audio/Mixers/Wonderland_Main.mixer`
- updated prefabs for landmarks, signs, attraction cues, and VFX emitters

Owner breakdown:

- Wenao owns lighting, shaders, material system, VFX language, and final visual cohesion
- Tongyan owns UI readability, audio pass, onboarding language, and haptic event coverage
- Haobo supports attraction composition and scenic beats in the regions
- Xuanyuan supports technical hookups for data-driven VFX and state-based transitions
- Yu Fu supports rendering/performance compatibility and integration fixes

Exit criteria:

- the slice no longer feels like Unity template content with custom mechanics added on top
- every attraction has at least one memorable visual or audio hook
- the first 5 minutes feel presentable enough for capture or showcase

### M5: Optimization And Final Release Candidate
### Days 25-30

Objective:

- stabilize the vertical slice into a showcase-ready build
- tune comfort and performance without sacrificing identity

Deliverables:

- full regression pass completed
- known issue list reduced to minor bugs only
- CPU and GPU profiling completed on target hardware
- heavy VFX and lighting hotspots optimized
- ride comfort and scale transition comfort tuned in headset
- one guided demo path documented and tested
- release candidate build prepared

Required repo outputs:

- bugfixes across all production paths
- tests added where practical under `_Project/Core/Tests` and feature test folders
- final milestone report with known issues and next-step backlog

Owner breakdown:

- Yu Fu owns performance profiling, build stabilization, and final integration
- Xuanyuan owns system bug fixing and state-machine cleanup
- Haobo owns route pacing cleanup, blocker removal, and scene polish support
- Wenao owns final VFX/light/material optimization decisions
- Tongyan owns playtest validation, comfort signoff, onboarding signoff, and audio polish bug list

Exit criteria:

- full slice is completable from start to finale without critical failure
- target framerate is met during normal play on target hardware
- visual effects are readable and do not cause major comfort issues
- the build is good enough to present publicly as a serious vertical slice

## 7. Detailed Day-By-Day Schedule

| Day | Primary Goal | Owner | Must Deliver | Repo Targets |
| --- | --- | --- | --- | --- |
| 1 | Scope lock, owner map, route lock | Yu Fu | approved target slice and owner matrix | this doc, ticket list |
| 2 | Repo hygiene and workflow lock | Yu Fu | merge policy, naming policy, issue board | `_Project` work conventions |
| 3 | Visual and UX target lock | Wenao / Tongyan | visual board, comfort rules, onboarding priorities | referenced in docs and task board |
| 4 | Master park scene and XR rig setup | Yu Fu | production XR rig prefab and park scene shell | `Core/XR`, `World/Persistent` |
| 5 | Zone root structure and blockout prefab pipeline | Yu Fu / Haobo | master scene roots and zone prefab workflow are in place | `World/Persistent`, `World/Regions/*` |
| 6 | Greybox route and major landmarks | Haobo | playable route through all target lands inside the master scene | blockout prefabs and park scene |
| 7 | Comfort menu and onboarding stub | Tongyan | settings panel and onboarding prompt path | `UI/Panels`, `Core/Settings` |
| 8 | First headset smoke build | Yu Fu | build runs through route without blocker | build output external to repo |
| 9 | Scale shift v1 | Xuanyuan | blink transition, rig update, tuning hooks | `Features/ScaleShift` |
| 10 | Weather v1 | Xuanyuan | 4 presets with visual response | `Features/Weather` |
| 11 | Growth v1 | Tongyan | one traversable growth route | `Features/Growth` |
| 12 | Particle vitality v1 | Xuanyuan | gather, hold, 3 preset shapes | `Features/ParticleVitality` |
| 13 | Lotus attraction v1 | Tongyan | note, ripple, dual input support | `Features/LotusPond` |
| 14 | Mount and fireworks v1 | Haobo / Xuanyuan / Wenao | guided cat route and first fireworks sequence | `Features/Mounts`, `Features/Fireworks` |
| 15 | System integration pass 1 | Yu Fu | all systems connected into the master park scene flow | `Core/Runtime`, park scene |
| 16 | Human Entry and Flower Field flow pass | Haobo | start sequence and first attraction readability | `World/Regions/HumanEntry`, `FlowerField` |
| 17 | Lotus and Cat Route flow pass | Haobo / Tongyan | lagoon attraction and cat ride pacing | `World/Regions/LotusPond`, `CatRoute` |
| 18 | Fireworks finale flow pass | Wenao / Xuanyuan | finale trigger and payoff moment | `World/Regions/FireworksClearing` |
| 19 | Full-slice playtest 1 | Tongyan | confusion list, comfort list, blocker list | test notes external or docs |
| 20 | Lighting direction lock | Wenao | global light, fog, sky, color script | `World/Shared/Lighting` |
| 21 | Shader and material pass | Wenao | toon, foliage, water baseline | `Art/Shaders`, `World/Shared/Materials` |
| 22 | VFX pass 1 | Wenao | scale, pollen, weather, ripple, fireworks VFX | feature prefabs and VFX assets |
| 23 | Audio and haptics pass 1 | Tongyan | attraction ambience and response cues | `Audio/*`, hookups in prefabs |
| 24 | Onboarding and diegetic guidance pass | Tongyan / Haobo | signs, markers, guidance cleanup | `UI/Prefabs`, park scene |
| 25 | Performance profiling pass 1 | Yu Fu | profiling report, top bottlenecks ranked | profiling notes external |
| 26 | Optimization pass 1 | Yu Fu / Wenao | VFX, light, and scene cost reductions | affected prefabs/materials/scenes |
| 27 | Bug triage and polish cut review | all | final cut list and fix assignments | task board |
| 28 | Spectacle polish pass | Wenao / Haobo | wow moments for scale reveal, cat ride, fireworks | master park scene and VFX |
| 29 | Full regression and RC prep | Yu Fu | candidate build and known issues list | build output + notes |
| 30 | Release candidate and showcase review | all | final vertical slice, next-step backlog | milestone closeout doc |

## 8. Weekly Review Gates

### End Of Week 1 Gate

Questions:

- can the player move through the route in headset without critical failure?
- does the master park scene exist, open cleanly, and keep all zones organized without missing references?
- is the team working only in `_Project` for new production content?

If not passed:

- stop new feature work
- spend Days 8-9 fixing foundation before continuing

### End Of Week 2 Gate

Questions:

- do all signature systems exist at prototype quality?
- can each system be demonstrated in isolation?
- are all systems data-driven enough to tune without code edits every time?

If not passed:

- cut optional variants immediately
- freeze smooth scale transition, manual cat control, and formula-input features

### End Of Week 3 Gate

Questions:

- does the project read as one connected amusement-park slice?
- can a tester understand the route without hand-holding?
- are there at least three standout moments worth showing publicly?

If not passed:

- collapse world scope and remove one land if necessary
- focus only on Human Entry, Lotus, Cat Route, and Fireworks finale

### End Of Week 4 Gate

Questions:

- is the build stable enough for a recorded demo?
- does it hold target performance during normal play?
- does it feel professional rather than academic prototype-only?

If not passed:

- cut low-value polish tasks
- fix only blockers, comfort issues, and showcase-critical presentation issues

## 9. Quality Standards

### Visual Quality Standard

The build should avoid looking like unmodified VR template content. To meet the standard:

- each land needs its own silhouette, palette bias, and environmental identity
- shared materials must still feel part of one coherent park art direction
- particle systems must have readable shapes, disciplined timing, and good scale readability
- fireworks, scale shift, and lotus ripple effects must feel intentional and staged

### Comfort Standard

The build is not acceptable if:

- scale shift causes camera instability or disorientation
- cat ride movement feels jerky or over-accelerated
- VFX obstruct the center view excessively
- onboarding or navigation forces excessive wandering in headset

### Engineering Standard

The build is not acceptable if:

- critical features only work in one scene by fragile scene references
- feature logic lives directly in world scenes without prefabs or data assets
- content depends on editing template/sample files instead of `_Project`
- scene loading or XR setup breaks after branch merges

## 10. Daily Working Rhythm

Every day should include:

- 15-minute standup: blockers, owner updates, merge risks
- one midday sync for any master-scene or zone-ownership conflicts
- one headset smoke test before end of day
- one short written update in the task board with files touched, current status, and blocker level

Every three days:

- run a profiling pass on the latest integrated build
- test the full path from Human Entry to Fireworks Clearing
- capture one short gameplay video for alignment

## 11. Scope Control And Planned Cuts

If schedule pressure appears, cut in this order:

1. manual cat control mode
2. smooth scale transition mode
3. extra fireworks patterns beyond the minimum
4. extra weather variants beyond the minimum four
5. extra particle shapes beyond the minimum three
6. Mushroom Grove from the Day 30 route, if necessary

Do not cut these without replacing them:

- stable XR locomotion
- scale shift
- one polished attraction loop
- one strong finale spectacle
- visual cohesion and readable onboarding

## 12. Final Day 30 Acceptance Checklist

The Day 30 milestone is complete only if all items below are true:

- the build starts in `World_WonderlandPark` and places the player correctly in Human Entry
- the player can comfortably traverse the park loop in headset
- scale shift works reliably and feels immersive
- at least one weather change has visible world impact
- at least one growth route changes traversal
- particle shaping is usable and legible
- lotus interaction works and feels staged as an attraction
- the cat ride is memorable and comfortable
- the fireworks finale lands as a payoff moment
- the art, lighting, VFX, audio, and UI feel coherent
- target hardware performance is acceptable during normal play
- known issues are documented and none are critical blockers

## 13. Immediate Next Actions

The team should do these next, in order:

1. Review and approve this milestone document.
2. Confirm whether the proposed owners match actual team strengths.
3. Create implementation tickets for Days 1-8 immediately.
4. Start producing all new runtime work inside `Assets/_Project` only.
5. Treat the current Unity template scene as temporary reference, not production structure.

## 14. Notes On Existing Repo State

This plan is intentionally grounded in the current repository state:

- `Assets/_Project` already contains the correct high-level folder structure
- `Assets/_Project/World/Regions` already contains the right first-slice region folders
- `Docs/ExploreTheWonderland_PRD_TDD_v1.3.2.md` now frames the project correctly as a wonderland park
- the actual game-specific implementation is still mostly unbuilt, so the next 30 days should focus on production execution, not more concept expansion
