# Fireworks System

Production-ready point-cloud fireworks module for desktop testing and later VR integration.

## Prefab

Use `Assets/_Project/Features/Fireworks/Prefabs/FireworksSystem.prefab`.

Hierarchy:

- `FireworksSystem`
- `Runtime`: controller, launch pad, keyboard tester
- `Renderers`: point-cloud particle renderer
- `Anchors`: launch point and future target anchors
- `Audio`: optional launch/burst audio source

This mirrors the portable rig pattern used by `PetalPollenMagicRig`.

## Desktop Test Controls

The prefab includes `FireworkKeyboardTester`.

- `T`: launch `showcaseText`
- `C`: run configured sequence
- `A`: run all sequence
- `1`: heart
- `2`: DNA helix
- `3`: spiral
- `4`: sphere
- `5`: flower
- `6`: star
- `7`: mobius
- `Esc`: stop current sequence

Keyboard testing uses Unity Input System (`Keyboard.current`) so it stays compatible with the later VR input stack.

## VR Integration

Use `VRFireworkMenuController` for the menu/virtual keyboard layer. It calls `FireworkLaunchPad`, so the visual system does not depend on the UI implementation.

Recommended runtime flow:

1. UI calls `FireworkLaunchPad.TriggerTextFirework(text)` or one of the math trigger methods.
2. `FireworkLaunchPad` validates play state and forwards to `FireworkController`.
3. `FireworkController` builds a `PointCloudFireworkRequest`.
4. `FireworkPointCloudGenerator` samples text/math points.
5. `PointCloudFireworkRenderer` animates launch, bloom, hold, fade, ember fall, and pattern-specific rotation.

## Tuning

Main settings live on `FireworkController`:

- `showcaseText`
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
6. Test with keyboard before wiring VR UI.
7. Wire VR menu buttons to `FireworkLaunchPad` methods.

## Legacy Compatibility

`FireworkPatternLibrary_SO` and `LegacyFireworkPatternTypes` are retained only so older sandbox/world prefabs keep their serialized references. New scenes should use `FireworksSystem.prefab`, `FireworkController`, `FireworkLaunchPad`, and the point-cloud math/text pipeline.
