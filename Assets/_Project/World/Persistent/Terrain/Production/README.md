# World Persistent Production Terrain

This folder owns the TerrainData assets used by:

- `Assets/_Project/World/Persistent/World_WonderlandPark.unity`

Rules:

- The persistent world scene should reference only TerrainData assets from this folder.
- Do not reuse these assets in sandbox, member, or test scenes.
- For personal terrain work, duplicate the needed TerrainData into the relevant sandbox folder first.
- Do not rename, replace, or sculpt these production TerrainData assets without coordinating the main world integration owner.
- Shared materials, terrain layers, vegetation prefabs, and textures may stay shared when they are treated as read-only dependencies.

