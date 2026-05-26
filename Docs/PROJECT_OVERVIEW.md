# Project Overview

## Project Identity

Wonderland (internal name also: *Wonderful World*) is a first-person PC VR fantasy park experience built in Unity 6 with OpenXR and URP, developed for the **MAMF45 — Virtual Reality in Theory and Practice** course. The player explores a handcrafted park slice made of connected attraction zones, creature-scale moments, magical interaction systems, ambient ecology, stylised vegetation, music, fireworks, and comfort-first VR movement.

The production build is a bounded but seamless-feeling park slice. It should feel open, alive, and explorable without depending on an endless terrain or a mission chain.

## Target Runtime

- Platform: Windows PC VR.
- Headset path: Meta Quest 3 through Link Cable.
- XR stack: OpenXR through Unity XR Management.
- Render pipeline: Universal Render Pipeline.
- Stereo rendering: Single Pass Instanced.
- Target frame pacing: stable 72 Hz minimum, 90 Hz target, higher when the headset and PC allow it.

## Current Production Scene

The only enabled build scene is:

```text
Assets/_Project/World/Persistent/World_WonderlandPark.unity
```

The scene currently contains the production park, XR rig, world regions, UI system, lighting, terrain, and attraction content.

## Region Roots

The production scene contains six `Region_*` roots under `World_Regions/`:

| Region root | Player-facing label | Notes |
| --- | --- | --- |
| `Region_FlowerGarden` | Magical Particle Garden | A pink-magenta crystal (`PetalPollenTrigger.useCrystalStoneVisual = true`) is the interactable, not a flower. |
| `Region_LotusPond` | Lotus Pond | Seven-note diatonic music sequencer + one score starter. |
| `Region_CatGarden` | Animal Forest | Three mounts: cat (`SmallOnly`), dog (`SmallOnly`), horse (`NormalOnly`). |
| `Region_FireworksClearing` | Waterfall & Fireworks Ground | Hosts the stylised waterfall and the magic mortar. |
| `Region_MushroomGrowth` | Mushroom Growth | Mushroom seeding zone and cultivation. |
| `Region_CherryGarden` | Cherry Garden | Crystal orb activates tree growth + petal vortex. |

The entry experience is **not** a region root — it lives in the `UI/WelcomePanel`
flow driven by [`WelcomeFlowController`](../Assets/_Project/UI/Scripts/WelcomeFlowController.cs).

## Feature Inventory

- XR rig and comfort locomotion: `Assets/_Project/Core/XR`.
- Scale shift (Normal / Small 0.25× / Large 1.75×): `Assets/_Project/Features/ScaleShift`.
- Weather presets and regional response: `Assets/_Project/Features/Weather`.
- Growth and mushroom planting: `Assets/_Project/Features/Growth`.
- Particle vitality (crystal-driven petal/pollen magic): `Assets/_Project/Features/ParticleVitality`.
- Lotus pond music sequencer: `Assets/_Project/Features/LotusPond`.
- Mount system (cat / dog / horse) + guide butterflies: `Assets/_Project/Features/Mounts`.
- Fireworks interaction and showcase: `Assets/_Project/Features/Fireworks`.
- Cherry garden crystal orb and tree growth: `Assets/_Project/Features/CherryGarden`.
- World-space UI, welcome panel, notice boards, localisation (EN/ZH/SV), VR system menu: `Assets/_Project/UI`.
- Shared world terrain, vegetation, lighting, and region content: `Assets/_Project/World`.

## Closeout Goals

The closeout phase has two equal priorities:

1. Standardize the project so the scene, assets, naming, and documentation are maintainable.
2. Optimize the Quest 3 Link PC VR experience without flattening the existing art direction.

Optimization decisions should be based on profiler data, headset metrics, and frame-debugger evidence. Prefer LOD, culling, batching, instancing, shader cleanup, baked lighting, and targeted effect budgets over broad visual downgrades.
