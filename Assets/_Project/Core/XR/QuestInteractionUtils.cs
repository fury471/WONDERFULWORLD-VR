using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

public static class QuestInteractionUtils
{
    private const float DefaultHapticFrequency = 0f;
    private const float FailedLookupRetrySeconds = 0.5f;

    private static readonly Dictionary<string, Transform> transformCache = new Dictionary<string, Transform>(16);
    private static readonly Dictionary<string, float> transformRetryTimes = new Dictionary<string, float>(16);
    private static readonly Transform[] cachedControllerRayOrigins = new Transform[2];
    private static readonly HapticImpulsePlayer[] cachedHapticPlayers = new HapticImpulsePlayer[2];
    private static readonly float[] nextControllerLookupTimes = new float[2];
    private static readonly float[] nextHapticLookupTimes = new float[2];

    private static Transform cachedHeadTransform;
    private static Camera cachedHeadCamera;
    private static float nextHeadLookupTime;
    private static float nextHeadCameraLookupTime;
    private static int cachedSceneSignature = int.MinValue;

    public static Transform FindHeadTransform()
    {
        EnsureSceneCacheValid();
        if (cachedHeadTransform != null)
        {
            return cachedHeadTransform;
        }

        if (!CanRetry(ref nextHeadLookupTime))
        {
            return null;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cachedHeadCamera = mainCamera;
            cachedHeadTransform = mainCamera.transform;
            return cachedHeadTransform;
        }

        Transform found = FindInScene("Main Camera");
        if (found != null)
        {
            cachedHeadCamera = found.GetComponent<Camera>();
            cachedHeadTransform = found;
            return cachedHeadTransform;
        }

        found = FindInScene("CenterEyeAnchor");
        if (found != null)
        {
            cachedHeadCamera = found.GetComponent<Camera>();
            cachedHeadTransform = found;
            return cachedHeadTransform;
        }

        found = FindInScene("Camera Offset");
        if (found != null)
        {
            Camera childCamera = found.GetComponentInChildren<Camera>(true);
            if (childCamera != null)
            {
                cachedHeadCamera = childCamera;
                cachedHeadTransform = childCamera.transform;
                return cachedHeadTransform;
            }
        }

        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera != null && camera.stereoTargetEye != StereoTargetEyeMask.None)
            {
                cachedHeadCamera = camera;
                cachedHeadTransform = camera.transform;
                return cachedHeadTransform;
            }
        }

        cachedHeadCamera = cameras.Length > 0 ? cameras[0] : null;
        cachedHeadTransform = cachedHeadCamera != null ? cachedHeadCamera.transform : null;
        return cachedHeadTransform;
    }

    public static Camera FindHeadCamera()
    {
        EnsureSceneCacheValid();
        if (cachedHeadCamera != null)
        {
            return cachedHeadCamera;
        }

        Transform headTransform = FindHeadTransform();
        if (headTransform != null)
        {
            cachedHeadCamera = headTransform.GetComponent<Camera>();
            if (cachedHeadCamera != null)
            {
                return cachedHeadCamera;
            }

            cachedHeadCamera = headTransform.GetComponentInChildren<Camera>(true);
            if (cachedHeadCamera != null)
            {
                return cachedHeadCamera;
            }

            cachedHeadCamera = headTransform.GetComponentInParent<Camera>();
            if (cachedHeadCamera != null)
            {
                return cachedHeadCamera;
            }
        }

        if (!CanRetry(ref nextHeadCameraLookupTime))
        {
            return null;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cachedHeadCamera = mainCamera;
            cachedHeadTransform = mainCamera.transform;
            return cachedHeadCamera;
        }

        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera != null && camera.stereoTargetEye != StereoTargetEyeMask.None)
            {
                cachedHeadCamera = camera;
                cachedHeadTransform = camera.transform;
                return cachedHeadCamera;
            }
        }

        cachedHeadCamera = cameras.Length > 0 ? cameras[0] : null;
        cachedHeadTransform = cachedHeadCamera != null ? cachedHeadCamera.transform : cachedHeadTransform;
        return cachedHeadCamera;
    }

    public static Transform FindControllerRayOrigin(bool rightHand)
    {
        EnsureSceneCacheValid();
        int cacheIndex = ToHandCacheIndex(rightHand);
        if (cachedControllerRayOrigins[cacheIndex] != null)
        {
            return cachedControllerRayOrigins[cacheIndex];
        }

        if (!CanRetry(ref nextControllerLookupTimes[cacheIndex]))
        {
            return null;
        }

        string[] candidateNames = rightHand
            ? new[]
            {
                "Right Controller Stabilized Attach",
                "Right Controller Teleport Stabilized Origin",
                "Right Controller"
            }
            : new[]
            {
                "Left Controller Stabilized Attach",
                "Left Controller Teleport Stabilized Origin",
                "Left Controller"
            };

        for (int i = 0; i < candidateNames.Length; i++)
        {
            Transform found = FindInScene(candidateNames[i]);
            if (found != null)
            {
                cachedControllerRayOrigins[cacheIndex] = found;
                return found;
            }
        }

        return null;
    }

    public static HapticImpulsePlayer FindHapticPlayer(bool rightHand, Transform preferredOrigin = null)
    {
        EnsureSceneCacheValid();
        int cacheIndex = ToHandCacheIndex(rightHand);

        if (preferredOrigin != null)
        {
            HapticImpulsePlayer localPlayer = preferredOrigin.GetComponentInParent<HapticImpulsePlayer>(true);
            if (localPlayer != null)
            {
                cachedHapticPlayers[cacheIndex] = localPlayer;
                return localPlayer;
            }
        }

        if (cachedHapticPlayers[cacheIndex] != null)
        {
            return cachedHapticPlayers[cacheIndex];
        }

        if (!CanRetry(ref nextHapticLookupTimes[cacheIndex]))
        {
            return null;
        }

        string handName = rightHand ? "Right Controller" : "Left Controller";
        HapticImpulsePlayer[] players = Object.FindObjectsByType<HapticImpulsePlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            HapticImpulsePlayer player = players[i];
            if (player != null && TransformPathContains(player.transform, handName))
            {
                cachedHapticPlayers[cacheIndex] = player;
                return player;
            }
        }

        return null;
    }

    public static void SendHaptic(HapticImpulsePlayer player, float amplitude, float duration)
    {
        if (player == null || amplitude <= 0f || duration <= 0f)
        {
            return;
        }

        player.SendHapticImpulse(Mathf.Clamp01(amplitude), Mathf.Max(0f, duration), DefaultHapticFrequency);
    }

    public static bool TryReadTriggerButton(bool rightHand, out bool pressed)
    {
        XRInputDevice device = InputDevices.GetDeviceAtXRNode(rightHand ? XRNode.RightHand : XRNode.LeftHand);
        if (device.isValid && device.TryGetFeatureValue(XRCommonUsages.triggerButton, out pressed))
        {
            return true;
        }

        pressed = false;
        return false;
    }

    public static bool TryReadPrimaryButton(bool rightHand, out bool pressed)
    {
        XRInputDevice device = InputDevices.GetDeviceAtXRNode(rightHand ? XRNode.RightHand : XRNode.LeftHand);
        if (device.isValid && device.TryGetFeatureValue(XRCommonUsages.primaryButton, out pressed))
        {
            return true;
        }

        pressed = false;
        return false;
    }

    public static bool TryReadSecondaryButton(bool rightHand, out bool pressed)
    {
        XRInputDevice device = InputDevices.GetDeviceAtXRNode(rightHand ? XRNode.RightHand : XRNode.LeftHand);
        if (device.isValid && device.TryGetFeatureValue(XRCommonUsages.secondaryButton, out pressed))
        {
            return true;
        }

        pressed = false;
        return false;
    }

    public static bool TryReadPrimary2DAxis(bool rightHand, out Vector2 axis)
    {
        XRInputDevice device = InputDevices.GetDeviceAtXRNode(rightHand ? XRNode.RightHand : XRNode.LeftHand);
        if (device.isValid && device.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out axis))
        {
            return true;
        }

        axis = Vector2.zero;
        return false;
    }

    public static bool TryReadPrimary2DAxisClick(bool rightHand, out bool pressed)
    {
        XRInputDevice device = InputDevices.GetDeviceAtXRNode(rightHand ? XRNode.RightHand : XRNode.LeftHand);
        if (device.isValid && device.TryGetFeatureValue(XRCommonUsages.primary2DAxisClick, out pressed))
        {
            return true;
        }

        pressed = false;
        return false;
    }

    public static Transform FindInScene(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        EnsureSceneCacheValid();
        string cacheKey = targetName.ToLowerInvariant();
        if (transformCache.TryGetValue(cacheKey, out Transform cached))
        {
            if (cached != null)
            {
                return cached;
            }

            transformCache.Remove(cacheKey);
        }

        if (Application.isPlaying &&
            transformRetryTimes.TryGetValue(cacheKey, out float nextRetryTime) &&
            Time.unscaledTime < nextRetryTime)
        {
            return null;
        }

        transformRetryTimes[cacheKey] = Application.isPlaying
            ? Time.unscaledTime + FailedLookupRetrySeconds
            : 0f;

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                continue;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindChildRecursive(roots[i].transform, targetName);
                if (found != null)
                {
                    transformCache[cacheKey] = found;
                    return found;
                }
            }
        }

        return null;
    }

    public static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    public static bool TransformPathContains(Transform transform, string token)
    {
        return transform != null && !string.IsNullOrEmpty(token) &&
               GetTransformPath(transform).IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string GetTransformPath(Transform transform)
    {
        if (transform == null)
        {
            return string.Empty;
        }

        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private static int ToHandCacheIndex(bool rightHand)
    {
        return rightHand ? 1 : 0;
    }

    private static bool CanRetry(ref float nextRetryTime)
    {
        if (!Application.isPlaying)
        {
            return true;
        }

        if (Time.unscaledTime < nextRetryTime)
        {
            return false;
        }

        nextRetryTime = Time.unscaledTime + FailedLookupRetrySeconds;
        return true;
    }

    private static void EnsureSceneCacheValid()
    {
        int sceneSignature = GetLoadedSceneSignature();
        if (sceneSignature == cachedSceneSignature)
        {
            return;
        }

        cachedSceneSignature = sceneSignature;
        cachedHeadTransform = null;
        cachedHeadCamera = null;
        nextHeadLookupTime = 0f;
        nextHeadCameraLookupTime = 0f;
        transformCache.Clear();
        transformRetryTimes.Clear();

        for (int i = 0; i < cachedControllerRayOrigins.Length; i++)
        {
            cachedControllerRayOrigins[i] = null;
            cachedHapticPlayers[i] = null;
            nextControllerLookupTimes[i] = 0f;
            nextHapticLookupTimes[i] = 0f;
        }
    }

    private static int GetLoadedSceneSignature()
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded)
                {
                    hash = hash * 31 + scene.handle;
                }
            }

            return hash;
        }
    }
}
