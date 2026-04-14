# Explore The Wonderland
## Start Here: M1 Kickoff Guide

- Date: March 27, 2026
- Audience: Group 5 kickoff team
- Goal: Start M1 cleanly from the current repo without guessing what to do first

Current demonstration note:

- the park should feel exploratory and open, and any route-order language in this guide should be read as a suggested orientation spine for readability and demoing rather than a forced player sequence

## 1. What This Repo Is Right Now

The repo is ready for M1 planning-wise, but not implementation-complete yet.

Current confirmed baseline:

- Unity version is locked to `6000.3.11f1`
- Render pipeline is `URP`
- XR stack is `OpenXR` on tethered `PCVR`
- Input path is the Unity `Input System`
- Production content must live under `Assets/_Project`

Current known gaps before the slice becomes playable:

- the intended production master scene `Assets/_Project/World/Persistent/World_WonderlandPark.unity` does not exist yet
- the repo still contains template scenes under `Assets/Scenes`
- build settings are still expected to point at template content until M1 work replaces that setup
- most `_Project` folders exist, but still need real production assets and scripts

This means the correct way to start is:

1. confirm the team baseline
2. create the production scene and XR foundation
3. build blockout prefabs for the route
4. integrate the route in one master scene
5. do the first headset smoke pass

## 2. First Team Sync

Do this before anyone starts editing Unity scenes:

1. Confirm every teammate installs Unity `6000.3.11f1`.
2. Confirm the Day 30 attraction arc is locked for demoing and readability:
   `Arrival Plaza -> Flower Field -> Lotus Lagoon -> Cat Route -> Fireworks Plaza`
3. Confirm branch naming rules from the execution packet.
4. Copy the issue seed from `Docs/ExploreTheWonderland_M0_Issue_Board_Seed_v1.0.md` into your real tracker.
5. Record one active owner for the future master scene before scene edits begin.
6. Agree that new production work goes only under `Assets/_Project`.

## 3. Machine Setup

Each teammate should do this on their own machine:

1. Clone the repo.
2. Open the project in Unity Hub using editor `6000.3.11f1`.
3. Let Unity finish package import and script compilation fully.
4. Open `Packages/manifest.json` and verify the project resolved the XR and URP stack:
   `com.unity.xr.openxr`
   `com.unity.xr.interaction.toolkit`
   `com.unity.xr.hands`
   `com.unity.render-pipelines.universal`
5. Open the project once without changing files just to confirm there are no import-blocking errors.
6. Do not begin production work inside `Assets/Samples` or `Assets/Scenes`.

## 4. Branch Setup

Before doing any work, create a focused branch.

Use one of these patterns:

- `feature/<owner>-<area>-<short-name>`
- `fix/<owner>-<area>-<short-name>`
- `art/<owner>-<area>-<short-name>`
- `prototype/<owner>-<area>-<short-name>`
- `docs/<owner>-<area>-<short-name>`

Examples:

- `feature/yu-fu-world-master-scene`
- `feature/haobo-xu-world-zone-blockouts`
- `feature/tongyan-sun-ui-onboarding-stub`
- `art/wenao-li-world-lighting-pass`

## 5. Day 1 Start Order

Start in this order so the team does not block itself.

### Step 1: Create the production master scene

Owner: Yu Fu

Do this first:

1. Create `Assets/_Project/World/Persistent/World_WonderlandPark.unity`.
2. Open it and verify it saves cleanly.
3. Add clean root objects for:
   `GlobalSystems`
   `XR`
   `HumanEntry`
   `FlowerField`
   `LotusPond`
   `CatRoute`
   `FireworksClearing`
   `Lighting`
   `Debug`
4. Commit this before broader world integration starts.

This completes the foundation for:

- `M1-01 Create master production park scene`
- `M1-03 Create master scene zone root structure`

### Step 2: Create the production XR rig prefab

Owner: Yu Fu

Immediately after the scene exists:

1. Create `Assets/_Project/Core/XR/WonderlandXROrigin.prefab`.
2. Base it on the current XR template only as reference, not as the final ownership location.
3. Make sure it is production-owned under `_Project`.
4. Place the prefab into the `XR` root of `World_WonderlandPark.unity`.
5. Verify spawn location and basic headset tracking.

This completes:

- `M1-02 Create production XR rig prefab`

### Step 3: Build zone blockout prefabs outside the master scene

Owner: Haobo Xu

Create these prefabs in parallel after the master scene path is established:

1. `Assets/_Project/World/Regions/HumanEntry/HumanEntry_Blockout.prefab`
2. `Assets/_Project/World/Regions/FlowerField/FlowerField_Blockout.prefab`
3. `Assets/_Project/World/Regions/LotusPond/LotusPond_Blockout.prefab`
4. `Assets/_Project/World/Regions/CatRoute/CatRoute_Blockout.prefab`
5. `Assets/_Project/World/Regions/FireworksClearing/FireworksClearing_Blockout.prefab`

Rules for this step:

- build them as reusable prefabs, not scene-only assemblies
- use placeholders first
- optimize for route readability, comfort, and landmark clarity
- keep scene logic minimal

This covers:

- `M1-04` through `M1-08`

### Step 4: Create UI stubs needed for onboarding

Owner: Tongyan Sun

Create:

1. `Assets/_Project/UI/Panels/SettingsPanel.prefab`
2. `Assets/_Project/UI/Panels/OnboardingPanel.prefab`

Keep them simple:

- placeholder text is fine
- they only need enough structure to support the first onboarding pass
- comfort settings should be clearly reserved in the UI layout

This covers:

- `M1-09`
- `M1-10`

### Step 5: Support work while M1 foundations are being built

Recommended parallel support:

- Xuanyuan Qin reviews feature module folders and creates runtime script stubs only if it helps unblock M2
- Wenao Li prepares placeholder lighting, material, and VFX direction under `Assets/_Project/World/Shared` and `Assets/_Project/Art`
- nobody except the active scene owner edits the master scene during this phase

## 6. Day 2 To Day 4 Integration Order

Once the prefabs exist, continue in this order:

1. Place all zone blockout prefabs into `World_WonderlandPark.unity`.
2. Build a readable orientation spine in the locked order.
3. Confirm the next destination is always visually readable.
4. Add the onboarding panel where the route begins.
5. Add temporary lighting and scale landmarks so the route reads in-headset.
6. Run the first headset smoke build.

This corresponds to:

- `M1-11 Place all zone prefabs into the master park scene and build the route`
- `M1-12 First headset smoke build`

## 7. First Acceptance Check

Do not move on to M2 systems until all of this is true:

1. The project opens without import blockers.
2. `World_WonderlandPark.unity` exists and opens cleanly.
3. The XR rig prefab is in the scene and spawns correctly.
4. The suggested orientation spine is readable and walkable:
   Human Entry -> Flower Field -> Lotus Pond -> Cat Route -> Fireworks Clearing
5. The scene has no critical missing references.
6. The team can do one headset smoke pass without a show-stopping blocker.

## 8. What Not To Do

Avoid these mistakes at the start:

- do not build new production logic in `Assets/Samples`
- do not keep using `Assets/Scenes/SampleScene.unity` as the real slice scene
- do not let multiple people co-edit the master scene without an active owner
- do not put mechanic logic directly into the scene when it should live in prefabs or feature scripts
- do not wait for final art before validating traversal, comfort, and route readability

## 9. Immediate Next Tickets After M1 Foundation

When the first headset smoke pass succeeds, start M2 in this order:

1. `ScaleManager.cs`
2. `WeatherManager.cs`
3. `GrowthController.cs`
4. `ParticleShapeSystem.cs`
5. `LotusNoteTrigger.cs`
6. `MountController.cs`
7. `FireworkController.cs`

Keep each system inside its feature folder under `Assets/_Project/Features`.

## 10. Reference Docs

Use these as the source of truth:

- `Docs/ExploreTheWonderland_PRD_TDD_v1.3.2.md`
- `Docs/ExploreTheWonderland_30_Day_Production_Milestone_Plan_v1.0.md`
- `Docs/ExploreTheWonderland_M0_Execution_Packet_v1.0.md`
- `Docs/ExploreTheWonderland_M0_Issue_Board_Seed_v1.0.md`
- `Docs/ExploreTheWonderland_M0_Closeout_Checklist_v1.0.md`

If the team follows this order, M1 starts with the least merge risk and the clearest path to a playable vertical-slice foundation.
