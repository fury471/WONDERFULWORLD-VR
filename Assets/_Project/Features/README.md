# Features

Each major mechanic should live in its own folder.

## How to use this folder

- Put feature code in `Runtime`.
- Put reusable gameplay prefabs in `Prefabs`.
- Put data assets in `ScriptableObjects`.
- Put isolated feature tests in `Tests`.
- Keep each feature as independent as possible from unrelated features.

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
