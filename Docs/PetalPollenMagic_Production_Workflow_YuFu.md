# Petals & Pollen Magic Production Workflow

- Owner: Yu Fu
- Feature Area: `Assets/_Project/Features/ParticleVitality`
- Goal: turn flower interaction into a shocking, artistic, VR-safe magical performance
- Status: production workflow guide, replacing small prototype thinking

## 1. Feature Vision

The petals and pollen system should not feel like a utility particle collector.

It should feel like the player is conducting living flower energy:

1. The player approaches flowers.
2. Pollen and petals wake up before being collected.
3. Flower energy flows along visible magical routes into the player's hand.
4. The particles condense into a living sphere.
5. On release, the sphere compresses, flashes, and becomes a surprise magical show.
6. Rarely, the show becomes a small galaxy wrapping around the player.

The target emotion is:

- wonder
- shock
- softness
- player agency
- "I caused this magical world to respond to me"

The Chinese emotional target:

> 花粉和花瓣不是被吸走，而是被玩家唤醒，然后像星河一样围绕玩家展开。

## 2. Mental Model

Think of this feature as four layers, not one script.

### Layer A: Interaction

This answers:

- What starts the magic?
- Which hand is used?
- How close must the player be to flowers?
- How long has the player charged the spell?
- What happens on release?

This layer should stay simple. It should not contain all visual math.

### Layer B: Choreography

This is the artistic brain of the feature.

It controls phases:

- Wake
- Flow
- Hold
- Implode
- Bloom
- Final Show
- Dissolve

Most of the "art feeling" comes from this layer.

### Layer C: Rendering

This answers:

- How are particles drawn?
- Are pollen and petals separate materials?
- Are particles billboards, mesh petals, or VFX Graph particles?
- How many can appear on screen?
- How do they fade, shimmer, and rotate?

For the first production pass, use controlled `ParticleSystem.SetParticles`.
Later, upgrade heavy visual detail to VFX Graph only if needed.

### Layer D: Authoring Data

This is how Yu makes the effect beautiful without rewriting code every time.

Examples:

- release mode weights
- galaxy radius
- petal rain height
- colors
- particle count
- flow speed
- curve style
- charged-release thresholds

These values should eventually live in ScriptableObjects.

## 3. Production Architecture

Recommended final structure:

```text
Assets/_Project/Features/ParticleVitality/
  Runtime/
    PetalPollenMagicController.cs
    PetalPollenSource.cs
    PetalPollenReleaseMode.cs
    PetalPollenParticleRenderer.cs
    PetalPollenChoreographer.cs
    PetalPollenProfile_SO.cs
  Prefabs/
    PetalPollenMagicRig.prefab
    PetalPollenSource_Flower.prefab
  ScriptableObjects/
    PetalPollenProfile_Default.asset
    PetalPollenProfile_Showcase.asset
  Materials/
    M_PollenGlow.mat
    M_PetalSoft.mat
  Textures/
    T_Petal_01.png
    T_Pollen_Glow.png
```

The current implementation may start with fewer files, but the mental ownership should follow this split.

## 4. Core Animation Timeline

Every release should use the same dramatic structure.

```text
0.00 - 0.15s   release input detected
0.15 - 0.38s   living sphere implodes into a bright seed
0.38 - 0.52s   silence / flash / haptic pulse
0.52 - 1.20s   bloom expansion starts
1.20 - 5.50s   random final show plays
5.50 - 7.00s   particles dissolve, fall, or drift away
```

The key production rule:

**Do not start with explosion. Start with compression.**

Compression creates anticipation. The bloom creates surprise.

## 5. Final Show Modes

Use random final show modes, but keep all of them beautiful.

Recommended modes:

```csharp
public enum PetalPollenReleaseMode
{
    PetalRain,
    SpiralBloom,
    FlowerConstellation,
    GalaxyVeil
}
```

### PetalRain

Feeling:

- soft
- romantic
- peaceful

Animation:

- particles burst upward
- petals hang briefly
- pollen sparkles
- petals fall slowly around the player

### SpiralBloom

Feeling:

- energetic
- magical
- performance-like

Animation:

- particles expand as a double helix
- pollen makes golden light trails
- petals rotate more slowly around the helix

### FlowerConstellation

Feeling:

- artistic
- readable
- mathematical

Animation:

- particles bloom outward
- briefly lock into a flower curve
- tremble like stars
- dissolve downward

### GalaxyVeil

Feeling:

- rare
- shocking
- unforgettable

Animation:

- the sphere collapses into one star
- two spiral arms open around the player
- pollen becomes stars
- petals become nebula fragments
- a tilted galaxy surrounds the player's body
- the galaxy slowly loses shape and becomes golden dust

Chinese description:

> 散落并形成一个小型银河，把玩家包裹在中间。

## 6. Randomness Design

Randomness should create surprise, not mess.

Suggested base weights:

```text
PetalRain: 30%
SpiralBloom: 25%
FlowerConstellation: 20%
GalaxyVeil: 25%
```

If the player holds the spell for more than 3 seconds:

```text
increase GalaxyVeil chance
increase particle count
increase bloom radius
increase audio intensity
```

This teaches the player:

> charging the spell makes the world respond more dramatically.

## 7. Unity Scene Workflow

### Step 1: Create Flower Sources

For each important flower cluster:

1. Create an empty GameObject named `PetalPollenSource_Flower`.
2. Put it near the flower center.
3. Add `PetalPollenSource`.
4. Set pollen color and petal color.
5. Adjust spawn radius so particles emerge from the flower volume.

Do not attach logic to every tiny flower. Use one source per flower cluster.

### Step 2: Create Magic Rig

Create an empty GameObject near the player rig:

```text
PetalPollenMagicRig
```

Add:

- `PetalPollenMagicController`
- a child ParticleSystem named `_PetalPollenMagicParticles`

Assign:

- hand anchor
- player head
- collect input action
- flower sources

### Step 3: Test With Keyboard First

Before testing VR input, enable keyboard fallback and use Space.

Testing order:

1. press and hold Space
2. confirm particles flow from flowers to hand
3. confirm living sphere forms
4. release Space
5. confirm final show appears

Only after this works should you bind the VR trigger.

### Step 4: Tune One Phase At A Time

Never tune everything at once.

Recommended tuning order:

1. Flow route
2. Living sphere
3. Implosion timing
4. Bloom expansion
5. Individual final modes
6. Random weights
7. Color/material polish
8. Audio and haptics
9. Performance

This is production thinking: isolate one variable at a time.

## 8. Artistic Tuning Targets

### Flow Route

Good signs:

- particles curve visibly
- route feels intentional
- flow has height and swirl
- flower-to-hand movement is readable

Bad signs:

- particles move in straight lines
- particles appear from nowhere
- particles move too fast to appreciate

### Living Sphere

Good signs:

- sphere breathes
- particles orbit
- petals sit slightly outside pollen
- nothing is completely static

Bad signs:

- sphere looks like a frozen ball
- jitter is too noisy
- sphere blocks too much of the player's view

### GalaxyVeil

Good signs:

- most particles orbit around the player, not directly in the eyes
- ring has tilt
- there are two visible spiral arms
- the player feels surrounded but not blinded

Bad signs:

- galaxy is too close to the headset
- particles fill the center of vision
- radius is too large and becomes unreadable

## 9. VR Comfort Rules

This effect is beautiful only if it stays comfortable.

Rules:

- keep dense particles outside the center of the player's view
- avoid full-screen white flashes
- keep the galaxy radius around 1.4m to 2.2m
- keep release duration under about 7 seconds
- use soft fade, not hard disappearance
- do not shake the camera
- use haptics lightly, not continuously

## 10. Production Skill Checklist

When Yu works on this feature, practice these skills:

- define the emotional target before coding
- split interaction, choreography, rendering, and data
- make one debug path first
- tune one animation phase at a time
- use scene gizmos to understand source positions
- compare effect at human scale and small scale
- test in headset before final values are locked
- profile particle count before adding more detail
- write down good settings once found

## 11. Next Implementation Milestones

### Milestone 1: Playable Choreography

- flowers emit particles
- hand collects them
- living sphere forms
- release plays random show
- GalaxyVeil exists

### Milestone 2: Authoring Quality

- move tuning values into `PetalPollenProfile_SO`
- create showcase profile
- make flower source prefab
- make magic rig prefab

### Milestone 3: Visual Upgrade

- separate pollen and petal renderers
- custom pollen glow material
- soft petal sprite material
- optional trail layer for light-flow feeling

### Milestone 4: Integration

- hook into real VR trigger
- place sources in FlowerField
- add audio
- add haptic pulse
- test in headset

### Milestone 5: Showcase Polish

- tune random weights
- tune charged release
- tune GalaxyVeil for shock without discomfort
- record video and compare against vision

## 12. Feature Owner Mindset

Yu owns not just code, but the player's memory of the moment.

The goal is not:

> I implemented particle collection.

The goal is:

> The player touched flowers, and the world opened into a private galaxy.

