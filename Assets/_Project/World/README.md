# World

This folder assembles the seamless-feeling park slice that carries the project's open-world illusion.

Current demonstration direction:

- `Docs/Current_Demonstration_Direction_v1.0.md`

## Structure

- `Persistent`: the current master playable world scene plus shared world-level scene content.
- `Regions`: zone blockouts, landmark prefabs, attraction staging prefabs, scenic connectors, and future zone content for places such as FlowerField or LotusPond.
- `Shared`: prefabs, lighting, materials, and audio reused across the park.

## Current M1 decision

- The project uses one big scene that includes all major park parts.
- The planned scene is `World_WonderlandPark.unity` inside `Persistent`.
- `Regions` does not mean separate playable scenes for the current demo. It means content folders that feed prefabs and organized assets into the master scene.
- The park should feel open, exploratory, and larger than its playable footprint through strong sightlines, themed lands, scenic connectors, and soft boundaries.
- Separate additive region scenes are a possible later optimization, not the current workflow.

## Development rule

Do not put all gameplay logic directly into the master world scene. The scene should mostly place prefabs, connect feature modules, and stage the illusion of a complete magical park.

## Region workflow

1. Prototype a mechanic in a sandbox scene.
2. Turn it into a reusable prefab.
3. Place it into the correct zone folder under `Regions`.
4. Instantiate and test it inside the master world scene before merging.
5. If the content helps define the playable perimeter, make sure it feels like a believable park boundary rather than a visible hard wall.
