# Wonderland Park — Interaction Bindings

> Authoritative reference for every player-facing interactable in the production scene
> [`World_WonderlandPark.unity`](../Assets/_Project/World/Persistent/World_WonderlandPark.unity).
> All values below have been cross-checked against the runtime scripts under
> `Assets/_Project` and the serialized values inside the production scene file.

---

## Conventions

- "Right trigger" / "Right A" refer to the Meta Quest controller buttons.
  On the right hand: A = `primaryButton`, B = `secondaryButton`.
  On the left hand: X = `primaryButton`, Y = `secondaryButton`.
- "Ray" means a controller-origin `Physics.RaycastNonAlloc` forward from the
  controller (typically the Stabilized Attach transform).
- Distances are in metres, durations in seconds, angles in degrees.
- Scale-shift values are read from
  [`ScaleSettings_SO.asset`](../Assets/_Project/Features/ScaleShift/ScriptableObjects/ScaleSettings_SO.asset).
- **Right-hand Menu is owned by the Oculus system shell** and must never be bound.
  **Left-hand Menu is bound to the in-experience system menu** (see §1.5).

---

## 1. Global Layer

These systems are always active and live on the XR rig
([`WonderlandXROrigin.prefab`](../Assets/_Project/Core/XR/WonderlandXROrigin.prefab))
or on a dedicated system node.

### 1.1 Locomotion

Driver: [`QuestLocomotionComfortProfile`](../Assets/_Project/Core/XR/QuestLocomotionComfortProfile.cs).
Movement mode and turn mode are exclusive pairs, swappable at runtime via the settings menu.

| # | Mode | Input | Notes |
|---|---|---|---|
| L1 | **Teleport (default)** | Push left thumbstick forward → arc preview → release/left trigger to commit | `teleportDelayTime = 0.08 s`. Teleport surfaces honour a slope clamp at `maxTeleportSlopeDegrees = 38°`. |
| L2 | **Smooth Move (alt.)** | Left thumbstick = continuous walk | `smoothMoveSpeed = 1.6 m/s`. Strafe and vertical fly are disabled. |
| L3 | **Snap Turn (default)** | Right thumbstick left/right = stepped turn | `snapTurnAmount = 30°`, `snapTurnDebounceTime = 0.35 s`, `snapTurnDelayTime = 0.05 s`. |
| L4 | **Smooth Turn (alt.)** | Right thumbstick left/right = continuous turn | `smoothTurnSpeed = 45°/s`. |
| L5 | **Comfort Vignette** | Automatic during L1–L4 | Aperture per mode: teleport `0.52`, snap/smooth turn `0.58 / 0.62`, smooth move `0.58`. Feathering `0.30`, easeIn `0.10 s`, easeOut `0.20 s`, easeOut delay `0.06 s`. |

### 1.2 Scale Shift

Driver: [`ScaleManager`](../Assets/_Project/Features/ScaleShift/Runtime/ScaleManager.cs)
on the `ScaleShiftSystem` node. **Disabled while any
`CatRideControllerV2.IsRideActive` is true.**

| Gesture (right thumbstick **click / press-in**) | Effect |
|---|---|
| **Double-click within `0.32 s`** | Normal → Small, Large → Normal |
| **Long-press `≥ 0.45 s`** | Normal → Large, Small → Normal |

| Profile | `playerScale` | `moveSpeed×` | `interactionDistance×` | `nearClip` | `eyeHeight×` |
|---|---|---|---|---|---|
| **Normal** | 1.00 | 1.00 | 1.00 | 0.05 | 1.00 |
| **Small** | **0.25** | 0.65 | 1.00 | 0.01 | 0.55 |
| **Large** | **1.75** | 1.35 | 1.40 | 0.08 | 1.25 |

Transitions go through [`ScaleTransitionController`](../Assets/_Project/Features/ScaleShift/Runtime/ScaleTransitionController.cs):
`blinkDuration = 0.40 s` (0.10 fade-out + 0.12 hold + 0.18 fade-in), `cooldown = 0.50 s` between switches.

Right-hand turn input is suppressed for `thumbstickLocomotionSuppressSeconds = 0.15 s`
after each scale gesture so the thumbstick movement that completed the gesture does
not also rotate the player.

### 1.3 Recenter View

Driver: [`RecenterController`](../Assets/_Project/Core/XR/RecenterController.cs)
on the XR rig.

| Aspect | Behaviour |
|---|---|
| Input | **Hold right B (`secondaryButton`) for `holdSecondsToConfirm = 0.40 s`.** Editor fallback: hold `R` (`enableKeyboardDebug = true`). Also accepts an `InputActionReference`. |
| Haptics | `chargeStart` 0.04 s @ 0.25 amplitude on press, `confirm` 0.12 s @ 0.6 amplitude on commit. |
| Visual | Black fade via [`ScaleTransitionController`](../Assets/_Project/Features/ScaleShift/Runtime/ScaleTransitionController.cs) (`blinkDuration = 0.45 s`). |
| When **not riding** | Reorients the rig so the head's horizontal forward aligns with world Z+, or with `recenterAnchor.forward` when wired. Position is preserved unless `snapToAnchorPosition = true`. Ground recovery lifts the rig if it ends up below the ground (`recoverIfBelowGroundAfterRecenter = true`). |
| When **riding** | Routes to `CatRideControllerV2.RecenterMountedView()`: re-aligns the rig to seat forward and re-snaps the head to the `MountedViewAnchor`. |
| Hard disable | Mid scale-shift blink (`disableWhileScaleTransitioning = true`). |
| Anti-rebound | Player must release B before a new charge can start. |
| Public API | `RequestRecenter()` for programmatic invocation. |

### 1.4 Horse Summon

Driver: [`HorseSummonV2`](../Assets/_Project/Features/Mounts/Runtime/v2/HorseSummonV2.cs)
attached to `Horse_001_rig` only (Cat and Dog cannot be summoned).

| Aspect | Behaviour |
|---|---|
| Input | **Left X (`primaryButton`)**, gated by `enableQuestLeftXButton = true`. Editor fallback: `X` key. |
| Behaviour | Horse trots to a point `standFrontDistance = 2.0 m` in front of the player, then rotates to face the player. |
| Speed | `summonMoveSpeed = 7.8125 m/s`, `summonRotateSpeed = 240°/s`. |
| Terrain | Motion projected to ground (`projectSummonMotionToGround = true`); visual root tilts up to `summonMaxGroundTiltAngle = 32°` along the slope normal. |
| Animator | Drives `Vert` and `State` floats for the run/idle blend. |
| Block | Cannot summon while `rideController.IsRideActive`. |

### 1.5 System Menu

Driver: [`VRSystemMenuController`](../Assets/_Project/UI/Scripts/VRSystemMenuController.cs)
on the `WW_VRSystemMenu` prefab.

| Aspect | Behaviour |
|---|---|
| Input | **Left Menu** (`XRCommonUsages.menuButton` on `XRNode.LeftHand`), gated by `useLeftHandMenuFallback = true`. Also accepts an `InputActionReference` and an Escape-key editor fallback. |
| Panels | Main → Settings / Tutorial / Exit / Restart; Back / Cancel return to main. |
| Restart | Reloads the active scene and re-queues the Welcome flow on next load. |
| Placement | World-space billboard following the head camera at `distanceFromCamera = 1.3 m` with `cameraLocalOffset = (0, −0.12, 0)`. |

---

## 2. Welcome Flow (Entry)

Driver: [`WelcomeFlowController`](../Assets/_Project/UI/Scripts/WelcomeFlowController.cs)
on `WelcomePanel` under the scene's `UI` root.

| Aspect | Behaviour |
|---|---|
| Trigger | Auto-shown on scene load (and after `RestartCurrentScene`). |
| Buttons | **Start** (begins the experience), **English / 中文 / Svenska** (language selector). |
| Language switch | Calls `UILanguageService.SetLanguage(...)` and updates all `LocalizedUIText` instances in the scene. |
| Lifecycle | `destroyAfterStart = true` — the panel is destroyed after Start is clicked. |
| Lock options | `lockLocomotionWhileShown` and `disableThumbstickScaleWhileShown` are exposed but currently off in the scene. |

---

## 3. Region: Flower Garden — Particle Vitality (Pink Crystal)

Path: `World_Regions/Region_FlowerGarden/`.

The visible interactable in this region is a **magical crystal stone**, not a flower
— the prefab is legacy-named `PetalPollenSource_Flower` but the
[`PetalPollenTrigger`](../Assets/_Project/Features/ParticleVitality/Runtime/PetalPollenTrigger.cs)
on it sets `useCrystalStoneVisual = true` and renders a pink-magenta crystal with
emission glow (`crystalEmissionColor = (1, 0.36, 0.66)`, intensity `0.62`,
highlight scale `1.06×`).

### 3.1 Region content

| Component | Count | Role |
|---|---|---|
| [`PetalPollenTrigger`](../Assets/_Project/Features/ParticleVitality/Runtime/PetalPollenTrigger.cs) | 1 | Ray-hover target (the crystal). Owns a list of child sources and picks one (random by default) per extraction. |
| [`PetalPollenSource`](../Assets/_Project/Features/ParticleVitality/Runtime/PetalPollenSource.cs) | 5 | Root source + four directional `HiddenSource_North/East/South/West` to give 360° emission coverage. |
| [`PetalPollenMagicController`](../Assets/_Project/Features/ParticleVitality/Runtime/PetalPollenMagicController.cs) | 1 | Lives on `PetalPollenMagicRig.prefab`. Manages particle flow, the held sphere, and the release modes. |

### 3.2 Interaction loop

| Action | Input | Behaviour |
|---|---|---|
| **Begin collect** | Right-hand ray hits the `PetalPollenTrigger` + **hold right trigger** | Trigger picks one of its 5 sources; particles spawn at the source and flow along a quadratic-Bezier arc into a hovering sphere at `holdDistance = 0.48 m` in front of the player's head (view-locked, not hand-locked). `holdRadius = 0.32 m`. |
| **Hold and charge** | Keep right trigger held | Particles accumulate up to `maxParticles = 900` at `particlesPerSecond = 180`. After `chargedHoldSeconds = 3 s` the release becomes "charged" with extra radius / height / brightness / size boosts. |
| **Release** | Release right trigger | The sphere bursts into one of six procedural patterns: `SpiralBloom`, `MathRibbon`, `TornadoVortex`, `AizawaFountain`, `DreamAttractor`, `GalaxyVeil`. When `randomizeReleaseMode = true`, the mode is chosen by hold duration; otherwise it uses `fixedReleaseMode`. |

`petalChance = 0.18` of the collected particles are stylised petals (the rest are
pollen). Hover feedback: crystal highlight scale-up + right haptic. Input lock:
while a release is dispersing, further collects are blocked.

---

## 4. Region: Lotus Pond — Music Sequencer

Path: `World_Regions/Region_LotusPond/`.

Driver: [`LotusEitherHandDriver`](../Assets/_Project/Features/LotusPond/Runtime/LotusEitherHandDriver.cs)
on the `LotusInteractionDriver` node. Per-leaf logic:
[`LotusNoteTrigger`](../Assets/_Project/Features/LotusPond/Runtime/LotusNoteTrigger.cs).
Score state:
[`LotusSongManager`](../Assets/_Project/Features/LotusPond/Runtime/LotusSongManager.cs)
and
[`LotusSongUIController`](../Assets/_Project/Features/LotusPond/Runtime/LotusSongUIController.cs)
(via `LotusMusicUI.prefab`).

### 4.1 Note pads

Seven `LotusNoteTrigger` instances, one per pad (`LotusPad_A` through `LotusPad_G`),
tuned to the **seven-note diatonic major scale**: **do · re · mi · fa · sol · la · si**.

| Action | Input |
|---|---|
| **Play a note** | Either-hand ray at a lotus pad + **trigger on that hand** |
| **Editor / debug fallback** | Left mouse click or right mouse click on screen-space pointer (`enableMouseDebug = true`) |

Ray distance: `rayDistance = 20 m`. Hover outline: cyan
`(0.38, 0.95, 1, 0.62)`. The ray itself is drawn cyan-blue when
`showQuestRays = true`.

Triggering a pad fires a curved water-magic projectile from the controller toward
the leaf, then plays:

1. **Audio** — leaf-specific `noteClip` from a 3D `AudioSource` (linear rolloff,
   spatial blend `0.35`, max distance ≥ 24 m).
2. **Ripple** — `LotusRippleController.PlayRipple()` on the leaf.
3. **Water impact effect** — pooled particle burst at the hit point.
4. **Physical wobble** — spring-damped tilt of the leaf about the axis
   perpendicular to the incoming direction (`wobbleIntensity = 5`,
   `stiffness = 200`, `damping = 10`, `duration = 0.5 s`).
5. **Water-drop slide** — child `WaterDropSlide` droplets slide down the tilted leaf.

Per-pad cooldown: `cooldownSeconds = 0.25` (overridable per pad via
`LotusScaleSettingsSO`). The projectile is pooled per pad to avoid per-trigger
allocations.

### 4.2 Score selector

One additional `LotusNoteTrigger` — not a note pad — acts as the **score starter**.
Triggering it asks `LotusSongManager` to randomly pick a song from its repertoire;
the player then plays the seven pads in that order. `LotusSongUIController`
(`LotusMusicUI.prefab`) renders the current score and progress.

> Total `LotusNoteTrigger` count in the scene: **8** (7 pads + 1 score starter). This is intentional.

---

## 5. Region: Cat Garden — Mount System

Path: `World_Regions/Region_CatGarden/`.

Three independent mounts, all sharing the same script:
[`CatRideControllerV2`](../Assets/_Project/Features/Mounts/Runtime/v2/CatRideControllerV2.cs).
Each animal has its own root prefab: `MountRig_Cat`, `MountRig_Dog`, `MountRig_Horse`.

### 5.1 Shared mount behaviour

| Action | Input | Conditions |
|---|---|---|
| **Mount** | Right-hand ray on the mount + **right trigger** | Ray distance ≤ `questRayDistance = 7 m`; head-to-seat distance ≤ `questMountMaxDistance = 2.6 m`; **per-animal scale gate** (see §5.2). |
| **Dismount** | **Right A (`primaryButton`)** | `allowQuestPrimaryButtonDismount = true`. Right trigger dismount is `allowQuestTriggerDismount = false` by default. |
| **Move (manual ride)** | Left thumbstick | `manualMoveSpeed = 6.25 m/s` (scene-configured). |
| **Turn (manual ride)** | Right thumbstick | `manualTurnSpeed = 100°/s` (scene-configured). |
| **Recenter view (mid-ride)** | Hold right B (via `RecenterController`) | Routes to `RecenterMountedView()`. |

Hover feedback while idle: outline colour `(1, 0.66, 0.28, 0.64)` plus right-hand haptic pulse.

Comfort: a per-ride tunneling vignette overlay (`rideVignetteAperture = 0.58`)
activates while move/turn input exceeds `rideVignetteInputDeadzone = 0.08`.
Terrain motion is projected to ground (`projectRideMotionToGround = true`) and the
visual root tilts up to `rideMaxGroundTiltAngle = 32°` along the slope normal.

Locomotion lock: while mounted, the rig is parented to `seatAnchor`, the player's
`CharacterController` is disabled, all locomotion behaviours on `locomotionRoot`
are disabled, and the XR Device Simulator (if present) is suspended.

### 5.2 Per-animal scale requirement (verified from scene)

The scale gate is configured per `MountRig_*` instance via
`mountScaleRequirement: MountScaleRequirement`:

| Mount root | `mountScaleRequirement` (enum / scene value) | Required scale | Notes |
|---|---|---|---|
| `MountRig_Cat` | `SmallOnly` (`1`) | **Small** | The cat is roughly the same size as the player at small scale. |
| `MountRig_Dog` | `SmallOnly` (`1`) | **Small** | Same reasoning. |
| `MountRig_Horse` | `NormalOnly` (`2`) | **Normal** | The horse is sized for a normal-scale rider. |

The enum is defined in `CatRideControllerV2.MountScaleRequirement`:
`Any = 0`, `SmallOnly = 1`, `NormalOnly = 2`, `LargeOnly = 3`.

### 5.3 Per-animal extras

| Animal | Auto-route trigger | Summon |
|---|---|---|
| **Kitty** (`Kitty_001`) | `CatRideAutoTriggerV2` — when the player is already **mounted on Kitty** and walks into `AutoRigerZone_V2`, the ride switches from manual to auto-route along `autoRoutePoints`. | — |
| **Dog** (`Dog_001`) | — | — |
| **Horse** (`Horse_001`) | — | **`HorseSummonV2`** (see §1.4) |

`BeginAutoRide()` requires `currentState == RideState.MountedManual`; it is not a
"walk-up" auto-start from idle.

### 5.4 Animal voice proximity

Three pairs of `VoiceTrigger / VoiceAnchor` driven by
[`AnimalVoiceProximityPlayer`](../Assets/_Project/Features/Mounts/Runtime/v2/AnimalVoiceProximityPlayer.cs).
No controller input. Walking into the trigger plays the corresponding animal's
vocal SFX.

### 5.5 Guide butterflies

Three instances of `GuideButterfly_V2 / V3 / V4` driven by
[`ButterflyFlightControllerV2`](../Assets/_Project/Features/Mounts/Runtime/v2/ButterflyFlightControllerV2.cs)
and
[`ButterflyAutoTriggerV2`](../Assets/_Project/Features/Mounts/Runtime/v2/ButterflyAutoTriggerV2.cs).
No controller input. When the player (riding the cat) approaches a trigger zone,
the butterfly takes off along `FlightPoint_XX-a/b/c`, then hides at
`catApproachDistance = 1.5 m` and respawns after
`hiddenDurationBeforeReappear = 0.25 s`.

---

## 6. Region: Mushroom Growth

Path: `World_Regions/Region_MushroomGrowth/`.

### 6.1 Seed zone

Driver: [`GrowthSeedZoneDriver`](../Assets/_Project/Features/Growth/Runtime/GrowthSeedZoneDriver.cs)
on the `GrowthSeedZone` node. The zone is the `Collider` bound to `growthZone`;
mushrooms can only spawn inside it.

Default input source: **right trigger only** (`rightTriggerOnly = true`).
Left trigger and keyboard `G` fallback exist but are disabled by default.

| Action | Input | Behaviour |
|---|---|---|
| **Single-tap seed** | Right-hand ray at zone ground + **right trigger tap** | An "earth magic" projectile arcs from the controller to the hit point (cubic-Bezier, `earthMagicFlightSeconds = 1.55 s`); on impact, one mushroom is instantiated with random yaw and a 0.85–1.2× duration jitter. `tapMushroomsPerSeed = 1`. |
| **Charged burst** | **Hold right trigger ≥ `chargedHoldSeconds = 0.65 s`**, then release | A glowing `EarthMagicChargeOrb` builds at the controller during the hold. On release, 5–8 mushrooms (`chargedMinMushroomsPerSeed = 5`, `chargedMaxMushroomsPerSeed = 8`) spawn in a ring of `chargedBurstRadius = 4 m` around the hit point. |

Spawn constraints:
- `minSpacingBetweenPlants = 0.75 m`
- `minSpawnDistanceFromPlayer = 1.6 m`
- `requireTerrainColliderForNewMushrooms = true`
- `blockWhenPointingAtInteractable = true`

### 6.2 Cultivate existing mushrooms

Each pre-placed mushroom carries
[`GrowthPlant`](../Assets/_Project/Features/Growth/Runtime/GrowthPlant.cs).
When the right-hand ray hovers an existing mushroom (outline colour
`(0.74, 0.5, 0.2, 0.66)`), pressing trigger cultivates it rather than seeding a new one.

| Action | Input | Effect |
|---|---|---|
| **Cultivate (tap)** | Trigger tap on existing mushroom | `+0.35×` scale step, lerped over `0.45 s`, capped at `existingMushroomMaxScale = 2.4×`. |
| **Cultivate (charged)** | Hold + release on existing mushroom | `+3 × 0.35× = +1.05×` in one go (still capped at 2.4×), lerped over `0.45 × √3 ≈ 0.78 s`. |

### 6.3 Pre-placed content

Eleven `GrowthPlant_01..11` instances sit inside the zone, plus a single
`growth_energy.prefab` instance hosting ambient growth VFX.

---

## 7. Region: Fireworks Clearing (Waterfall + Fireworks)

Path: `World_Regions/Region_FireworksClearing/`.

This region also hosts the stylised waterfall (`Waterfall_Stylized`,
`Audio_Waterfall_Main`, `Audio_Waterfall_Splash`) and is referred to as the
"waterfall + fireworks ground" in player-facing copy.

### 7.1 F1 — Magic mortar device (player-driven)

| Field | Value |
|---|---|
| Object | `FireworkMagicMortarDevice` |
| Scripts | [`FireworkMagicActivator`](../Assets/_Project/Features/Fireworks/Runtime/FireworkMagicActivator.cs), [`FireworkLaunchPad`](../Assets/_Project/Features/Fireworks/Runtime/FireworkLaunchPad.cs), [`FireworkController`](../Assets/_Project/Features/Fireworks/Runtime/FireworkController.cs), [`FireworkRandomParticlePlayer`](../Assets/_Project/Features/Fireworks/Runtime/FireworkRandomParticlePlayer.cs) |
| Input | Right-hand ray at the device + **right trigger** (via `interactAction` `InputActionReference`) |
| Ray range | `maxInteractDistance = 36 m`, `recognitionRadius = 1.25 m` |
| Visual aim ray | Optional `showQuestAimRay`; idle warm orange `(1, 0.46, 0.12, 0.18)`, hover bright orange `(1, 0.68, 0.2, 0.78)` |
| Sequence | 1. Right haptic select pulse. 2. Spiral-strand fire ribbon flies from controller along a cubic-Bezier path to the device (`projectileFlightSeconds = 1.55 s`). 3. Impact spark burst + haptic impact pulse. 4. Wait `launchDelayAfterArrival = 2.25 s`. 5. `FireworkLaunchPad.TriggerShowcase()` runs the point-cloud firework showcase. |
| Lock-out | `lockUntilShowcaseEnds = true`; the device refuses further input until the showcase ends, or `fallbackShowcaseLockSeconds = 34 s` — whichever first. |

### 7.2 F2 — Supplementary firework animation (passive)

A second `FireworkLaunchPad` without a `FireworkMagicActivator` —
**intentional ambient/companion animation**. Either auto-played or driven
indirectly by F1. Does not consume controller input.

---

## 8. Region: Cherry Garden — Crystal Orb + Tree Growth

Path: `World_Regions/Region_CherryGarden/`.

### 8.1 What the player sees

A glowing crystal orb floats above the cherry tree. Aiming the right controller
ray at it and pressing trigger collapses the orb, which then triggers the tree's
growth animation and a swirling petal vortex.

### 8.2 How the orb gets there

[`CherryGardenCrystalOrbTrigger`](../Assets/_Project/Features/CherryGarden/Runtime/CherryGardenCrystalOrbTrigger.cs)
spawns the orb itself. There are two entry paths:

1. **In-scene placement (default).** The trigger component is placed in the
   scene; its `Awake()` calls `CreateOrbIfNeeded()` when `createOrbOnStart = true`.
2. **Runtime bootstrap fallback.** `CherryGardenCrystalOrbBootstrap` (at the
   bottom of the same file) is decorated with
   `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`. If no
   `CherryGardenCrystalOrbTrigger` exists in the scene, it finds a
   `TreeGrowthController` + `FlowerVortexEffect`, creates a new GameObject
   `CherryGarden_CrystalOrbTrigger`, attaches the trigger, and calls
   `Configure(treeGrowth, flowerVortex)`.

Either way, the orb is a primitive `Sphere` at
`treeGrowthController.transform.position + treeRelativeOffset = (0, 2.35, 0)`,
`orbRadius = 1.05 m`, with a `1.18×` halo sphere child, a `PointLight`, and a
`QuestInteractableFeedback`.

### 8.3 Bindings

| Action | Input | Effect |
|---|---|---|
| **Activate orb** | Right-hand ray at orb + **right trigger** | One-shot. Orb collapses over `collapseDuration = 0.72 s` with shake amplitude `0.16` and frequency `58 Hz`. |
| Ray range | `maxInteractDistance = 36 m`, `recognitionRadius = 1.2 m` | — |
| Hover feedback | Outline colour `(1, 0.48, 0.72, 0.74)` plus right haptic | — |
| Chained effect | `TreeGrowthController.PlayGrowthOnce()` runs the four-phase growth animation; `FlowerVortexEffect.PlayEffect()` blooms petals. | — |
| Single-shot | `activated = true` afterwards — cannot be re-triggered. | — |

The cherry tree itself defaults to `playOnEnable = false` and
`setSeedStateWhenWaiting = true`, so it stays in seed state until the orb is
activated.

---

## 9. Park Decorations — Wooden Swing

Path: `Decorations/Swings/TFF_Wooden_Swing_01A`.

The wooden swing is **not** part of any `Region_*` root — it lives under the
scene's `Decorations` layer alongside other park dressing. Driver:
[`QuestSwingRideController`](../Assets/_Project/Features/Mounts/Runtime/QuestSwingRideController.cs)
on the swing prefab. The seat (`TFF_Wooden_Swing_Seat_01A`) hangs below the
crossbar; while the player rides, the **frame stays still** and only the seat
pivots, so other riders in the scene see a real pendulum.

### 9.1 Scale gate (verified from script default)

`mountScaleRequirement` defaults to `SwingScaleRequirement.NormalOnly` and is
not overridden in the scene instance.

| Enum value | Required scale |
|---|---|
| `Any` | any |
| `SmallOnly` | Small |
| **`NormalOnly` (default in scene)** | **Normal** |
| `LargeOnly` | Large |

Mid scale-transition the swing refuses mount so the rig is never left in a
half-scaled state (`scaleManager.IsTransitioning` check).

### 9.2 Mount / dismount

| Action | Input | Conditions |
|---|---|---|
| **Sit** | Right-hand ray on the **seat** + **right trigger** | Player must be in **Normal** scale; ray distance ≤ `rayDistance = 7 m`; head-to-seat horizontal distance ≤ `mountDistance = 2.4 m`. With `restrictRayToSeatOnly = true`, pointing at the frame, ropes, or posts does **not** trigger a hover or mount. |
| **Dismount** | **Right A (`primaryButton`)** | No conditions other than being currently seated. Drop-off position is `dismountSideDistance = 1.15 m` to the seat's local side + `dismountBackDistance = 0.45 m` back, projected to ground via `groundProbeDistance = 5 m`. |
| **Recenter (mid-ride)** | Hold right B (via `RecenterController`) | Routes to `QuestSwingRideController.RecenterMountedView()`: yaw-only re-align to the seat's initial forward, head XZ snapped to the swing seat centre, head Y preserved. |

Hover feedback: outline colour `(0.78, 0.94, 1, 0.62)` plus right-hand haptic.

Locomotion lock: while seated, the player's `CharacterController` is disabled,
every `LocomotionProvider` under `locomotionRoot` is disabled, the comfort
profile is runtime-locked, and the swing's idle `Animator` is disabled so the
script can drive the seat directly. All of these are restored on dismount.

### 9.3 Pendulum physics (scene values vs. script defaults)

The script implements a simple gravity-restoring pendulum with damping; the
scene instance overrides a few values to give the wooden swing a livelier feel
than the script default:

| Parameter | Script default | **Scene value** | Meaning |
|---|---|---|---|
| `swingLength` | 1.10 m | 1.10 m | Rider sits this far below the pivot. |
| `maxAngleDegrees` | 22° | **60°** | Hard clamp on the local-Z swing angle; at the clamp the angular velocity is mirrored with `× -0.12`. |
| `pumpAcceleration` | 26 | **36** | How hard the left-stick pump impulse pushes the swing. |
| `gravityAcceleration` | 9.5 | 9.5 | Restoring force coefficient. |
| `angularDamping` | 1.15 | 1.15 | Continuous air damping. |
| `autoSettleDamping` | 0.5 | 0.5 | Extra damping while the player isn't pumping. |
| `mountedEyeHeight` | 0.62 m | 0.62 m | Sitting eye height above the rider's seated position. |

Pump input: **left thumbstick Y axis** (forward/back), dead zone `0.08`.

View comfort: while mounted, `rideAnchor` is set every frame to the seat-local
rider position, but the rig's **rotation** is locked to the seat's initial
horizontal forward (`Quaternion.LookRotation(restRiderViewForward, Vector3.up)`).
The rider feels horizontal translation only — no roll, no pitch — which is the
standard VR pendulum-comfort trick.

### 9.4 Swing comfort vignette

The script ships a dynamic comfort vignette that closes the ring only when the
rider is moving **backwards** (mapped to `swingVignetteFullSpeed = 0.55 m/s`,
dead zone `0.08 m/s`, smoothstep eased toward `swingVignetteAperture = 0.58`).
On the production scene's swing instance, however, the vignette is explicitly
disabled (`enableSwingComfortVignette = 0`) — the global comfort profile still
syncs via `syncSwingVignetteWithComfortProfile = 1` so the look stays
consistent if the design re-enables it later.

---

## 10. Passive UI Hints

| # | Element | Script | Trigger |
|---|---|---|---|
| 10.1 | Static `InteractionPrompt` instance | [`InteractionPrompt`](../Assets/_Project/UI/Scripts/InteractionPrompt.cs) | Player walks into the prompt's trigger collider |
| 10.2 | Floating gaze prompt | [`FloatingInteractionPrompt`](../Assets/_Project/UI/Scripts/FloatingInteractionPrompt.cs) | Player's head is within `triggerDistance = 20 m` and gaze alignment ≥ `gazeThreshold = 0.85` for ≥ `delayTime = 3 s` |

Neither consumes a controller button.

---

## 11. Controller Button Reverse Lookup

### 11.1 Right Quest controller

| Button | Consumers (production scene) | Notes |
|---|---|---|
| **Trigger** | §3 petal/pollen crystal · §4 lotus notes (right hand) · §5.1 mount entry · §6.1 mushroom seed · §6.2 mushroom cultivate · §7.1 firework mortar · §8.3 cherry orb · §9.2 wooden swing seat | All raycast-based and mutually exclusive — each tick the ray hits a single target. |
| **A (`primaryButton`)** | §5.1 mount dismount · §9.2 wooden swing dismount | Only consumed while riding (mount) or seated (swing). |
| **B (`secondaryButton`)** | §1.3 recenter (also re-routed mid-swing via `QuestSwingRideController.RecenterMountedView`) | The only consumer. |
| **Thumbstick push** | §1.1 L3 / L4 turn · §5.1 ride turn | Mutually exclusive: locomotion turn is disabled while mounted/seated; ride turn replaces it. |
| **Thumbstick click** | §1.2 scale shift | Disabled while riding. |
| **Menu** | — | ⚠ Reserved by the Oculus system shell. Do not bind. |

### 11.2 Left Quest controller

| Button | Consumers (production scene) | Notes |
|---|---|---|
| **Trigger** | §1.1 L1 teleport confirm (teleport mode) · §4 lotus notes (left hand) | ⚠ Possible overlap: in teleport mode the player could aim a teleport arc at a lotus pad. Mitigation: exclude the lotus layer from `QuestLocomotionComfortProfile.teleportRaycastMask`. |
| **X (`primaryButton`)** | §1.4 horse summon | Single consumer. |
| **Y (`secondaryButton`)** | — | Free. |
| **Thumbstick push** | §1.1 L1 teleport aim · L2 smooth move · §5.1 ride move · §9.3 swing pump | Mutually exclusive by locomotion mode, ride state, and swing-seated state. |
| **Thumbstick click** | — | Free. |
| **Menu** | §1.5 system menu | Bound by `VRSystemMenuController` (`useLeftHandMenuFallback = true`). |

### 11.3 Head / proximity / posture

| Input | Consumers |
|---|---|
| Head pose (gaze + distance) | §10.2 floating gaze prompt |
| Entering a trigger collider | §5.3 Kitty auto-route (only while mounted) · §5.5 guide butterflies · §5.4 animal voice · §10.1 static prompt |
| Hand pose | Not consumed in this scene. The `HandProxy` / `LandingTarget` butterfly-landing-on-palm system from `Assets/Scripts/Interaction/` is **not** wired into the production scene. |

---

## 12. Known Conflicts and Open Items

| # | Concern | Recommended action |
|---|---|---|
| 12.1 | **Left-trigger overlap** between teleport confirm and lotus pad activation. In teleport mode, aiming a teleport arc that grazes a lotus pad could double-fire. | Put `LotusNoteTrigger` colliders on a dedicated layer and exclude that layer from `QuestLocomotionComfortProfile.teleportRaycastMask`. |
| 12.2 | **Right-trigger raycast ordering.** Multiple features (firework mortar, lotus, cherry orb, mount, petal-pollen, mushroom seed, wooden swing) all listen for right trigger on right-hand raycast. They each do their own raycast with their own `LayerMask`. | Audit each feature's `LayerMask` so the colliders for each interaction live on a unique layer. All features currently use a non-default mask, but layer assignment on the scene objects should be spot-checked before submission. The swing additionally hard-restricts hits to the seat collider via `restrictRayToSeatOnly`. |
| 12.3 | **Right A** is shared between §5.1 mount dismount and §9.2 swing dismount, but mount and swing are mutually exclusive states (you can't be on a mount and the swing at the same time). The pause menu binding lives on **left Menu**, so there is no dismount-while-paused edge case. Keep it that way. | — |

---

## Appendix A — System inventory by region

| Region root | Interactable objects | Active scripts |
|---|---|---|
| `Region_FireworksClearing` | `FireworkMagicMortarDevice` (F1), supplementary `FireworkLaunchPad` (F2), `Waterfall_Stylized` | `FireworkMagicActivator`, `FireworkLaunchPad` ×2, `FireworkController`, `FireworkRandomParticlePlayer` |
| `Region_FlowerGarden` | `PetalPollenSource_Flower` (legacy name — visual is the **pink crystal**) with 5 sources + trigger, `PetalPollenMagicRig` | `PetalPollenMagicController`, `PetalPollenSource` ×5, `PetalPollenTrigger` |
| `Region_CatGarden` | `MountRig_Cat` / `_Dog` / `_Horse`, voice triggers, butterflies | `CatRideControllerV2` ×3, `CatIdlePaceV2` ×3, `HorseSummonV2`, `CatRideAutoTriggerV2`, `ButterflyFlightControllerV2` ×3, `ButterflyAutoTriggerV2` ×3, `AnimalVoiceProximityPlayer` ×3 |
| `Region_CherryGarden` | `HeroCherryTree_GrowthRig` (passive), runtime-spawned `CherryGarden_CrystalOrb` | `TreeGrowthController`, `FlowerVortexEffect`, `CherryGardenCrystalOrbTrigger` |
| `Region_LotusPond` | `LotusPad_A..G` (7), score starter (1), `LotusMusicUI` | `LotusNoteTrigger` ×8, `LotusEitherHandDriver`, `LotusSongManager`, `LotusSongUIController` |
| `Region_MushroomGrowth` | `GrowthSeedZone`, `GrowthPlant_01..11`, `growth_energy` | `GrowthSeedZoneDriver`, `GrowthController`, `GrowthDriver`, `GrowthPlant` ×11 |
| `Decorations / Swings` | `TFF_Wooden_Swing_01A` (and its `TFF_Wooden_Swing_Seat_01A` child as the mount target) | `QuestSwingRideController` |
| Global / system | `WonderlandXROrigin`, `ScaleShiftSystem`, `GlobalSystem`, `WW_UI_System`, `WelcomePanel` | `QuestLocomotionComfortProfile`, `QuestInteractableFeedback`, `QuestRayVisualBroker`, `QuestRayVisualLengthProfile`, `ScaleManager`, `ScaleTransitionController`, `RecenterController`, `VRSystemMenuController`, `WelcomeFlowController`, `UILanguageService` |

The production scene does **not** have a `Region_HumanEntry` root — the entry
experience is delivered through the `WelcomePanel` + `WW_UI_System` UI layer.

---

## Appendix B — File index

Core XR
- [`QuestLocomotionComfortProfile.cs`](../Assets/_Project/Core/XR/QuestLocomotionComfortProfile.cs)
- [`QuestInteractionUtils.cs`](../Assets/_Project/Core/XR/QuestInteractionUtils.cs)
- [`QuestInteractableFeedback.cs`](../Assets/_Project/Core/XR/QuestInteractableFeedback.cs)
- [`QuestRayVisualBroker.cs`](../Assets/_Project/Core/XR/QuestRayVisualBroker.cs)
- [`QuestRayVisualLengthProfile.cs`](../Assets/_Project/Core/XR/QuestRayVisualLengthProfile.cs)
- [`QuestHapticsInteractionProfile.cs`](../Assets/_Project/Core/XR/QuestHapticsInteractionProfile.cs)
- [`PCVRPerformanceBootstrap.cs`](../Assets/_Project/Core/XR/PCVRPerformanceBootstrap.cs)
- [`RecenterController.cs`](../Assets/_Project/Core/XR/RecenterController.cs)

Scale
- [`ScaleManager.cs`](../Assets/_Project/Features/ScaleShift/Runtime/ScaleManager.cs)
- [`ScaleTransitionController.cs`](../Assets/_Project/Features/ScaleShift/Runtime/ScaleTransitionController.cs)
- [`ScaleState.cs`](../Assets/_Project/Features/ScaleShift/Runtime/ScaleState.cs)

Mounts (v2)
- [`CatRideControllerV2.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/CatRideControllerV2.cs)
- [`CatRideAutoTriggerV2.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/CatRideAutoTriggerV2.cs)
- [`CatIdlePaceV2.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/CatIdlePaceV2.cs)
- [`HorseSummonV2.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/HorseSummonV2.cs)
- [`ButterflyFlightControllerV2.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/ButterflyFlightControllerV2.cs)
- [`ButterflyAutoTriggerV2.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/ButterflyAutoTriggerV2.cs)
- [`AnimalVoiceProximityPlayer.cs`](../Assets/_Project/Features/Mounts/Runtime/v2/AnimalVoiceProximityPlayer.cs)

Decorations — Wooden Swing
- [`QuestSwingRideController.cs`](../Assets/_Project/Features/Mounts/Runtime/QuestSwingRideController.cs)

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

Particle Vitality (Flower Garden crystal)
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
- [`VRSystemMenuController.cs`](../Assets/_Project/UI/Scripts/VRSystemMenuController.cs)
- [`WelcomeFlowController.cs`](../Assets/_Project/UI/Scripts/WelcomeFlowController.cs)
- [`UILanguageService.cs`](../Assets/_Project/UI/Scripts/UILanguageService.cs)
- [`InteractionPrompt.cs`](../Assets/_Project/UI/Scripts/InteractionPrompt.cs)
- [`FloatingInteractionPrompt.cs`](../Assets/_Project/UI/Scripts/FloatingInteractionPrompt.cs)

---
