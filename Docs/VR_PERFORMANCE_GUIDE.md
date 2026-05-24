# VR Performance Guide

## Performance Target

The target runtime is Quest 3 through Link Cable from a Windows PC. The experience should hold stable headset frame pacing:

- Minimum: stable 72 Hz.
- Target: stable 90 Hz.
- Stretch: 90 Hz or higher with visual quality preserved.

Frame pacing matters more than average FPS. Any dropped frames, compositor misses, black flicker, tearing, or camera jitter should be treated as release blockers until profiled.

## Current Rendering Baseline

Static project audit found:

- Unity version: `6000.3.11f1`.
- URP version: `17.3.0`.
- Active build scene: `World_WonderlandPark.unity`.
- Project stereo rendering path: Single Pass Instanced.
- Current quality index: `Low`.
- Standalone default quality: `Low`, so Windows Quest Link builds use the performance URP asset by default.
- Active custom render pipeline for Low quality: `Assets/Settings/Project Configuration/Performance URP Config.asset`.
- Performance URP config: render scale `1.0`, MSAA `4x`, HDR off, opaque texture off, SRP Batcher on, additional lights disabled, main light shadows enabled, shadow distance `2.5`.
- Fixed timestep: `0.01111111`, aligned to 90 Hz physics timing.
- Maximum allowed timestep: `0.03333334`, limiting physics catch-up bursts after a hitch.
- `PCVRPerformanceBootstrap` applies frame pacing before the scene loads: Low quality, VSync off, compositor-controlled target frame rate, render-frame interval `1`, stereo camera post-processing off, HDR off, and eye texture scale clamped to `1.0`.

These settings are a reasonable starting point for Quest 3 Link, but headset-side metrics must decide the final tradeoffs.

## Profiling Order

1. Use OVR Metrics Tool or OpenXR Toolkit to confirm the actual headset refresh rate, app frame time, compositor frame time, dropped frames, and reprojection state.
2. Use Unity Profiler in Play Mode through Link Cable.
3. Separate CPU, GPU, rendering, physics, audio, and GC allocation bottlenecks.
4. Use Frame Debugger to inspect draw calls, SetPass count, render pass copies, opaque texture usage, transparent layers, and full-screen effects.
5. Only then apply targeted optimizations.

## Rendering Checks

Verify:

- Single Pass Instanced is active.
- Render scale stays at `1.0` unless measured GPU headroom requires a change.
- MSAA is preferred over heavy post-process antialiasing in VR.
- HDR, motion blur, depth of field, heavy bloom, and SSAO are disabled unless there is measured headroom.
- Opaque texture is enabled only if water, distortion, or other effects require it.
- SRP Batcher remains enabled.
- GPU instancing is enabled on repeated vegetation, flowers, props, and simple materials where compatible.

## Scene Checks

Track these in profiler and Frame Debugger:

- Draw calls.
- SetPass calls.
- Transparent overdraw.
- Particle count.
- Terrain detail density and visible distance.
- Realtime light count.
- Shadow distance and shadow map resolution.
- Skinned mesh count.
- Active audio sources.
- Physics colliders and trigger volume count.

Prefer LOD groups, culling, batching, instancing, terrain detail tuning, and baked lighting over removing signature art.

## Script Checks

Watch for:

- `FindObject*`, `GameObject.Find`, and `Camera.main` in per-frame paths.
- Per-frame `GetComponent` calls on many objects.
- `Physics.RaycastAll` allocations.
- LINQ allocations in runtime update loops.
- Runtime `Instantiate` and `Destroy` spikes.
- GC allocations from particle or UI refresh logic.

Recent closeout code changes reduced repeated XR scene searches and replaced high-frequency mushroom hover raycasts with non-alloc raycasts.

Current runtime lookup status:

- Direct production `Camera.main` usage is centralized in `QuestInteractionUtils`.
- UI prompts, notice board targeting, fireworks, lotus interaction, growth interaction, cherry garden effects, scale shift, and swing mounting use cached head/controller lookup helpers.
- Remaining `FindObject*` calls are mostly startup/bootstrap discovery paths or periodic system-level cache refreshes; inspect them with Profiler markers if CPU spikes remain.
- Do not reintroduce per-frame global object searches when adding new interactions. Prefer serialized references, feature-local bootstrap wiring, or `QuestInteractionUtils` cached helpers.

Recent spike-reduction pass:

- `QuestRayVisualLengthProfile` now scans for XRI curve visuals at a lower cadence and avoids per-frame lowercase string allocations while resolving hand ownership.
- `RecenterController` throttles missing-reference rediscovery instead of attempting global searches every frame.
- `QuestHapticsInteractionProfile` reuses hover update scratch lists instead of allocating temporary lists during stable-hover haptic pulses.
- `PCVRPerformanceBootstrap` reuses the refresh-rate reflection argument buffer during the startup refresh-rate request window.
- `LotusMusicStaff` reuses its note-removal scratch list when the score display refreshes.

## Black Flicker Triage

If black blocks or flicker appear in the headset, check these first:

1. Skybox material and camera clear flags.
2. Camera near and far clip planes.
3. Transparent material sorting and overlapping additive effects.
4. Render textures and any camera output target.
5. Post-processing volumes.
6. Opaque texture usage and distortion shaders.
7. Custom shaders under Single Pass Instanced.
8. XR display provider and Link runtime settings.

Custom hand-written shaders should include stereo instancing macros when they define vertex and fragment passes.

## Tearing And Jitter Triage

If tearing, jitter, or delayed head rotation appears:

1. Confirm headset refresh rate and Link bandwidth.
2. Confirm app frame time is below the refresh budget.
3. Disable expensive overlays and debugging tools while testing.
4. Check whether ASW or reprojection is engaging.
5. Keep Unity VSync disabled for XR and let the XR compositor pace frames.
6. Avoid artificial camera smoothing that follows head pose late.
7. Check physics spikes and reduce fixed timestep catch-up if physics work is causing bursts.

## Signoff Criteria

A closeout build is ready only when:

- The production scene opens without missing scripts.
- Play Mode has no blocking console errors.
- Quest 3 Link headset testing holds the selected refresh rate without visible frame pacing artifacts.
- All major interactions still work after optimization.
- Any intentional visual tradeoff is documented with profiler evidence.
