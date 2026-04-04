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

    [Header("Raycast")]
    [SerializeField] private float rayDistance = 20f;
    [SerializeField] private LayerMask rayMask = Physics.DefaultRaycastLayers;
    [SerializeField] private bool drawDebugRays = true;

    [Header("Input")]
    [SerializeField] private bool useGripButton = true;
    [SerializeField] private bool useTriggerButton = true;
    [SerializeField] private bool enableKeyboardFallback = true;
    [SerializeField] private Key leftKeyboardFallbackKey = Key.Q;
    [SerializeField] private Key rightKeyboardFallbackKey = Key.E;

    [Header("Debug")]
    [SerializeField] private bool logHits = true;
    [SerializeField] private bool logMisses = true;
    [SerializeField] private bool logInputState = true;
    [SerializeField] private bool logDeviceRefresh = true;
    [SerializeField] private bool logRayDetails = true;

    private XRInputDevice leftDevice;
    private XRInputDevice rightDevice;
    private bool leftPressedLastFrame;
    private bool rightPressedLastFrame;
    private bool leftDeviceWasValid;
    private bool rightDeviceWasValid;
    private bool leftInputWasPressed;
    private bool rightInputWasPressed;
    private int lastLeftDeviceRefreshFrame = -1;
    private int lastRightDeviceRefreshFrame = -1;

    private void Awake()
    {
        AutoAssignRayOrigins();
    }

    private void Update()
    {
        EnsureDevices();

        bool leftPressed = IsPressed(leftDevice, true) || IsKeyboardFallbackPressed(leftKeyboardFallbackKey, "LeftHand");
        bool rightPressed = IsPressed(rightDevice, false) || IsKeyboardFallbackPressed(rightKeyboardFallbackKey, "RightHand");

        if (drawDebugRays)
        {
            if (leftRayOrigin != null)
            {
                Debug.DrawRay(leftRayOrigin.position, leftRayOrigin.forward * rayDistance, Color.green);
            }

            if (rightRayOrigin != null)
            {
                Debug.DrawRay(rightRayOrigin.position, rightRayOrigin.forward * rayDistance, Color.yellow);
            }
        }

        if (leftPressed && !leftPressedLastFrame)
        {
            TryTrigger(leftRayOrigin, "LeftHand");
        }

        if (rightPressed && !rightPressedLastFrame)
        {
            TryTrigger(rightRayOrigin, "RightHand");
        }

        leftPressedLastFrame = leftPressed;
        rightPressedLastFrame = rightPressed;
    }

    private void AutoAssignRayOrigins()
    {
        if (leftRayOrigin == null)
        {
            leftRayOrigin = FindInScene("Left Controller Teleport Stabilized Origin");
        }

        if (rightRayOrigin == null)
        {
            rightRayOrigin = FindInScene("Right Controller Teleport Stabilized Origin");
        }

        if (leftRayOrigin == null)
        {
            leftRayOrigin = FindInScene("Left Controller Stabilized Attach");
        }

        if (rightRayOrigin == null)
        {
            rightRayOrigin = FindInScene("Right Controller Stabilized Attach");
        }

        if (logMisses)
        {
            Debug.Log($"LotusEitherHandDriver: leftRayOrigin={(leftRayOrigin != null ? leftRayOrigin.name : "NULL")}, rightRayOrigin={(rightRayOrigin != null ? rightRayOrigin.name : "NULL")}");
        }
    }

    private void EnsureDevices()
    {
        if (!leftDevice.isValid && lastLeftDeviceRefreshFrame != Time.frameCount)
        {
            lastLeftDeviceRefreshFrame = Time.frameCount;
            leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (logDeviceRefresh)
            {
                Debug.Log($"LotusEitherHandDriver: refreshed LeftHand device. valid={leftDevice.isValid}, name={leftDevice.name}, characteristics={leftDevice.characteristics}");
            }
        }

        if (!rightDevice.isValid && lastRightDeviceRefreshFrame != Time.frameCount)
        {
            lastRightDeviceRefreshFrame = Time.frameCount;
            rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (logDeviceRefresh)
            {
                Debug.Log($"LotusEitherHandDriver: refreshed RightHand device. valid={rightDevice.isValid}, name={rightDevice.name}, characteristics={rightDevice.characteristics}");
            }
        }

        if (logDeviceRefresh && leftDeviceWasValid != leftDevice.isValid)
        {
            Debug.Log($"LotusEitherHandDriver: LeftHand device validity changed to {leftDevice.isValid}.");
            leftDeviceWasValid = leftDevice.isValid;
        }

        if (logDeviceRefresh && rightDeviceWasValid != rightDevice.isValid)
        {
            Debug.Log($"LotusEitherHandDriver: RightHand device validity changed to {rightDevice.isValid}.");
            rightDeviceWasValid = rightDevice.isValid;
        }
    }

    private bool IsPressed(XRInputDevice device, bool isLeftHand)
    {
        if (!device.isValid)
        {
            return false;
        }

        bool pressed = false;
        bool gripSupported = false;
        bool triggerSupported = false;
        bool gripPressed = false;
        bool triggerPressed = false;

        if (useGripButton && device.TryGetFeatureValue(XRCommonUsages.gripButton, out gripPressed))
        {
            gripSupported = true;
            if (gripPressed)
            {
                pressed = true;
            }
        }

        if (useTriggerButton && device.TryGetFeatureValue(XRCommonUsages.triggerButton, out triggerPressed))
        {
            triggerSupported = true;
            if (triggerPressed)
            {
                pressed = true;
            }
        }

        if (logInputState)
        {
            bool previousPressed = isLeftHand ? leftInputWasPressed : rightInputWasPressed;

            if (pressed != previousPressed || gripPressed || triggerPressed)
            {
                Debug.Log(
                    "LotusEitherHandDriver: input state " +
                    $"device={device.name}, valid={device.isValid}, " +
                    $"gripSupported={gripSupported}, gripPressed={gripPressed}, " +
                    $"triggerSupported={triggerSupported}, triggerPressed={triggerPressed}, " +
                    $"pressed={pressed}");
            }

            if (isLeftHand)
            {
                leftInputWasPressed = pressed;
            }
            else
            {
                rightInputWasPressed = pressed;
            }
        }

        return pressed;
    }

    private bool IsKeyboardFallbackPressed(Key key, string handLabel)
    {
        if (!enableKeyboardFallback || Keyboard.current == null)
        {
            return false;
        }

        if (!Keyboard.current[key].wasPressedThisFrame)
        {
            return false;
        }

        if (logInputState)
        {
            Debug.Log($"LotusEitherHandDriver: keyboard fallback triggered for {handLabel} via key {key}.");
        }

        return true;
    }

    private void TryTrigger(Transform rayOrigin, string handLabel)
    {
        if (rayOrigin == null)
        {
            if (logMisses)
            {
                Debug.Log($"LotusEitherHandDriver: {handLabel} ray origin is missing.");
            }
            return;
        }

        if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, rayDistance, rayMask, QueryTriggerInteraction.Collide))
        {
            if (logMisses)
            {
                Debug.Log(
                    $"LotusEitherHandDriver: {handLabel} hit nothing. " +
                    $"origin={rayOrigin.name}, position={rayOrigin.position}, forward={rayOrigin.forward}, distance={rayDistance}, mask={rayMask.value}");
            }
            return;
        }

        if (logRayDetails)
        {
            Debug.Log(
                $"LotusEitherHandDriver: {handLabel} ray hit collider={hit.collider.name}, object={hit.collider.gameObject.name}, " +
                $"point={hit.point}, distance={hit.distance}, normal={hit.normal}");
        }

        LotusNoteTrigger trigger = hit.collider.GetComponentInParent<LotusNoteTrigger>();
        if (trigger == null)
        {
            if (logMisses)
            {
                Debug.Log($"LotusEitherHandDriver: {handLabel} hit {hit.collider.name}, but no LotusNoteTrigger was found.");
            }
            return;
        }

        if (logHits)
        {
            Debug.Log($"LotusEitherHandDriver: {handLabel} triggered {trigger.name} via {hit.collider.name}.");
        }

        trigger.TriggerNote();
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
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

    private static Transform FindInScene(string targetName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
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
}
