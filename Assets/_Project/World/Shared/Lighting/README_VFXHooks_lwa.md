# VFX Hooks (M2 - Wenao)

This folder contains **decoupled visual/VFX hooks** for signature systems, meant for M3 integration into an exploration-first magical park slice.

Current demonstration direction:

- use VFX hooks to support attraction identity, skyline spectacle, land readability, and soft-boundary atmosphere
- avoid using them as hard mission rails or rigid sequence controllers

## What you get

- `WeatherVfxHooks_lwa` : listens to `WeatherManager.WeatherChanged` and outputs
  - global shader params (`_WW_*`), optional
  - `WeatherVfxChannel_SO_lwa` ScriptableObject event channel, optional
- `FireworksVfxHooks_lwa` : listens to `FireworkController.PatternSpawned` and outputs
  - global shader params (`_WW_*`), optional
  - `FireworkVfxChannel_SO_lwa` event channel, optional
- `LotusVfxHooks_lwa` : listens to `LotusNoteTrigger.NoteTriggered` / `LotusRippleController.RippleStarted` and outputs
  - global shader params (`_WW_*`), optional
  - `LotusVfxChannel_SO_lwa` event channel, optional

## Shader interface

Global shader properties are defined in:

- `Assets/_Project/Art/Shaders/VFXHooks_lwa/WonderfulWorldVfxGlobals_lwa.hlsl`

and written from C# via:

- `WonderfulWorldVfxShaderGlobals_lwa`

Property names (stable):

- `_WW_WeatherParams` : `(weatherType, windStrength, particleEmissionMultiplier, fogDensity)`
- `_WW_WeatherFogColor` : `(fogColor.rgb, 1)`
- `_WW_WeatherAmbientColor` : `(ambient.rgb, 1)`
- `_WW_WeatherDirectionalColor` : `(dirLight.rgb, intensity)`
- `_WW_LastFireworkPosTime` : `(worldPos.xyz, time)`
- `_WW_LastFireworkColor` : `(color.rgba)`
- `_WW_LastLotusPosTime` : `(worldPos.xyz, time)`
