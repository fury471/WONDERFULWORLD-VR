# Cleanup And Standardization

## Guiding Rule

Do not move or rename Unity assets through the operating system. Use the Unity Project window, `AssetDatabase.MoveAsset`, or the production cleanup tools so `.meta` files and GUID references remain intact.

## Hierarchy Rules

The production scene should stay grouped by responsibility:

- `GlobalSystem`: managers and scene-wide services.
- `XR`: XR Origin, camera, controllers, input, and comfort providers.
- `Lighting`: sky, directional light, probes, volumes, and atmosphere controllers.
- `Terrain`: terrain tiles, terrain colliders, terrain data instances.
- `World_Regions`: one child per major region or attraction zone.
- `Decorations`: cross-region world art and set dressing.
- `UI`: notice board system, system menu, prompts, and world-space UI.
- `Debug`: temporary helpers only; remove before release when empty.

Before deleting any object, check for scene references, prefab overrides, event bindings, serialized fields, and runtime bootstrap dependencies.

## Asset Directory Rules

The project uses a hybrid type-and-module structure:

- Shared reusable assets can use type folders such as `Materials`, `Textures`, `Models`, `Prefabs`, `Audio`, `Animations`, and `VFX`.
- Feature-owned assets should stay inside their feature module, for example `Assets/_Project/Features/LotusPond/Prefabs`.
- World-owned assets should stay under `Assets/_Project/World`.
- Sandbox content must not be referenced by the production build.
- `_Recovery` and `_TempArt` content must be validated for references before deletion.

## Naming Rules

Use these prefixes for new or renamed production assets:

- `T_`: texture.
- `M_`: material.
- `SM_`: project-authored static mesh or mesh model.
- `P_`: generic prefab.
- `SFX_`: sound effect.
- `AMB_`: ambient loop.
- `MUS_`: music.
- `SO_` or `FeatureName_SO`: ScriptableObject data.
- `WW_`: shared Wonderful World assets where the prefix is already established.

Existing feature prefixes such as `Lotus`, `Growth`, `CatRide`, `CherryGarden`, and `Firework` may remain when they are clearer than a generic prefix.

## Safe Cleanup Process

1. Create or switch to a cleanup branch.
2. Run `Wonderful World > Production > Generate Production Audit`.
3. Run `Wonderful World > Production > Generate Asset Reference Audit`.
4. Fix missing scripts, empty inactive roots, duplicate root UI grouping, and decorative orphan roots first.
5. If `_TempArt` is still referenced, run `Wonderful World > Production > Internalize Referenced Temp Art`.
6. Move any remaining assets only in Unity, preferably in small batches.
7. After each batch, open the production scene and enter Play Mode.
8. Commit hierarchy, asset organization, documentation, and performance work separately.

## Deletion Rules

Delete only after verifying references:

- Empty disabled debug roots.
- Obsolete milestone documents.
- Template scenes not referenced by build settings.
- Recovery scenes after confirming they are not needed.
- Temporary art candidates after final assets have been imported, credited, and referenced from production folders.

Keep third-party package folders unless the team has confirmed no asset, material, shader, prefab, or scene reference remains.
