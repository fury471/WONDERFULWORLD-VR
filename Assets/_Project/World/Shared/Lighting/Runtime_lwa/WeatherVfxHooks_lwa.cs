using UnityEngine;

namespace WonderfulWorld.World.Shared.VfxHooks
{
    [DisallowMultipleComponent]
    public class WeatherVfxHooks_lwa : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private WeatherManager weatherManager;

        [Header("Outputs")]
        [SerializeField] private bool setShaderGlobals = true;
        [SerializeField] private WeatherVfxChannel_SO_lwa eventChannel;

        private void Reset()
        {
            weatherManager = GetComponent<WeatherManager>();
        }

        private void OnEnable()
        {
            if (weatherManager == null)
            {
                weatherManager = FindFirstObjectByType<WeatherManager>();
            }

            if (weatherManager != null)
            {
                weatherManager.WeatherChanged += OnWeatherChanged;
            }
        }

        private void Start()
        {
            if (weatherManager != null && weatherManager.CurrentPreset != null)
            {
                OnWeatherChanged(weatherManager.CurrentPreset);
            }
        }

        private void OnDisable()
        {
            if (weatherManager != null)
            {
                weatherManager.WeatherChanged -= OnWeatherChanged;
            }
        }

        private void OnWeatherChanged(WeatherPreset preset)
        {
            if (preset == null)
            {
                return;
            }

            WeatherVfxEvent evt = new WeatherVfxEvent
            {
                weatherType = preset.weatherType,
                windStrength = preset.windStrength,
                particleEmissionMultiplier = preset.particleEmissionMultiplier,
                rainParticlesEnabled = preset.rainParticlesEnabled,
                windParticlesEnabled = preset.windParticlesEnabled,
                fogColor = preset.fogColor,
                fogDensity = preset.fogDensity,
                ambientLightColor = preset.ambientLightColor,
                directionalLightColor = preset.directionalLightColor,
                directionalLightIntensity = preset.directionalLightIntensity
            };

            if (setShaderGlobals)
            {
                WonderfulWorldVfxShaderGlobals_lwa.SetWeather(evt);
            }

            eventChannel?.Raise(evt);
        }
    }
}
