# Explore The Wonderland
## M0 Issue Board Seed v1.0

- Date: March 27, 2026
- Purpose: Provide an owner-tagged starter board so M1 work can begin immediately
- Usage: Copy into GitHub Projects, Jira, Trello, or use directly as the in-repo planning board

## 1. Label Set

Owner labels:

- `owner:yu-fu`
- `owner:xuanyuan-qin`
- `owner:haobo-xu`
- `owner:wenao-li`
- `owner:tongyan-sun`

Area labels:

- `area:core`
- `area:xr`
- `area:world`
- `area:scale-shift`
- `area:weather`
- `area:growth`
- `area:particle-vitality`
- `area:lotus-pond`
- `area:mounts`
- `area:fireworks`
- `area:ui`
- `area:audio`
- `area:art`
- `area:vfx`
- `area:docs`

Priority labels:

- `priority:p0`
- `priority:p1`
- `priority:p2`

Milestone labels:

- `milestone:m1`
- `milestone:m2`
- `milestone:m3`
- `milestone:m4`
- `milestone:m5`

Status labels:

- `status:ready`
- `status:in-progress`
- `status:blocked`
- `status:done`

## 2. Ready-To-Start Tickets

Single-scene implementation note:

- M1 is based on one master park scene containing all major parts
- zone folders still exist, but they feed prefabs into the master scene rather than separate playable scenes

| ID | Title | Owner | Labels | Repo Target | Depends On | Definition Of Done |
| --- | --- | --- | --- | --- | --- | --- |
| M1-01 | Create master production park scene | Yu Fu | `owner:yu-fu`, `area:core`, `area:world`, `priority:p0`, `milestone:m1`, `status:ready` | `Assets/_Project/World/Persistent/World_WonderlandPark.unity` | none | master scene exists, opens cleanly, and is ready to host XR, globals, and all park zones |
| M1-02 | Create production XR rig prefab | Yu Fu | `owner:yu-fu`, `area:core`, `area:xr`, `priority:p0`, `milestone:m1`, `status:ready` | `Assets/_Project/Core/XR/WonderlandXROrigin.prefab` | M1-01 | prefab exists, is not template-only, and is ready for production hooks |
| M1-03 | Create master scene zone root structure | Yu Fu | `owner:yu-fu`, `area:core`, `area:world`, `priority:p0`, `milestone:m1`, `status:ready` | `Assets/_Project/World/Persistent/World_WonderlandPark.unity` | M1-01 | scene contains clean roots for GlobalSystems, XR, HumanEntry, FlowerField, LotusPond, CatRoute, FireworksClearing, Lighting, and Debug |
| M1-04 | Create Human Entry blockout prefab | Haobo Xu | `owner:haobo-xu`, `area:world`, `priority:p0`, `milestone:m1`, `status:ready` | `Assets/_Project/World/Regions/HumanEntry/HumanEntry_Blockout.prefab` | M1-01 | prefab exists with start-space composition and onboarding-friendly sightlines |
| M1-05 | Create Flower Field blockout prefab | Haobo Xu | `owner:haobo-xu`, `area:world`, `priority:p0`, `milestone:m1`, `status:ready` | `Assets/_Project/World/Regions/FlowerField/FlowerField_Blockout.prefab` | M1-01 | prefab exists with basic scale-awe route space and landmark silhouette |
| M1-06 | Create Lotus Pond blockout prefab | Haobo Xu | `owner:haobo-xu`, `area:world`, `priority:p0`, `milestone:m1`, `status:ready` | `Assets/_Project/World/Regions/LotusPond/LotusPond_Blockout.prefab` | M1-01 | prefab exists with attraction staging area and clear focal point |
| M1-07 | Create Cat Route blockout prefab | Haobo Xu | `owner:haobo-xu`, `area:world`, `priority:p0`, `milestone:m1`, `status:ready` | `Assets/_Project/World/Regions/CatRoute/CatRoute_Blockout.prefab` | M1-01 | prefab exists with route corridor and ride staging anchors |
| M1-08 | Create Fireworks Clearing blockout prefab | Haobo Xu | `owner:haobo-xu`, `area:world`, `priority:p0`, `milestone:m1`, `status:ready` | `Assets/_Project/World/Regions/FireworksClearing/FireworksClearing_Blockout.prefab` | M1-01 | prefab exists with finale reveal space and open-sky composition |
| M1-09 | Create settings panel stub | Tongyan Sun | `owner:tongyan-sun`, `area:ui`, `priority:p1`, `milestone:m1`, `status:ready` | `Assets/_Project/UI/Panels/SettingsPanel.prefab` | M1-02 | panel exists with placeholders for comfort settings |
| M1-10 | Create onboarding panel stub | Tongyan Sun | `owner:tongyan-sun`, `area:ui`, `priority:p1`, `milestone:m1`, `status:ready` | `Assets/_Project/UI/Panels/OnboardingPanel.prefab` | M1-02 | panel exists and supports step-by-step onboarding text |
| M1-11 | Place all zone prefabs into the master park scene and build the route | Haobo Xu | `owner:haobo-xu`, `area:world`, `priority:p0`, `milestone:m1`, `status:ready` | `Assets/_Project/World/Persistent/World_WonderlandPark.unity` and blockout prefabs | M1-04, M1-05, M1-06, M1-07, M1-08 | route is navigable inside the master scene and major destinations are visually readable |
| M1-12 | First headset smoke build | Yu Fu | `owner:yu-fu`, `area:core`, `area:xr`, `priority:p0`, `milestone:m1`, `status:ready` | build output | M1-01 through M1-11 | player can enter headset and move through the route without a critical blocker |
| M2-01 | Implement scale manager v1 | Xuanyuan Qin | `owner:xuanyuan-qin`, `area:scale-shift`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/ScaleShift/Runtime/ScaleManager.cs` | M1-02 | blink scale transition works and updates rig state safely |
| M2-02 | Implement weather manager v1 | Xuanyuan Qin | `owner:xuanyuan-qin`, `area:weather`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/Weather/Runtime/WeatherManager.cs` | M1-01 | 4 presets switch global look and ambience cleanly |
| M2-03 | Implement growth traversal v1 | Tongyan Sun | `owner:tongyan-sun`, `area:growth`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/Growth/Runtime/GrowthController.cs` | M1-05 | one growth interaction changes traversal state and collider state |
| M2-04 | Implement particle vitality v1 | Xuanyuan Qin | `owner:xuanyuan-qin`, `area:particle-vitality`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/ParticleVitality/Runtime/ParticleShapeSystem.cs` | M1-05 | gather, hold, and 3 shapes work without obvious instability |
| M2-05 | Implement lotus interaction v1 | Tongyan Sun | `owner:tongyan-sun`, `area:lotus-pond`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/LotusPond/Runtime/LotusNoteTrigger.cs` | M1-06 | note + ripple + cooldown works with both hands |
| M2-06 | Implement cat ride route v1 | Haobo Xu | `owner:haobo-xu`, `area:mounts`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/Mounts/Runtime/MountController.cs` | M1-07 | guided ride starts, traverses route, and ends safely |
| M2-07 | Implement fireworks controller v1 | Xuanyuan Qin | `owner:xuanyuan-qin`, `area:fireworks`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/Fireworks/Runtime/FireworkController.cs` | M1-08 | fireworks sequence triggers with at least 3 patterns |
| M2-08 | Establish slice lighting and VFX language | Wenao Li | `owner:wenao-li`, `area:art`, `area:vfx`, `priority:p1`, `milestone:m2`, `status:ready` | `Assets/_Project/World/Shared/Lighting`, `Assets/_Project/Art/Shaders` | M1-11 | first-pass park lighting and VFX direction exists for integration |

## 3. Suggested Board Columns

Recommended board columns:

1. Ready
2. In Progress
3. Needs Review
4. Blocked
5. Done

Recommended working rule:

- no more than one `In Progress` ticket that edits the master park scene at a time without explicit coordination
- all `priority:p0` M1 tickets should be created in the real issue tracker before M1 begins

## 4. Minimum M0 Board Completion Standard

M0 is ready to hand off to M1 when:

- all `M1-01` through `M1-12` tickets exist in the team's chosen board
- every ticket has an owner label and milestone label
- every ticket has a repo target and definition of done
