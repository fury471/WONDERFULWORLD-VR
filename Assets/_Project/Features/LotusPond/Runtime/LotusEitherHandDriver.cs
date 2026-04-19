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
    [SerializeField] private bool showDebugRays = true; 

    [Header("Input Logic")]
    [SerializeField] private bool useTriggerButton = true;
    [SerializeField] private bool enableMouseDebug = true; 

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
        bool mouseClicked = enableMouseDebug && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if ((leftTrigger && !leftPressedLastFrame) || mouseClicked)
        {
            TryTrigger(mouseClicked ? rightRayOrigin : leftRayOrigin, mouseClicked ? "Mouse" : "LeftHand");
        }

        if (rightTrigger && !rightPressedLastFrame)
        {
            TryTrigger(rightRayOrigin, "RightHand");
        }

        leftPressedLastFrame = leftTrigger;
        rightPressedLastFrame = rightTrigger;
    }

    private void TryTrigger(Transform rayOrigin, string label)
    {
        if (rayOrigin == null)
        {
            Debug.LogWarning($"[LotusDriver] {label} has NO Ray Origin assigned!");
            return;
        }

        // Log the firing attempt
        Debug.Log($"[LotusDriver] {label} firing ray from {rayOrigin.name}");

        if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, rayDistance, rayMask, QueryTriggerInteraction.Collide))
        {
            // DEBUG POINT 1: What did we actually hit?
            Debug.Log($"[LotusDriver] {label} HIT: {hit.collider.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");

            LotusNoteTrigger trigger = hit.collider.GetComponentInParent<LotusNoteTrigger>();
            
            if (trigger != null)
            {
                // DEBUG POINT 2: Script found and triggered
                Debug.Log($"[LotusDriver] SUCCESS! Calling TriggerNote on {trigger.name}");
                trigger.TriggerNote();
            }
            else
            {
                // DEBUG POINT 3: Hit something, but it's not a Lotus
                Debug.LogWarning($"[LotusDriver] FAIL: Hit {hit.collider.name}, but NO LotusNoteTrigger found on it or its parents.");
            }
        }
        else
        {
            // DEBUG POINT 4: Ray went into space
            Debug.Log($"[LotusDriver] {label} MISSED everything within {rayDistance} range.");
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