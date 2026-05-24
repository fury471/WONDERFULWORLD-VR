# Wonderland

A handcrafted PC VR fantasy park — a first-person, comfort-first wonderland of butterflies, lotus music, flower magic, friendly mounts, growing mushrooms, and a blossoming cherry tree.

**Languages:** **English** · [中文](README.zh-CN.md) · [Svenska](README.sv.md)

---

## About

Wonderland (also known internally as *Butterfly House* / *Wonderful World*) is a single-player, exploration-first VR experience built in **Unity 6** with **OpenXR** and the **Universal Render Pipeline**. The player wanders a seamless-feeling park slice made of seven connected attraction zones — each one a small piece of magic instead of a quest chain.

The design priorities are clear and unchanging:

1. **Comfort first.** Stable frame pacing, tunneling vignette, teleport-by-default, blink scale transitions, no forced motion.
2. **Discoverable wonder.** Every region is a self-contained interaction worth finding, not a checkpoint to complete.
3. **Stylized, not photoreal.** A toon-shaded look (Toon Fantasy Nature + custom shaders) over Single Pass Instanced URP, tuned for Quest 3 Link.

The current production scene is [`Assets/_Project/World/Persistent/World_WonderlandPark.unity`](Assets/_Project/World/Persistent/World_WonderlandPark.unity) and ships as **v1.0.0**.

---

## Highlights

- **Seven themed regions in one continuous park** — Human Entry, Flower Garden, Lotus Pond, Cat Garden, Fireworks Clearing, Mushroom Growth, Cherry Garden.
- **Three player scales** — switch between Normal, Small (0.25×), and Large (1.75×) with a 0.4 s blink transition; eye height, movement speed, and interaction reach adapt automatically.
- **Lotus pond music sequencer** — seven floating lotus pads tuned to the *do · re · mi · fa · sol · la · si* diatonic scale, played by shooting curved water-magic projectiles from your controller. A score selector picks a random song to follow.
- **Cat Garden mount system** — three independent mounts (Kitty, Dog, Horse), each with its own ride route, idle pacing, hover outline, and proximity voice. Riding requires *Small* scale. The horse can be summoned from anywhere with the left X button; the cat is auto-mounted on approach.
- **Guide butterflies** — three real-time butterflies that take off along splined flight points when the player approaches while mounted.
- **Petal & pollen magic** — hold the right trigger over the giant flower to draw particles into a hovering sphere in front of your head, then release for one of six procedural bursts (`SpiralBloom`, `MathRibbon`, `TornadoVortex`, `AizawaFountain`, `DreamAttractor`, `GalaxyVeil`).
- **Mushroom planting** — tap to seed a single mushroom or hold for a charged ring of 5–8. Cultivate any existing mushroom with another trigger pull.
- **Fireworks finale** — aim at the magic mortar to launch a spiral fire-ribbon along a cubic-Bezier arc, then watch the point-cloud firework showcase.
- **Cherry orb** — a runtime-spawned crystal orb above the cherry tree; activating it plays the four-phase tree growth animation and a petal vortex.
- **Quest 3 Link comfort layer** — purpose-built locomotion comfort profile, recenter-on-hold, mount-aware view recentering, and a per-mode tunneling vignette.

---

## Tech Stack

| Area | Tool / Version |
| --- | --- |
| Engine | Unity `6000.3.12f1` (Unity 6) |
| Render pipeline | Universal Render Pipeline `17.3.0` |
| Stereo rendering | Single Pass Instanced |
| XR runtime | OpenXR `1.16.1` via XR Management `4.5.4` |
| Interaction | XR Interaction Toolkit `3.3.1`, XR Hands `1.7.3` |
| Input | Unity Input System `1.19.0` |
| Scripting backend | IL2CPP (release), Mono (editor) |
| Target headset | Meta Quest 3 over Link Cable, Windows PC VR |
| Frame pacing | 72 Hz minimum, 90 Hz target |

---

## Quick Start

### Prerequisites

- Windows 10/11 PC with a VR-capable GPU
- Meta Quest 3 + Link Cable (or compatible USB-C cable supporting Quest Link)
- [Meta Quest Link](https://www.meta.com/quest/setup/) desktop app
- Unity `6000.3.12f1` (install via Unity Hub)
- Git with [Git LFS](https://git-lfs.com/) recommended for art assets

### Clone

```bash
git clone https://github.com/fury471/WONDERFULWORLD-VR.git
cd WONDERFULWORLD-VR
```

### Open in Unity

1. Launch **Unity Hub** → *Add project from disk* → select this folder.
2. Open with Unity `6000.3.12f1`. Let the editor import for the first run (Library/ rebuilds locally).
3. Confirm there are no compile errors in the Console.
4. Open the scene **[`Assets/_Project/World/Persistent/World_WonderlandPark.unity`](Assets/_Project/World/Persistent/World_WonderlandPark.unity)**.

### Play on Quest 3 (Link)

1. Connect Quest 3 with a Link Cable and confirm the headset is detected by the *Meta Quest Link* desktop app.
2. Enter **Quest Link** from inside the headset.
3. In Unity, press **Play**. The XR Origin should track your head and hands.
4. Or run the prebuilt Windows binary at [`Builds/Windows/WONDERFULWORLD.exe`](Builds/Windows/WONDERFULWORLD.exe).

> The closeout build target is **Windows / x86_64 / IL2CPP / Linear color space / Single Pass Instanced**.

---

## Controls Cheat Sheet

### Global

| Action | Input |
| --- | --- |
| Teleport (default) | Push left thumbstick forward → release |
| Smooth move (alt.) | Push left thumbstick |
| Snap turn (default) | Push right thumbstick left/right (30° step) |
| Smooth turn (alt.) | Push right thumbstick left/right |
| Scale: Normal ↔ Small | Right thumbstick **double-click** |
| Scale: Normal ↔ Large | Right thumbstick **long-press 0.45 s** |
| Recenter view | Hold **right B** for 0.40 s |
| Summon horse | Press **left X** |
| Pause / system menu | Reserved for **left Menu** (right Menu is owned by the Oculus shell) |

### Interactions (Right trigger on a controller ray)

| Where | Effect |
| --- | --- |
| Lotus pad | Plays one of seven notes; wobbles the leaf and ripples the water |
| Flower (hold) | Charges a particle sphere; release for a procedural bloom |
| Mushroom zone | Tap to seed one mushroom; hold-then-release for a ring of 5–8 |
| Existing mushroom | Tap to cultivate (+0.35× scale, up to 2.4×) |
| Firework mortar | Sends a fire ribbon into the device and triggers the showcase |
| Cherry orb | Collapses the orb and plays the tree growth + petal vortex |
| Mount (Small scale only) | Right A dismounts; left stick moves, right stick turns |

Full reference: [`Docs/InteractionBindings.md`](Docs/InteractionBindings.md).

---

## Project Structure

```text
Assets/
  _Project/              # All team-owned content lives here
    Art/                 # Shaders, materials, textures, props
    Audio/               # Music, SFX, ambient loops
    Characters/          # Creature-specific assets
    Core/                # Shared runtime systems (XR rig, comfort profile, recenter)
    Editor/              # In-editor production tooling
    Features/            # Modular gameplay systems (one folder each)
      CherryGarden/      #   - runtime crystal orb + tree growth + petal vortex
      Fireworks/         #   - magic mortar + launch pad + showcase
      Growth/            #   - mushroom seed zone + cultivation
      LotusPond/         #   - 7-note music sequencer
      Mounts/            #   - cat/dog/horse ride controllers + guide butterflies
      ParticleVitality/  #   - petal/pollen magic
      ScaleShift/        #   - Normal/Small/Large player scaling
      Weather/           #   - weather presets + regional response
    UI/                  # World-space UI, notice boards, localization, system menu
    World/               # Master scene, terrain, regions, shared world art
      Persistent/        #   - World_WonderlandPark.unity (the production scene)
      Regions/           #   - Per-region staging content (FlowerField, LotusPond, ...)
      Shared/            #   - Lighting/audio/materials reused across the park
Builds/Windows/          # Last shipped Windows build
Docs/                    # Production documentation (English)
Packages/                # Unity package manifest
ProjectSettings/         # Unity project settings (Linear, SPI, IL2CPP, etc.)
```

Third-party content (Toon Fantasy Nature, NamuFX, ithappy, XR Interaction Toolkit samples) stays inside its vendor folders and is referenced — not copied — from the production scene.

---

## Performance Targets

The runtime target is Quest 3 over Link Cable. Frame pacing matters more than average FPS — any dropped frames, tearing, black flicker, or jitter is treated as a release blocker.

| Metric | Minimum | Target |
| --- | --- | --- |
| Stable headset refresh | 72 Hz | 90 Hz |
| Render scale | 1.0 | 1.0 |
| MSAA | 4× | 4× |
| HDR | off | off |
| Opaque texture | off (unless required) | off |
| SRP Batcher | on | on |
| Stereo rendering | Single Pass Instanced | Single Pass Instanced |

Profiling and triage workflow: [`Docs/VR_PERFORMANCE_GUIDE.md`](Docs/VR_PERFORMANCE_GUIDE.md).

---

## Documentation

All maintained docs live in [`Docs/`](Docs/) and are English-only by policy:

- [Project Overview](Docs/PROJECT_OVERVIEW.md) — product framing, target platform, current scene, feature inventory
- [Build & Run](Docs/BUILD_AND_RUN.md) — Unity version, Quest 3 Link workflow, smoke test steps
- [System Structure](Docs/SYSTEM_STRUCTURE.md) — folder layout, main scene hierarchy, core prefabs, runtime systems
- [Interaction Bindings](Docs/InteractionBindings.md) — every player-facing interactable in the production scene
- [Cleanup & Standardization](Docs/CLEANUP_AND_STANDARDIZATION.md) — hierarchy, asset, naming, and documentation rules
- [Asset Reference Audit](Docs/Asset_Reference_Audit.md) — current external dependency snapshot
- [VR Performance Guide](Docs/VR_PERFORMANCE_GUIDE.md) — profiling workflow, target budgets, triage steps
- [Scale Shift Controller Flow](Docs/ScaleShiftCharacterControllerFlow.md) — the safe `CharacterController` mutation order during scale changes
- [Final Release Checklist](Docs/FINAL_RELEASE_CHECKLIST.md) — Editor, Play Mode, and Quest 3 Link signoff steps

---

## Editor Tooling

Production editor tools live under the Unity menu **Wonderful World > Production**:

- *Create Standard Project Folders*
- *Generate Production Audit*
- *Generate Asset Reference Audit*
- *Internalize Referenced Temp Art*
- *Normalize Main Scene Hierarchy*

Always move and rename Unity assets through the **Project window** or `AssetDatabase` — never through the operating system — so `.meta` files and GUID references survive.

---

## Credits

Wonderland is built on top of generously licensed third-party content. The major pieces are:

- **Toon Fantasy Nature** — stylized environment art (trees, rocks, pavilions, swings, decorations).
- **NamuFX – Stylized Water Effects** — water materials, ripples, splashes, and bubble effects.
- **ithappy – Animals FREE** — cat, dog, and horse meshes, materials, and animation controllers.
- **Unity XR Interaction Toolkit – Starter Assets** and **XR Device Simulator** — controller prefabs, teleport reticle, tunneling vignette source, hand expression captures.
- **Liberation Sans (TextMesh Pro)** — fallback fonts.
- **Butterfly (Ulysses)** — base butterfly mesh and animation controller.
- **freesound.org – `jaz_the_man_2`** — lotus pond note samples (`do`, `re`, `mi`, `fa`, `sol`, `la`, `si`).

All vendor content stays inside its original folder under `Assets/`. See [`Docs/Asset_Reference_Audit.md`](Docs/Asset_Reference_Audit.md) for the full dependency snapshot.

---

## Contributing

This repository is the production source for the v1.0.0 closeout build. Before opening a pull request:

1. Branch from `main`.
2. Open the project in Unity `6000.3.12f1` and confirm zero compile errors.
3. Run **Wonderful World > Production > Generate Production Audit** and **Generate Asset Reference Audit**.
4. Run the [smoke test](Docs/BUILD_AND_RUN.md#smoke-test) through Quest 3 Link.
5. Commit hierarchy, asset organization, documentation, and performance work in separate commits where possible.

---

## License

Project source and team-authored assets are © the Wonderland team. Third-party assets remain under their respective licenses — see each vendor folder under `Assets/` and the [Asset Reference Audit](Docs/Asset_Reference_Audit.md).

---

*Made with care for the headset.*
