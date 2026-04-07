# Explore The Wonderland
## M1 Issue Board Seed v1.0

- Date: April 2, 2026
- Purpose: Provide a milestone-specific M1 board summary and closeout task set
- Usage: Copy into GitHub Projects, Jira, Trello, or use directly as the in-repo M1 tracker

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
- `area:ui`
- `area:docs`
- `area:qa`

Priority labels:

- `priority:p0`
- `priority:p1`
- `priority:p2`

Milestone labels:

- `milestone:m1`

Status labels:

- `status:ready`
- `status:in-progress`
- `status:blocked`
- `status:done`
- `status:needs-human-signoff`

## 2. M1 Ticket Summary

| ID | Title | Owner | Labels | Repo Target | Current State | Remaining Work |
| --- | --- | --- | --- | --- | --- | --- |
| M1-01 | Create master production park scene | Yu Fu | `owner:yu-fu`, `area:world`, `priority:p0`, `milestone:m1`, `status:done` | `Assets/_Project/World/Persistent/World_WonderlandPark.unity` | Done | none |
| M1-02 | Create production XR rig prefab | Yu Fu | `owner:yu-fu`, `area:core`, `area:xr`, `priority:p0`, `milestone:m1`, `status:done` | `Assets/_Project/Core/XR/WonderlandXROrigin.prefab` | Done | none |
| M1-03 | Create master scene zone root structure | Yu Fu | `owner:yu-fu`, `area:world`, `priority:p0`, `milestone:m1`, `status:done` | `Assets/_Project/World/Persistent/World_WonderlandPark.unity` | Done | none |
| M1-04 | Create Human Entry blockout prefab | Haobo Xu | `owner:haobo-xu`, `area:world`, `priority:p0`, `milestone:m1`, `status:done` | `Assets/_Project/World/Regions/HumanEntry/HumanEntry_Blockout.prefab` | Done | none |
| M1-05 | Create Flower Field blockout prefab | Haobo Xu | `owner:haobo-xu`, `area:world`, `priority:p0`, `milestone:m1`, `status:done` | `Assets/_Project/World/Regions/FlowerField/FlowerField_Blockout.prefab` | Done | none |
| M1-06 | Create Lotus Pond blockout prefab | Haobo Xu | `owner:haobo-xu`, `area:world`, `priority:p0`, `milestone:m1`, `status:done` | `Assets/_Project/World/Regions/LotusPond/LotusPond_Blockout.prefab` | Done | none |
| M1-07 | Create Cat Route blockout prefab | Haobo Xu | `owner:haobo-xu`, `area:world`, `priority:p0`, `milestone:m1`, `status:done` | `Assets/_Project/World/Regions/CatRoute/CatRoute_Blockout.prefab` | Done | none |
| M1-08 | Create Fireworks Clearing blockout prefab | Haobo Xu | `owner:haobo-xu`, `area:world`, `priority:p0`, `milestone:m1`, `status:done` | `Assets/_Project/World/Regions/FireworksClearing/FireworksClearing_Blockout.prefab` | Done | none |
| M1-09 | Create settings panel stub | Tongyan Sun | `owner:tongyan-sun`, `area:ui`, `priority:p1`, `milestone:m1`, `status:done` | `Assets/_Project/UI/Panels/SettingsPanel.prefab` | Done | none |
| M1-10 | Create onboarding panel stub | Tongyan Sun | `owner:tongyan-sun`, `area:ui`, `priority:p1`, `milestone:m1`, `status:done` | `Assets/_Project/UI/Panels/OnboardingPanel.prefab` | Done | none |
| M1-11 | Place all zone prefabs into the master park scene and build the route | Haobo Xu / Yu Fu | `owner:haobo-xu`, `owner:yu-fu`, `area:world`, `priority:p0`, `milestone:m1`, `status:done` | master park scene plus blockout prefabs | Done | none |
| M1-12 | First headset smoke build | Yu Fu | `owner:yu-fu`, `area:xr`, `area:qa`, `priority:p0`, `milestone:m1`, `status:needs-human-signoff` | build output external to repo | Pending human verification | complete one headset smoke pass and record result |

## 3. M1 Closeout Tickets

| ID | Title | Owner | Labels | Repo Target | Depends On | Definition Of Done |
| --- | --- | --- | --- | --- | --- | --- |
| M1-C01 | Record M1 smoke-check results | Yu Fu | `owner:yu-fu`, `area:qa`, `priority:p0`, `milestone:m1`, `status:ready` | `Docs/M1/ExploreTheWonderland_M1_Closeout_Checklist_v1.0.md` | M1-12 | section 5 is filled with one real smoke pass result |
| M1-C02 | Team decision on teleport scope | all | `area:xr`, `area:qa`, `priority:p1`, `milestone:m1`, `status:ready` | closeout checklist | M1-C01 | team explicitly records whether teleport is required for M1 signoff |
| M1-C03 | Team decision on locomotion vignette scope | all | `area:xr`, `area:qa`, `priority:p1`, `milestone:m1`, `status:ready` | closeout checklist | M1-C01 | team explicitly records whether vignette is required for M1 signoff |
| M1-C04 | Final M1 signoff | all | `area:docs`, `area:qa`, `priority:p0`, `milestone:m1`, `status:ready` | `Docs/M1/ExploreTheWonderland_M1_Closeout_Checklist_v1.0.md` | M1-C01, M1-C02, M1-C03 | all section 6 checkboxes are resolved |

## 4. Suggested Board Columns

Recommended board columns:

1. Done In Repo
2. Needs Human Signoff
3. Blocked
4. Signed Off
