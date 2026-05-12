# Fireworks System

Production-ready point-cloud fireworks module for VR integration and mouse-only editor debug.

## Prefab

Use `Assets/_Project/Features/Fireworks/Prefabs/FireworksSystem.prefab`.

Hierarchy:

- `FireworksSystem`
- `Runtime`: controller and launch pad
- `Renderers`: point-cloud particle renderer
- `Anchors`: launch point and future target anchors
- `Audio`: optional launch/burst audio source

This mirrors the portable rig pattern used by `PetalPollenMagicRig`.

## Editor Debug

The production world scene uses `FireworkMagicActivator` on the ground firework device. In Play Mode, aim at the device and click with the mouse to simulate the controller interaction path.

## VR Integration

Use `VRFireworkMenuController` or controller bindings to call `FireworkLaunchPad`, so the visual system does not depend on the UI implementation.

Recommended runtime flow:

1. UI or controller binding calls `FireworkLaunchPad.TriggerShowcase()`, `TriggerShowcaseStep(index)`, `TriggerText(text)`, or `StopShowcase()`.
2. `FireworkLaunchPad` validates play state and forwards to `FireworkController`.
3. `FireworkController` builds a `PointCloudFireworkRequest`.
4. `FireworkPointCloudGenerator` samples text/math points.
5. `PointCloudFireworkRenderer` animates launch, bloom, hold, fade, ember fall, and pattern-specific rotation.

## Tuning

Main settings live on `FireworkController`:

- `showcaseText`
- `showcaseSequence` (single production source for showcase order; add, reorder, enable, or disable steps here)
- `pointCloudHeightOffset`
- `pointCloudForwardOffset`
- `pointCloudScale`
- `textPointCloudScaleMultiplier`
- `mathPointCloudScaleMultiplier`
- `textPointBudget`
- `mathPointBudget`
- `qualityMode`
- `useProceduralAudioFallback`

Audio is self-contained by default. If `launchAudioClip` or `burstAudioClip` is empty and `useProceduralAudioFallback` is enabled, the controller generates lightweight runtime launch and burst clips. Assign real clips to override the fallback.

Performance recommendation:

- Use `Balanced` for development and normal VR demos.
- Use `Performance` for lower-end PCs or streaming bottlenecks.
- Use `Showcase` only for controlled demos where the PC has headroom.

## Migration Checklist

1. Drag `FireworksSystem.prefab` into the target scene.
2. Place the root near the intended launch location.
3. Rotate the root forward toward the audience.
4. Set `showcaseText`.
5. Assign launch and burst audio clips when available.
6. Test with the mouse debug activator in the world scene before wiring VR UI.
7. Wire VR controller/menu buttons to the generic `FireworkLaunchPad` methods.

## Legacy Compatibility

`FireworkPatternLibrary_SO` and `LegacyFireworkPatternTypes` are retained only so older sandbox/world prefabs keep their serialized references. New scenes should use `FireworksSystem.prefab`, `FireworkController`, `FireworkLaunchPad`, and the point-cloud math/text pipeline. Showcase playback is driven only by `showcaseSequence`. The double-helix point-cloud pattern is `MathFireworkPattern.DoubleHelix`.
