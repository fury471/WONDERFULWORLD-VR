# Wonderland Terrain Detail Flowers

These flower assets are designed for the main VR terrain detail workflow.

- `WW_Flower_Detail_GoldenCluster`: low golden flower clusters, fixed terrain detail size `0.44 x 0.50`.
- `WW_Flower_Detail_WhiteDaisy`: medium white daisy flowers, fixed terrain detail size `0.50 x 0.58`.
- `WW_Flower_Detail_RedPoppy`: taller red flowers, fixed terrain detail size `0.56 x 0.76`.
- `WW_GroundPetals_Detail`: existing CherryGarden petal scatter texture, flat terrain detail size `1.10-1.85`.

The installer keeps `minWidth == maxWidth` and `minHeight == maxHeight` for every flower type, so Unity will not squash or stretch them with random width/height variation.

Install or refresh the terrain detail prototypes from:

`Wonderland > World > Install Toon Vegetation Detail Prototypes In Wonderland Park`

After installation, paint them from Terrain Paint Details just like the grass. Use flowers in small patches and path/lake accents; avoid dense full-map flower coverage for VR.

For `WW_GroundPetals_Detail`, paint lightly under cherry trees, beside paths, and around rest areas. The texture already contains a scattered petal pattern, so use lower brush opacity/density than grass.
