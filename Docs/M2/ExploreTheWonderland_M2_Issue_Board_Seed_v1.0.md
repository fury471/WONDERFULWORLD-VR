# Explore The Wonderland
## M2 Issue Board Seed v1.0

- Date: April 7, 2026
- Purpose: Provide an owner-tagged M2 tracker with current repo status
- Usage: Copy into GitHub Projects, Jira, Trello, or use directly as the in-repo M2 tracker

## 1. Label Set

Owner labels:

- `owner:yu-fu`
- `owner:xuanyuan-qin`
- `owner:haobo-xu`
- `owner:wenao-li`
- `owner:tongyan-sun`

Area labels:

- `area:scale-shift`
- `area:weather`
- `area:growth`
- `area:particle-vitality`
- `area:lotus-pond`
- `area:mounts`
- `area:fireworks`
- `area:art`
- `area:vfx`
- `area:core`
- `area:docs`
- `area:qa`

Priority labels:

- `priority:p0`
- `priority:p1`
- `priority:p2`

Milestone labels:

- `milestone:m2`

Status labels:

- `status:ready`
- `status:in-progress`
- `status:blocked`
- `status:needs-review`
- `status:done`

## 2. Current M2 Ticket Status

M2 implementation note:

- every system should work first in a sandbox scene or isolated test setup
- master-scene integration belongs to M3 unless a tiny support hook is truly required

| ID | Title | Owner | Labels | Repo Target | Current Repo State | What Is Left |
| --- | --- | --- | --- | --- | --- | --- |
| M2-01 | Implement scale manager v1 | Xuanyuan Qin | `owner:xuanyuan-qin`, `area:scale-shift`, `priority:p0`, `milestone:m2`, `status:needs-review` | `Assets/_Project/Features/ScaleShift/Runtime/ScaleManager.cs` | runtime and tuning asset landed | isolated demo and QA confirmation |
| M2-02 | Implement weather manager v1 | Xuanyuan Qin | `owner:xuanyuan-qin`, `area:weather`, `priority:p0`, `milestone:m2`, `status:needs-review` | `Assets/_Project/Features/Weather/Runtime/WeatherManager.cs` | runtime plus 4 preset assets landed | isolated demo and world-response validation |
| M2-03 | Implement growth traversal v1 | Tongyan Sun | `owner:tongyan-sun`, `area:growth`, `priority:p0`, `milestone:m2`, `status:needs-review` | `Assets/_Project/Features/Growth/Runtime/GrowthController.cs` | runtime and growth profile landed | isolated traversal demo |
| M2-04 | Implement particle vitality v1 | Xuanyuan Qin | `owner:xuanyuan-qin`, `area:particle-vitality`, `priority:p0`, `milestone:m2`, `status:needs-review` | `Assets/_Project/Features/ParticleVitality/Runtime/ParticleShapeSystem.cs` | runtime and shape library landed | isolated 3-shape demo |
| M2-05 | Implement lotus interaction v1 | Tongyan Sun | `owner:tongyan-sun`, `area:lotus-pond`, `priority:p0`, `milestone:m2`, `status:in-progress` | `Assets/_Project/Features/LotusPond/Runtime/LotusNoteTrigger.cs` | runtime landed, debug helpers landed | `LotusScale_SO.asset` and isolated demo |
| M2-06 | Implement cat ride route v1 | Haobo Xu | `owner:haobo-xu`, `area:mounts`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/Mounts/Runtime/MountController.cs` | folder still empty | runtime, settings asset, and first demo |
| M2-07 | Implement fireworks controller v1 | Xuanyuan Qin | `owner:xuanyuan-qin`, `area:fireworks`, `priority:p0`, `milestone:m2`, `status:needs-review` | `Assets/_Project/Features/Fireworks/Runtime/FireworkController.cs` | runtime and pattern library landed | isolated 3-pattern demo |
| M2-08 | Establish slice lighting and VFX language | Wenao Li | `owner:wenao-li`, `area:art`, `area:vfx`, `priority:p1`, `milestone:m2`, `status:ready` | `Assets/_Project/World/Shared/Lighting`, `Assets/_Project/Art/Shaders` | no repo assets landed yet | first-pass look-dev hooks |
| M2-09 | Create first-pass ScriptableObject tuning assets | Xuanyuan Qin / Tongyan Sun / Haobo Xu | `area:core`, `area:docs`, `priority:p1`, `milestone:m2`, `status:in-progress` | feature `ScriptableObjects/` folders | ScaleShift, Weather, Growth, ParticleVitality, Fireworks assets landed | Lotus and Mounts assets still missing |
| M2-10 | Review XR/player-rig handoff hooks for M3 | Yu Fu | `owner:yu-fu`, `area:core`, `priority:p1`, `milestone:m2`, `status:in-progress` | `_Project/Core`, `_Project/Core/XR` | XR rig touched recently | explicit handoff notes or integration review still needed |

## 3. Suggested Board Columns

Recommended board columns:

1. Ready
2. In Progress
3. Needs Review
4. Blocked
5. Done

Recommended working rule:

- no feature is considered done until it works outside the master park scene
