# Quest Controller Locomotion Comfort Upgrade

Date: May 17, 2026
Scope: production Quest controller locomotion profile for `WonderlandXROrigin`.

## Shipped Locomotion Model

This build uses a comfort-first Quest locomotion model:

- Left controller: teleport aiming and teleport commit only.
- Right controller: snap yaw only.
- Continuous translation is disabled.
- Smooth artificial turning is disabled.
- 180 degree turn-around is disabled.
- Teleport and snap turn both trigger tunneling vignette.

This is now owned by `QuestLocomotionComfortProfile` on `Assets/_Project/Core/XR/WonderlandXROrigin.prefab`.

## Why This Is The Default

Headset testing showed strong sickness with smooth movement and smooth turn. The production profile therefore removes sustained artificial camera velocity from the default control scheme. The only artificial motion left is discontinuous teleport repositioning and discrete yaw steps, both routed through XRI locomotion providers so the XR Origin, Character Controller, Locomotion Mediator, reticles, and vignette stay in one consistent stack.

The previous red-teleport failure was caused by ordinary world colliders not being XRI teleport interactables. XRI teleport rays do not teleport to arbitrary colliders. They need a `TeleportationArea`, `TeleportationAnchor`, or related teleport interactable. The profile now installs `TeleportationArea` components at runtime on valid standing surfaces.

## Runtime Surface Rules

At startup and whenever a Unity scene is loaded, the profile scans active colliders and installs teleport areas on candidates that pass all of these checks:

- Collider is enabled and active.
- Collider is not part of the XR rig.
- Collider is on the configured teleport surface mask, currently Default and LotusPad.
- Collider is not a trigger.
- Collider is not attached to a non-kinematic rigidbody.
- Collider is not already part of another XR interactable.
- Collider has enough horizontal footprint to avoid tiny decorative targets.

Installed and existing teleport interactables are configured with:

- `TeleportationProvider` from the XR rig.
- `MatchOrientation.WorldSpaceUp`.
- `TeleportTrigger.OnSelectExited`.
- hit-normal filtering enabled.
- max accepted slope of `38` degrees.
- all XRI interaction layers, matching the starter ray interactors.

The teleport ray physics mask is now Default, LotusPad, and the XRI teleport helper layer. It no longer relies on the earlier mask that omitted LotusPad and included UI.

## Comfort Parameters

- Teleport delay: `0.08s`, to give the vignette a short comfort lead-in.
- Teleport vignette aperture: `0.52`.
- Snap turn amount: `30` degrees.
- Snap turn debounce: `0.35s`.
- Snap turn delay: `0.05s`.
- Snap turn vignette aperture: `0.58`.
- Vignette feathering: `0.30`.
- Vignette ease in/out: `0.10s` / `0.20s`.

## Haptics Policy

Controller haptics are now owned by `QuestHapticsInteractionProfile` on `WonderlandXROrigin`.

The policy removes the XRI Starter Assets default hover buzz because those prefabs ship with `Play Hover Entered` enabled on ray, poke, direct, near-far, and teleport interactors. In a rich animated scene this can feel like random vibration when a ray jitters across petals, VFX, moving colliders, or auto-installed teleport areas.

Current production behavior:

- Raw XRI hover entered/exited/canceled haptics are disabled at runtime.
- Teleport interactors do not produce haptic feedback.
- Controller haptic player amplitude is globally damped to `0.55`.
- Real selectable or activatable non-teleport hover targets can emit one subtle stable-affordance pulse after `0.18s` of continuous hover.
- Each hand has at least `0.35s` between stable hover pulses, even when sweeping across multiple valid targets.
- The same interactor/target pair is cooled down for `0.9s`, so hover jitter cannot machine-gun the controller.
- Visual-only particle and cherry petal paths are suppressed from stable hover pulses.
- Selection confirmation remains short and deliberate: amplitude `0.18`, duration `0.025s`.

This keeps vibration as a trustable interaction cue instead of background noise.

## References

- Unity XR Interaction Toolkit Teleportation: https://docs.unity.cn/Packages/com.unity.xr.interaction.toolkit@3.1/manual/teleportation.html
- Unity XR Interaction Toolkit Tunneling Vignette Controller: https://docs.unity.cn/Packages/com.unity.xr.interaction.toolkit@2.1/manual/tunneling-vignette-controller.html
- Unity XR Interaction Toolkit Simple Haptic Feedback: https://docs.unity.cn/Packages/com.unity.xr.interaction.toolkit@3.0/manual/simple-haptic-feedback.html
- Oculus VR Best Practices Guide, motion and acceleration guidance: https://brianschrank.com/vrgames/resources/OculusBestPractices.pdf
