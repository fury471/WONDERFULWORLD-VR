using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

public static class QuestInteractionUtils
{
    private const float DefaultHapticFrequency = 0f;

    public static Transform FindHeadTransform()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera.transform;
        }

        Transform found = FindInScene("Main Camera");
        if (found != null)
        {
            return found;
        }

        found = FindInScene("CenterEyeAnchor");
        if (found != null)
        {
            return found;
        }

        found = FindInScene("Camera Offset");
        if (found != null)
        {
            Camera childCamera = found.GetComponentInChildren<Camera>(true);
            if (childCamera != null)
            {
                return childCamera.transform;
            }
        }

        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera != null && camera.stereoTargetEye != StereoTargetEyeMask.None)
            {
                return camera.transform;
            }
        }

        return cameras.Length > 0 && cameras[0] != null ? cameras[0].transform : null;
    }

    public static Transform FindControllerRayOrigin(bool rightHand)
    {
        if (rightHand)
        {
            Transform found = FindInScene("Right Controller Stabilized Attach");
            if (found != null)
            {
                return found;
            }

            found = FindInScene("Right Controller Teleport Stabilized Origin");
            if (found != null)
            {
                return found;
            }

            return FindInScene("Right Controller");
        }

        Transform left = FindInScene("Left Controller Stabilized Attach");
        if (left != null)
        {
            return left;
        }

        left = FindInScene("Left Controller Teleport Stabilized Origin");
        if (left != null)
        {
            return left;
        }

        return FindInScene("Left Controller");
    }

    public static HapticImpulsePlayer FindHapticPlayer(bool rightHand, Transform preferredOrigin = null)
    {
        if (preferredOrigin != null)
        {
            HapticImpulsePlayer localPlayer = preferredOrigin.GetComponentInParent<HapticImpulsePlayer>(true);
            if (localPlayer != null)
            {
                return localPlayer;
            }
        }

        string handName = rightHand ? "Right Controller" : "Left Controller";
        HapticImpulsePlayer[] players = Object.FindObjectsByType<HapticImpulsePlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            HapticImpulsePlayer player = players[i];
            if (player != null && TransformPathContains(player.transform, handName))
            {
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

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, targetName);
            if (found != null)
            {
                return found;
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
}
