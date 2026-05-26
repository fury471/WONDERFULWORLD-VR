# Build And Run

## Required Unity Version

Open the project with the version recorded in `ProjectSettings/ProjectVersion.txt`:

```text
Unity 6000.3.12f1
```

Do not upgrade packages or migrate the project during production closeout unless the team explicitly schedules that as a separate task.

## Required Packages

Important packages currently used by the project include:

- `com.unity.render-pipelines.universal` 17.3.0
- `com.unity.xr.management` 4.5.4
- `com.unity.xr.openxr` 1.16.1
- `com.unity.xr.interaction.toolkit` 3.3.1
- `com.unity.xr.hands` 1.7.3
- `com.unity.inputsystem` 1.19.0
- `com.unity.cloud.gltfast` 6.18.0

## PC VR Setup For Quest 3 Link

1. Connect Quest 3 with a Link Cable.
2. Confirm the Meta Quest Link app sees the headset and cable connection.
3. In the headset, enter Quest Link.
4. Open the project in Unity.
5. Confirm the active build scene is `World_WonderlandPark.unity`.
6. Press Play in the editor for headset smoke testing.

## Build Settings

The enabled scene in `ProjectSettings/EditorBuildSettings.asset` is:

```text
Assets/_Project/World/Persistent/World_WonderlandPark.unity
```

Recommended build target for the closeout build:

- Platform: Windows, Mac, Linux Standalone.
- Target OS: Windows.
- Architecture: x86_64.
- Scripting backend: IL2CPP for release builds.
- Input: Unity Input System.
- Color space: Linear.
- Stereo rendering path: Single Pass Instanced.

## Smoke Test

Run this after every cleanup or optimization batch:

1. Open `World_WonderlandPark.unity`.
2. Enter Play Mode through Quest 3 Link.
3. Confirm the XR Origin tracks head and hands correctly.
4. Test teleport, snap turn, recenter, and the VR system menu.
5. Walk through the Welcome flow (`WelcomePanel`) and visit the six region roots under `World_Regions/`: `Region_FlowerGarden`, `Region_LotusPond`, `Region_CatGarden`, `Region_FireworksClearing`, `Region_MushroomGrowth`, `Region_CherryGarden`.
6. Test notice boards, audio, major animations, the Flower Garden particle crystal, lotus notes, the three Cat Garden mounts (cat/dog at Small scale, horse at Normal scale), horse summon, mushroom planting + cultivation, the fireworks mortar, the cherry orb, and the left-Menu system menu.
7. Watch for black flicker, tearing, jitter, delayed head rotation, missing materials, missing scripts, or console errors.

## Validation Tools

Use these tools before signing off a performance build:

- Unity Profiler: CPU, GPU, rendering, physics, audio, and GC allocations.
- Unity Frame Debugger: draw calls, transparent ordering, render passes, and unexpected full-screen copies.
- Memory Profiler: texture, mesh, material, and runtime allocation pressure.
- OVR Metrics Tool or OpenXR Toolkit: headset-side frame rate, app frame time, compositor frame time, dropped frames, ASW/reprojection behavior, and Link performance.
