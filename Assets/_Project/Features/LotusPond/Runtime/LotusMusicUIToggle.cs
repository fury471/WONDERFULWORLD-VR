using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

public class LotusMusicUIToggle : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private bool startHidden = true;

    [Header("Input")]
    [SerializeField] private bool enableGripToggle = true;
    [SerializeField] private bool enableDebugKeyboardToggle = true;

    private bool leftGripWasDown;
    private bool rightGripWasDown;

    private void Reset()
    {
        uiRoot = gameObject;
    }

    private void Awake()
    {
        if (uiRoot == null)
            uiRoot = gameObject;

        if (startHidden)
            uiRoot.SetActive(false);
    }

    private void Update()
    {
        if (enableDebugKeyboardToggle && Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            Toggle();
        }

        if (enableGripToggle)
        {
            bool leftDown = TryGetGripDown(XRNode.LeftHand);
            bool rightDown = TryGetGripDown(XRNode.RightHand);

            bool leftPressedThisFrame = leftDown && !leftGripWasDown;
            bool rightPressedThisFrame = rightDown && !rightGripWasDown;

            leftGripWasDown = leftDown;
            rightGripWasDown = rightDown;

            if (leftPressedThisFrame || rightPressedThisFrame)
                Toggle();
        }
    }

    private void Toggle()
    {
        if (uiRoot == null)
            return;

        uiRoot.SetActive(!uiRoot.activeSelf);
    }

    private static bool TryGetGripDown(XRNode node)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
            return false;

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool pressed))
            return pressed;

        return false;
    }
}
