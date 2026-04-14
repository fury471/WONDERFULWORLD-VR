# Features

Each major mechanic should live in its own folder.

Current demonstration direction:

- systems should behave like discoverable attractions or environmental affordances inside an exploration-first magical park
- route references in milestone docs are readability guides, not required mission sequencing

## How to use this folder

- Put feature code in `Runtime`.
- Put reusable gameplay prefabs in `Prefabs`.
- Put data assets in `ScriptableObjects`.
- Put isolated feature tests in `Tests`.
- Keep each feature as independent as possible from unrelated features.
- Design each feature so it can be discovered in flexible order unless a later milestone explicitly requires stronger sequencing.

## Current feature modules

- `ScaleShift`
- `Weather`
- `Growth`
- `ParticleVitality`
- `Fireworks`
- `Mounts`
- `LotusPond`

## Collaboration rule

If a script is used only by one mechanic, keep it inside that mechanic's folder instead of moving it to `Core` too early.
