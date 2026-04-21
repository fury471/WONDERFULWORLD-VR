#ifndef WONDERFULWORLD_VFX_GLOBALS_LWA_INCLUDED
#define WONDERFULWORLD_VFX_GLOBALS_LWA_INCLUDED

// Weather
// _WW_WeatherParams: x=WeatherType, y=WindStrength, z=ParticleEmissionMultiplier, w=FogDensity
float4 _WW_WeatherParams;
float4 _WW_WeatherFogColor;
float4 _WW_WeatherAmbientColor;
// _WW_WeatherDirectionalColor: rgb=DirectionalLightColor, a=DirectionalLightIntensity
float4 _WW_WeatherDirectionalColor;

// Fireworks
// _WW_LastFireworkPosTime: xyz=WorldPosition, w=Time.time
float4 _WW_LastFireworkPosTime;
float4 _WW_LastFireworkColor;

// Lotus
// _WW_LastLotusPosTime: xyz=WorldPosition, w=Time.time
float4 _WW_LastLotusPosTime;

#endif
