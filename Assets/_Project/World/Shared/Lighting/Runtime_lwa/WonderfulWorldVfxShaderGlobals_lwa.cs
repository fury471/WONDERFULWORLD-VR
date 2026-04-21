using UnityEngine;

namespace WonderfulWorld.World.Shared.VfxHooks
{
    public static class WonderfulWorldVfxShaderGlobals_lwa
    {
        public const string WeatherParamsName = "_WW_WeatherParams";
        public const string WeatherFogColorName = "_WW_WeatherFogColor";
        public const string WeatherAmbientColorName = "_WW_WeatherAmbientColor";
        public const string WeatherDirectionalColorName = "_WW_WeatherDirectionalColor";

        public const string LastFireworkPosTimeName = "_WW_LastFireworkPosTime";
        public const string LastFireworkColorName = "_WW_LastFireworkColor";

        public const string LastLotusPosTimeName = "_WW_LastLotusPosTime";

        public static readonly int WeatherParamsId = Shader.PropertyToID(WeatherParamsName);
        public static readonly int WeatherFogColorId = Shader.PropertyToID(WeatherFogColorName);
        public static readonly int WeatherAmbientColorId = Shader.PropertyToID(WeatherAmbientColorName);
        public static readonly int WeatherDirectionalColorId = Shader.PropertyToID(WeatherDirectionalColorName);
        public static readonly int LastFireworkPosTimeId = Shader.PropertyToID(LastFireworkPosTimeName);
        public static readonly int LastFireworkColorId = Shader.PropertyToID(LastFireworkColorName);
        public static readonly int LastLotusPosTimeId = Shader.PropertyToID(LastLotusPosTimeName);

        public static void SetWeather(in WeatherVfxEvent evt)
        {
            Shader.SetGlobalVector(
                WeatherParamsId,
                new Vector4((float)evt.weatherType, evt.windStrength, evt.particleEmissionMultiplier, evt.fogDensity));
            Shader.SetGlobalVector(WeatherFogColorId, evt.fogColor);
            Shader.SetGlobalVector(WeatherAmbientColorId, evt.ambientLightColor);
            Shader.SetGlobalVector(
                WeatherDirectionalColorId,
                new Vector4(evt.directionalLightColor.r, evt.directionalLightColor.g, evt.directionalLightColor.b, evt.directionalLightIntensity));
        }

        public static void SetLastFirework(in FireworkVfxEvent evt)
        {
            Shader.SetGlobalVector(
                LastFireworkPosTimeId,
                new Vector4(evt.worldPosition.x, evt.worldPosition.y, evt.worldPosition.z, evt.time));
            Shader.SetGlobalVector(LastFireworkColorId, evt.color);
        }

        public static void SetLastLotus(in LotusVfxEvent evt)
        {
            Shader.SetGlobalVector(
                LastLotusPosTimeId,
                new Vector4(evt.worldPosition.x, evt.worldPosition.y, evt.worldPosition.z, evt.time));
        }
    }
}
