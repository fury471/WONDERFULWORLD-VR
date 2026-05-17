# Wonderland Grass Runtime Set

This folder contains production-owned grass assets derived from the vendor package at `Assets/Toon Fantasy Nature`.

Use these prefabs for terrain detail painting instead of dragging vendor prefabs directly:

- `Prefabs/WW_Grass_Detail_01A.prefab`
- `Prefabs/WW_Grass_Detail_01B.prefab`
- `Prefabs/WW_Grass_Detail_02A.prefab`
- `Prefabs/WW_Grass_Detail_02D.prefab`

The vendor package remains a source library. Project scenes should reference `_Project` assets so shader, naming, and performance settings stay stable.

## Reference Meadow Set

Use these when painting grass toward the stylized deep-green, light-green, yellow-green reference look:

- `Prefabs/WW_Grass_Detail_ReferenceMeadow_Lush.prefab` - main dense deep-green grass.
- `Prefabs/WW_Grass_Detail_ReferenceMeadow_Mixed.prefab` - yellow-green color variation.
- `Prefabs/WW_Grass_Detail_ReferenceMeadow_WarmAccent.prefab` - sparse warm yellow dry grass accent.

Suggested paint ratio: about 60% Lush, 30% Mixed, 10% WarmAccent. Keep WarmAccent sparse so the field reads green overall with yellow broken-up patches instead of turning dry or brown.

Each material uses an independent single-clump alpha texture:

- `Textures/WW_Grass_ReferenceMeadow_Lush_T_A.png`
- `Textures/WW_Grass_ReferenceMeadow_Mixed_T_A.png`
- `Textures/WW_Grass_ReferenceMeadow_WarmAccent_T_A.png`

`Textures/WW_Grass_ReferenceMeadow_T_A.png` is an old reference atlas and should not be assigned to detail materials.

## Lighting

The grass shader is intentionally stylized, but it still reacts to scene lighting:

- `_LightInfluence` controls how much main light and ambient light affect the grass. Keep this around `0.55-0.75` when the grass must follow time-of-day brightness.
- `_AmbientFloor` is scene-scaled fill, not a fixed brightness floor. It only adds fill when the scene is already lit. Keep this around `0.1-0.22`.
- `_ShadowStrength` controls the fake toon light/dark split on grass cards. Lower values stay flatter and more painterly.

For this set, the production materials use stronger lighting influence so time-of-day and weather changes darken the grass naturally without destroying the hand-painted color palette.
