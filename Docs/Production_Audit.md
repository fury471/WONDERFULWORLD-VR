# Production Audit

- Project: Butterfly House / Wonderful World
- Main scene: `Assets/_Project/World/Persistent/World_WonderlandPark.unity`
- Unity version: `6000.3.11f1`
- Target runtime: PC VR through Quest 3 Link, OpenXR, URP

## Scene Hierarchy

- Root objects: 10

| Root | Active | Direct children | Total objects | Missing scripts |
| --- | --- | ---: | ---: | ---: |
| `GlobalSystem` | True | 3 | 4 | 0 |
| `XR` | True | 1 | 65 | 0 |
| `Lighting` | True | 1 | 2 | 0 |
| `Terrain` | True | 10 | 11 | 0 |
| `World_Regions` | True | 6 | 792 | 0 |
| `Decorations` | True | 9 | 1028 | 0 |
| `UI` | True | 8 | 265 | 0 |
| `Debug` | False | 1 | 2 | 0 |
| `TFF_Fire_01A` | True | 4 | 5 | 0 |
| `TFF_Rock_Large_01A` | True | 0 | 1 | 0 |

Recommended root grouping:

- `GlobalSystem`: event system, global managers, language/settings services, runtime profiles.
- `XR`: the production XR Origin and controller rig only.
- `Lighting`: directional light, sky, probes, volumes, and time-of-day atmosphere.
- `Terrain`: terrain tiles, terrain data instances, and terrain-only colliders.
- `World_Regions`: Human Entry, Flower Garden, Lotus Pond, Cat Garden, Fireworks Clearing, Mushroom Growth, Cherry Garden.
- `Decorations`: world art that is not owned by a specific region.
- `UI`: world-space UI, welcome boards, notice board overlay, and system menu.
- `Debug`: temporary disabled helpers only; delete it when empty.

## Asset Organization

Production-owned assets should live under `Assets/_Project`. Third-party packages may stay under their vendor folders. Temporary, recovery, and sandbox content must not be referenced by the production scene.

| Top-level folder | Asset count | Classification |
| --- | ---: | --- |
| `Assets/_Project` | 888 | Production-owned |
| `Assets/Toon Fantasy Nature` | 552 | Third-party, package, or Unity template support |
| `Assets/Samples` | 314 | Third-party, package, or Unity template support |
| `Assets/VRTemplateAssets` | 208 | Third-party, package, or Unity template support |
| `Assets/ithappy` | 63 | Third-party, package, or Unity template support |
| `Assets/_TempArt` | 59 | Cleanup candidate; verify references before deleting |
| `Assets/NamuFX` | 57 | Third-party, package, or Unity template support |
| `Assets/Scripts` | 34 | Legacy/template candidate; should not be referenced by production |
| `Assets/TextMesh Pro` | 33 | Third-party, package, or Unity template support |
| `Assets/Scenes` | 10 | Legacy/template candidate; should not be referenced by production |
| `Assets/XR` | 10 | Third-party, package, or Unity template support |
| `Assets/Settings` | 7 | Third-party, package, or Unity template support |
| `Assets/_Recovery` | 5 | Cleanup candidate; verify references before deleting |
| `Assets/BlobShadows` | 4 | Review and either move through AssetDatabase or document as vendor content |
| `Assets/butterfly` | 4 | Review and either move through AssetDatabase or document as vendor content |
| `Assets/Editor` | 4 | Review and either move through AssetDatabase or document as vendor content |
| `Assets/URPDefaultResources` | 4 | Third-party, package, or Unity template support |
| `Assets/LayeredGrass` | 3 | Review and either move through AssetDatabase or document as vendor content |
| `Assets/XRI` | 3 | Third-party, package, or Unity template support |
| `Assets/Drawing` | 2 | Review and either move through AssetDatabase or document as vendor content |
| `Assets/TutorialInfo` | 2 | Review and either move through AssetDatabase or document as vendor content |
| `Assets/DefaultVolumeProfile.asset` | 1 | Review and either move through AssetDatabase or document as vendor content |
| `Assets/GeometryGrass` | 1 | Review and either move through AssetDatabase or document as vendor content |

Naming conventions:

- Textures: `T_Description`.
- Materials: `M_Description`.
- Static meshes and model assets: `SM_Description` when project-authored.
- Prefabs: `P_Description` for generic prefabs, or feature prefixes such as `WW_`, `Lotus`, `Growth`, and `CatRide` where already established.
- Audio: `SFX_Description`, `AMB_Description`, or `MUS_Description`.
- ScriptableObjects: `FeatureName_SO` or a descriptive feature-local asset name.

## Closeout Progress

Completed in the current closeout pass:

- Documentation has been consolidated into English-only production docs under `Docs`.
- Main scene hierarchy tooling is available under `Wonderful World > Production`.
- Runtime XR camera, controller ray, and haptics lookup paths are centralized through `QuestInteractionUtils` with scene-aware caching and retry throttling.
- High-frequency interaction scripts no longer call `Camera.main`, `GameObject.Find`, or tag-based camera lookup directly.
- Growth seed targeting uses non-alloc raycasts for the known high-frequency hover and placement checks.
- XR ray profile scans, recenter reference recovery, haptic hover pulses, PCVR refresh-rate requests, and lotus score refreshes have been adjusted to reduce periodic CPU and GC spikes.
- PC VR frame pacing guardrails are applied through `PCVRPerformanceBootstrap` before scene load.
- Scale shift character controller step offset is clamped before resizing and before re-enabling the controller.

Still requires Unity Editor and headset validation:

- Run `Wonderful World > Production > Normalize Main Scene Hierarchy` in Unity, inspect the result, then save the scene only after references are confirmed.
- Move or rename scattered assets only through the Unity Project window or `AssetDatabase` tooling.
- Open the main scene, enter Play Mode, and verify console errors, prefab bindings, UI interactions, audio, animation, and region triggers.
- Test through Quest 3 Link with headset metrics before making any further visual tradeoffs.

## Validation Checklist

Run this checklist after each cleanup or optimization batch:

1. Open `World_WonderlandPark.unity` in Unity with no missing-script warnings.
2. Enter Play Mode and confirm XR Origin, teleport, snap turn, recenter, system menu, notice boards, audio, and onboarding still work.
3. Walk each region: Human Entry, Flower Garden, Lotus Pond, Cat Garden, Fireworks Clearing, Mushroom Growth, Cherry Garden.
4. Use Unity Profiler and Frame Debugger in a headset-linked Play Mode session.
5. Use OVR Metrics Tool or OpenXR Toolkit to confirm stable 72/90 Hz frame pacing through Quest 3 Link.
6. Specifically inspect skybox, camera clear flags, near/far clipping, transparent effects, render textures, post volumes, and custom shaders if black blocks or flicker are visible.

