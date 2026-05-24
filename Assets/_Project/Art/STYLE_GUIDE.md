# Wonderland Art Style Guide

## Style Baseline

The current look is stylized toon rendering for a warm fantasy park. The goal is readable VR silhouettes, soft color grouping, clean material families, and enough atmospheric detail to keep the world alive without sacrificing frame pacing.

## Shader Families

| Shader | Use | Notes |
| --- | --- | --- |
| `ChiliMilk/Toon` | Buildings, props, and imported stylized models | Keep the included shader files intact. |
| `Toon/TFF_CustomToon` | Toon Fantasy Nature vegetation and natural assets | Preferred for vendor vegetation already authored for this package. |
| `Toon/TFF_CustomToonOutline` | TFF vegetation that needs outline support | Use sparingly on dense vegetation. |
| `Wonderland/Props/Toon Band Lit URP` | Simple project-owned props | Good for lightweight toon materials. |
| `Wonderland/UI/Notice Board Toon URP` | Welcome board and notice board surfaces | Includes outline and rim support for readable world UI. |

## Material Rules

- New project-owned materials should use the `M_` prefix.
- New textures should use the `T_` prefix.
- Keep material instances feature-local when they are tuned for one attraction.
- Put shared reusable art materials under a shared art or world folder.
- Enable GPU instancing on repeated simple materials when the shader supports it.
- Avoid unnecessary transparent materials in dense regions.
- Do not use heavy post effects to solve local material problems.

## Toon Consistency

Use a restrained outline and rim style:

- Dark warm outline colors usually read better than pure black in headset.
- Wide outlines should be reserved for important silhouettes or distant readability.
- Rim lighting should support shape readability without making every object glow.
- Emission should be reserved for magical props, fireworks, lotus feedback, UI highlights, and directed attraction beats.

## VR Performance Notes

- Dense vegetation should use LOD, instancing, or terrain detail systems rather than many unique mesh renderers.
- Transparent petals, particles, water, and bloom-like effects should be profiled for overdraw.
- Prefer baked or single-main-light lighting for most environmental art.
- Keep realtime shadows short and intentional.
- Validate custom shaders in Single Pass Instanced.

## Import Workflow

1. Import new art into a temporary or feature-local staging folder.
2. Confirm licensing and attribution before production use.
3. Create or remap external materials in Unity.
4. Move finalized assets into their production folder through Unity, not through the operating system.
5. Run the production scene and Frame Debugger before considering the asset finished.

## Do Not

- Do not delete third-party shader include files unless the package is being fully removed.
- Do not mix multiple unrelated toon shader families on the same hero object without an art reason.
- Do not leave final assets in `_TempArt`.
- Do not add new production references to sandbox-only folders.
- Do not use copyrighted character-IP derivatives in production content unless the team has explicit clearance.
