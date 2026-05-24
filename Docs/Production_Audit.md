# Production Audit

- Project: Butterfly House / Wonderful World
- Main scene: `Assets/_Project/World/Persistent/World_WonderlandPark.unity`
- Unity version: `6000.3.11f1`
- Target runtime: PC VR through Quest 3 Link, OpenXR, URP

## Scene Hierarchy

- Root objects: 8

| Root | Active | Direct children | Total objects | Missing scripts |
| --- | --- | ---: | ---: | ---: |
| `GlobalSystem` | True | 3 | 4 | 0 |
| `XR` | True | 1 | 65 | 0 |
| `Lighting` | True | 1 | 2 | 0 |
| `Terrain` | True | 10 | 11 | 0 |
| `World_Regions` | True | 6 | 792 | 0 |
| `Decorations` | True | 11 | 1034 | 0 |
| `UI` | True | 8 | 265 | 0 |
| `Debug` | False | 1 | 2 | 0 |

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
| `Assets/_Project` | 918 | Production-owned |
| `Assets/Toon Fantasy Nature` | 552 | Third-party, package, or Unity template support |
| `Assets/Samples` | 314 | Third-party, package, or Unity template support |
| `Assets/VRTemplateAssets` | 208 | Third-party, package, or Unity template support |
| `Assets/ithappy` | 63 | Third-party, package, or Unity template support |
| `Assets/NamuFX` | 57 | Third-party, package, or Unity template support |
| `Assets/Scripts` | 34 | Legacy/template candidate; should not be referenced by production |
| `Assets/TextMesh Pro` | 33 | Third-party, package, or Unity template support |
| `Assets/_TempArt` | 30 | Cleanup candidate; verify references before deleting |
| `Assets/Scenes` | 10 | Legacy/template candidate; should not be referenced by production |
| `Assets/XR` | 10 | Third-party, package, or Unity template support |
| `Assets/Settings` | 7 | Third-party, package, or Unity template support |
| `Assets/_Recovery` | 6 | Cleanup candidate; verify references before deleting |
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

## Main Scene Dependencies

- Asset dependencies discovered by `AssetDatabase.GetDependencies`: 728

| Top-level folder | Referenced assets | Classification |
| --- | ---: | --- |
| `Assets/_Project` | 329 | Production-owned |
| `Assets/Toon Fantasy Nature` | 267 | Third-party, package, or Unity template support |
| `Assets/Samples` | 89 | Third-party, package, or Unity template support |
| `Assets/NamuFX` | 26 | Third-party, package, or Unity template support |
| `Assets/ithappy` | 10 | Third-party, package, or Unity template support |
| `Assets/TextMesh Pro` | 4 | Third-party, package, or Unity template support |
| `Assets/butterfly` | 3 | Review and either move through AssetDatabase or document as vendor content |

Referenced assets outside `Assets/_Project`:

- `Assets/butterfly/AC_Butterfly.controller`
- `Assets/butterfly/scene.gltf`
- `Assets/butterfly/textures/UlyssesButterfly_mat_baseColor.png`
- `Assets/ithappy/Animals_FREE/Animations/Animation_Controllers/Dog.controller`
- `Assets/ithappy/Animals_FREE/Animations/Animation_Controllers/Horse.controller`
- `Assets/ithappy/Animals_FREE/Animations/Animation_Controllers/Kitty.controller`
- `Assets/ithappy/Animals_FREE/Materials/Material.mat`
- `Assets/ithappy/Animals_FREE/Meshes/Dog_001.fbx`
- `Assets/ithappy/Animals_FREE/Meshes/Horse_001.fbx`
- `Assets/ithappy/Animals_FREE/Meshes/Kitty_001.fbx`
- `Assets/ithappy/Animals_FREE/Scripts/CreatureMover.cs`
- `Assets/ithappy/Animals_FREE/Scripts/MovePlayerInput.cs`
- `Assets/ithappy/Animals_FREE/Textures/Texture.png`
- `Assets/NamuFX/_SharedAssets/Images/Common/Gradient01.tga`
- `Assets/NamuFX/_SharedAssets/Images/Noise/Noise06.tga`
- `Assets/NamuFX/_SharedAssets/Images/Noise/noise11.png`
- `Assets/NamuFX/_SharedAssets/Models/FX_Models/FBX_ring.fbx`
- `Assets/NamuFX/_SharedAssets/Models/FX_Models/RoundPlane.fbx`
- `Assets/NamuFX/_SharedAssets/Shader/MasterShader/Dissolve Function.shadersubgraph`
- `Assets/NamuFX/_SharedAssets/Shader/MasterShader/FlipUV.shadersubgraph`
- `Assets/NamuFX/_SharedAssets/Shader/MasterShader/SH_Master_improved.shadergraph`
- `Assets/NamuFX/StylizedWaterEffects/Materials/M_Particles_Alp.mat`
- `Assets/NamuFX/StylizedWaterEffects/Materials/M_Particles_Alp_Depthalways.mat`
- `Assets/NamuFX/StylizedWaterEffects/Materials/M_Particles_Alp_Distortion.mat`
- `Assets/NamuFX/StylizedWaterEffects/Materials/M_Water_03.mat`
- `Assets/NamuFX/StylizedWaterEffects/Materials/M_Water_05.mat`
- `Assets/NamuFX/StylizedWaterEffects/Materials/M_WaterTrail.mat`
- `Assets/NamuFX/StylizedWaterEffects/Prefabs/Bubble_Explosion.prefab`
- `Assets/NamuFX/StylizedWaterEffects/Prefabs/Bubbles_Vertical_Loop.prefab`
- `Assets/NamuFX/StylizedWaterEffects/Prefabs/Water_Impact.prefab`
- `Assets/NamuFX/StylizedWaterEffects/Shaders/SH_Particles.shadergraph`
- `Assets/NamuFX/StylizedWaterEffects/Shaders/SH_Water_Base.shadergraph`
- `Assets/NamuFX/StylizedWaterEffects/Textures/Mask_Distortion.tga`
- `Assets/NamuFX/StylizedWaterEffects/Textures/PSD/Tex_Particles_01_PSD.psd`
- `Assets/NamuFX/StylizedWaterEffects/Textures/PSD/Tex_Water_02_PSD.psd`
- `Assets/NamuFX/StylizedWaterEffects/Textures/Tex_Particles_01.tga`
- `Assets/NamuFX/StylizedWaterEffects/Textures/Tex_Water_01.tga`
- `Assets/NamuFX/StylizedWaterEffects/Textures/Tex_Water_03.tga`
- `Assets/NamuFX/StylizedWaterEffects/Textures/Tex_WaterTrail_01.tga`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/AffordanceThemes/PokeSphereColor.asset`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Animations/ArrowBounce.anim`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Animations/Climb Teleport Arrow.controller`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/DemoSceneAssets/Materials/Lit White.mat`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/DemoSceneAssets/Sprites/Forward.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/DemoSceneAssets/Sprites/LegibilityMask.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/DemoSceneAssets/Textures/Concrete_Normal.tif`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Filters/AnyGazedAtTeleportAnchorFilter.asset`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Materials/Controller_Grey.mat`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Materials/Controller_White.mat`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Materials/Flat Blue.mat`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Materials/FresnelHighlight.mat`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Materials/Interactable.mat`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Materials/Telport Anchor.mat`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Materials/UI-NoZTest.mat`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Models/BlinkVisual.fbx`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Models/Pinch_Pointer_LOD0.fbx`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Models/Reticle_Torus.fbx`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Models/UniversalController.fbx`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Prefabs/Affordances/PokePointerAffordance.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Prefabs/Controllers/XR Controller Left.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Prefabs/Controllers/XR Controller Right.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Prefabs/Interactors/Gaze Interactor.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Prefabs/Interactors/Left_NearFarInteractor.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Prefabs/Interactors/Poke Interactor.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Prefabs/Interactors/Right_NearFarInteractor.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Prefabs/Interactors/Teleport Interactor.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Prefabs/Teleport/Blocking Teleport Reticle.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Prefabs/Teleport/Climb Teleport Arrow.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Prefabs/Teleport/Directional Teleport Reticle.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Scripts/ClimbTeleportDestinationIndicator.cs`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Scripts/ControllerAnimator.cs`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Scripts/ControllerInputActionManager.cs`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Scripts/DynamicMoveProvider.cs`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Scripts/GazeInputManager.cs`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Shaders/Interactable.shadergraph`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Shaders/UI-NoZTest.shader`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Shaders/Unlit_Fresnel.shadergraph`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Textures/DefaultMaterial_AO.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/TunnelingVignette/TunnelingVignette.mat`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/TunnelingVignette/TunnelingVignette.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/TunnelingVignette/TunnelingVignette.shader`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/TunnelingVignette/TunnelingVignetteHemisphere.fbx`
- `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/XRI Default Input Actions.inputactions`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/Hand Expression Captures/Fist Expression Capture.asset`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/Hand Expression Captures/Grab Expression Capture.asset`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/Hand Expression Captures/Open Expression Capture.asset`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/Hand Expression Captures/Pinch Expression Capture.asset`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/Hand Expression Captures/Poke Expression Capture.asset`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/Hand Expression Captures/Resting Expression Capture.asset`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/Hand Expression Captures/Thumb Expression Capture.asset`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/Scripts/XRDeviceSimulatorControllerUI.cs`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/Scripts/XRDeviceSimulatorHandsUI.cs`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/Scripts/XRDeviceSimulatorUI.cs`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Controller/ControllerLeft.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Controller/ControllerOverlayLinesLeft.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Controller/ControllerOverlayLinesRight.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Controller/ControllerRight.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Controller/xr_ctlr.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/General/btn_bgbottom.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/General/CloseWindow.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/General/CycleXRDevices.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/General/DeviceSimUI_bg.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/General/Gripper.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/General/KeyboardIcon.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/General/Look.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/General/OpenWindow.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Hands/hand.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Hands/Hand_Default.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Hands/Hand_Fist.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Hands/Hand_Grab.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Hands/Hand_Open.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Hands/Hand_Pinch.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Hands/Hand_Poke.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Hands/Hand_Thumb.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Head/HMD.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Head/HMD_d.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Head/Movement.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Head/MoveRotateTool.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Head/xr_hmd.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Mouse/Cursor.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Mouse/Mouse.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Mouse/MouseR.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/Mouse/MouseR_d.png`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/UI/XR Device Simulator UI.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/XR Device Controller Controls.inputactions`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/XR Device Hand Controls.inputactions`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/XR Device Simulator Controls.inputactions`
- `Assets/Samples/XR Interaction Toolkit/3.3.1/XR Device Simulator/XR Device Simulator.prefab`
- `Assets/TextMesh Pro/Fonts/LiberationSans.ttf`
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset`
- `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader`
- `Assets/Toon Fantasy Nature/Animations/TFF_Fire_01.controller`
- `Assets/Toon Fantasy Nature/Animations/TFF_Fire_Idle_01.anim`
- `Assets/Toon Fantasy Nature/Animations/TFF_Wooden_Swing_01A.controller`
- `Assets/Toon Fantasy Nature/Animations/TFF_Wooden_Swing_01A_Idle.anim`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Birch_Tree_01A_MeshCollider.fbx`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Birch_Tree_02A_MeshCollider.fbx`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Rock_Large_01A_MeshCollider.fbx`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Rock_Large_02A_MeshCollider.fbx`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Rock_Large_03A_MeshCollider.fbx`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Rock_Large_05A_MeshCollider.fbx`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Rock_Large_06A_MeshCollider.fbx`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Rock_Medium_01A_MeshCollider.fbx`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Rock_Medium_02A_MeshCollider.fbx`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Tree_Broken_01A_MeshCollider.fbx`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Tree_Fallen_01A_MeshCollider.fbx`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Tree_Log_03A_MeshCollider.fbx`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Wooden_Pavilion_01A_MeshCollider.fbx`
- `Assets/Toon Fantasy Nature/Models/Colliders/TFF_Wooden_Swing_01A_MeshCollider.fbx`
- ...and 249 more.

Unresolved scene GUIDs:

- None.

## Production Debug Flags

- No enabled production debug flags found in non-sandbox scenes or prefabs.

## Validation Checklist

Run this checklist after each cleanup or optimization batch:

1. Open `World_WonderlandPark.unity` in Unity with no missing-script warnings.
2. Enter Play Mode and confirm XR Origin, teleport, snap turn, recenter, system menu, notice boards, audio, and onboarding still work.
3. Walk each region: Human Entry, Flower Garden, Lotus Pond, Cat Garden, Fireworks Clearing, Mushroom Growth, Cherry Garden.
4. Use Unity Profiler and Frame Debugger in a headset-linked Play Mode session.
5. Use OVR Metrics Tool or OpenXR Toolkit to confirm stable 72/90 Hz frame pacing through Quest 3 Link.
6. Specifically inspect skybox, camera clear flags, near/far clipping, transparent effects, render textures, post volumes, and custom shaders if black blocks or flicker are visible.

