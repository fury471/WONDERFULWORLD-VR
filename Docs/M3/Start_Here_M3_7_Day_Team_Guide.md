# Explore The Wonderland
## Start Here: M3 7-Day Team Guide

- Date: April 14, 2026
- Audience: Group 5 M3 integration team
- Goal: Finish the exploration-first magical park slice within 7 days and enter M4 safely
- Source Docs:
  - `Docs/Current_Demonstration_Direction_v1.0.md`
  - `Docs/M3/ExploreTheWonderland_M3_Execution_Packet_v1.0.md`
  - `Docs/M3/ExploreTheWonderland_M3_Issue_Board_Seed_v1.0.md`
  - `Docs/M3/ExploreTheWonderland_M3_Closeout_Checklist_v1.0.md`

## 1. Team-Wide Rules For The Next 7 Days

1. The current demo is a **bounded but seamless-feeling magical park slice**.
2. Do not design M3 like a mission chain.
3. Use the old route order only as a readability spine for testing and staging.
4. Every change should improve one of these:
   - exploration freedom
   - attraction readability
   - scenic continuity
   - believable park boundaries
   - first-time usability
5. Yu Fu is the active owner of `World_WonderlandPark.unity` during M3.
6. No one broad-edits the master scene without coordinating through Yu Fu.
7. Feature owners should hand off prefabs, ScriptableObjects, or exact hookup instructions rather than editing the scene freely.
8. Every day should end with:
   - one scene sync
   - one quick simulator play pass
   - one written blocker update

## 2. What “Done” Looks Like At The End Of 7 Days

By the end of this 7-day push:

- the player starts in Human Entry and understands how to explore
- the park feels open, magical, and worth roaming
- Flower Field, Lotus Pond, Cat Route, and Fireworks Clearing feel like parts of one connected park
- the core interactions are integrated into the real park scene, not only sandbox scenes
- the perimeter feels intentional and diegetic
- there is no rigid mission flow, but the player can still discover meaningful content naturally

## 3. Owner Summary

### Yu Fu

- own the master scene, scene boot order, and integration safety
- create and maintain:
  - `Assets/_Project/Core/Runtime/GameFlowManager.cs`
  - `Assets/_Project/Core/Runtime/ParkAttractionState.cs`
- decide what gets integrated into the scene and in what order
- protect the exploration-first direction

### Haobo Xu

- own park layout, scenic pacing, sightlines, and soft boundaries
- integrate the cat ride as a scenic connector
- help stage Growth and Lotus inside the real park

### Xuanyuan Qin

- own system wiring for ScaleShift, Weather, ParticleVitality, and Fireworks
- support scene-safe state transitions and reliability

### Tongyan Sun

- own onboarding clarity, prompts, first-time usability, and Lotus readability
- run the first free-roam confusion pass

### Wenao Li

- own skyline read, spectacle staging, VFX continuity, and soft-boundary atmosphere
- apply M2 VFX hooks in the real park scene

## 4. Day 1: Bootstrap And Scene Safety

### Yu Fu

1. Create `GameFlowManager.cs`.
2. Create `ParkAttractionState.cs`.
3. Add lightweight logging for attraction discovery/readiness.
4. Confirm `World_WonderlandPark.unity` still opens cleanly after adding the new core objects.
5. Create a branch such as:
   - `feature/yu-fu-m3-bootstrap-exploration`

### Haobo Xu

1. Walk the park and mark every current dead edge or obvious hard stop.
2. Write down candidate soft-boundary treatments for each one:
   - hedge
   - fence
   - fog wall
   - water edge
   - cliff
   - decorative gate

### Xuanyuan Qin

1. Review ScaleShift, Weather, Particle, and Fireworks handoff needs.
2. Prepare minimal “integration-ready” notes for each feature:
   - required objects
   - required references
   - expected behavior

### Tongyan Sun

1. Review Human Entry onboarding from a first-time user view.
2. Identify anything that still feels like a forced flow rather than free exploration.

### Wenao Li

1. Review current skyline and landmark read in the real scene.
2. Identify where the park feels empty, too flat, or too obviously bounded.

### End-Of-Day Check

- `GameFlowManager.cs` and `ParkAttractionState.cs` exist
- everyone has written one blocker/risk note
- no red Console errors from the new bootstrap code

## 5. Day 2: Human Entry And Flower Field

### Yu Fu + Tongyan

1. Rework onboarding so it teaches controls and then releases the player.
2. Make sure the onboarding does not imply strict attraction order.
3. Add subtle guidance only if needed.

### Yu Fu + Xuanyuan

1. Integrate ScaleShift into Flower Field.
2. Make it discoverable through staging, not just instruction text.

### Haobo + Wenao

1. Improve first major sightlines from Human Entry into Flower Field.
2. Make the first visible attractions feel inviting.

### End-Of-Day Check

- a first-time tester can spawn, read the environment, and choose a direction
- ScaleShift is visible and understandable in Flower Field

## 6. Day 3: Weather And Growth

### Xuanyuan + Wenao

1. Integrate one weather response into a real land.
2. Make the weather response noticeable within a few seconds.
3. Use weather to improve mood or exploration curiosity, not just visuals for their own sake.

### Haobo + Tongyan

1. Integrate Growth into a readable traversal reveal.
2. Fix any “it triggered but I cannot tell what changed” problem in the actual park scene.
3. Make the resulting path visually obvious enough that the player notices the reward.

### Yu Fu

1. Confirm the state flow does not force sequence, but still supports discovery/readability tracking.

### End-Of-Day Check

- at least one weather response works in the real park scene
- Growth changes traversal in a clearly readable way

## 7. Day 4: Lotus And Particle

### Tongyan + Haobo

1. Integrate Lotus into the real park as a destination or ambient attraction.
2. Remove dependence on hidden debug-only knowledge in the integrated slice.
3. Follow up the ray-input issue inside the real scene.

### Xuanyuan + Haobo

1. Integrate ParticleVitality so it leads into a visible payoff or mini-spectacle.
2. Make sure it feels like a magical attraction, not only a debug tool.

### Wenao

1. Support Lotus and Particle with visual staging and spectacle readability.

### End-Of-Day Check

- Lotus works without relying on hidden debug assumptions
- Particle interaction leads to a readable payoff moment

## 8. Day 5: Cat Ride And Scenic Connectors

### Haobo + Yu Fu

1. Integrate the cat ride into the real park scene.
2. Make the cat ride feel like a scenic connector between spaces.
3. Check mount start, route pacing, and dismount placement in the real park.

### Haobo + Wenao

1. Improve scenic views and connectors during the ride.
2. Make the ride reinforce the illusion of a larger park.

### End-Of-Day Check

- cat ride works safely in the real park scene
- ride feels scenic, not just functional

## 9. Day 6: Fireworks And Soft Boundaries

### Xuanyuan + Wenao

1. Integrate Fireworks as a skyline or destination payoff.
2. Use Wenao's VFX hook assets in the real scene.

### Haobo + Wenao

1. Implement the highest-priority soft-boundary treatments.
2. Make the perimeter feel like believable park space rather than obvious level edges.

### Yu Fu

1. Check that all systems coexist in the real park scene without major state or load problems.

### End-Of-Day Check

- fireworks are staged as a payoff
- the park feels larger than its literal playable footprint
- the boundaries do not feel like blunt air walls

## 10. Day 7: Free-Roam Playtest And Triage

### Tongyan

1. Run free-roam playtest pass 1.
2. Let a tester wander with minimal instruction.
3. Write down:
   - confusion list
   - comfort list
   - dead-space list
   - blocker list

### Yu Fu

1. Triage blockers into:
   - must-fix before M4
   - acceptable caveat
   - later polish
2. Update `Docs/M3/ExploreTheWonderland_M3_Closeout_Checklist_v1.0.md`.

### Whole Team

1. Review whether the slice now feels like:
   - one magical park
   - exploratory and open
   - bounded but believable
   - attraction-rich rather than mission-driven

## 11. Daily Meeting Pattern

Use this every day for the 7-day push:

### Morning

- 15-minute owner sync
- confirm one active master-scene owner
- confirm the one integration goal for the day

### Midday

- 10-minute scene merge-risk check
- decide whether any owner handoff needs to happen before evening

### End Of Day

- one simulator play pass
- one blocker summary
- one decision on whether the next day needs more integration or more readability/polish

## 12. What Not To Do During This 7-Day Push

- do not turn the project into a rigid mission flow
- do not add huge empty space just to look more open-world
- do not use obvious hard walls if a believable park boundary can do the job
- do not keep attraction logic hidden behind debug-only knowledge in the integrated scene
- do not let multiple people broad-edit `World_WonderlandPark.unity` without explicit coordination
- do not sacrifice readability for feature count

## 13. Final Reminder

The goal of this 7-day push is not to fake an infinite map.

The goal is to make a **bounded magical amusement park slice feel complete, coherent, and open enough to sell the open-world fantasy**.
