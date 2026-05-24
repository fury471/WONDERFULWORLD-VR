# Final Release Checklist

Use this checklist for the last Unity and Quest 3 Link pass. Items marked "local static pass complete" have been handled in this repository without opening Unity.

## Local Static Pass Complete

- English production documentation consolidated under `Docs`.
- Runtime `Camera.main`, tag lookup, and direct `GameObject.Find` usage centralized or removed from high-frequency gameplay paths.
- Character controller scale-shift step offset clamped before resize and restore.
- PC VR frame pacing bootstrap added for Quest Link.
- Runtime scan and GC spike reduction pass applied to XR ray visuals, recenter, haptics, lotus score UI, and refresh-rate requests.
- Production debug logging flags disabled in the main scene and production region prefabs.
- Floating prompt camera lookup and washi light renderer toggles throttled for smoother Editor plus Link testing.
- Production editor tooling added under `Wonderful World > Production`.
- Static checks pass for whitespace, maintained docs CJK text, runtime non-ASCII comments, and direct scene-search hotspots.

## Unity Editor Pass

1. Open the project in Unity `6000.3.11f1`.
2. Confirm there are no compile errors.
3. Open `Assets/_Project/World/Persistent/World_WonderlandPark.unity`.
4. Run `Wonderful World > Production > Generate Production Audit`.
5. Run `Wonderful World > Production > Generate Asset Reference Audit`.
6. Run `Wonderful World > Production > Normalize Main Scene Hierarchy`.
7. If `_TempArt` is still referenced, run `Wonderful World > Production > Internalize Referenced Temp Art`.
8. Re-run both audits after hierarchy and asset moves.
9. Inspect the hierarchy before committing:
   - `WW_UI_System` should be under `UI`.
   - decorative orphan roots such as `TFF_*` should be under `Decorations`.
   - production roots should follow the standard order.
10. Delete temporary or recovery folders only after `Docs/Asset_Reference_Audit.md` shows zero production references and the team confirms they are obsolete.

## Play Mode Smoke Test

1. Start Play Mode with Quest 3 Link active.
2. Verify head tracking, hand tracking, teleport, snap turn, recenter, scale shift, and system menu.
3. Visit Human Entry, Flower Garden, Lotus Pond, Cat Garden, Fireworks Clearing, Mushroom Growth, and Cherry Garden.
4. Test notice boards, lotus notes, petal/pollen magic, mushroom growth, mount interactions, fireworks, cherry orb, audio, and major animations.
5. Confirm no blocking console errors appear.

## Quest 3 Link Performance Signoff

1. Use OVR Metrics Tool or OpenXR Toolkit.
2. Confirm the headset refresh rate and Link runtime settings.
3. Confirm stable 72 Hz minimum and target 90 Hz if the PC allows it.
4. Watch for dropped frames, ASW/reprojection engagement, compositor misses, tearing, black flicker, and delayed head rotation.
5. Use Unity Profiler and Frame Debugger for any remaining spikes before reducing visual quality.
