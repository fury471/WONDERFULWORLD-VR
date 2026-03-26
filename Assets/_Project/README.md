# _Project

This folder contains all team-owned game content for Wonderful World.

## High-level rules

- Put new team work here, not in `Assets/Samples`, `Assets/VRTemplateAssets`, `Assets/XR`, or `Assets/XRI`.
- `Core` is for shared systems used by many features.
- `Features` is for gameplay modules that should stay as decoupled as possible.
- `World` is where regions and persistent scenes assemble the game world.
- `Sandbox` is for personal prototype scenes and experiments.
- Use placeholder assets first to validate logic before final art.

## Suggested workflow

1. Create a feature branch.
2. Build the mechanic inside its feature folder.
3. Test it in your personal sandbox scene.
4. Integrate the prefab into the appropriate world region.
5. Merge back to `main` only after the branch is stable.

## Folder overview

- `Core`: shared runtime systems, input, scene management, settings, audio, tests.
- `Features`: one folder per gameplay system.
- `World`: persistent scene content and region-based world assembly.
- `Characters`: creature-specific content, such as the cat.
- `Art`: shared shaders, materials, textures, models, placeholders.
- `Audio`: project audio content.
- `UI`: menus, panels, fonts, and world-space UI prefabs.
- `Sandbox`: one folder per teammate for safe prototyping.
