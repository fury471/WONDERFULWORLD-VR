# Project Overview

## Project Identity

Butterfly House / Wonderful World is a first-person PC VR fantasy park experience built in Unity 6 with OpenXR and URP. The player explores a handcrafted wonderland made of connected attraction zones, creature-scale moments, magical interaction systems, ambient ecology, stylized vegetation, music, fireworks, and comfort-first VR movement.

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

## Feature Inventory

- XR rig and comfort locomotion: `Assets/_Project/Core/XR`.
- Scale shift: `Assets/_Project/Features/ScaleShift`.
- Weather presets and regional response: `Assets/_Project/Features/Weather`.
- Growth and mushroom planting: `Assets/_Project/Features/Growth`.
- Petal and pollen magic: `Assets/_Project/Features/ParticleVitality`.
- Lotus pond music interaction: `Assets/_Project/Features/LotusPond`.
- Cat ride and mount systems: `Assets/_Project/Features/Mounts`.
- Fireworks interaction and finale content: `Assets/_Project/Features/Fireworks`.
- Cherry garden interaction and art: `Assets/_Project/Features/CherryGarden`.
- World-space UI, notice boards, localization, and VR system menu: `Assets/_Project/UI`.
- Shared world terrain, vegetation, lighting, and region content: `Assets/_Project/World`.

## Closeout Goals

The closeout phase has two equal priorities:

1. Standardize the project so the scene, assets, naming, and documentation are maintainable.
2. Optimize the Quest 3 Link PC VR experience without flattening the existing art direction.

Optimization decisions should be based on profiler data, headset metrics, and frame-debugger evidence. Prefer LOD, culling, batching, instancing, shader cleanup, baked lighting, and targeted effect budgets over broad visual downgrades.
