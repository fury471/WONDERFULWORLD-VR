using System;
using System.Collections;
using UnityEngine;

public enum WeatherType
{
    Clear,
    Overcast,
    Rain,
    Windy
}

[CreateAssetMenu(fileName = "WeatherPreset_", menuName = "WonderfulWorld/Weather/Weather Preset")]
public class WeatherPreset : ScriptableObject
{
    [Header("Identity")]
    public WeatherType weatherType;

    [Header("Sky")]
    public Material skyboxMaterial;

    [Header("Lighting")]
    public Color directionalLightColor = Color.white;
    [Range(0f, 8f)] public float directionalLightIntensity = 1f;
    public Color ambientLightColor = Color.gray;

    [Header("Fog")]
    public bool fogEnabled = true;
    public Color fogColor = Color.gray;
    [Range(0f, 0.1f)] public float fogDensity = 0.01f;

    [Header("Particles")]
    [Range(0f, 1f)] public float particleEmissionMultiplier = 0f;
    public bool rainParticlesEnabled;
    public bool windParticlesEnabled;

    [Header("Audio And Wind")]
    [Range(0f, 1f)] public float ambienceVolume = 1f;
    [Range(0f, 1f)] public float windStrength = 0f;
}

public class WeatherManager : MonoBehaviour
{
    [Header("Presets")]
    [SerializeField] private WeatherPreset clearPreset;
    [SerializeField] private WeatherPreset overcastPreset;
    [SerializeField] private WeatherPreset rainPreset;
    [SerializeField] private WeatherPreset windyPreset;

    [Header("Global Targets")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private ParticleSystem rainParticles;
    [SerializeField] private ParticleSystem windParticles;

    [Header("Blend")]
    [SerializeField] private float transitionDuration = 2.5f;
    [SerializeField] private WeatherType startingWeather = WeatherType.Clear;

    public event Action<WeatherPreset> WeatherChanged;

    public WeatherPreset CurrentPreset { get; private set; }
    public float CurrentWindStrength { get; private set; }

    private Coroutine transitionRoutine;

    private void Start()
    {
        WeatherPreset startingPreset = GetPreset(startingWeather);
        if (startingPreset == null)
        {
            Debug.LogWarning("[Weather] Missing starting preset.");
            return;
        }

        ApplyImmediate(startingPreset);
    }

    public void SetClear()
    {
        SetWeather(WeatherType.Clear);
    }

    public void SetOvercast()
    {
        SetWeather(WeatherType.Overcast);
    }

    public void SetRain()
    {
        SetWeather(WeatherType.Rain);
    }

    public void SetWindy()
    {
        SetWeather(WeatherType.Windy);
    }

    public void SetWeather(WeatherType weatherType)
    {
        WeatherPreset targetPreset = GetPreset(weatherType);
        if (targetPreset == null)
        {
            Debug.LogWarning($"[Weather] Missing preset for {weatherType}.");
            return;
        }

        if (CurrentPreset == targetPreset)
            return;

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(BlendToPreset(targetPreset));
    }

    private IEnumerator BlendToPreset(WeatherPreset targetPreset)
    {
        if (CurrentPreset == null)
        {
            ApplyImmediate(targetPreset);
            yield break;
        }

        WeatherPreset sourcePreset = CurrentPreset;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, transitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyBlended(sourcePreset, targetPreset, t);
            yield return null;
        }

        ApplyImmediate(targetPreset);
        transitionRoutine = null;
    }

    private void ApplyImmediate(WeatherPreset preset)
    {
        CurrentPreset = preset;
        ApplySkybox(preset);
        ApplyLighting(preset);
        ApplyFog(preset);
        ApplyParticles(preset);
        CurrentWindStrength = preset.windStrength;

        WeatherChanged?.Invoke(preset);

        Debug.Log($"[Weather] Applied preset {preset.weatherType}");
    }

    private void ApplyBlended(WeatherPreset from, WeatherPreset to, float t)
    {
        if (t >= 0.5f && to.skyboxMaterial != null && RenderSettings.skybox != to.skyboxMaterial)
        {
            RenderSettings.skybox = to.skyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }

        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(from.directionalLightColor, to.directionalLightColor, t);
            directionalLight.intensity = Mathf.Lerp(from.directionalLightIntensity, to.directionalLightIntensity, t);
        }

        RenderSettings.ambientLight = Color.Lerp(from.ambientLightColor, to.ambientLightColor, t);
        RenderSettings.fog = t < 0.5f ? from.fogEnabled : to.fogEnabled;
        RenderSettings.fogColor = Color.Lerp(from.fogColor, to.fogColor, t);
        RenderSettings.fogDensity = Mathf.Lerp(from.fogDensity, to.fogDensity, t);

        CurrentWindStrength = Mathf.Lerp(from.windStrength, to.windStrength, t);

        ApplyParticleBlend(rainParticles, from.rainParticlesEnabled, to.rainParticlesEnabled, from.particleEmissionMultiplier, to.particleEmissionMultiplier, t);
        ApplyParticleBlend(windParticles, from.windParticlesEnabled, to.windParticlesEnabled, from.particleEmissionMultiplier, to.particleEmissionMultiplier, t);
    }

    private void ApplySkybox(WeatherPreset preset)
    {
        if (preset.skyboxMaterial == null)
            return;

        if (RenderSettings.skybox != preset.skyboxMaterial)
        {
            RenderSettings.skybox = preset.skyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }
    }

    private void ApplyLighting(WeatherPreset preset)
    {
        if (directionalLight != null)
        {
            directionalLight.color = preset.directionalLightColor;
            directionalLight.intensity = preset.directionalLightIntensity;
        }

        RenderSettings.ambientLight = preset.ambientLightColor;
    }

    private void ApplyFog(WeatherPreset preset)
    {
        RenderSettings.fog = preset.fogEnabled;
        RenderSettings.fogColor = preset.fogColor;
        RenderSettings.fogDensity = preset.fogDensity;
    }

    private void ApplyParticles(WeatherPreset preset)
    {
        ApplyParticleImmediate(rainParticles, preset.rainParticlesEnabled, preset.particleEmissionMultiplier);
        ApplyParticleImmediate(windParticles, preset.windParticlesEnabled, preset.particleEmissionMultiplier);
    }

    private static void ApplyParticleImmediate(ParticleSystem particleSystem, bool enabledState, float emissionMultiplier)
    {
        if (particleSystem == null)
            return;

        var emission = particleSystem.emission;
        emission.rateOverTimeMultiplier = enabledState ? Mathf.Max(1f, emissionMultiplier * 100f) : 0f;

        if (enabledState && !particleSystem.isPlaying)
            particleSystem.Play();
        else if (!enabledState && particleSystem.isPlaying)
            particleSystem.Stop();
    }

    private static void ApplyParticleBlend(
        ParticleSystem particleSystem,
        bool sourceEnabled,
        bool targetEnabled,
        float sourceMultiplier,
        float targetMultiplier,
        float t)
    {
        if (particleSystem == null)
            return;

        float sourceRate = sourceEnabled ? Mathf.Max(1f, sourceMultiplier * 100f) : 0f;
        float targetRate = targetEnabled ? Mathf.Max(1f, targetMultiplier * 100f) : 0f;

        var emission = particleSystem.emission;
        emission.rateOverTimeMultiplier = Mathf.Lerp(sourceRate, targetRate, t);

        bool shouldPlay = emission.rateOverTimeMultiplier > 0.01f;
        if (shouldPlay && !particleSystem.isPlaying)
            particleSystem.Play();
        else if (!shouldPlay && particleSystem.isPlaying)
            particleSystem.Stop();
    }

    private WeatherPreset GetPreset(WeatherType weatherType)
    {
        switch (weatherType)
        {
            case WeatherType.Overcast:
                return overcastPreset;
            case WeatherType.Rain:
                return rainPreset;
            case WeatherType.Windy:
                return windyPreset;
            default:
                return clearPreset;
        }
    }
}
