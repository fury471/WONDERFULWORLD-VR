using System;
using UnityEngine;

namespace WonderfulWorld.World.Shared.VfxHooks
{
    [Serializable]
    public struct WeatherVfxEvent
    {
        public WeatherType weatherType;
        public float windStrength;
        public float particleEmissionMultiplier;
        public bool rainParticlesEnabled;
        public bool windParticlesEnabled;
        public Color fogColor;
        public float fogDensity;
        public Color ambientLightColor;
        public Color directionalLightColor;
        public float directionalLightIntensity;
    }

    [Serializable]
    public struct FireworkVfxEvent
    {
        public string patternName;
        public int patternIndex;
        public Vector3 worldPosition;
        public Color color;
        public float sizeMultiplier;
        public float time;
    }

    [Serializable]
    public struct LotusVfxEvent
    {
        public Vector3 worldPosition;
        public float time;
        public float cooldownSeconds;
    }

    public static class VfxHookDefaults
    {
        public const string WeatherMenuRoot = "WonderfulWorld/VFX Hooks/Weather";
        public const string FireworksMenuRoot = "WonderfulWorld/VFX Hooks/Fireworks";
        public const string LotusMenuRoot = "WonderfulWorld/VFX Hooks/Lotus";
    }
}
