using UnityEngine;

namespace WonderfulWorld.World.Shared.Lighting
{
    public class DreamlikeDemoAtmosphere : MonoBehaviour
    {
        [Header("Lighting")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private Color sunColor = new Color(1f, 0.86f, 0.62f, 1f);
        [SerializeField] private float sunIntensity = 1.25f;
        [SerializeField] private Color ambientColor = new Color(0.62f, 0.78f, 0.92f, 1f);

        [Header("Fog")]
        [SerializeField] private bool enableFog = true;
        [SerializeField] private Color fogColor = new Color(0.58f, 0.77f, 0.92f, 1f);
        [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;
        [SerializeField] private float fogDensity = 0.011f;

        [Header("Gentle Motion")]
        [SerializeField] private bool animateLight = true;
        [SerializeField] private float lightBreathSpeed = 0.18f;
        [SerializeField] private float lightBreathAmount = 0.08f;

        [Header("Lifecycle")]
        [SerializeField] private bool applyOnStart = true;
        [SerializeField] private bool restoreOnDisable;

        private bool captured;
        private bool previousFog;
        private Color previousFogColor;
        private FogMode previousFogMode;
        private float previousFogDensity;
        private Color previousAmbientColor;
        private Color previousSunColor;
        private float previousSunIntensity;

        private void Awake()
        {
            if (directionalLight == null)
            {
                directionalLight = FindDirectionalLight();
            }
        }

        private void Start()
        {
            if (applyOnStart)
            {
                ApplyAtmosphere();
            }
        }

        private void OnDisable()
        {
            if (restoreOnDisable)
            {
                RestoreAtmosphere();
            }
        }

        private void Update()
        {
            if (!animateLight || directionalLight == null)
            {
                return;
            }

            float breath = 1f + Mathf.Sin(Time.time * lightBreathSpeed) * lightBreathAmount;
            directionalLight.intensity = sunIntensity * breath;
        }

        [ContextMenu("Apply Dreamlike Atmosphere")]
        public void ApplyAtmosphere()
        {
            CapturePreviousState();

            RenderSettings.fog = enableFog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogDensity = Mathf.Max(0f, fogDensity);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;

            if (directionalLight != null)
            {
                directionalLight.color = sunColor;
                directionalLight.intensity = sunIntensity;
            }
        }

        [ContextMenu("Restore Previous Atmosphere")]
        public void RestoreAtmosphere()
        {
            if (!captured)
            {
                return;
            }

            RenderSettings.fog = previousFog;
            RenderSettings.fogColor = previousFogColor;
            RenderSettings.fogMode = previousFogMode;
            RenderSettings.fogDensity = previousFogDensity;
            RenderSettings.ambientLight = previousAmbientColor;

            if (directionalLight != null)
            {
                directionalLight.color = previousSunColor;
                directionalLight.intensity = previousSunIntensity;
            }
        }

        private void CapturePreviousState()
        {
            if (captured)
            {
                return;
            }

            previousFog = RenderSettings.fog;
            previousFogColor = RenderSettings.fogColor;
            previousFogMode = RenderSettings.fogMode;
            previousFogDensity = RenderSettings.fogDensity;
            previousAmbientColor = RenderSettings.ambientLight;

            if (directionalLight != null)
            {
                previousSunColor = directionalLight.color;
                previousSunIntensity = directionalLight.intensity;
            }

            captured = true;
        }

        private static Light FindDirectionalLight()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    return lights[i];
                }
            }

            return null;
        }
    }
}
