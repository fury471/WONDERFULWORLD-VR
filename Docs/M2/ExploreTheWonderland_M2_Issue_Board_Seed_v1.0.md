# Explore The Wonderland
## M2 Issue Board Seed v1.0

- Date: April 2, 2026
- Purpose: Provide an owner-tagged starter board so M2 work can proceed in isolated feature branches
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

## 2. Ready-To-Start Tickets

M2 implementation note:

- every system should work first in a sandbox scene or isolated test setup
- master-scene integration belongs to M3 unless a tiny support hook is truly required

| ID | Title | Owner | Labels | Repo Target | Depends On | Definition Of Done |
| --- | --- | --- | --- | --- | --- | --- |
| M2-01 | Implement scale manager v1 | Xuanyuan Qin | `owner:xuanyuan-qin`, `area:scale-shift`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/ScaleShift/Runtime/ScaleManager.cs` | M1 repo-side complete | blink transition works and exposes tuning hooks |
| M2-02 | Implement weather manager v1 | Xuanyuan Qin | `owner:xuanyuan-qin`, `area:weather`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/Weather/Runtime/WeatherManager.cs` | M1 repo-side complete | 4 presets switch global look and ambience cleanly |
| M2-03 | Implement growth traversal v1 | Tongyan Sun | `owner:tongyan-sun`, `area:growth`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/Growth/Runtime/GrowthController.cs` | M1 repo-side complete | one growth interaction changes traversal and collider state |
| M2-04 | Implement particle vitality v1 | Xuanyuan Qin | `owner:xuanyuan-qin`, `area:particle-vitality`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/ParticleVitality/Runtime/ParticleShapeSystem.cs` | M1 repo-side complete | gather, hold, and 3 preset shapes work without obvious instability |
| M2-05 | Implement lotus interaction v1 | Tongyan Sun | `owner:tongyan-sun`, `area:lotus-pond`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/LotusPond/Runtime/LotusNoteTrigger.cs` | M1 repo-side complete | note + ripple + cooldown works with both hands |
| M2-06 | Implement cat ride route v1 | Haobo Xu | `owner:haobo-xu`, `area:mounts`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/Mounts/Runtime/MountController.cs` | M1 repo-side complete | guided ride starts, traverses route, and ends safely |
| M2-07 | Implement fireworks controller v1 | Xuanyuan Qin | `owner:xuanyuan-qin`, `area:fireworks`, `priority:p0`, `milestone:m2`, `status:ready` | `Assets/_Project/Features/Fireworks/Runtime/FireworkController.cs` | M1 repo-side complete | fireworks sequence triggers with at least 3 patterns |
| M2-08 | Establish slice lighting and VFX language | Wenao Li | `owner:wenao-li`, `area:art`, `area:vfx`, `priority:p1`, `milestone:m2`, `status:ready` | `Assets/_Project/World/Shared/Lighting`, `Assets/_Project/Art/Shaders` | M1 repo-side complete | first-pass lighting and VFX direction exists for integration |
| M2-09 | Create first-pass ScriptableObject tuning assets | Xuanyuan Qin / Tongyan Sun / Haobo Xu | `area:core`, `area:docs`, `priority:p1`, `milestone:m2`, `status:ready` | feature `ScriptableObjects/` folders | M2-01 through M2-07 parallel | all core M2 systems have non-scene-only tuning assets |
| M2-10 | Review XR/player-rig handoff hooks for M3 | Yu Fu | `owner:yu-fu`, `area:core`, `priority:p1`, `milestone:m2`, `status:ready` | `_Project/Core`, `_Project/Core/XR` | M2 systems begin landing | shared integration risks are documented before M3 |

## 3. Suggested Board Columns

Recommended board columns:

1. Ready
2. In Progress
3. Needs Review
4. Blocked
5. Done

Recommended working rule:

- no feature is considered done until it works outside the master park scene
