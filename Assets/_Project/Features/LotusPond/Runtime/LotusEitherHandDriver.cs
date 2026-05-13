using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

public class LotusEitherHandDriver : MonoBehaviour
{
    [Header("Ray Origins")]
    [SerializeField] private Transform leftRayOrigin;
    [SerializeField] private Transform rightRayOrigin;

    [Header("Raycast Settings")]
    [SerializeField] private float rayDistance = 20f;
    [SerializeField] private LayerMask rayMask = Physics.DefaultRaycastLayers;
    [SerializeField] private bool showDebugRays;

    [Header("Input Logic")]
    [SerializeField] private bool useTriggerButton = true;
    [SerializeField] private bool enableMouseDebug = true;

    [Header("Debug")]
    [SerializeField] private bool logDebugMessages;

    private XRInputDevice leftDevice;
    private XRInputDevice rightDevice;
    private bool leftPressedLastFrame;
    private bool rightPressedLastFrame;

    private void Awake() => AutoAssignRayOrigins();

    private void Update()
    {
        EnsureDevices();

        // Visual helper to see where you are aiming
        if (showDebugRays) DrawVisualRays();

        bool leftTrigger = IsPressed(leftDevice);
        bool rightTrigger = IsPressed(rightDevice);
        bool mouseLeft = enableMouseDebug && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool mouseRight = enableMouseDebug && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        if ((leftTrigger && !leftPressedLastFrame) || mouseLeft)
        {
            if (mouseLeft)
            {
                TryTriggerMouse();
            }
            else
            {
                TryTrigger(leftRayOrigin, "LeftHand");
            }
        }

        if ((rightTrigger && !rightPressedLastFrame) || mouseRight)
        {
            if (mouseRight)
            {
                TryTriggerMouse();
            }
            else
            {
                TryTrigger(rightRayOrigin, "RightHand");
            }
        }

        leftPressedLastFrame = leftTrigger;
        rightPressedLastFrame = rightTrigger;
    }

    private void TryTrigger(Transform rayOrigin, string label)
    {
        if (rayOrigin == null)
        {
            if (logDebugMessages)
            {
                Debug.LogWarning($"[LotusDriver] {label} has no ray origin assigned.");
            }
            return;
        }

        TryTriggerRay(new Ray(rayOrigin.position, rayOrigin.forward), label);
    }

    private void TryTriggerMouse()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            if (logDebugMessages)
            {
                Debug.LogWarning("[LotusDriver] Mouse debug has no Main Camera.");
            }

            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        TryTriggerRay(ray, "Mouse");
    }

    private void TryTriggerRay(Ray ray, string label)
    {
        if (showDebugRays)
        {
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red, 1f);
        }

        RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, rayMask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
        {
            if (logDebugMessages)
            {
                Debug.Log($"[LotusDriver] {label} missed within {rayDistance}m.");
            }

            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            LotusNoteTrigger trigger = hit.collider.GetComponentInParent<LotusNoteTrigger>();
            if (trigger == null)
            {
                trigger = hit.collider.GetComponentInChildren<LotusNoteTrigger>();
            }

            if (trigger == null)
            {
                continue;
            }

            trigger.TriggerNote(hit.point, ray.origin);
            return;
        }

        if (logDebugMessages)
        {
            Debug.LogWarning($"[LotusDriver] {label} hit colliders, but no LotusNoteTrigger was found.");
        }
    }

    private void DrawVisualRays()
    {
        if (leftRayOrigin != null) Debug.DrawRay(leftRayOrigin.position, leftRayOrigin.forward * rayDistance, Color.green);
        if (rightRayOrigin != null) Debug.DrawRay(rightRayOrigin.position, rightRayOrigin.forward * rayDistance, Color.yellow);
    }

    private void EnsureDevices()
    {
        if (!leftDevice.isValid) leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!rightDevice.isValid) rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    private bool IsPressed(XRInputDevice device)
    {
        if (!device.isValid) return false;
        if (useTriggerButton && device.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool pressed)) return pressed;
        return false;
    }

    private void AutoAssignRayOrigins()
    {
        if (leftRayOrigin == null) leftRayOrigin = FindInScene("Left Controller Stabilized Attach");
        if (rightRayOrigin == null) rightRayOrigin = FindInScene("Right Controller Stabilized Attach");
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || root.name == targetName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindInScene(string targetName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, targetName);
            if (found != null) return found;
        }
        return null;
    }
}
