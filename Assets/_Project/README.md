# _Project

This folder contains all team-owned game content for Wonderful World.

Current demonstration direction:

- `Docs/Current_Demonstration_Direction_v1.0.md`

## High-level rules

- Put new team work here, not in `Assets/Samples`, `Assets/VRTemplateAssets`, `Assets/XR`, or `Assets/XRI`.
- `Core` is for shared systems used by many features.
- `Features` is for gameplay modules that should stay as decoupled as possible.
- `World` is where the master park scene, zone prefabs, shared lighting, and world assembly content live.
- `Sandbox` is for personal prototype scenes and experiments.
- Use placeholder assets first to validate logic before final art.

## Current World Workflow

- For the current demonstration, the team is building one master playable park scene, not separate playable region scenes.
- The master scene should live in `World/Persistent` as `World_WonderlandPark.unity`.
- `World/Regions` should store zone blockouts, landmarks, attraction staging prefabs, scenic connectors, and other content that gets placed into the master scene.
- The park should feel open and explorable, but it is intentionally bounded.
- Any route or order language in milestone docs should be treated as a suggested orientation spine for demoing and testing, not as a mandatory player sequence.
- If performance or collaboration later require a scene split, that can be introduced in a later milestone.

## Suggested workflow

1. Create a feature branch.
2. Build the mechanic inside its feature folder.
3. Test it in your personal sandbox scene.
4. Integrate the prefab into the appropriate park zone content under `World/Regions`, then place it into the master world scene as part of a discoverable attraction, scenic connector, or soft-boundary illusion.
5. Merge back to `main` only after the branch is stable.

## Folder overview

- `Core`: shared runtime systems, input, scene management, settings, audio, tests.
- `Features`: one folder per gameplay system.
- `World`: master scene content, zone prefab content, shared lighting, shared materials, world assembly, and soft-boundary world illusion design.
- `Characters`: creature-specific content, such as the cat.
- `Art`: shared shaders, materials, textures, models, placeholders.
- `Audio`: project audio content.
- `UI`: menus, panels, fonts, and world-space UI prefabs.
- `Sandbox`: one folder per teammate for safe prototyping.
