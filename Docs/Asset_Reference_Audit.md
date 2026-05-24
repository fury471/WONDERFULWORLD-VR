# Asset Reference Audit

- Main scene: `Assets/_Project/World/Persistent/World_WonderlandPark.unity`
- Rule: move or rename Unity assets only through Unity/AssetDatabase so GUID references remain intact.

## Main Scene Dependencies

- Asset dependencies discovered by `AssetDatabase.GetDependencies`: 728

| Top-level folder | Referenced assets | Classification |
| --- | ---: | --- |
| `Assets/_Project` | 304 | Production-owned |
| `Assets/Toon Fantasy Nature` | 267 | Third-party, package, or Unity template support |
| `Assets/Samples` | 89 | Third-party, package, or Unity template support |
| `Assets/NamuFX` | 26 | Third-party, package, or Unity template support |
| `Assets/_TempArt` | 25 | Cleanup candidate; verify references before deleting |
| `Assets/ithappy` | 10 | Third-party, package, or Unity template support |
| `Assets/TextMesh Pro` | 4 | Third-party, package, or Unity template support |
| `Assets/butterfly` | 3 | Review and either move through AssetDatabase or document as vendor content |

Referenced assets outside `Assets/_Project`:

- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/scene.gltf`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Dragon_HA_baseColor.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Dragon_HA_emissive.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Dragon_HA_normal.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Dragon_Tail_baseColor.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Dragon_Tail_normal.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/JP_Lantern_Stone_baseColor.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/JP_Lantern_Stone_normal.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/khan_baseColor.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/khan_normal.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/material_baseColor.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Oni_Horns__Teeths_baseColor.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Oni_Horns__Teeths_normal.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Oni_Mask_baseColor.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Oni_Mask_normal.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/quan_ao_baseColor.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/quan_ao_normal.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/skin_baseColor.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/skin_normal.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Stone_baseColor.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Stone_normal.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Umbrella_baseColor.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Umbrella_normal.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Wood_baseColor.png`
- `Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/textures/Wood_normal.png`
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
- ...and 274 more.

Unresolved scene GUIDs:

- None.

## Cleanup Candidates

| Folder | Assets | Referenced by main scene | Recommendation |
| --- | ---: | ---: | --- |
| `Assets/_TempArt` | 59 | 25 | Keep referenced assets; move confirmed production assets through AssetDatabase only. |
| `Assets/_Recovery` | 5 | 0 | Candidate for removal after team confirmation. |
| `Assets/Scenes` | 10 | 0 | Candidate for removal after team confirmation. |
| `Assets/Scripts` | 34 | 0 | Candidate for removal after team confirmation. |

## Naming Audit

- Warnings: 677

- `Assets/_Project/Art/Materials/Animals/MAT_Animals_Toon.mat` should usually start with `M_`.
- `Assets/_Project/Art/Materials/Animals/MAT_Animals_Toon_Outline 1.mat` should usually start with `M_`.
- `Assets/_Project/Art/Materials/Animals/MAT_Animals_Toon_Outline.mat` should usually start with `M_`.
- `Assets/_Project/Art/Materials/Mat_PondWater.mat` should usually start with `M_`.
- `Assets/_Project/Art/Materials/Mat_WaterDroplet.mat` should usually start with `M_`.
- `Assets/_Project/Art/Materials/Shader Graphs_SG_LotusHintRing_NoBloom.mat` should usually start with `M_`.
- `Assets/_Project/Art/Materials/Shader Graphs_SG_Water.mat` should usually start with `M_`.
- `Assets/_Project/Art/Materials/Shader Graphs_Toon_Water.mat` should usually start with `M_`.
- `Assets/_Project/Art/Props/fireworks/source/fireworks .fbx` should usually start with `SM_`.
- `Assets/_Project/Art/Props/fireworks/textures/1000_F_365653492_Kxv1ypncMKANNM0KoiI95TL4GbjFZrhj.jpeg` should usually start with `T_`.
- `Assets/_Project/Art/Props/fireworks/textures/27_high_resolution_3k_architectural_fine_wood_seamless_textu.jpeg` should usually start with `T_`.
- `Assets/_Project/Art/Props/fireworks/textures/360_F_408614527_Cy6CRkt9WOFq3VFUIh61VQzaspCit7Jv.jpeg` should usually start with `T_`.
- `Assets/_Project/Art/Props/fireworks/textures/42c8774da0d84a352d318f26f2e9f4fa.jpeg` should usually start with `T_`.
- `Assets/_Project/Art/Props/fireworks/textures/barber-colored-liner-background-blue-red-vector-pattern-diag.png` should usually start with `T_`.
- `Assets/_Project/Art/Props/fireworks/textures/christmas-candy-stripe-seamless-pattern-red-green-candy-cane.png` should usually start with `T_`.
- `Assets/_Project/Art/Props/fireworks/textures/Flag_of_Denmark.svg.png` should usually start with `T_`.
- `Assets/_Project/Art/Props/fireworks/textures/HD-wallpaper-old-knitted-texture-brown-fabric-background-fab.jpeg` should usually start with `T_`.
- `Assets/_Project/Art/Props/fireworks/textures/image.png` should usually start with `T_`.
- `Assets/_Project/Art/Props/fireworks/textures/pngtree-colorful-abstract-geometric-comic-background-modern-.jpeg` should usually start with `T_`.
- `Assets/_Project/Art/Props/fireworks/textures/red-and-white-striped-pattern-repeat-removable-wallpaper-des.jpeg` should usually start with `T_`.
- `Assets/_Project/Art/Props/fireworks/textures/white-polka-dot-with-colorful-background_58702-5653.png` should usually start with `T_`.
- `Assets/_Project/Art/Props/fireworks/textures/wood-texture-seamless-repeat-print-free-vector.jpeg` should usually start with `T_`.
- `Assets/_Project/Art/Props/fireworks/textures/загружено (4).jpeg` should usually start with `T_`.
- `Assets/_Project/Art/Props/StylizedVinePergola/Materials/ToonGenerated/BambooStructure_01_ToonPalette.png` should usually start with `T_`.
- `Assets/_Project/Art/Props/StylizedVinePergola/Materials/ToonGenerated/GardenVegetation_ToonOutlined.png` should usually start with `T_`.
- `Assets/_Project/Art/Props/StylizedVinePergola/Materials/ToonGenerated/WisteriaBark_ToonPalette.png` should usually start with `T_`.
- `Assets/_Project/Art/Props/StylizedVinePergola/Prefabs/StylizedVinePergola_Ready.prefab` should usually start with `P_`.
- `Assets/_Project/Art/Textures/note.png` should usually start with `T_`.
- `Assets/_Project/Art/Textures/SoftGlowCircle.png` should usually start with `T_`.
- `Assets/_Project/Art/waterfall/Shader Graphs_waterShader Graph.mat` should usually start with `M_`.
- `Assets/_Project/Art/waterfall/Voronoi04.png` should usually start with `T_`.
- `Assets/_Project/Art/waterfall/Waterfall.prefab` should usually start with `P_`.
- `Assets/_Project/Art/waterfall/WaterRibble.prefab` should usually start with `P_`.
- `Assets/_Project/Art/waterfall/WW_StylizedWaterfall_Surface.mat` should usually start with `M_`.
- `Assets/_Project/Art/waterfall/WW_WaterfallSplash_Soft.mat` should usually start with `M_`.
- `Assets/_Project/Audio/Music/animals/cat.mp3` should usually start with `SFX_`.
- `Assets/_Project/Audio/Music/animals/dog.mp3` should usually start with `SFX_`.
- `Assets/_Project/Audio/Music/animals/horse.mp3` should usually start with `SFX_`.
- `Assets/_Project/Audio/Music/Music Note/316898__jaz_the_man_2__do.wav` should usually start with `SFX_`.
- `Assets/_Project/Audio/Music/Music Note/316902__jaz_the_man_2__la.wav` should usually start with `SFX_`.
- `Assets/_Project/Audio/Music/Music Note/316904__jaz_the_man_2__fa.wav` should usually start with `SFX_`.
- `Assets/_Project/Audio/Music/Music Note/316906__jaz_the_man_2__mi.wav` should usually start with `SFX_`.
- `Assets/_Project/Audio/Music/Music Note/316908__jaz_the_man_2__re.wav` should usually start with `SFX_`.
- `Assets/_Project/Audio/Music/Music Note/316912__jaz_the_man_2__sol.wav` should usually start with `SFX_`.
- `Assets/_Project/Audio/Music/Music Note/316913__jaz_the_man_2__si.wav` should usually start with `SFX_`.
- `Assets/_Project/Core/XR/WonderlandXROrigin.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/BambooStructure_01_Mat.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/Door_01_Mat.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/DoorGlass_01_Mat.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/Paper_01.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/PlasterWall_01_Mat.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/Roof_01_Mat.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/Roof_Tiles_01.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/RoofOrnament_01_Mat.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/StoneSlab_01_Mat.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/Tatami_01_Mat.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/TrimRoof_01_Mat.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/TrimWooden_01_Mat.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/TrimWooden_02_Mat.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/WashiLight_Mat.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/Window_01_Mat.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Materials/WoodPlank_01_MAT.mat` should usually start with `M_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/BambooStructure_Frame_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/BambooStructure_Top_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/BeamWooden_13x16x380_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/BeamWooden_13x16x380_03_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/BeamWooden_20x330_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Ceiling_300x600_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Ceiling_600x600_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Cupboard_300x50x100_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Door_100x200_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Door_100x200_01_WashiLight_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Door_SlidingRails_200x200_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Door_SlidingRails_300x215_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Door_SlidingRails_400x200_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/DoorFusuma_100x200_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/DoorGlass_100x200_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/DoorGlass_100x200_02_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Nakabashira_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Roof_Ornament_400_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Roof_tile_800x800_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Shelve_200x50_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/StoneSlab_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Tatami_200x100_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Tatami_200x100_02_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Tobukuro_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Tokonoma_Step_200x100_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Wall_100x170_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Wall_100x200_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Wall_200x170_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Wall_200x200_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Wall_200x55_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Wall_200x55_02_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Wall_300x170_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Wall_300x200_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Wall_Window_200x200_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Window_100x200_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Window_100x200_01_WashiLight_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Window_96x50_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Window_96x50_01_WashiLight_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Window_Upper_200x100_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Window_Upper_200x170_02_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Window_Upper_300x170_03_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Window_Upper_400x100_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Window_Upper_400x170_02_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Window_Upper_400x170_03_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Wooden_Platform_200x200_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/Wooden_Platform_400x200_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/WoodenPlatform_InteriorAngle_200x200_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Meshes/WoodenSupport_15x80_01_Mesh.fbx` should usually start with `SM_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/BambooStructure_Frame_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/BambooStructure_Top_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/BeamWooden_13x16x380_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/BeamWooden_13x16x380_03_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/BeamWooden_20x330_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Ceiling_300x600_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Ceiling_600x600_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Cupboard_300x50x100_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Door_100x200_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Door_100x200_01_Prefab_Flipped.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Door_SlidingRails_200x200_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Door_SlidingRails_300x215_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Door_SlidingRails_400x200_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/DoorFusuma_100x200_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/DoorFusuma_100x200_01_Prefab_Flipped.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/DoorGlass_100x200_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/DoorGlass_100x200_01_Prefab_Flipped.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/DoorGlass_100x200_02_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Nakabashira_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Roof_Ornament_400_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Roof_tile_800x800_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Shelve_200x50_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/StoneSlab_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Tatami_200x100_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Tatami_200x100_02_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Tobukuro_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Tokonoma_Step_200x100_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Wall_100x170_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Wall_100x200_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Wall_200x170_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Wall_200x200_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Wall_200x55_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Wall_200x55_02_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Wall_300x170_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Wall_300x200_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Wall_Window_200x200_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Window_100x200_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Window_96x50_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Window_Upper_200x100_01_Prefab.prefab` should usually start with `P_`.
- `Assets/_Project/Features/CherryGarden/Art/Architecture/Prefabs/Window_Upper_200x170_02_Prefab.prefab` should usually start with `P_`.
- ...and 527 more.

## Asset Move Plan

Use this as a review queue. Do not move these paths from the operating system.

1. Keep vendor and package assets in their vendor folders unless the team decides to internalize them.
2. For project-authored assets outside `Assets/_Project`, use Unity Project window drag/drop or `AssetDatabase.MoveAsset`.
3. Re-run this report after each move batch and open the main scene before committing.
4. Delete `_Recovery`, `_TempArt`, template scenes, or legacy scripts only after this report shows zero production references and the team confirms they are obsolete.

