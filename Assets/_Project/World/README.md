# World

This folder assembles the open world.

## Structure

- `Persistent`: the current master playable world scene plus shared world-level scene content.
- `Regions`: zone blockouts, landmark prefabs, attraction staging prefabs, and future zone content for places such as FlowerField or LotusPond.
- `Shared`: prefabs, lighting, materials, and audio reused across the park.

## Current M1 decision

- For M1, the project uses one big scene that includes all major park parts.
- The planned scene is `World_WonderlandPark.unity` inside `Persistent`.
- `Regions` does not mean separate playable scenes for M1. It means content folders that feed prefabs and organized assets into the master scene.
- Separate additive region scenes are a possible later optimization, not the current workflow.

## Development rule

Do not put all gameplay logic directly into the master world scene. The scene should mostly place prefabs and connect existing feature modules.

## Region workflow

1. Prototype a mechanic in a sandbox scene.
2. Turn it into a reusable prefab.
3. Place it into the correct zone folder under `Regions`.
4. Instantiate and test it inside the master world scene in headset before merging.