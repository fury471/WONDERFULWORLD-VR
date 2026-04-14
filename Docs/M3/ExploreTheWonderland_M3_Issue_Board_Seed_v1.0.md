# Explore The Wonderland
## M3 Issue Board Seed v1.0

- Date: April 14, 2026
- Purpose: Provide an owner-tagged starter board so M3 exploratory park integration can begin immediately
- Usage: Copy into GitHub Projects, Jira, Trello, or use directly as the in-repo M3 tracker

## 1. Label Set

Owner labels:

- `owner:yu-fu`
- `owner:xuanyuan-qin`
- `owner:haobo-xu`
- `owner:wenao-li`
- `owner:tongyan-sun`

Area labels:

- `area:core`
- `area:world`
- `area:ui`
- `area:exploration`
- `area:boundaries`
- `area:scale-shift`
- `area:weather`
- `area:growth`
- `area:particle-vitality`
- `area:lotus-pond`
- `area:mounts`
- `area:fireworks`
- `area:art`
- `area:vfx`
- `area:qa`

Priority labels:

- `priority:p0`
- `priority:p1`
- `priority:p2`

Milestone labels:

- `milestone:m3`

Status labels:

- `status:ready`
- `status:in-progress`
- `status:blocked`
- `status:needs-review`
- `status:done`

## 2. Ready-To-Start Tickets

M3 integration note:

- the park should feel exploratory and open
- the player should not be forced through a strict sequence
- attraction logic should be staged as discoverable destinations and connectors
- route references below describe the suggested readability spine for integration, not a mandatory mission order

| ID | Title | Owner | Labels | Repo Target | Depends On | Definition Of Done |
| --- | --- | --- | --- | --- | --- | --- |
| M3-01 | Create park bootstrap and discovery manager | Yu Fu | `owner:yu-fu`, `area:core`, `area:exploration`, `priority:p0`, `milestone:m3`, `status:ready` | `Assets/_Project/Core/Runtime/GameFlowManager.cs` | M2 closeout state | park bootstrap and attraction discovery state are explicit without enforcing strict sequence |
| M3-02 | Create attraction state container | Yu Fu | `owner:yu-fu`, `area:core`, `area:exploration`, `priority:p0`, `milestone:m3`, `status:ready` | `Assets/_Project/Core/Runtime/ParkAttractionState.cs` | M3-01 | attraction availability, discovery, and readability state are explicit |
| M3-03 | Integrate onboarding release flow in Human Entry | Tongyan Sun / Yu Fu | `owner:tongyan-sun`, `owner:yu-fu`, `area:ui`, `area:exploration`, `priority:p0`, `milestone:m3`, `status:ready` | `World_WonderlandPark.unity`, `UI/Panels/OnboardingPanel.prefab` | M3-01 | onboarding teaches controls and then releases the player into exploration |
| M3-04 | Build soft park perimeter treatments | Haobo Xu / Wenao Li | `owner:haobo-xu`, `owner:wenao-li`, `area:world`, `area:boundaries`, `priority:p0`, `milestone:m3`, `status:ready` | `World_WonderlandPark.unity`, `World/Shared`, `Art` | M3-01 | playable boundaries feel like believable park architecture, atmosphere, or scenic limits |
| M3-05 | Integrate scale shift into Flower Field attraction read | Yu Fu / Xuanyuan Qin | `owner:yu-fu`, `owner:xuanyuan-qin`, `area:scale-shift`, `area:world`, `priority:p0`, `milestone:m3`, `status:ready` | `World_WonderlandPark.unity`, `Features/ScaleShift` | M3-01, M2 scale pass | scale shift is readable as a discoverable attraction beat |
| M3-06 | Integrate weather mood response in one land | Xuanyuan Qin / Wenao Li | `owner:xuanyuan-qin`, `owner:wenao-li`, `area:weather`, `area:vfx`, `priority:p1`, `milestone:m3`, `status:ready` | `World_WonderlandPark.unity`, `Features/Weather`, `World/Shared/Lighting` | M3-01, M2 weather pass | at least one weather change makes the park feel more alive or inviting |
| M3-07 | Integrate growth discovery unlock | Haobo Xu / Tongyan Sun | `owner:haobo-xu`, `owner:tongyan-sun`, `area:growth`, `area:world`, `priority:p0`, `milestone:m3`, `status:ready` | `World_WonderlandPark.unity`, `Features/Growth` | M3-01, M2 growth pass | growth creates a readable new traversal opportunity in the park |
| M3-08 | Integrate particle spectacle beat | Xuanyuan Qin / Haobo Xu | `owner:xuanyuan-qin`, `owner:haobo-xu`, `area:particle-vitality`, `area:world`, `priority:p1`, `milestone:m3`, `status:ready` | `World_WonderlandPark.unity`, `Features/ParticleVitality` | M3-01, M2 particle pass | particle interaction leads to a clear spectacle or reward moment |
| M3-09 | Integrate lotus attraction and follow up ray readability | Tongyan Sun / Haobo Xu | `owner:tongyan-sun`, `owner:haobo-xu`, `area:lotus-pond`, `area:world`, `priority:p0`, `milestone:m3`, `status:ready` | `World_WonderlandPark.unity`, `Features/LotusPond` | M3-01, M2 lotus pass | lotus attraction is readable, reachable, and no longer dependent on debug-only knowledge |
| M3-10 | Integrate cat ride as scenic connector | Haobo Xu / Yu Fu | `owner:haobo-xu`, `owner:yu-fu`, `area:mounts`, `area:world`, `priority:p0`, `milestone:m3`, `status:ready` | `World_WonderlandPark.unity`, `Features/Mounts` | M3-01, M2 mount pass | cat ride feels like a scenic park connector and returns the player safely |
| M3-11 | Integrate fireworks skyline payoff | Xuanyuan Qin / Wenao Li | `owner:xuanyuan-qin`, `owner:wenao-li`, `area:fireworks`, `area:vfx`, `priority:p0`, `milestone:m3`, `status:ready` | `World_WonderlandPark.unity`, `Features/Fireworks`, `World/Shared/Lighting` | M3-01, M2 fireworks pass | fireworks feel like a destination payoff rather than a mission checkpoint |
| M3-12 | Run free-roam playtest pass 1 | Tongyan Sun | `owner:tongyan-sun`, `area:qa`, `priority:p0`, `milestone:m3`, `status:ready` | notes external or docs | M3-03 through M3-11 | confusion list, comfort list, dead-space list, and blocker list are captured |

## 3. Suggested Board Columns

Recommended board columns:

1. Ready
2. In Progress
3. Needs Review
4. Blocked
5. Done

Recommended working rule:

- no more than one broad edit to `World_WonderlandPark.unity` at a time without explicit coordination
