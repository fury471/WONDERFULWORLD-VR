# System Structure

## Asset Layout

Production-owned content lives under:

```text
Assets/_Project
```

Current primary folders:

- `Art`: shared shaders, imported art processing, stylized materials, water, and art style support.
- `Audio`: project-owned audio content.
- `Characters`: character-specific assets.
- `Core`: shared runtime systems, especially XR and comfort systems.
- `Editor`: project-owned editor utilities.
- `Features`: feature modules with runtime code, prefabs, ScriptableObjects, and tests.
- `Sandbox`: personal or experimental scenes that must not be treated as production scenes.
- `UI`: world-space UI, notice boards, localization, and system menu.
- `World`: production scene, region content, terrain, shared vegetation, and lighting.

Third-party, Unity sample, template, and vendor content may remain in vendor folders, but production scene references should be intentional and documented.

## Main Scene Hierarchy

The production scene should use these root groups:

- `GlobalSystem`: event system, global managers, language/settings services, runtime profiles.
- `XR`: the production XR Origin and controller rig only.
- `Lighting`: directional light, sky, probes, volumes, and atmosphere.
- `Terrain`: terrain tiles, terrain data instances, and terrain colliders.
- `World_Regions`: region-level content and attraction staging.
- `Decorations`: shared world art that is not owned by a specific region.
- `UI`: world-space UI, notice board overlay, and system menu.
- `Debug`: temporary disabled helpers only; delete when empty.

The current static audit found an inactive empty `Debug` root and both `UI` and `WW_UI_System` at scene root level. The intended cleanup is to move `WW_UI_System` under `UI` and delete the empty inactive `Debug` root through the Unity Editor tooling.

## Core Runtime Systems

- `WonderlandXROrigin.prefab`: production XR rig.
- `QuestLocomotionComfortProfile`: teleport and snap-turn comfort profile.
- `RecenterController`: right-hand recenter flow and mount-aware recenter handling.
- `QuestRayVisualLengthProfile`: short, comfort-safe controller ray visuals.
- `QuestInteractionUtils`: shared cached lookup helpers for head, controller, and haptic references.
- `ScaleManager` and `ScaleTransitionController`: player scale state and blink transition support.

## UI Systems

- `WW_UI_System`: shared world-space UI system in the production scene.
- `WW_VRSystemMenu.prefab`: runtime menu for settings and exit.
- `WW_NoticeBoardOverlayPanel.prefab`: shared notice board popup.
- `LocalizedNoticeBoardContent`: per-board localized content assets.
- `UILanguageService`: global language state and UI update dispatch.

## Production Editor Tools

The menu `Wonderful World > Production` contains editor tools for safe closeout work:

- `Create Standard Project Folders`
- `Generate Production Audit`, including a scan for enabled production debug logging flags
- `Generate Asset Reference Audit`
- `Normalize Main Scene Hierarchy`

These tools run inside Unity, so folder creation, hierarchy edits, and scene saving happen through Unity APIs.
