# World

This folder assembles the open world.

## Structure

- `Persistent`: shared world scene content and always-loaded systems references.
- `Regions`: additive region scenes such as FlowerField or LotusPond.
- `Shared`: prefabs, lighting, materials, and audio reused by multiple regions.

## Development rule

Do not put all gameplay logic directly into region scenes. Region scenes should mostly place prefabs and connect existing feature modules.

## Region workflow

1. Prototype a mechanic in a sandbox scene.
2. Turn it into a reusable prefab.
3. Place it into the correct region.
4. Test the region in headset before merging.
