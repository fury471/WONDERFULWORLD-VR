# Explore the Wonderland
## Product Requirements Document (PRD) + Technical Development Document (TDD)

- Document Version: 1.3.2
- Date: March 27, 2026
- Team (Group 5): Xuanyuan Qin, Tongyan Sun, Haobo Xu, Wenao Li, Yu Fu
- Product Type: Open-World PCVR Fantasy Amusement Park Experience
- Platform: PCVR (SteamVR / OpenXR Tethered)
- Engine Target: Unity 2022.3 LTS or newer
- Repository: GitHub (with Git LFS for binary source assets)
- Refinement Note: This version keeps the open-world direction while reframing the project as a magical, attraction-driven wonderland park with stronger production logic, VR comfort, and implementation clarity.

# PART 1: PRODUCT REQUIREMENTS DOCUMENT (PRD)

## 1. Executive Summary & Vision

"Explore the Wonderland" is a first-person open-world PCVR fantasy experience built around one core emotional idea: scale transforms meaning. The player begins in a whimsical human-scale wonderland park, then gains the magical ability to shrink to an insect-like scale and experience the same environment as a vast fantasy attraction landscape. Grass becomes forest, puddles become lakes, lotus leaves become platforms, and a domestic cat becomes a giant living mount.

The project is not intended to be a single linear ride or a combat game. It is an open-world amusement park experience in which discovery, atmosphere, magical interaction, creature-scale contrast, and authored spectacle moments drive engagement. The world should feel contiguous and explorable, but technically it should be built as a seamless-feeling park made of connected lands, attractions, and streamed content sectors rather than an unlimited sandbox.

The product fantasy is:

- Explore a magical open-world wonderland park freely in first-person VR.
- Move between themed lands, attractions, scenic routes, and playful interaction spaces.
- Shift between human and microscopic scales to reinterpret space.
- Use magic to alter the environment, sculpt particles, and create fireworks.
- Bond with creatures and use the cat as a traversal companion.
- Experience a cohesive 3D-to-2D storybook visual identity.

## 2. Product Objectives & KPIs

### 2.1 Primary Product Objectives

#### 2.1.1 Immersion Through Scale

Deliver convincing scale transitions that create awe without breaking first-person presence or causing disorientation.

#### 2.1.2 Exploratory Open-World Presence

Create a wonderland park that feels open, traversable, and worth revisiting. Exploration should be driven by themed landmarks, attractions, scale-gated routes, magical interactions, and spectacle reveals rather than mission markers alone.

#### 2.1.3 Creative Agency

Allow the player to express themselves through weather manipulation, plant growth, particle shaping, and fireworks.

#### 2.1.4 Aesthetic Consistency

Maintain a clear hand-painted 3D-to-2D NPR style across environment, VFX, UI, and creatures.

#### 2.1.5 VR Comfort and Technical Stability

Sustain comfort-first interaction, stable first-person control, and high framerate suitable for PCVR.

#### 2.1.6 Extendable and Decoupled Development

The open world should be designed as an extendable platform rather than a tightly interdependent sequence of mechanics. Major functions and features should be as decoupled as possible so the team can:

- implement systems one by one in incremental development
- develop multiple systems simultaneously in parallel
- disable, replace, or polish one feature without breaking unrelated features
- add future regions, creatures, or interactions with minimal refactoring

### 2.2 Experience KPIs

- At least 80 percent of playtesters should describe the scale shift as a highlight.
- At least 75 percent of playtesters should be able to navigate between major lands or attractions without getting lost for more than 2 minutes.
- At least 80 percent of playtesters should understand how to use one major magic system after onboarding.
- Fewer than 10 percent of playtesters should report moderate or severe VR sickness during a 15-minute guided test.

### 2.3 Technical KPIs

- Locked 90 FPS on target PCVR hardware.
- No critical interaction failure during a full traversal through the park from one major attraction zone to another.
- Scale shifting, mount riding, particle shaping, and weather transitions must remain stable in-headset.

## 3. Core Gameplay Mechanics & Features

### 3.0 Core Experience Loop

The open-world loop is:

1. Explore the park and discover themed lands, attractions, paths, creatures, and magical points of interest.
2. Observe an attraction, traversal barrier, mystery, or interactive opportunity.
3. Use scale shifting, weather magic, growth magic, or creature traversal to change how the space functions.
4. Reach a new vista, attraction node, hidden route, or performance space.
5. Gather particles and use them for shaping, expression, or fireworks.
6. Continue exploring with the park feeling larger, stranger, more playful, and more alive.

The player should feel that the same open world gains new meaning as more abilities are unlocked or better understood, and that the park works as one connected wonderland rather than a list of disconnected mini-games.

### 3.1 The Perspective Shift (First-Person View / FPV)

#### Description

The foundational system remains a strict first-person experience. The player can shrink from standard human scale to a microscopic scale and return to normal size. This contrast is the main emotional and visual driver of the project.

#### Open-World Role

Scale shifting is not only a visual effect. It is a traversal and discovery system:

- Human scale is used for surveying the environment and identifying landmarks.
- Micro scale is used for entering hidden routes, riding creatures, traversing plant surfaces, and experiencing the world as a fantasy landscape.
- Certain world interactions should only become available or meaningful at one scale.

#### Transition Mechanisms

##### Option A: Instant Blink Transition (Default Comfort Mode)

- Screen fades to black.
- Scale state changes during the blackout.
- Screen fades back in.
- Audio pitch, ambience, and haptic cues reinforce the change.

##### Option B: Rapid Smooth Transition (Optional Comfort Setting)

- The player changes scale over a short duration.
- Peripheral vignetting is enabled during the transition.
- Movement input is temporarily softened or disabled during the scaling animation.

#### Refined Design Rules

- The game should launch with the blink transition as the default mode.
- Smooth transition should be optional, not mandatory.
- The player should never feel like the camera has detached from the headset.
- The world should remain legible during and after the shift.

#### Acceptance Criteria

- The scale change preserves full head tracking and stereo stability.
- Human-scale landmarks become dramatically different at micro scale.
- The system does not require a scene reload.
- Controller alignment, interaction rays, locomotion speed, and collision dimensions adapt correctly to the new scale.

### 3.2 Dynamic, Breathing Environment

The environment is a major gameplay partner in this project. It should feel alive, magical, and reactive, but the reactivity must be designed as layered authored systems rather than unrestricted simulation.

#### 3.2.1 Open-World Environmental Structure

The open world should be organized into interconnected themed lands and attraction zones such as:

- Flower Promenade / Tall Grass Adventure Zone
- Mushroom Midway / Fungi Grove
- Lotus Lagoon / Musical Performance Zone
- Cat Route / Giant Companion Ride Zone
- Fireworks Plaza / Sky Reveal Area

These lands should connect naturally and support free roaming, but each one should also have a strong identity, landmark silhouette, and at least one signature attraction or interaction.

#### 3.2.2 Immersive Weather System

##### Design Intent

Players use a magic wand, gesture, or spell interface to alter weather and mood across the world.

##### Refined Logic

Instead of fully procedural weather simulation, the game should use authored weather states and blend rules:

- Clear / Sunlit
- Cloudy / Overcast
- Rain / Drizzle
- Wind-Intensified Variant

Each weather state can modify:

- Skybox or sky gradient
- Directional light intensity and color
- Fog density and fog tint
- Ambient sound layers
- Particle emitters such as rain, mist, pollen drift, or leaf debris
- Global wind strength used by both VFX and foliage animation

##### Open-World Function

Weather should not be cosmetic only. It can also affect exploration:

- Rain can create reflective puddles or activate water-related interactions.
- Wind can intensify pollen flow and make particles easier to gather.
- Cloud cover can change mood, visibility, and firework readability.

##### Acceptance Criteria

- Weather change is readable within 2 to 3 seconds.
- Weather transitions work across connected world regions without obvious popping.
- At least one traversal or interaction outcome changes under a different weather state.

#### 3.2.3 Growth Simulation

##### Design Intent

Interactive flora should respond to magic and make the world feel enchanted and alive.

##### Refined Logic

Use authored growth stages, shader animation, spline animation, or blend shape animation rather than trying to simulate real plant biology.

Each growable plant should support defined states such as:

- Dormant
- Sprouting
- Bloomed
- Overgrown / Traversable

State changes can drive:

- Mesh swap or mesh blend
- Material parameter updates
- Collider resize or replacement
- New footholds, routes, bridges, or cover
- New particle or audio behavior

##### Open-World Function

Growth should create exploration changes such as:

- a flower stem rising into a climbable route
- a mushroom cap widening into a platform
- a vine bridge growing across a puddle edge

##### Acceptance Criteria

- Growth visibly changes the environment.
- Collision and traversal update correctly.
- Growth is performant and deterministic enough for repeated testing.

#### 3.2.4 Particle Vitality (Telekinesis & Shaping Mechanic)

##### Design Intent

Players gather pollen, petals, spores, or magical particles from flowers and use them as a semi-physical creative material.

##### Input Flow Based on Original Draft

- Point the left controller at flowers or particle sources.
- Hold the left index trigger to extract and capture particles.
- Press the left thumbstick or assigned mode button to enter levitation/manipulation state.
- While maintaining capture, use the right-hand side menu to choose shapes, text, or math-based patterns.
- Optional `Lock Grip` setting digitally holds the left trigger state to reduce fatigue.

##### Refined Logic

The system should support three authoring modes:

- Preset shape mode: star, heart, spiral, ring, butterfly, etc.
- Text mode: short words or names.
- Formula mode: safe mathematical templates or a restricted parser, not arbitrary code execution.

To keep the mechanic believable and stable:

- The shape should form around a preview anchor in front of the player.
- If the player has too few particles, supplemental particles can be drawn from nearby flowers, wind streams, or off-camera spawn volumes.
- Supplemental spawning must have visual caps and performance caps.
- When released, particles either dissipate back into the world or launch using the player's filtered hand velocity.

##### Comfort Refinement

- `Lock Grip` must be accessible from the side menu or a quick-toggle input.
- Long-duration holding should not require forceful squeezing.
- The side menu should appear near the right hand, wrist, or a stable world anchor.

##### Acceptance Criteria

- The player can create a recognizable particle shape in under 10 seconds.
- Throw release feels intentional rather than random.
- The system remains readable even in windy or rainy conditions.

#### 3.2.5 DIY Fireworks

##### Design Intent

DIY Fireworks is the large-scale spectacle version of the particle shaping mechanic. It transforms player-authored text or math-based forms into sky-borne magical fireworks.

##### Refined Logic

The fireworks system should reuse the same point-cloud generation pipeline as particle shaping, but rendered at larger scale and greater brightness.

Supported launch content should include:

- Preset patterns
- Short custom text
- Curated mathematical forms such as heart, sphere, flower, or spiral equations

If open text or formula entry is provided, it must use:

- a restricted grammar
- string length limits
- point-count limits
- timeout and validation rules

##### Open-World Role

Fireworks should be tied to spectacle plazas and exploration payoff points such as:

- a scenic overlook
- a central fireworks plaza
- the lotus lagoon at night
- a cat-riding destination reveal

##### Acceptance Criteria

- At least 5 distinct firework patterns are supported.
- Custom text or formula input does not freeze the game.
- Fireworks are visible and legible from the player's current location and scale.

### 3.3 Creature Bonding & Creative Play

#### 3.3.1 The Vehicle System (Mounts)

##### Design Intent

At microscopic scale, a domestic cat becomes a giant living traversal companion and signature attraction. This is one of the key scale-payoff moments in the game.

##### Core Mechanic Based on Original Draft

- The player approaches the cat at micro scale.
- The player mounts via a designated saddle or socket zone on the cat's back.
- The player experiences the ride in locked first-person from the saddle position.
- The cat can move autonomously or under player command.

##### Refined Open-World Logic

Because this is an exploratory open-world project, the cat should function as both:

- a traversal tool for crossing larger micro-scale distances
- a signature ride-like attraction with scenic reveals
- a living landmark that reinforces the player's changed scale

The mount system should support two riding modes:

###### Mode A: Guided / Auto-Path Ride

- Best default mode for comfort.
- Cat follows authored routes, splines, or NavMesh paths between world regions.
- Good for scenic rides, reveals, and travel between major attractions.

###### Mode B: Assisted Manual Control

- Activated from the side menu or mount settings.
- Right thumbstick controls steering intent or route selection.
- A/X can trigger jump only at safe jump windows, ledges, or authored jump links.
- Speed, turning rate, and jump acceleration must be capped for comfort.

This preserves the original manual control concept while making it safer for VR.

##### Acceptance Criteria

- Mounting and dismounting are consistent and bug-free.
- Head look remains fully independent while riding.
- The cat can traverse meaningful open-world park distances.
- Guided mode is comfortable enough for first-time players.

#### 3.3.2 Musical Platforms (Lotus Pond)

##### Design Intent

The lotus pond is a magical environmental performance space combining sound, motion, visual response, and attraction-like staging.

##### Core Mechanic Based on Original Draft

- Players point either controller at lotus leaves.
- Pressing the index trigger launches a magical droplet or projectile.
- A hit triggers a note and a water ripple.
- Both hands can be used simultaneously for rhythmic or melodic play.

##### Refined Logic

- Notes should map to a pentatonic or similarly forgiving scale.
- Leaves should have different pitch ranges or timbres based on size, distance, or region.
- Ripple VFX and audio timing must feel tightly synchronized.
- Repeated hits should be rate-limited to avoid audio clutter.

##### Open-World Role

The lotus pond should act as:

- a landmark visible from afar
- a peaceful interaction zone
- a destination attraction inside the wider park
- a place where weather, particles, and sound combine strongly

##### Acceptance Criteria

- Dual-hand play works reliably.
- The interaction is readable both near the pond and from a short distance.
- Performance remains stable even during rapid note triggering.

## 4. Open-World Park Design Requirements

### 4.1 Definition of Open World for This Project

For this project, open world means:

- the player can move through a large handcrafted wonderland park in a mostly non-linear order
- the world contains multiple interconnected lands, attractions, and landmarks
- backtracking is meaningful because scale and magic change how space can be traversed
- content is discovered through exploration, attraction flow, and environmental curiosity, not only linear mission order

It does not require an infinite map or a gigantic realistic terrain. The correct production interpretation is a seamless-feeling, carefully authored wonderland park with multiple themed lands, attraction loops, and scenic connectors.

### 4.2 Recommended World Structure

A practical world layout should include:

- Arrival Plaza / Overview Hub: where the player first surveys the park and chooses a direction
- Tall Grass / Flower Land: introduces scale awe and pollen gathering
- Mushroom / Fungi Land: emphasizes strange silhouettes and vertical platforms
- Lotus Lagoon Land: musical interaction and reflective atmosphere
- Cat Route Network: links distant micro-scale attractions and scenic routes
- Fireworks Vista Plaza: a reward space with open sky visibility

### 4.3 Exploration Logic

The world should support soft gating through mechanics instead of hard locks:

- a route becomes meaningful only at micro scale
- weather changes the mood or function of specific attractions
- growth magic creates new traversal geometry or opens a show space
- the cat mount crosses otherwise tedious ground and links destination moments
- particle shaping and fireworks are used for expression, spectacle, and park identity, not only puzzles

### 4.4 Navigation and Guidance

Because this is first-person VR, the project should avoid overloading the player with flat HUD markers.

Preferred navigation aids:

- strong landmarks
- strong sightlines to major attractions
- color-coded lands
- themed signage, props, or entrance arches
- wind direction or pollen trails
- musical cues
- glowing magical shrines or spell nodes
- occasional diegetic world map or compass object

### 4.5 Extendable World Content Strategy

The open world should be able to grow over time without forcing every land to depend on every other land.

Refined rule set:

- each world region should plug into shared global systems through clean interfaces
- a region may support only the feature modules it actually needs
- new regions should be addable without rewriting the scale, weather, mount, or particle core systems
- major features should enrich exploration across the world, but they should not be hard-coded into one giant monolithic scene controller

Examples:

- a lotus lagoon land can use weather, music, and particle modules without depending on the mount system
- a cat ride route can use scale and mount systems without requiring fireworks logic
- a new future attraction land can subscribe to global weather and growth systems without changing existing lands

## 5. UX, Accessibility, and Comfort Requirements

### 5.1 Comfort Defaults

- Teleport locomotion enabled by default
- Snap turn enabled by default
- Smooth locomotion optional
- Smooth turn optional
- Blink scale transition default
- Guided mount mode default

### 5.2 World-Space UI Rules

- Menus must be readable in VR at typical play distance.
- Text input should use a world-space keyboard or formula selection panel.
- Frequently used magic actions should require minimal menu depth.
- Important toggles such as `Lock Grip` must be discoverable and fast to use.

### 5.3 Accessibility Options

Recommended settings menu:

- dominant hand selection
- movement mode toggle
- turn mode toggle
- vignette strength
- subtitle toggle
- audio level sliders
- mount comfort mode toggle

# PART 2: TECHNICAL DEVELOPMENT DOCUMENT (TDD)

## 6. System Architecture & Tech Stack

### 6.1 Engine

Recommended stable target: Unity 2022.3 LTS or newer.

Important repository note:

- The current repository appears to already use a Unity 6000.x editor branch.
- This is acceptable because it is newer than 2022.3, but the whole team must standardize on one exact editor version before production work continues.

### 6.2 XR Framework

- Unity XR Interaction Toolkit (XRI)
- OpenXR Plugin
- Unity Input System (Action-based)
- XR Hands optional for future gesture-based interactions, but controller-driven input should remain the primary production path unless hand tracking is fully tested in PCVR

### 6.3 Render Pipeline

- Universal Render Pipeline (URP)
- Shader Graph for toon / cel shading and stylized material control
- VFX Graph recommended for pollen, petals, rain accents, and fireworks

Technical note:

- If VFX Graph is not yet added to the project packages, it must be installed before building the particle-heavy systems described in this document.

### 6.4 Supporting Runtime Systems

Recommended supporting systems:

- NavMesh for creature navigation
- one master scene for the current prototype slice, with optional later split or streaming if performance and collaboration require it
- ScriptableObjects for weather presets, spell presets, mount settings, growth stage data, and firework pattern definitions

### 6.5 Extendable Modular Architecture

This open-world project should be implemented as a modular feature architecture, not as one large tightly coupled gameplay script.

Recommended architecture layers:

#### Shared Core Layer

Responsible for systems that every region or feature may use:

- player rig and locomotion
- input actions
- world scene organization
- audio routing
- save/settings
- world state and weather state

#### Feature Module Layer

Each major mechanic should exist as its own module with its own prefabs, configs, and runtime logic:

- scale shift module
- weather module
- growth module
- particle vitality module
- fireworks module
- mount module
- lotus music module

#### Region Integration Layer

Each region should reference only the modules it actually uses and should bind to them through prefabs, ScriptableObjects, events, or lightweight interfaces.

Implementation rules:

- avoid direct hard dependencies between feature modules whenever possible
- use shared data, events, and service-style interfaces instead of cross-referencing unrelated systems
- each module should be testable in isolation in a sandbox scene
- each module should still work when another optional feature module is disabled

This structure supports both incremental development and parallel development.

### 6.6 Placeholder-First Development Strategy

The team will use placeholder assets to validate core gameplay logic before final art production.

This means:

- use primitive meshes, temporary models, flat materials, simple particles, and dummy audio to prove interactions first
- implement gameplay logic against prefabs, anchors, sockets, interfaces, and data assets rather than against final art assets
- make sure placeholder content can later be replaced by final art without rewriting the feature logic
- validate scale, collision, traversal, and input flow before investing heavily in polish

Recommended placeholder-first order:

1. build the interaction with primitive or proxy assets
2. test it in a sandbox scene
3. integrate it into one open-world region
4. replace placeholder visuals with production assets only after the logic is stable

Examples:

- the cat mount can begin as a simple animated blockout creature with a saddle socket
- the lotus pond can begin with discs, colliders, note triggers, and simple ripple placeholders
- growth plants can begin as mesh swaps or scaled capsules before final plant art exists
- particle systems can begin with simple colored points before final pollen and petal visuals are authored

## 7. Collaboration & Version Control Protocol (CRITICAL)

### 7.1 Git Large File Storage (LFS)

Git LFS is still required, but the original rule should be refined.

Track large binary source assets and media in LFS, for example:

```gitattributes
*.psd filter=lfs diff=lfs merge=lfs -text
*.fbx filter=lfs diff=lfs merge=lfs -text
*.obj filter=lfs diff=lfs merge=lfs -text
*.png filter=lfs diff=lfs merge=lfs -text
*.jpg filter=lfs diff=lfs merge=lfs -text
*.wav filter=lfs diff=lfs merge=lfs -text
*.mp3 filter=lfs diff=lfs merge=lfs -text
*.tga filter=lfs diff=lfs merge=lfs -text
*.exr filter=lfs diff=lfs merge=lfs -text
```

Refinement:

- Do not automatically place `.unity`, `.prefab`, `.mat`, and general text-serialized `.asset` files in LFS if the project uses Force Text serialization.
- Those files should remain diffable in Git when possible.
- Use Unity text serialization and visible meta files across the entire team.

### 7.2 The "Anti-Merge Conflict" Unity Workflow

The original workflow is correct and should be expanded for an open-world project.

#### Rule 1: Do Not Co-Edit the Same Production Scene Without Coordination

For the current prototype slice, the team is using one master world scene with multiple named park zones inside it. Because of that, the active scene owner must be explicit. If the project is later split into additive sectors, each sector should also have a clear owner during a work period.

#### Rule 2: Prefab-First Workflow

Every interactable object, system, creature, and landmark should be authored as a prefab or prefab variant.

Examples:

- `Cat_Mount.prefab`
- `Lotus_Pond.prefab`
- `WeatherShrine.prefab`
- `GrowthFlower_Pink.prefab`
- `ParticleSource_Bluebell.prefab`

Placeholder refinement:

- start with placeholder prefabs for gameplay testing
- separate gameplay prefab structure from final art where possible
- replace meshes, materials, audio, and VFX through clearly defined prefab children or data references rather than rewriting scripts

#### Rule 3: Personal Sandbox Scenes

Each developer should work in a personal sandbox scene for prototyping before integrating into the shared world.

#### Rule 4: Single Master Scene For The Current Slice

For the current prototype and first polished vertical slice, use one master production scene that contains all major park parts instead of separate playable scenes.

Suggested structure for the current slice:

- `World_WonderlandPark.unity`
- root zones inside the scene for `HumanEntry`, `FlowerField`, `MushroomGrove`, `LotusPond`, `CatRoute`, and `FireworksClearing`
- prefab-driven content stored in matching folders under `Assets/_Project/World/Regions/`

If performance, merge risk, or world scale later make this too costly, the project can be split into additive sectors in a later phase.

#### Rule 5: Module Ownership Enables Parallel Work

To support simultaneous development, assign ownership by feature module as well as by region.

Example ownership model:

- one developer owns scale and player systems
- one developer owns weather and world state
- one developer owns growth and particle systems
- one developer owns mount and traversal systems
- one developer owns world integration, art pipeline, and optimization

This way, the team can work in parallel while keeping integration points controlled.

### 7.3 Branching Strategy (Feature Branch Workflow)

Planned team workflow:

- `main`: shared stable branch and merge target
- `feature/issue-#-name`: separate working branch for each feature or task

Optional branch naming variants are acceptable if they stay clear, for example:

- `feature/scale-shifting`
- `feature/lotus-pond`
- `feature/cat-mount`
- `art/toon-shader`
- `prototype/particle-shaping`

Rules:

- all development work happens in separate branches, not directly in `main`
- merge back to `main` through pull requests or reviewed merges
- sync branches frequently from `main` to reduce Unity merge conflicts
- test the feature in a sandbox or isolated region before merging
- only merge when the branch does not break the shared project

This simpler branch model is suitable for a student team as long as `main` remains stable and merges are disciplined.

## 8. Rendering & Art Pipeline (3D-to-2D)

### 8.1 Visual Direction

The project should retain the original "Studio Ghibli / storybook" inspiration while avoiding imitation. The goal is an illustrative, soft, painterly, hand-authored look suitable for VR.

### 8.2 Shading Model

- Do not rely on default Standard Lit materials for hero assets.
- Build a URP toon shader or stylized lit shader with quantized lighting bands.
- Preserve readable silhouettes and material separation at both human and micro scale.

### 8.3 Outlines

The original desire for stable outline thickness is correct, but the implementation must be VR-aware.

Recommended options:

- screen-space outline based on normals/depth if stable in stereo rendering
- inverted-hull outline with distance compensation if screen-space solution causes stereo artifacts

Refinement:

- Test outline readability at both scale states.
- Keep outlines subtle enough that grass, petals, and foliage do not shimmer excessively.

### 8.4 Texture Strategy

- Hand-painted albedo textures should carry most of the visual identity.
- Stylized normals are acceptable if they reinforce brush-like form.
- Avoid noisy physically realistic roughness patterns.
- Keep color palettes region-based so each world zone is memorable.

### 8.5 Placeholder Art Policy

During early logic development, placeholder art is explicitly allowed and encouraged.

Rules:

- placeholder art should prioritize readability, scale clarity, and collision accuracy
- placeholder assets should communicate gameplay purpose clearly even if they are visually simple
- final NPR polish should happen after core interaction logic is stable
- placeholder and final assets should share the same spatial anchors and prefab structure whenever possible

### 8.6 World-Scale Readability

Because the player can explore at two drastically different scales:

- hero plants need readable silhouettes from above and up close
- bark, soil, petals, and leaf surfaces must still look intentional at micro distance
- repeated tiling textures should be minimized in areas the player may inspect closely at insect scale

## 9. Core Systems Implementation Details

### 9.1 Open-World Scene & Streaming Strategy

This is the main refinement needed for the project's open-world ambition.

The world should feel seamless. For the current prototype and first vertical slice, it should be built as one master park scene containing all major parts. Later, the project may be split into modular sectors if performance or collaboration require it.

Recommended architecture for the current slice:

- one master production scene containing player rig, global systems, and all major park zones
- clear root objects for each land or attraction area
- shared lighting strategy using baked data and one real-time directional light for weather mood shifts
- prefab-driven zone content stored in matching region folders for maintainability

Advantages for the current phase:

- faster manual world assembly
- easier to review the whole attraction route in one play session
- fewer early architectural blockers while the park is still being blocked out

Tradeoffs to monitor:

- master-scene merge conflicts can become painful without discipline
- profiling must watch for scene complexity growth
- additive split can still be introduced later if needed

### 9.2 Scale Shifting Manager (`ScaleManager.cs`)

#### Core Rule

Do not scale the entire environment.

#### Recommended Approach

- Scale the player rig or the XR origin hierarchy after prototype validation.
- Keep headset tracking 1:1.
- Do not manually modify the headset's real hardware IPD value.
- Instead, preserve XR stereo behavior and adapt the gameplay systems around the new perceived scale.

#### Systems That Must Update on Scale Change

- character controller height and radius
- locomotion speed
- step height / slope behavior if used
- controller ray length or grab distances
- camera near clip plane
- audio attenuation tuning if necessary
- interaction layer assumptions for tiny props and surfaces

#### Acceptance Criteria

- No clipping explosion when the player looks closely at small objects.
- No controller offset drift after repeated scale transitions.
- No physics instability when moving across tiny surfaces.

### 9.3 Weather & World-State Manager (`WeatherManager.cs`, `WorldStateManager.cs`)

Recommended structure:

- `WorldStateManager` controls persistent high-level world variables.
- `WeatherManager` applies current weather preset and blends global values.
- Local regions may read the global state and apply region-specific responses.

Recommended data assets:

- `WeatherPreset_SO`
- `WindProfile_SO`
- `RegionResponse_SO`

### 9.4 Growth System (`GrowthController.cs`)

Each growable object should expose:

- current growth stage
- valid next stages
- transition animation or timeline
- collider set per stage
- optional traversal marker data

Avoid making growth fully procedural unless a single specific plant type needs it.

### 9.5 Particle Vitality & Fireworks (`ParticleShapeSystem.cs`, `FireworkController.cs`)

Recommended runtime pipeline:

1. Acquire particles from source emitters or proxy pools.
2. Store active particle ownership in a managed gameplay state.
3. Convert selected target pattern into a 3D point set.
4. Send point set to GPU or simulation layer.
5. Blend between free-floating state and locked shape state.
6. On release, apply either dissipate, drift, or throw behavior.

Formula safety refinement:

- parse only a restricted mathematical grammar
- impose point budget limits
- clamp generation time
- reject malformed expressions gracefully

### 9.6 Mount System (`VehicleController.cs` or `MountController.cs`)

Recommended hybrid architecture:

- NavMeshAgent or spline-driven cat body root
- saddle anchor transform for rider attachment
- mount state machine: `Idle`, `Mounting`, `MountedAuto`, `MountedManualAssist`, `Dismounting`
- locomotion disabled or remapped while mounted
- head-tracking always preserved

Jump refinement:

- jumping should use authored jump nodes or links
- avoid free jump spam in first-person VR
- use haptic warning and small comfort vignette during sudden motion if necessary

### 9.7 Lotus Pond System (`LotusNoteTrigger.cs`)

Recommended components:

- projectile hit receiver
- note trigger
- ripple VFX trigger
- cooldown or retrigger gate
- pitch mapping data asset

## 10. Performance Budgets (PCVR Target)

### 10.1 Framerate Target

- Target Framerate: 90 FPS strict
- Frame Time: less than 11.1 ms total frame time

### 10.2 Open-World Performance Refinement

For an exploratory open-world VR amusement park project, the main risks are not only triangles but also:

- overdraw from foliage and transparent VFX
- too many dynamic lights
- expensive outlines
- large unpartitioned scenes
- excessive CPU work from particle control logic and AI

### 10.3 Practical Budgets

- Visible draw calls / batches: target under 1200 to 1500 in heavy scenes
- Real-time shadow casters: keep minimal
- Transparent particle-heavy scenes: profile separately
- Visible triangles: use LODs and culling aggressively rather than trusting a single fixed number

### 10.4 Required Optimization Strategies

- baked GI for environment
- light probes and reflection probes
- GPU instancing where useful
- LOD groups for foliage, rocks, and repeated props
- occlusion culling for major blockers
- strong hierarchy organization, culling, and prefab modularity inside the master scene, with later scene splitting only if needed
- pooled projectiles and pooled interaction VFX

## 11. Production Risks and Corrected Unreasonable Assumptions

### 11.1 Open World Does Not Mean Infinite Simulation

Refinement:

- build a handcrafted, interconnected wonderland park
- do not attempt fully emergent ecosystem simulation
- use authored states, presets, and controlled interactions

### 11.2 IPD Camera Offset Should Not Be Manually Authored

Refinement:

- preserve XR runtime stereo handling
- adapt scale through rig, clipping, interaction distance, and controller logic instead

### 11.3 Arbitrary Formula Input Is Unsafe

Refinement:

- use a safe math grammar or curated equation library
- cap point counts and compute time

### 11.4 Free Manual Mount Physics Can Be Uncomfortable

Refinement:

- keep guided riding and assisted manual control
- author jump windows and route logic
- treat full chaos riding as out of scope unless comfort testing proves it safe

### 11.5 Git LFS Should Not Swallow All Unity YAML Assets

Refinement:

- keep binary source assets in LFS
- keep text-serialized Unity content diffable where possible

## 12. Next Steps / Action Items for Group 5

### 12.0 Incremental and Parallel Development Strategy

Recommended development principle:

- build the shared core first
- implement feature modules one by one when needed
- allow different teammates to prototype separate modules simultaneously
- integrate modules through stable contracts instead of ad hoc scene references
- keep every major system runnable in a sandbox before world integration

### 12.1 Repository & Project Setup

- lock one Unity editor version across the team
- confirm OpenXR and XRI baseline
- verify Force Text serialization and visible meta files
- refine `.gitattributes` to match the corrected LFS policy
- define the branch naming convention and merge review rule for `main`

### 12.2 Placeholder Core Logic Pass

- create placeholder prefabs for cat mount, lotus leaves, growth plants, weather shrines, and particle sources
- use simple meshes and colliders to prove interaction logic first
- keep placeholder prefabs replaceable so final art can be swapped in later
- make each feature runnable in its own sandbox scene before world integration

### 12.3 Open-World Blockout

- build a greybox of the connected park lands
- establish major attraction sightlines and cat traversal routes
- validate that the park feels open without becoming too large for VR traversal

### 12.4 Comfort Prototype

- implement scale shift first
- test blink and smooth scale options
- test mount riding in guided and assisted-manual mode

### 12.5 Signature Systems Prototype

- weather state switching
- one growable plant chain
- one particle shaping interaction
- lotus pond note interaction
- one fireworks reveal zone

### 12.6 Art Direction Lock

- finalize the toon shader pipeline
- define land palettes, attraction silhouettes, and material rules
- test outline behavior at both player scales

## 13. Final Refined Product Statement

"Explore the Wonderland" should remain an open-world PCVR fantasy where players roam a magical wonderland park, shift scale, and creatively transform the spaces around them. The refinement in this document does not reduce that vision. It clarifies that the experience should feel like a connected amusement park of themed lands, attractions, scenic rides, and magical performance spaces, while still respecting the technical constraints required to keep the project immersive, comfortable, and buildable.


