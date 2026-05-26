<p align="center">
  <img src="Docs/images/wonderland-cover.png" alt="Wonderland — Explore here and enjoy yourself" width="100%">
</p>

<p align="center"><em>MAMF45 — Virtual Reality in Theory and Practice</em></p>

# Wonderland

> *Explore here and enjoy yourself.*

A PC VR fantasy park — a first-person, comfort-first experience with butterflies, a lotus-pond music platform, particle magic, animal mounts, a hand-planted mushroom grove, a blossoming cherry tree, and fireworks.

**Languages:** **English** · [中文](README.zh-CN.md) · [Svenska](README.sv.md)

---

## About

Wonderland (internal name: *Wonderful World*) is a single-player VR exploration experience built in **Unity 6** with **OpenXR** and the **Universal Render Pipeline**. The player roams a seamless-feeling park slice made of seven connected attraction zones — each one a small piece of magic instead of a quest chain.

The design priorities are:

1. **Comfort first.** Stable frame pacing, per-mode tunneling vignette, teleport-by-default, blink scale transitions, no forced motion.
2. **Discoverable wonder.** Every zone is a self-contained interaction worth finding, not a checkpoint to complete.
3. **Stylised, not photoreal.** A cel-shaded look (Toon Fantasy Nature) over Single Pass Instanced URP.

The production scene is [`Assets/_Project/World/Persistent/World_WonderlandPark.unity`](Assets/_Project/World/Persistent/World_WonderlandPark.unity), released as **v1.0.0**.

---

## Highlights

- **Seven zones in one continuous park** — Welcome entry, Magical Particle Garden, Lotus Pond, Animal Forest (Cat Garden), Waterfall & Fireworks Ground, Mushroom Growth, Cherry Garden.
- **Three player scales** — switch between Normal, Small (0.25×), and Large (1.75×) via a right-thumbstick-click gesture (double-click within 0.32 s or long-press 0.45 s); eye height, movement speed, and interaction reach adapt automatically through a 0.4 s blink transition.
- **Lotus pond music sequencer** — seven floating lotus pads tuned to the *do · re · mi · fa · sol · la · si* diatonic major scale, played by firing curved water-magic projectiles from either controller. An eighth pad is a score starter that randomly selects a song for the player to play back.
- **Animal Forest mount system** — three independent mounts (Kitty, Dog, Horse), each with idle pacing, hover outline, and a proximity voice. Scale-gated per animal: **cat and dog require Small scale, the horse requires Normal scale**. The horse can be summoned from anywhere with **left X**.
- **Guide butterflies** — three real-time butterflies that take off along splined flight points when the player approaches **while riding the cat**.
- **Pink crystal — petal & pollen magic** — hold the right trigger over the magical crystal stone in the Particle Garden; particles flow into a sphere held in front of your head. After 3 s the release is "charged". Release for one of six procedural bursts: `SpiralBloom`, `MathRibbon`, `TornadoVortex`, `AizawaFountain`, `DreamAttractor`, `GalaxyVeil`.
- **Mushroom planting** — tap to seed a single mushroom (1.55 s earth-magic projectile flight). Hold ≥ 0.65 s and release for a charged ring of 5–8 mushrooms within a 4 m radius. Tap an existing mushroom to cultivate it (+0.35× scale, capped at 2.4×).
- **Fireworks mortar** — aim at the magic mortar (range 36 m); a spiral fire ribbon flies along a cubic-Bezier arc to the device, then triggers the point-cloud firework showcase.
- **Cherry crystal orb** — a glowing crystal floats above the cherry tree (radius 1.05 m, spawned by `CherryGardenCrystalOrbTrigger`). Right trigger collapses it in 0.72 s and plays the four-phase tree growth animation and a petal vortex.
- **Wooden swing** — sit on the wooden swing in the park (`TFF_Wooden_Swing_01A` under `Decorations/Swings`): aim the right ray at the **seat** + right trigger to sit down; pump the left thumbstick forward/back to build the pendulum arc (clamped to ±60° around the seat's local Z axis); right A to step off. View stays horizontally locked to the seat's initial forward — translation only, no roll. Right-B hold (0.40 s) re-centers you on the seat mid-ride. **Normal scale only.** Driven by [`QuestSwingRideController`](Assets/_Project/Features/Mounts/Runtime/QuestSwingRideController.cs).
- **Quest 3 Link comfort layer** — purpose-built `QuestLocomotionComfortProfile`, hold-to-recenter on right B (0.40 s), mount-aware recenter, and a per-mode tunneling vignette.

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

> **Read this first.** This repository stores all binary assets (scenes, prefabs, materials, textures, audio, FBX models, animation `.asset` files) in **Git LFS**. A plain `git clone` will leave those files as ~100-byte pointer text and Unity will not be able to open the project. Follow the steps below in order.

### Step 0 — Install prerequisites (one-time per machine)

| What | Where / How |
| --- | --- |
| Hardware | Windows 10/11 PC with a VR-capable GPU + Meta Quest 3 + Link Cable (or any USB-C 3.0+ cable that supports Quest Link) |
| Unity Hub | <https://unity.com/download> |
| Unity Editor `6000.3.12f1` | Install from Unity Hub → **Installs → Install Editor**. In the modules step, **tick `Windows Build Support (IL2CPP)`**. (You can also tick *Documentation* and your preferred IDE.) |
| Git for Windows | <https://git-scm.com/download/win> |
| Git LFS | <https://git-lfs.com/> — after installing, open any terminal and run `git lfs install` once. |
| Meta Quest Link desktop app | <https://www.meta.com/quest/setup/> |

### Step 1 — Clone the repo *with LFS content*

Open a terminal (PowerShell, Git Bash, or Windows Terminal) in the folder where you want the project to live, then:

```bash
git lfs install                                              # one-time per machine; safe to re-run
git clone https://github.com/fury471/WONDERFULWORLD-VR.git
cd WONDERFULWORLD-VR
git lfs pull                                                 # download all LFS-tracked binary assets
```

Expected total download: **~2–3 GB**. The clone will finish quickly; `git lfs pull` is the long step.

**Sanity check.** Once `git lfs pull` finishes, the production scene should be a real binary file, not a pointer:

```bash
# PowerShell
(Get-Item Assets/_Project/World/Persistent/World_WonderlandPark.unity).Length
# Git Bash / WSL
wc -c < Assets/_Project/World/Persistent/World_WonderlandPark.unity
```

A healthy result is several **megabytes**. If you see only a few hundred bytes, LFS did not pull — run `git lfs pull` again.

> Already cloned without LFS? You don't need to re-clone. Just enter the folder and run `git lfs install && git lfs pull`.

### Step 2 — Open the project in Unity

1. Launch **Unity Hub** → **Add** → **Add project from disk** → select the `WONDERFULWORLD-VR` folder.
2. The project card will show the required editor version, `6000.3.12f1`. If it isn't installed yet, Unity Hub offers to install it — accept, and **make sure `Windows Build Support (IL2CPP)` is ticked** in the modules list.
3. Click the project to open it. The first import builds the local `Library/` from scratch and **typically takes 10–30 minutes** depending on disk and CPU speed. **Do not close Unity during this import.**
4. When import finishes, look at the **Console** window — there should be **no compile errors**.
5. In the **Project** window, double-click [`Assets/_Project/World/Persistent/World_WonderlandPark.unity`](Assets/_Project/World/Persistent/World_WonderlandPark.unity) to load the production scene.

### Step 3 — Play on Quest 3 via Link

1. Plug your Quest 3 into the PC with a Link Cable (or any USB-C 3.0+ cable that supports Quest Link).
2. Open the **Meta Quest Link** desktop app on Windows and confirm the headset is detected (status: *Connected*).
3. Put on the headset. Accept the **"Enable Quest Link?"** prompt, or open the headset's universal menu → **Quick Settings → Quest Link** and start a session.
4. Switch back to Unity on the PC and press **▶ Play**. Put the headset on within a few seconds — the XR Origin should track your head and both hands.

### (Optional) Make a Windows build

The repo does not ship a prebuilt binary (`Builds/` is gitignored). To produce one yourself:

1. In Unity, open **File → Build Profiles** (or **Build Settings**).
2. Select **Windows, Mac, Linux** with target platform **Windows** and architecture **x86_64**.
3. Confirm **Scripting Backend = IL2CPP** and **Color Space = Linear** in *Project Settings → Player*.
4. Click **Build** and pick an output folder (the suggested path is `Builds/Windows/`).

> Build target: **Windows / x86_64 / IL2CPP / Linear colour space / Single Pass Instanced**.

### Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| Pink/magenta materials, missing scripts, "Could not extract GUID" errors | LFS objects were not pulled | Run `git lfs install` then `git lfs pull` inside the repo, then re-import the project (right-click `Assets/` → *Reimport*) |
| Unity Hub says the editor version is missing | `6000.3.12f1` is not installed | Install it via Unity Hub → **Installs → Install Editor**, and tick **Windows Build Support (IL2CPP)** |
| The headset is not detected by Quest Link | Cable is USB-C 2.0, Link is disabled, or driver hiccup | Use a Quest Link or USB-C 3.0+ cable; in the headset, enable *Settings → System → Quest Link*; restart the Meta Quest Link desktop app |
| Lots of compile errors right after first open | Stale `Library/` or partial import | Close Unity, delete `Library/`, `Temp/`, `obj/`, reopen the project, let it fully import |
| Black flicker, tearing, or low frame rate inside the headset | Performance / settings issue | See the triage flow in [`Docs/VR_PERFORMANCE_GUIDE.md`](Docs/VR_PERFORMANCE_GUIDE.md) |
| `git lfs pull` is slow or stalls | LFS bandwidth or network issue | Re-run `git lfs pull`; LFS resumes from where it left off |

---

## Controls Cheat Sheet

### Global

| Action | Input |
| --- | --- |
| Teleport (default) | Push **left thumbstick** forward → release |
| Smooth move (alt.) | Push **left thumbstick** (`smoothMoveSpeed = 1.6 m/s`) |
| Snap turn (default) | **Right thumbstick** left/right (`snapTurnAmount = 30°`) |
| Smooth turn (alt.) | **Right thumbstick** left/right (`smoothTurnSpeed = 45°/s`) |
| Scale: Normal ↔ Small | **Right thumbstick click** — double-click within 0.32 s |
| Scale: Normal ↔ Large | **Right thumbstick click** — long-press ≥ 0.45 s |
| Recenter view | **Hold right B** for 0.40 s |
| Summon horse | Press **left X** |
| System menu | Press **left Menu** (right Menu is owned by the Oculus shell) |

### Zone interactions (controller ray + right trigger)

| Target | Effect |
| --- | --- |
| Lotus pad | Plays one of seven notes; wobbles the leaf and ripples the water |
| Particle crystal (tap or hold) | Charges a held particle sphere; release for a procedural burst |
| Mushroom zone ground | Tap to seed 1 mushroom; hold-and-release for a 5–8 ring |
| Existing mushroom | Tap to cultivate (+0.35× scale, capped 2.4×) |
| Firework mortar | Sends a fire ribbon into the device and triggers the showcase |
| Cherry crystal | Collapses the orb and plays tree growth + petal vortex |
| Mount | Right trigger to mount (per-animal scale gate); right A to dismount; left stick moves, right stick turns |
| Wooden swing seat | Right trigger to sit (Normal scale only, ray must hit the seat board); left thumbstick forward/back to pump (±60° pendulum); right A to step off |

Full reference: [`Docs/InteractionBindings.md`](Docs/InteractionBindings.md).

---

## Project Structure

```text
Assets/
  _Project/              # All team-owned content lives here
    Art/                 # Shaders, materials, textures, props
    Audio/               # Music, SFX, ambient loops
    Characters/          # Creature-specific assets
    Core/                # Shared runtime systems
      Runtime/           #   - GameFlowManager, ParkAttractionState
      XR/                #   - XR rig, locomotion comfort profile, recenter, ray broker, haptics, performance bootstrap
    Editor/              # In-editor production tooling
    Features/            # Modular gameplay systems (one folder each)
      CherryGarden/      #   - runtime crystal orb + tree growth + petal vortex
      Fireworks/         #   - magic mortar + launch pad + point-cloud showcase
      Growth/            #   - mushroom seed zone + cultivation
      LotusPond/         #   - 7-note diatonic music sequencer
      Mounts/            #   - cat/dog/horse ride controllers, horse summon, guide butterflies
      ParticleVitality/  #   - pink crystal: petal/pollen magic
      ScaleShift/        #   - Normal/Small/Large player scaling
      Weather/           #   - weather presets + regional response
    UI/                  # World-space UI: WelcomePanel, system menu, notice boards, localisation (EN/ZH/SV)
    World/               # Master scene, terrain, regions, shared world art
      Persistent/        #   - World_WonderlandPark.unity (the production scene)
      Regions/           #   - Per-region staging content
        CatRoute/        #     (in-scene root: Region_CatGarden)
        FireworksClearing/  #  (in-scene root: Region_FireworksClearing — waterfall + fireworks)
        FlowerField/     #     (in-scene root: Region_FlowerGarden — pink crystal)
        HumanEntry/      #     (staging content; entry is realised via UI/WelcomePanel)
        LotusPond/       #     (in-scene root: Region_LotusPond)
        MushroomGrove/   #     (in-scene root: Region_MushroomGrowth)
        Terrain/         #     (terrain tile content)
      Shared/            #   - Lighting/audio/materials reused across the park
Builds/Windows/          # Last shipped Windows build (WONDERFULWORLD.exe)
Docs/                    # Production documentation (English)
Packages/                # Unity package manifest
ProjectSettings/         # Unity project settings (Linear, SPI, IL2CPP, etc.)
```

Third-party content (Toon Fantasy Nature, NamuFX, ithappy, XR Interaction Toolkit samples) stays inside its vendor folders and is **referenced**, not copied, from the production scene.

---

## Performance Targets

The runtime target is Quest 3 over Link Cable.

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

- [Project Overview](Docs/PROJECT_OVERVIEW.md) — product framing, target platform, current scene, region inventory
- [Build & Run](Docs/BUILD_AND_RUN.md) — Unity version, Quest 3 Link workflow, smoke-test steps
- [System Structure](Docs/SYSTEM_STRUCTURE.md) — folder layout, main scene hierarchy, core prefabs, runtime systems
- [Interaction Bindings](Docs/InteractionBindings.md) — every player-facing interactable in the production scene, cross-checked against the scripts
- [Cleanup & Standardisation](Docs/CLEANUP_AND_STANDARDIZATION.md) — hierarchy, asset, naming, and documentation rules
- [Asset Reference Audit](Docs/Asset_Reference_Audit.md) — current external dependency snapshot
- [VR Performance Guide](Docs/VR_PERFORMANCE_GUIDE.md) — profiling workflow, target budgets, triage steps
- [Scale Shift Controller Flow](Docs/ScaleShiftCharacterControllerFlow.md) — safe `CharacterController` mutation order during scale changes
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

Wonderland uses generously licensed third-party content. The major pieces are:

- **Toon Fantasy Nature** — stylised environment art (trees, rocks, pavilions, swings, decorations).
- **NamuFX – Stylized Water Effects** — water materials, ripples, splashes, and bubble effects.
- **ithappy – Animals FREE** — cat, dog, and horse meshes, materials, and animation controllers.
- **Unity XR Interaction Toolkit – Starter Assets** and **XR Device Simulator** — controller prefabs, teleport reticle, tunneling vignette source, hand-expression captures.
- **Liberation Sans (TextMesh Pro)** — fallback fonts.
- **Butterfly (Ulysses)** — base butterfly mesh and animation controller.
- **freesound.org – `jaz_the_man_2`** — lotus pond note samples (`do`, `re`, `mi`, `fa`, `sol`, `la`, `si`).

All vendor content stays inside its original folder under `Assets/`. See [`Docs/Asset_Reference_Audit.md`](Docs/Asset_Reference_Audit.md) for the full dependency snapshot.

---

## Contributing

This repository is the production source for the v1.0.0 release. Before opening a pull request:

1. Branch from `main`.
2. Open the project in Unity `6000.3.12f1` and confirm zero compile errors.
3. Run **Wonderful World > Production > Generate Production Audit** and **Generate Asset Reference Audit**.
4. Run the [smoke test](Docs/BUILD_AND_RUN.md#smoke-test) through Quest 3 Link.
5. Commit hierarchy, asset organisation, documentation, and performance work in separate commits where possible.

---

## License

Released under the [MIT License](LICENSE). Third-party assets remain under their respective licenses — see each vendor folder under `Assets/` and the [Asset Reference Audit](Docs/Asset_Reference_Audit.md).
