# Explore The Wonderland
## M3 Execution Packet v1.0

- Date: April 14, 2026
- Purpose: Convert M3 from milestone scope into an executable integration baseline for the current exploration-first demonstration
- Source Docs:
  - `Docs/Current_Demonstration_Direction_v1.0.md`
  - `Docs/ExploreTheWonderland_PRD_TDD_v1.3.2.md`
  - `Docs/ExploreTheWonderland_30_Day_Production_Milestone_Plan_v1.0.md`
  - `Docs/M2/ExploreTheWonderland_M2_Closeout_Checklist_v1.0.md`

## 1. Current Starting Point

The team is entering M3 after simulator-based M2 validation.

Current starting assumptions:

- the core M2 systems exist in the repo
- the startup scene is `Assets/_Project/World/Persistent/World_WonderlandPark.unity`
- the park shell already exists across Human Entry, Flower Field, Lotus Pond, Cat Route, and Fireworks Clearing
- M2 is complete enough to begin M3 safely

Current known caveats carried into M3:

- Growth trigger path works, but visual readability should be reviewed during integration
- Lotus core logic works through the debug path, but simulator ray interaction still needs follow-up
- Wenao's VFX hook work is present in the repo, but still needs main-scene review during integration

## 2. Locked M3 Scope

M3 is the Integrated Park Slice milestone.

Objective:

- connect the systems into one coherent magical park experience
- prove that the slice feels open and exploratory without requiring an endless map
- make the demo feel like a complete park space rather than a chain of disconnected feature tests

Mandatory M3 outcomes:

1. the player starts in Human Entry with onboarding that teaches controls and then releases them into exploration
2. the park supports mostly non-linear exploration rather than a forced mission sequence
3. scale shift is discoverable and readable in Flower Field
4. at least one weather response is visible and helps the park feel alive
5. growth creates a meaningful new traversal possibility
6. particle interaction leads to an authored spectacle or reward moment
7. lotus attraction is readable, reachable, and enjoyable without debug knowledge
8. cat ride works as a scenic connector across park space
9. fireworks finale reads as a skyline or destination payoff rather than a mission checkpoint
10. the playable perimeter feels intentional and diegetic, not like a visible hard wall

M3 is not the milestone for final presentation polish.

The priority is:

- exploratory freedom
- readability through environment and staging
- strong art direction support for the illusion of a larger world
- minimal confusion for first-time testers

## 3. Required Repo Outputs

M3 should produce or update:

- scene integration across all vertical-slice zones inside the master park scene
- `Assets/_Project/Core/Runtime/GameFlowManager.cs`
- `Assets/_Project/Core/Runtime/ParkAttractionState.cs`
- `Assets/_Project/UI/Prefabs/AttractionMarker.prefab`
- `Assets/_Project/UI/Panels/OnboardingPanel.prefab`

Interpret these outputs using the current demonstration direction:

- `GameFlowManager` should coordinate bootstrap, discovery state, and soft guidance
- `ParkAttractionState` should track attraction availability, discovery, and readability state
- `AttractionMarker.prefab` should be optional, subtle, and ideally diegetic rather than flat HUD spam

## 4. Owner Map

- Haobo owns attraction layout, spatial pacing, scenic connectors, soft-boundary shaping, and region integration
- Yu Fu owns scene boot order, load safety, integration bug fixing, and final exploratory handoff
- Xuanyuan owns feature wiring, state transitions, and cross-system handoff stability
- Tongyan owns onboarding clarity, interaction prompts, attraction readability, and first-time-user usability
- Wenao owns visual continuity between lands, spectacle staging, skyline support, and atmospheric soft-boundary support

## 5. Integration Rules

### Scene Ownership

- Yu Fu is the active owner of `World_WonderlandPark.unity` during M3 integration
- system owners should prefer prefabs, ScriptableObjects, and small scene handoff changes over direct large scene edits
- if a feature needs a scene hookup, the owner should provide the exact object, script, and expected behavior before the scene owner wires it

### Design Rules

- do not convert the park into a rigid mission chain
- use route references as an orientation spine for readability and testing, not as mandatory player order
- use strong landmarks, sightlines, scenic connectors, and attraction identity to pull exploration
- use diegetic or environmental boundary treatments instead of obvious air walls
- if a hard stop is necessary, disguise it as believable park architecture, atmosphere, or attraction logic

### Branching

Use focused M3 branches such as:

- `feature/yu-fu-m3-bootstrap-exploration`
- `feature/haobo-m3-park-layout`
- `feature/tongyan-m3-onboarding-clarity`
- `feature/xuanyuan-m3-state-wiring`
- `art/wenao-m3-park-staging`

### Merge Rules

- no direct commits to `main`
- one focused PR per integration slice
- if a PR edits `World_WonderlandPark.unity`, Yu Fu reviews or owns it
- if a PR changes shared state behavior, Yu Fu and the relevant feature owner both review it

## 6. Recommended Integration Order

Use this order to reduce merge risk and keep the slice testable.

### Pass 1: Exploration bootstrap and state safety

1. Create `GameFlowManager.cs`
2. Create `ParkAttractionState.cs`
3. Define attraction discovery/readiness state, not mandatory mission order
4. Add lightweight logging for attraction activation, discovery, and blocker conditions

### Pass 2: Human Entry, onboarding, and park read

1. Make onboarding readable at spawn
2. Teach controls, comfort, and how to explore
3. Release the player into the park instead of chaining them immediately
4. Add or update subtle attraction markers only if environmental readability is not enough
5. Stage the first major sightlines so the player feels free but not lost

### Pass 3: Flower Field and exploratory discovery

1. Integrate scale shift into Flower Field as a discoverable attraction beat
2. Make weather response visible where it helps mood or curiosity
3. Integrate growth so it feels like a new possibility in the park, not only a puzzle gate

### Pass 4: Lotus, particles, and scenic connectors

1. Integrate lotus attraction so it reads as a destination or ambient performance space
2. Fix or replace any Lotus debug-only assumptions during scene integration
3. Connect particle payoff into the park as a show-like or expression-like moment
4. Make the cat ride a scenic connector between spaces, not just a required checkpoint

### Pass 5: Finale, spectacle, and world illusion

1. Integrate fireworks finale so it works as a skyline or destination payoff
2. Review Wenao's VFX hooks in the real park scene
3. Add soft-boundary treatments, skyline blockers, and atmospheric edges that imply more park beyond the playable slice
4. Make sure the park feels larger than the literal walkable footprint

### Pass 6: Exploratory playtest pass

1. Let a tester wander with minimal instruction
2. Capture where they naturally go first
3. Capture confusion, comfort issues, dead spaces, and invisible boundaries
4. Capture whether the park feels complete, connected, and worth exploring

## 7. Validation Checklist

M3 is ready to close only if the team can answer yes to these:

- can a tester start in Human Entry and understand how to explore without a long explanation?
- does the park feel open and worth roaming even though it is bounded?
- do the themed lands feel like parts of one coherent magical park?
- are the boundaries disguised well enough that the slice feels complete rather than cut off?
- are the attractions discoverable through staging, landmarks, and curiosity rather than only explicit instructions?
- do the main systems reinforce exploration instead of interrupting it?
- are there no major transition bugs, dead ends, or broken attraction handoffs?

## 8. M2 Handoff Notes To Respect

Carry these into M3:

- Growth works, but visual readability should be reviewed in the integrated slice
- Lotus works, but simulator ray interaction still needs follow-up during real scene integration
- simulator validation passed for the major M2 systems, but M3 should still be treated as the first true integrated proof pass


