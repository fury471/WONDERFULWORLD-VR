using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public class DynamicLightController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float maxDisplayDistance = 50f;
    [SerializeField, Min(0f)] private float attenuationDistance = 5f;
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve rangeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private Light cachedLight;
    private LightFlicker flicker;
    private float originalIntensity;
    private float originalRange;

    private void Awake()
    {
        cachedLight = GetComponent<Light>();
        flicker = GetComponent<LightFlicker>();
        originalIntensity = cachedLight.intensity;
        originalRange = cachedLight.range;
    }

    private void OnEnable()
    {
        DynamicLightsManager.AddLight(this);
    }

    private void OnDisable()
    {
        DynamicLightsManager.RemoveLight(this);
        ResetLight();
    }

    public void UpdateForCamera(Camera targetCamera)
    {
        if (!enabled || targetCamera == null || cachedLight == null)
        {
            return;
        }

        float distance = Vector3.Distance(targetCamera.transform.position, transform.position);
        if (distance >= maxDisplayDistance)
        {
            cachedLight.enabled = false;
            return;
        }

        cachedLight.enabled = true;
        float fadeStart = Mathf.Max(0f, maxDisplayDistance - attenuationDistance);
        float fadeFactor = 1f - Mathf.InverseLerp(fadeStart, maxDisplayDistance, distance);
        float sourceIntensity = flicker != null ? flicker.ModifiedIntensity : originalIntensity;

        cachedLight.intensity = intensityCurve.Evaluate(fadeFactor) * sourceIntensity;
        cachedLight.range = rangeCurve.Evaluate(fadeFactor) * originalRange;
    }

    public void ResetLight()
    {
        if (cachedLight == null)
        {
            return;
        }

        cachedLight.enabled = enabled;
        cachedLight.intensity = flicker != null ? flicker.ModifiedIntensity : originalIntensity;
        cachedLight.range = originalRange;
    }
}

public static class DynamicLightsManager
{
    private static readonly List<DynamicLightController> DynamicLights = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        DynamicLights.Clear();
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        Application.quitting -= OnApplicationQuitting;
        Application.quitting += OnApplicationQuitting;
    }

    private static void OnApplicationQuitting()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        Application.quitting -= OnApplicationQuitting;
        DynamicLights.Clear();
    }

    private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera targetCamera)
    {
#if UNITY_EDITOR
        if (targetCamera != null && targetCamera.name == "SceneCamera")
        {
            ResetAllLights();
            return;
        }
#endif
        UpdateLightsForCamera(targetCamera);
    }

    private static void UpdateLightsForCamera(Camera targetCamera)
    {
        for (int i = DynamicLights.Count - 1; i >= 0; i--)
        {
            DynamicLightController dynamicLight = DynamicLights[i];
            if (dynamicLight == null)
            {
                DynamicLights.RemoveAt(i);
                continue;
            }

            dynamicLight.UpdateForCamera(targetCamera);
        }
    }

    private static void ResetAllLights()
    {
        for (int i = DynamicLights.Count - 1; i >= 0; i--)
        {
            DynamicLightController dynamicLight = DynamicLights[i];
            if (dynamicLight == null)
            {
                DynamicLights.RemoveAt(i);
                continue;
            }

            dynamicLight.ResetLight();
        }
    }

    public static void AddLight(DynamicLightController dynamicLightController)
    {
        if (dynamicLightController != null && !DynamicLights.Contains(dynamicLightController))
        {
            DynamicLights.Add(dynamicLightController);
        }
    }

    public static void RemoveLight(DynamicLightController dynamicLightController)
    {
        DynamicLights.Remove(dynamicLightController);
    }
}
