# Wonderland Park — Main Scene Interaction Bindings

> Reference for every player-facing interactable in the production scene
> [`World_WonderlandPark.unity`](../Assets/_Project/World/Persistent/World_WonderlandPark.unity).
>
> Scope: interactables actually present at runtime in the production scene
> (direct components, prefab-internal components, and runtime-bootstrapped
> objects). Out-of-scene scripts, sandbox-only systems, and dead code are
> intentionally excluded.

---

## Conventions

- "Right trigger" / "Right A" refer to the Quest controller buttons.
  - On the right hand: A = `primaryButton`, B = `secondaryButton`.
  - On the left hand: X = `primaryButton`, Y = `secondaryButton`.
- "Ray" means a controller-origin raycast (typically forward from the
  Stabilized Attach transform).
- Distances are in metres, durations in seconds.
- Scale-shift values are read from
  [`ScaleSettings_SO.asset`](../Assets/_Project/Features/ScaleShift/ScriptableObjects/ScaleSettings_SO.asset).
- The right-hand Menu button is **reserved by the Oculus system shell**
  and must never be bound.

---

## 1. Global Layer

These systems are always active and live on the XR rig
([`WonderlandXROrigin.prefab`](../Assets/_Project/Core/XR/WonderlandXROrigin.prefab))
or on a dedicated system node.

### 1.1 Locomotion

Driven by [`QuestLocomotionComfortProfile`](../Assets/_Project/Core/XR/QuestLocomotionComfortProfile.cs).
Movement mode and turn mode are exclusive pairs swappable at runtime.

| # | Mode | Input | Notes |
|---|---|---|---|
| L1 | **Teleport (default)** | Push left thumbstick forward → arc preview → release / left trigger to commit | `delayTime = 0.08s` |
| L2 | **Smooth Move (alt.)** | Push left thumbstick = continuous walk | `moveSpeed = 1.6 m/s`, strafe & fly forced off |
| L3 | **Snap Turn (default)** | Push right thumbstick left/right = 30° step | Debounce `0.35s`, delay `0.05s` |
| L4 | **Smooth Turn (alt.)** | Push right thumbstick left/right = continuous turn | `turnSpeed = 45 °/s` |
| L5 | **Comfort Vignette** | Automatic during L1–L4 | Aperture per mode: teleport 0.52, turn 0.58, smooth-move 0.58, smooth-turn 0.62 |

### 1.2 Scale Shift

Driven by [`ScaleManager`](../Assets/_Project/Features/ScaleShift/Runtime/ScaleManager.cs)
on the `ScaleShiftSystem` node. Disabled while any
`CatRideControllerV2.IsRideActive` is true.

| Gesture (right thumbstick **click**) | Effect |
|---|---|
| **Double-click** | Normal → Small, Large → Normal |
| **Long-press 0.45 s** | Normal → Large, Small → Normal |

| Profile | `playerScale` | `moveSpeed×` | `interactionDistance×` | `nearClip` | `eyeHeight×` |
|---|---|---|---|---|---|
| **Normal** | 1.00 | 1.00 | 1.00 | 0.05 | 1.00 |
| **Small** | **0.25** | 0.65 | 1.00 | 0.01 | 0.55 |
| **Large** | **1.75** | 1.35 | 1.40 | 0.08 | 1.25 |

Transition: `blinkDuration = 0.40s` (0.10 fade-out + 0.12 hold + 0.18 fade-in),
`cooldown = 0.50s` between switches.

Right-hand turn is suppressed for `0.15s` after each scale gesture so the
thumbstick movement that completed the gesture does not also rotate the
player.

### 1.3 Recenter View

Driven by [`RecenterController`](../Assets/_Project/Core/XR/RecenterController.cs)
on the XR rig.

| Aspect | Behaviour |
|---|---|
| Input | **Hold right B for 0.40s** (configurable). Editor fallback: hold `R`. Also accepts an `InputActionReference`. |
| Confirm haptic | 0.04s light pulse on press start, 0.12s firm pulse on confirm |
| Visual | Black fade via [`ScaleTransitionController`](../Assets/_Project/Features/ScaleShift/Runtime/ScaleTransitionController.cs) (`blinkDuration = 0.45s`) |
| Behaviour when **not riding** | Reset camera yaw so the head's horizontal forward aligns with world Z+ (or `recenterAnchor.forward` if a Transform is wired). Position is preserved unless `snapToAnchorPosition` is set. |
| Behaviour when **riding** | Routes to `CatRideControllerV2.RecenterMountedView()`: re-aligns the rig to seat forward and re-snaps the head to the `MountedViewAnchor`. |
| Hard disable | Mid scale-shift blink |
| Anti-rebound | Player must release B before a new charge can start. |
| Public API | `RequestRecenter()` for programmatic invocation (future pause menu, etc.) |

### 1.4 Horse Summon

Driven by [`HorseSummonV2`](../Assets/_Project/Features/Mounts/Runtime/v2/HorseSummonV2.cs)
attached to the `Horse_001` rig only (Cat and Dog cannot be summoned).

| Aspect | Behaviour |
|---|---|
| Input | **Left X** (primary button). Editor fallback: `X` key. |
| Behaviour | Horse trots to a point in front of the player at `standFrontDistance = 2.0m`, then rotates to face the player. |
| Speed | `summonMoveSpeed = 5 m/s`, `summonRotateSpeed = 240 °/s` |
| Animator | Drives `Vert` and `State` floats for the run / idle blend |
| Mount-active block | Cannot summon while the horse is already being ridden |

---

## 2. Region: Cat Garden — Mount System

Path: `World_Regions/Region_CatGarden/`

Three independent mounts, all sharing the same script:
[`CatRideControllerV2`](../Assets/_Project/Features/Mounts/Runtime/v2/CatRideControllerV2.cs).
Each animal has its own root: `MountRig_Cat`, `MountRig_Dog`, and `MountRig_Horse`.

### 2.1 Shared mount behaviour

| Action | Input | Conditions |
|---|---|---|
| **Mount** | Right-hand ray on the mount + **right trigger** | `mountScaleRequirement = SmallOnly` — player must be in the **Small** scale; ray distance ≤ `questRayDistance = 7m`; head-to-seat distance ≤ `questMountMaxDistance = 2.6m`. |
| **Dismount** | **Right A (primary)** | `allowQuestPrimaryButtonDismount = true`. Right trigger dismount is `allowQuestTriggerDismount = false` by default — A is the canonical dismount. |
| **Move (manual ride)** | Left thumbstick | `manualMoveSpeed = 4 m/s` |
| **Turn (manual ride)** | Right thumbstick | `manualTurnSpeed = 120 °/s` |
| **Recenter view (mid-ride)** | Long-press right B (via `RecenterController`) | Routes to `RecenterMountedView()` |

Hover feedback while idle: outline colour `(1, 0.66, 0.28, 0.64)` plus right-hand
haptic pulse.

Comfort: enables a per-ride tunneling vignette overlay
(`rideVignetteAperture = 0.58`) so head-locked motion does not induce
sickness. Terrain motion is projected to ground (`projectRideMotionToGround`)
and the visual root tilts up to `rideMaxGroundTiltAngle = 32°` along the
slope normal.

Locomotion lock: while mounted, the rig is parented to `seatAnchor`, the
player's `CharacterController` is disabled, all locomotion behaviours on
`locomotionRoot` are disabled, and the XR Device Simulator (if present)
is suspended.

### 2.2 Per-animal specifics

| Animal | Auto-trigger | Summon | Route asset |
|---|---|---|---|
| **Kitty** (`Kitty_001`) | `CatRideAutoTriggerV2` — walking into the `AutoRigerZone_V2` collider starts an auto-ride | — | `RideRoute_Cat_V2` |
| **Dog** (`Dog_001`) | — | — | `RideRoute_Dog_V3` |
| **Horse** (`Horse_001`) | — | **`HorseSummonV2`** (see §1.4) | `RideRoute_Horse_V3` |

### 2.3 Animal voice proximity

Three pairs of `VoiceTrigger / VoiceAnchor` driven by
[`AnimalVoiceProximityPlayer`](../Assets/_Project/Features/Mounts/Runtime/v2/AnimalVoiceProximityPlayer.cs).
No controller input. Walking into the trigger plays the corresponding
animal's vocal SFX.

### 2.4 Guide butterflies

Three instances (`GuideButterfly_V2` / `V3` / one more) driven by
[`ButterflyFlightControllerV2`](../Assets/_Project/Features/Mounts/Runtime/v2/ButterflyFlightControllerV2.cs)
and [`ButterflyAutoTriggerV2`](../Assets/_Project/Features/Mounts/Runtime/v2/ButterflyAutoTriggerV2.cs).
No controller input. When the player (riding) approaches a trigger zone,
the butterfly takes off along `FlightPoint_XX-a/b/c`, then hides at
`catApproachDistance = 1.5m` and respawns after
`hiddenDurationBeforeReappear = 0.25s`.

---

## 3. Region: Lotus Pond — Music Sequencer

Path: `World_Regions/Region_LotusPond/`

Driver: [`LotusEitherHandDriver`](../Assets/_Project/Features/LotusPond/Runtime/LotusEitherHandDriver.cs)
on the `LotusInteractionDriver` node.
Per-leaf logic: [`LotusNoteTrigger`](../Assets/_Project/Features/LotusPond/Runtime/LotusNoteTrigger.cs).
Score state: [`LotusSongManager`](../Assets/_Project/Features/LotusPond/Runtime/LotusSongManager.cs)
and [`LotusSongUIController`](../Assets/_Project/Features/LotusPond/Runtime/LotusSongUIController.cs)
(via `LotusMusicUI.prefab`).

### 3.1 Note pads

Seven `LotusNoteTrigger` instances, one per pad
(`LotusPad_A` through `LotusPad_G`), tuned to the **seven-note diatonic
major scale**: **do · re · mi · fa · sol · la · si**.

| Action | Input |
|---|---|
| **Play a note** | Either-hand ray at a lotus pad + **trigger on that hand** |
| **Editor / debug fallback** | Left mouse click or right mouse click on screen-space pointer |

Ray distance: `rayDistance = 20m`. Hover outline: cyan
`(0.38, 0.95, 1, 0.62)`. The ray itself can also be drawn in cyan-blue
when `showQuestRays = true`.

Triggering a pad fires a curved water-magic projectile from the
controller toward the leaf, then plays:

1. **Audio** — leaf-specific `noteClip` from a 3D `AudioSource` (linear rolloff,
   spatial blend 0.35, max distance ≥ 24m).
2. **Ripple** — `LotusRippleController.PlayRipple()` on the leaf.
3. **Water impact effect** — pooled particle burst at the hit point.
4. **Physical wobble** — spring-damped tilt of the leaf about the
   axis perpendicular to the incoming direction
   (`wobbleIntensity = 5`, `stiffness = 200`, `damping = 10`,
   `duration = 0.5s`).
5. **Water-drop slide** — child `WaterDropSlide` droplets slide down the
   tilted leaf surface.

Per-pad cooldown: `cooldownSeconds = 0.25` (some pads override via
`LotusScaleSettingsSO`). The projectile is pooled per pad to avoid
per-trigger allocations.

### 3.2 Score selector

One additional `LotusNoteTrigger` — not a note pad — acts as the **score
starter**. Triggering it asks `LotusSongManager` to randomly pick a
song from its repertoire; the player then has to play the seven pads in
that order. `LotusSongUIController` (`LotusMusicUI.prefab`) renders the
current score and progress.

> Total `LotusNoteTrigger` count in the scene: **8** (7 pads + 1 score
> starter). This is intentional.

---

## 4. Region: Fireworks Clearing

Path: `World_Regions/Region_FireworksClearing/`

### 4.1 F1 — Magic mortar device (player-driven)

| Field | Value |
|---|---|
| Object | `FireworkMagicMortarDevice` |
| Scripts | [`FireworkMagicActivator`](../Assets/_Project/Features/Fireworks/Runtime/FireworkMagicActivator.cs), [`FireworkLaunchPad`](../Assets/_Project/Features/Fireworks/Runtime/FireworkLaunchPad.cs), [`FireworkController`](../Assets/_Project/Features/Fireworks/Runtime/FireworkController.cs), [`FireworkRandomParticlePlayer`](../Assets/_Project/Features/Fireworks/Runtime/FireworkRandomParticlePlayer.cs) |
| Input | Right-hand ray at the device + **right trigger** |
| Ray range | `maxInteractDistance = 36m`, `recognitionRadius = 1.25m` |
| Visual aim ray | Optional `showQuestAimRay`; idle warm orange, hover bright orange |
| Sequence | 1. Right haptic select pulse. 2. Spiral-strand fire ribbon flies from controller along a cubic-Bezier path to the device (`projectileFlightSeconds = 1.55s`). 3. Impact spark burst + haptic impact pulse. 4. Wait `launchDelayAfterArrival = 2.25s`. 5. `FireworkLaunchPad.TriggerShowcase()` runs the point-cloud firework showcase. |
| Lock-out | `lockUntilShowcaseEnds = true`; the device refuses further input until the showcase ends, or `fallbackShowcaseLockSeconds = 34s` whichever first |

### 4.2 F2 — Supplementary firework animation (passive)

A second `FireworkLaunchPad` without a `FireworkMagicActivator` —
**intentional ambient/companion animation**. Either auto-played or driven
indirectly by F1. Does not consume controller input.

---

## 5. Region: Flower Garden — Petal / Pollen Magic

Path: `World_Regions/Region_FlowerGarden/`

### 5.1 The flower (one instance)

[`PetalPollenSource_Flower.prefab`](../Assets/_Project/Features/ParticleVitality/Prefabs/PetalPollenSource_Flower.prefab)
contains:

| Component | Count | Role |
|---|---|---|
| [`PetalPollenTrigger`](../Assets/_Project/Features/ParticleVitality/Runtime/PetalPollenTrigger.cs) | 1 | Ray-hover target. Owns a list of child sources and picks one (random by default) per extraction. |
| [`PetalPollenSource`](../Assets/_Project/Features/ParticleVitality/Runtime/PetalPollenSource.cs) | 5 | Root flower + four directional `HiddenSource_North/East/South/West` to give 360° coverage. |

### 5.2 The player's magic rig

[`PetalPollenMagicRig.prefab`](../Assets/_Project/Features/ParticleVitality/Prefabs/PetalPollenMagicRig.prefab)
hosting [`PetalPollenMagicController`](../Assets/_Project/Features/ParticleVitality/Runtime/PetalPollenMagicController.cs).
One instance in the scene.

### 5.3 Interaction loop

| Action | Input | Behaviour |
|---|---|---|
| **Begin collect** | Right-hand ray hits the `PetalPollenTrigger` + **hold right trigger** | Trigger picks one of its 5 sources; particles spawn from that source and flow along a quadratic-Bezier arc into a hovering sphere ~0.85m in front of the player's head (view-locked, not hand-locked). |
| **Hold and charge** | Keep right trigger held | Particles accumulate up to `maxParticles = 900`. After `chargedHoldSeconds = 3s` the release is "charged" with extra radius / height / brightness / size. |
| **Release** | Release right trigger | Sphere bursts into one of six procedural patterns: `SpiralBloom`, `MathRibbon`, `TornadoVortex`, `AizawaFountain`, `DreamAttractor`, `GalaxyVeil` (chosen by hold duration when `randomizeReleaseMode = true`, otherwise fixed). |

Hover feedback: source outline colour `(1, 0.72, 0.34, 0.66)` plus right
haptic. Near-distance gate: `questNearInteractDistance = 4.2m`.

Input lock: while a release is dispersing, further collects are blocked
(`lockQuestInputUntilReleaseComplete = true`).

---

## 6. Region: Mushroom Growth

Path: `World_Regions/Region_MushroomGrowth/`

### 6.1 Seed zone

Driven by [`GrowthSeedZoneDriver`](../Assets/_Project/Features/Growth/Runtime/GrowthSeedZoneDriver.cs)
on the `GrowthSeedZone` node. The zone is a `Collider` bound by
`growthZone`; mushrooms can only spawn inside it.

| Action | Input | Behaviour |
|---|---|---|
| **Single-tap seed** | Right-hand ray at zone ground + **right trigger tap** | An "earth magic" projectile arcs from the controller to the hit point (cubic-Bezier, `earthMagicFlightSeconds = 1.55s`); on impact, one mushroom is instantiated with random yaw, scale `0.25–0.5×`, and a 0.85–1.2× duration jitter. `tapMushroomsPerSeed = 1`. |
| **Charged burst** | **Hold right trigger ≥ `chargedHoldSeconds = 0.65s`**, then release | A glowing `EarthMagicChargeOrb` builds at the controller during the hold. On release, 5–8 mushrooms (`chargedMin = 5`, `chargedMax = 8`) spawn in a ring of `chargedBurstRadius = 4m` around the hit point. |

Spawn constraints:
- `minSpacingBetweenPlants = 0.75m`
- `minSpawnDistanceFromPlayer = 1.6m`
- `requireTerrainColliderForNewMushrooms = true`
- `blockWhenPointingAtInteractable = true`

Default input source: right trigger only (`rightTriggerOnly = true`).
Left trigger and keyboard `G` fallback exist but are disabled by default.

### 6.2 Cultivate existing mushrooms

Each instantiated mushroom carries
[`GrowthPlant`](../Assets/_Project/Features/Growth/Runtime/GrowthPlant.cs).
When the right-hand ray hovers an existing mushroom (outline colour
`(0.74, 0.5, 0.2, 0.66)`), pressing trigger cultivates it rather than
seeding a new one.

| Action | Input | Effect |
|---|---|---|
| **Cultivate (tap)** | Trigger tap on existing mushroom | `+0.35×` scale step, lerped over `0.45s`, capped at `existingMushroomMaxScale = 2.4×` |
| **Cultivate (charged)** | Hold + release on existing mushroom | `+3 × 0.35× = +1.05×` in one go (still capped at 2.4×), lerped over `0.45 × √3 ≈ 0.78s` |

Settings are exposed: `existingMushroomScaleStep = 0.35`,
`existingMushroomMaxScale = 2.4`, `existingMushroomScaleSeconds = 0.45`,
`chargedExistingMushroomGrowthSteps = 3`.

### 6.3 Existing mushrooms in the scene

Eleven `GrowthPlant_01..11` instances are pre-placed in the zone, plus a
single `growth_energy.prefab` instance that hosts ambient growth VFX.

---

## 7. Region: Cherry Garden — Runtime Crystal Orb

Path: `World_Regions/Region_CherryGarden/`

### 7.1 What the player sees

A glowing crystal orb floats above the cherry tree. Aiming the right
controller ray at it and pressing trigger collapses the orb, which then
triggers the tree's growth animation and a swirling petal vortex.

### 7.2 What actually happens

The orb is **not present in the scene file**. It is spawned at runtime
by [`CherryGardenCrystalOrbBootstrap`](../Assets/_Project/Features/CherryGarden/Runtime/CherryGardenCrystalOrbTrigger.cs)
(at the bottom of `CherryGardenCrystalOrbTrigger.cs`), which uses
`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` to:

1. Check whether `CherryGardenCrystalOrbTrigger` already exists in the scene.
2. Find a `TreeGrowthController` and a `FlowerVortexEffect`.
3. If at least one exists, create a new `GameObject` named
   `CherryGarden_CrystalOrbTrigger`, attach the trigger script, and
   call `Configure(treeGrowth, flowerVortex)`.

The trigger script then primitive-creates a `Sphere` at
`treeGrowthController.transform.position + (0, 2.35, 0)` (radius
`orbRadius = 1.05m`), nests a `1.18×` glowing halo sphere inside it,
adds a `PointLight`, and a `QuestInteractableFeedback`.

### 7.3 Bindings

| Action | Input | Effect |
|---|---|---|
| **Activate orb** | Right-hand ray at orb + **right trigger** | One-shot. Orb collapses over `collapseDuration = 0.72s` with shake amplitude `0.16` and frequency `58Hz`. |
| Ray range | `maxInteractDistance = 36m`, `recognitionRadius = 1.2m` | — |
| Hover feedback | Outline colour `(1, 0.48, 0.72, 0.74)` plus right haptic | — |
| Chained effect | `TreeGrowthController.PlayGrowthOnce()` runs the four-phase growth animation; `FlowerVortexEffect.PlayEffect()` (via internal sequence) blooms petals. | — |
| Single-shot | `activated = true` afterwards — cannot be re-triggered. | — |

The cherry tree itself defaults to `playOnEnable = false` and
`setSeedStateWhenWaiting = true`, so it sits in seed state until the
orb is activated.

---

## 8. Passive UI Hints

| # | Element | Script | Trigger |
|---|---|---|---|
| 8.1 | Static `InteractionPrompt` instance | [`InteractionPrompt`](../Assets/_Project/UI/Scripts/InteractionPrompt.cs) | Player walks into the prompt's trigger collider |
| 8.2 | Floating gaze prompt | [`FloatingInteractionPrompt`](../Assets/_Project/UI/Scripts/FloatingInteractionPrompt.cs) | Player's head is within `triggerDistance = 20m` and gaze alignment ≥ `gazeThreshold = 0.85` for ≥ `delayTime = 3s` |

Neither consumes a controller button.

---

## 9. Controller Button Reverse Lookup

### 9.1 Right Quest controller

| Button | Consumers (main scene) | Notes |
|---|---|---|
| **Trigger** | F1 firework mortar · L1 lotus notes (right hand) · §2.1 mount · §5.3 petal-pollen collect/release · §6.1 mushroom seed · §6.2 mushroom cultivate · §7.3 cherry orb | All raycast-based and mutually exclusive — each tick the ray hits a single target. |
| **A** (primary) | §2.1 dismount | Only consumed while riding. |
| **B** (secondary) | §1.3 recenter | The only consumer. |
| **Thumbstick push** | L3/L4 turn · §2.1 ride turn | Mutually exclusive: locomotion turn is disabled while mounted, replaced by ride turn. |
| **Thumbstick click** | §1.2 scale shift | Disabled while riding. |
| **Menu** | — | ⚠ Reserved by the Oculus system shell. Do not bind. |

### 9.2 Left Quest controller

| Button | Consumers (main scene) | Notes |
|---|---|---|
| **Trigger** | L1 teleport confirm (teleport mode) · L1 lotus notes (left hand) | ⚠ Possible overlap: in teleport mode the player could aim a teleport arc at a lotus pad. Recommend excluding the lotus layer from `QuestLocomotionComfortProfile.teleportRaycastMask`. |
| **X** (primary) | §1.4 horse summon | Single consumer. |
| **Y** (secondary) | — | Free. Suggested for a future pause menu. |
| **Thumbstick push** | L1 teleport aim · L2 smooth move · §2.1 ride move | Mutually exclusive by locomotion mode and ride state. |
| **Thumbstick click** | — | Free. |
| **Menu** | — | Free. Strongly suggested binding: pause menu (right Menu is unusable on Quest). |

### 9.3 Head / proximity / posture

| Input | Consumers |
|---|---|
| Head pose (gaze + distance) | §8.2 floating gaze prompt |
| Entering a trigger collider | §2.2 Kitty auto-ride · §2.4 guide butterflies · §2.3 animal voice · §8.1 static prompt |
| Hand pose | Not consumed in this scene (the `HandProxy` / `LandingTarget` butterfly-landing-on-palm system from `Assets/Scripts/Interaction/` is not wired into the production scene). |

---

## 10. Known Conflicts and Open Items

| # | Concern | Recommended action |
|---|---|---|
| 10.1 | **Left-trigger overlap** between teleport confirm (L1) and lotus pad activation. In teleport mode, aiming a teleport arc that grazes a lotus pad could double-fire. | Put `LotusNoteTrigger` colliders on a dedicated layer and exclude that layer from `QuestLocomotionComfortProfile.teleportRaycastMask`. |
| 10.2 | **Right-trigger raycast ordering**. Multiple features (firework mortar, lotus, cherry orb, mount, petal-pollen, mushroom seed) all listen for right trigger on right-hand raycast. They each do their own raycast with their own `LayerMask`. | Audit each feature's `LayerMask` so the colliders for each interaction live on a unique layer. Confirmed today: all features use a non-default mask, but layer assignment on the scene objects should be spot-checked before submission. |
| 10.3 | **Right A** is shared by mount-dismount only. Once a future pause menu is added, ensure pause input is on the left Menu / left Y rather than right A to avoid the dismount-while-paused edge case. | Bind pause to left Menu (see §9.2). |

---

## Appendix A — System inventory by region

| Region | Interactable objects | Active scripts |
|---|---|---|
| `Region_FireworksClearing` | `FireworkMagicMortarDevice` (F1), supplementary `FireworkLaunchPad` (F2) | `FireworkMagicActivator`, `FireworkLaunchPad` ×2, `FireworkController`, `FireworkRandomParticlePlayer` |
| `Region_FlowerGarden` | `PetalPollenSource_Flower` (with 5 sources + trigger), `PetalPollenMagicRig` | `PetalPollenMagicController`, `PetalPollenSource` ×5, `PetalPollenTrigger` |
| `Region_CatGarden` | `Kitty_001`, `Dog_001`, `Horse_001`, voice triggers, butterflies | `CatRideControllerV2` ×3, `CatIdlePaceV2` ×3, `HorseSummonV2`, `CatRideAutoTriggerV2`, `ButterflyFlightControllerV2` ×3, `ButterflyAutoTriggerV2` ×3, `AnimalVoiceProximityPlayer` ×3 |
| `Region_CherryGarden` | `HeroCherryTree_GrowthRig` (passive), runtime-spawned `CherryGarden_CrystalOrb` | `TreeGrowthController`, `FlowerVortexEffect`, `CherryGardenCrystalOrbTrigger` (runtime) |
| `Region_LotusPond` | `LotusPad_A..G` (7), score starter (1), `LotusMusicUI` | `LotusNoteTrigger` ×8, `LotusEitherHandDriver`, `LotusSongManager`, `LotusSongUIController` |
| `Region_MushroomGrowth` | `GrowthSeedZone`, `GrowthPlant_01..11`, `growth_energy` | `GrowthSeedZoneDriver`, `GrowthController`, `GrowthDriver`, `GrowthPlant` ×11 |
| Global / system | `WonderlandXROrigin`, `ScaleShiftSystem`, `GlobalSystem` | `QuestLocomotionComfortProfile`, `QuestInteractableFeedback`, `QuestRayVisualBroker`, `QuestRayVisualLengthProfile`, `ScaleManager`, `ScaleTransitionController`, `RecenterController` |

---

## Appendix B — File index

Core XR
- [`QuestLocomotionComfortProfile.cs`](../Assets/_Project/Core/XR/QuestLocomotionComfortProfile.cs)
- [`QuestInteractionUtils.cs`](../Assets/_Project/Core/XR/QuestInteractionUtils.cs)
- [`QuestInteractableFeedback.cs`](../Assets/_Project/Core/XR/QuestInteractableFeedback.cs)
- [`QuestRayVisualBroker.cs`](../Assets/_Project/Core/XR/QuestRayVisualBroker.cs)
- [`QuestRayVisualLengthProfile.cs`](../Assets/_Project/Core/XR/QuestRayVisualLengthProfile.cs)
- [`RecenterController.cs`](../Assets/_Project/Core/XR/RecenterController.cs)

Scale
- [`ScaleManager.cs`](../Assets/_Project/Features/ScaleShift/Runtime/ScaleManager.cs)
- [`ScaleTransitionController.cs`](../Assets/_Project/Features/ScaleShift/Runtime/ScaleTransitionController.cs)
- [`ScaleSettings_SO.asset`](../Assets/_Project/Features/ScaleShift/ScriptableObjects/ScaleSettings_SO.asset)

Mounts
- [`CatRideControllerV2.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/CatRideControllerV2.cs)
- [`CatRideAutoTriggerV2.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/CatRideAutoTriggerV2.cs)
- [`CatIdlePaceV2.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/CatIdlePaceV2.cs)
- [`HorseSummonV2.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/HorseSummonV2.cs)
- [`ButterflyFlightControllerV2.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/ButterflyFlightControllerV2.cs)
- [`ButterflyAutoTriggerV2.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/ButterflyAutoTriggerV2.cs)
- [`AnimalVoiceProximityPlayer.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/AnimalVoiceProximityPlayer.cs)

Lotus Pond
- [`LotusEitherHandDriver.cs`](../Assets/_Project/Features/LotusPond/Runtime/LotusEitherHandDriver.cs)
- [`LotusNoteTrigger.cs`](../Assets/_Project/Features/LotusPond/Runtime/LotusNoteTrigger.cs)
- [`LotusSongManager.cs`](../Assets/_Project/Features/LotusPond/Runtime/LotusSongManager.cs)
- [`LotusSongUIController.cs`](../Assets/_Project/Features/LotusPond/Runtime/LotusSongUIController.cs)

Fireworks
- [`FireworkMagicActivator.cs`](../Assets/_Project/Features/Fireworks/Runtime/FireworkMagicActivator.cs)
- [`FireworkLaunchPad.cs`](../Assets/_Project/Features/Fireworks/Runtime/FireworkLaunchPad.cs)
- [`FireworkController.cs`](../Assets/_Project/Features/Fireworks/Runtime/FireworkController.cs)
- [`FireworkRandomParticlePlayer.cs`](../Assets/_Project/Features/Fireworks/Runtime/FireworkRandomParticlePlayer.cs)

Petal / Pollen Magic
- [`PetalPollenMagicController.cs`](../Assets/_Project/Features/ParticleVitality/Runtime/PetalPollenMagicController.cs)
- [`PetalPollenTrigger.cs`](../Assets/_Project/Features/ParticleVitality/Runtime/PetalPollenTrigger.cs)
- [`PetalPollenSource.cs`](../Assets/_Project/Features/ParticleVitality/Runtime/PetalPollenSource.cs)

Mushroom Growth
- [`GrowthSeedZoneDriver.cs`](../Assets/_Project/Features/Growth/Runtime/GrowthSeedZoneDriver.cs)
- [`GrowthController.cs`](../Assets/_Project/Features/Growth/Runtime/GrowthController.cs)
- [`GrowthDriver.cs`](../Assets/_Project/Features/Growth/Runtime/GrowthDriver.cs)
- [`GrowthPlant.cs`](../Assets/_Project/Features/Growth/Runtime/GrowthPlant.cs)

Cherry Garden
- [`CherryGardenCrystalOrbTrigger.cs`](../Assets/_Project/Features/CherryGarden/Runtime/CherryGardenCrystalOrbTrigger.cs)
- [`TreeGrowthController.cs`](../Assets/_Project/Features/CherryGarden/Runtime/TreeGrowthController.cs)
- [`FlowerVortexEffect.cs`](../Assets/_Project/Features/CherryGarden/Runtime/FlowerVortexEffect.cs)

UI
- [`InteractionPrompt.cs`](../Assets/_Project/UI/Scripts/InteractionPrompt.cs)
- [`FloatingInteractionPrompt.cs`](../Assets/_Project/UI/Scripts/FloatingInteractionPrompt.cs)

---
