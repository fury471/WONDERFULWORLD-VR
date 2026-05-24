using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public sealed class PCVRPerformanceBootstrap : MonoBehaviour
{
    private const string PreferredQualityName = "Low";
    private const int FallbackQualityIndex = 1;
    private const float TargetFixedDeltaTime = 1f / 90f;
    private const float MaximumDeltaTime = 1f / 30f;
    private const float PreferredRefreshRate = 90f;
    private const float MaximumEyeTextureScale = 1f;
    private const int RefreshRateRetryFrames = 180;

    private static readonly List<XRDisplaySubsystem> xrDisplays = new List<XRDisplaySubsystem>(2);
    private static readonly MethodInfo requestDisplayRefreshRateMethod = typeof(XRDisplaySubsystem).GetMethod(
        "TryRequestDisplayRefreshRate",
        BindingFlags.Instance | BindingFlags.Public,
        null,
        new[] { typeof(float) },
        null);

    private static bool initialized;
    private static bool logged;

    private int refreshRateAttempts;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        ApplyFramePacingSettings();
        CreateRuntimeHost();
#endif
    }

    private static void CreateRuntimeHost()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        var host = new GameObject("[PCVR Performance Bootstrap]")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        DontDestroyOnLoad(host);
        host.AddComponent<PCVRPerformanceBootstrap>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ApplySceneCameraSettings();
    }

    private void Update()
    {
        if (refreshRateAttempts >= RefreshRateRetryFrames)
        {
            return;
        }

        refreshRateAttempts++;
        if (TryRequestPreferredRefreshRate())
        {
            refreshRateAttempts = RefreshRateRetryFrames;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyFramePacingSettings();
        ApplySceneCameraSettings();
    }

    private static void ApplyFramePacingSettings()
    {
        ForcePerformanceQualityLevel();

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        OnDemandRendering.renderFrameInterval = 1;

        Time.fixedDeltaTime = TargetFixedDeltaTime;
        if (Time.maximumDeltaTime > MaximumDeltaTime)
        {
            Time.maximumDeltaTime = MaximumDeltaTime;
        }

        if (XRSettings.eyeTextureResolutionScale > MaximumEyeTextureScale)
        {
            XRSettings.eyeTextureResolutionScale = MaximumEyeTextureScale;
        }

        if (!logged)
        {
            logged = true;
            Debug.Log(
                "[PCVR] Applied Quest Link frame pacing: " +
                $"Quality={QualitySettings.names[QualitySettings.GetQualityLevel()]}, " +
                "vSync=0, targetFrameRate=-1, renderFrameInterval=1, " +
                $"fixedDeltaTime={Time.fixedDeltaTime:0.0000}, maxDeltaTime={Time.maximumDeltaTime:0.0000}.");
        }
    }

    private static void ForcePerformanceQualityLevel()
    {
        string[] qualityNames = QualitySettings.names;
        if (qualityNames == null || qualityNames.Length == 0)
        {
            return;
        }

        int targetQuality = Array.IndexOf(qualityNames, PreferredQualityName);
        if (targetQuality < 0)
        {
            targetQuality = Mathf.Clamp(FallbackQualityIndex, 0, qualityNames.Length - 1);
        }

        if (QualitySettings.GetQualityLevel() != targetQuality)
        {
            QualitySettings.SetQualityLevel(targetQuality, true);
        }
    }

    private static void ApplySceneCameraSettings()
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        Camera[] cameras = FindObjectsOfType<Camera>(false);
#pragma warning restore CS0618
#endif
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null || camera.stereoTargetEye == StereoTargetEyeMask.None)
            {
                continue;
            }

            camera.allowHDR = false;
            camera.allowDynamicResolution = true;

            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            if (cameraData != null)
            {
                cameraData.renderPostProcessing = false;
                cameraData.stopNaN = false;
                cameraData.dithering = false;
                cameraData.allowXRRendering = true;
            }
        }
    }

    private static bool TryRequestPreferredRefreshRate()
    {
        xrDisplays.Clear();
        SubsystemManager.GetSubsystems(xrDisplays);

        for (int i = 0; i < xrDisplays.Count; i++)
        {
            XRDisplaySubsystem display = xrDisplays[i];
            if (display == null || !display.running)
            {
                continue;
            }

#if UNITY_2021_2_OR_NEWER
            if (display.TryGetDisplayRefreshRate(out float currentRate) &&
                currentRate >= PreferredRefreshRate - 0.5f)
            {
                return true;
            }

            if (TryRequestDisplayRefreshRate(display, PreferredRefreshRate))
            {
                return true;
            }
#else
            return true;
#endif
        }

        return false;
    }

    private static bool TryRequestDisplayRefreshRate(XRDisplaySubsystem display, float refreshRate)
    {
        if (display == null || requestDisplayRefreshRateMethod == null)
        {
            return false;
        }

        object result = requestDisplayRefreshRateMethod.Invoke(display, new object[] { refreshRate });
        return result is bool requested && requested;
    }
}
