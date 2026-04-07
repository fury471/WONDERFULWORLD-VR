using UnityEngine;

public class RegionWeatherResponder : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private WeatherManager weatherManager;

    [Header("Optional Targets")]
    [SerializeField] private Light regionLight;
    [SerializeField] private ParticleSystem regionParticleSystem;
    [SerializeField] private Renderer[] renderersToTint;
    [SerializeField] private Color clearTint = Color.white;
    [SerializeField] private Color overcastTint = new Color(0.8f, 0.82f, 0.86f);
    [SerializeField] private Color rainTint = new Color(0.7f, 0.78f, 0.9f);
    [SerializeField] private Color windyTint = new Color(0.92f, 0.9f, 0.75f);

    [Header("Optional Gameplay Hook")]
    [SerializeField] private GameObject weatherLockedObject;
    [SerializeField] private WeatherType unlockWeather = WeatherType.Rain;

    private MaterialPropertyBlock propertyBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void OnEnable()
    {
        if (weatherManager != null)
            weatherManager.WeatherChanged += ApplyWeather;
    }

    private void Start()
    {
        propertyBlock ??= new MaterialPropertyBlock();

        if (weatherManager != null && weatherManager.CurrentPreset != null)
            ApplyWeather(weatherManager.CurrentPreset);
    }

    private void OnDisable()
    {
        if (weatherManager != null)
            weatherManager.WeatherChanged -= ApplyWeather;
    }

    public void ApplyWeather(WeatherPreset preset)
    {
        if (preset == null)
            return;

        if (regionLight != null)
        {
            regionLight.color = preset.directionalLightColor;
            regionLight.intensity = preset.directionalLightIntensity;
        }

        if (regionParticleSystem != null)
        {
            var emission = regionParticleSystem.emission;
            emission.rateOverTimeMultiplier = Mathf.Max(0f, preset.particleEmissionMultiplier * 50f);

            if (emission.rateOverTimeMultiplier > 0.01f && !regionParticleSystem.isPlaying)
                regionParticleSystem.Play();
            else if (emission.rateOverTimeMultiplier <= 0.01f && regionParticleSystem.isPlaying)
                regionParticleSystem.Stop();
        }

        ApplyTint(GetTint(preset.weatherType));

        if (weatherLockedObject != null)
            weatherLockedObject.SetActive(preset.weatherType == unlockWeather);
    }

    private void ApplyTint(Color tint)
    {
        if (renderersToTint == null)
            return;

        foreach (Renderer targetRenderer in renderersToTint)
        {
            if (targetRenderer == null)
                continue;

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, tint);
            propertyBlock.SetColor(ColorId, tint);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private Color GetTint(WeatherType weatherType)
    {
        switch (weatherType)
        {
            case WeatherType.Overcast:
                return overcastTint;
            case WeatherType.Rain:
                return rainTint;
            case WeatherType.Windy:
                return windyTint;
            default:
                return clearTint;
        }
    }
}
