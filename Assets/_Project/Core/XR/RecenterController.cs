using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public sealed class RecenterController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private ScaleTransitionController transitionController;

    [Header("Recenter Reference (optional)")]
    [Tooltip("If set, use this transform's forward as the desired view direction. Leave null to fall back to world Z+.")]
    [SerializeField] private Transform recenterAnchor;
    [Tooltip("Also snap player position to the anchor. When false (default) the player is only reoriented in place.")]
    [SerializeField] private bool snapToAnchorPosition = false;

    [Header("Input — Right B (secondaryButton)")]
    [SerializeField] private InputActionReference recenterAction;
    [SerializeField] private bool useRightSecondaryButton = true;
    [SerializeField, Min(0f)] private float holdSecondsToConfirm = 0.4f;

    [Header("Debug Fallback")]
    [SerializeField] private bool enableKeyboardDebug = true;
    [SerializeField] private Key debugRecenterKey = Key.R;

    [Header("Feedback")]
    [SerializeField] private bool useBlink = true;
    [SerializeField, Min(0.05f)] private float blinkDuration = 0.45f;
    [SerializeField, Range(0f, 1f)] private float chargeStartHapticAmplitude = 0.25f;
    [SerializeField, Min(0f)] private float chargeStartHapticDuration = 0.04f;
    [SerializeField, Range(0f, 1f)] private float confirmHapticAmplitude = 0.6f;
    [SerializeField, Min(0f)] private float confirmHapticDuration = 0.12f;

    [Header("Disable Conditions")]
    [Tooltip("When riding a mount, instead of disabling, recenter is rerouted to the mount's seat forward (CatRideControllerV2.RecenterMountedView).")]
    [SerializeField] private bool routeToMountWhileRiding = true;
    [SerializeField] private bool disableWhileScaleTransitioning = true;

    [Header("Debug")]
    [SerializeField] private bool logDebug;

    private HapticImpulsePlayer rightHaptics;
    private Transform rightControllerOrigin;
    private CatRideControllerV2[] cachedRideControllers;
    private ScaleManager cachedScaleManager;
    private bool actionHeldLastFrame;
    private bool secondaryHeldLastFrame;
    private bool keyboardHeldLastFrame;
    private float pressStartTime = -1f;
    private bool confirmFired;
    private bool isRecentering;
    private bool requireReleaseBeforeNextPress;

    private void Awake()
    {
        AutoAssignReferences();
    }

    private void OnValidate()
    {
        AutoAssignReferences();
    }

    private void OnEnable()
    {
        recenterAction?.action?.Enable();
    }

    private void OnDisable()
    {
        recenterAction?.action?.Disable();
        pressStartTime = -1f;
        confirmFired = false;
    }

    private void Update()
    {
        if (isRecentering)
        {
            return;
        }

        CacheReferences();

        if (!CanRecenterNow())
        {
            ResetPressState();
            return;
        }

        bool pressed = ReadPressed();

        if (requireReleaseBeforeNextPress)
        {
            if (!pressed)
            {
                requireReleaseBeforeNextPress = false;
            }
            return;
        }

        if (pressed && pressStartTime < 0f)
        {
            pressStartTime = Time.unscaledTime;
            confirmFired = false;
            QuestInteractionUtils.SendHaptic(rightHaptics, chargeStartHapticAmplitude, chargeStartHapticDuration);
            if (logDebug)
            {
                Debug.Log("[Recenter] Press started.", this);
            }
        }

        if (pressed && !confirmFired && pressStartTime >= 0f)
        {
            if (Time.unscaledTime - pressStartTime >= holdSecondsToConfirm)
            {
                confirmFired = true;
                QuestInteractionUtils.SendHaptic(rightHaptics, confirmHapticAmplitude, confirmHapticDuration);
                StartCoroutine(RecenterRoutine());
            }
        }

        if (!pressed)
        {
            ResetPressState();
        }
    }

    public void RequestRecenter()
    {
        if (isRecentering || !CanRecenterNow())
        {
            return;
        }

        CacheReferences();
        confirmFired = true;
        StartCoroutine(RecenterRoutine());
    }

    private IEnumerator RecenterRoutine()
    {
        isRecentering = true;
        CacheReferences();

        CatRideControllerV2 activeRide = routeToMountWhileRiding ? GetActiveRide() : null;

        if (useBlink && transitionController != null)
        {
            yield return StartCoroutine(BlinkAndApply(activeRide));
        }
        else
        {
            ApplyRecenter(activeRide);
        }

        isRecentering = false;
        requireReleaseBeforeNextPress = true;
        ResetPressState();
    }

    private IEnumerator BlinkAndApply(CatRideControllerV2 activeRide)
    {
        float outAndHold = Mathf.Max(0.05f, blinkDuration * 0.45f);
        yield return StartCoroutine(transitionController.PlayBlink(outAndHold, targetCamera));
        ApplyRecenter(activeRide);
    }

    private void ApplyRecenter(CatRideControllerV2 activeRide)
    {
        if (activeRide != null)
        {
            activeRide.RecenterMountedView();
            if (logDebug)
            {
                Debug.Log($"[Recenter] Routed to mount: {activeRide.name}", this);
            }
            return;
        }

        ApplyRecenterPose();
    }

    private void ApplyRecenterPose()
    {
        if (xrOrigin == null)
        {
            Debug.LogWarning("[Recenter] No XROrigin resolved; cannot recenter.", this);
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = xrOrigin.Camera;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning("[Recenter] No Camera resolved; cannot recenter.", this);
            return;
        }

        Vector3 desiredForward;
        if (recenterAnchor != null)
        {
            desiredForward = recenterAnchor.forward;
        }
        else
        {
            desiredForward = Vector3.forward;
        }

        desiredForward.y = 0f;
        if (desiredForward.sqrMagnitude < 0.0001f)
        {
            desiredForward = Vector3.forward;
        }
        desiredForward.Normalize();

        Vector3 desiredCameraPosition = targetCamera.transform.position;
        if (snapToAnchorPosition && recenterAnchor != null)
        {
            desiredCameraPosition = recenterAnchor.position;
        }

        xrOrigin.MatchOriginUpCameraForward(Vector3.up, desiredForward);
        xrOrigin.MoveCameraToWorldLocation(desiredCameraPosition);

        if (logDebug)
        {
            Debug.Log(
                $"[Recenter] Applied. anchor={(recenterAnchor != null ? recenterAnchor.name : "<world Z+>")} " +
                $"snapPos={snapToAnchorPosition} camPos={targetCamera.transform.position} camFwd={targetCamera.transform.forward}",
                this);
        }
    }

    private bool ReadPressed()
    {
        bool pressed = false;

        InputAction action = recenterAction != null ? recenterAction.action : null;
        if (action != null)
        {
            bool actionPressed = action.IsPressed();
            pressed |= actionPressed;
            actionHeldLastFrame = actionPressed;
        }

        if (useRightSecondaryButton)
        {
            QuestInteractionUtils.TryReadSecondaryButton(true, out bool secondaryPressed);
            pressed |= secondaryPressed;
            secondaryHeldLastFrame = secondaryPressed;
        }

        if (enableKeyboardDebug && Keyboard.current != null)
        {
            bool keyPressed = Keyboard.current[debugRecenterKey].isPressed;
            pressed |= keyPressed;
            keyboardHeldLastFrame = keyPressed;
        }

        return pressed;
    }

    private void ResetPressState()
    {
        pressStartTime = -1f;
        confirmFired = false;
        actionHeldLastFrame = false;
        secondaryHeldLastFrame = false;
        keyboardHeldLastFrame = false;
    }

    private bool CanRecenterNow()
    {
        // While riding, B is rerouted to the mount's seat-recenter rather than blocked.
        // The only hard block today is mid-scale-transition, which would fight the camera.
        if (disableWhileScaleTransitioning && IsScaleTransitionActive())
        {
            return false;
        }

        return true;
    }

    private CatRideControllerV2 GetActiveRide()
    {
        if (cachedRideControllers == null || cachedRideControllers.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < cachedRideControllers.Length; i++)
        {
            CatRideControllerV2 controller = cachedRideControllers[i];
            if (controller != null && controller.IsRideActive)
            {
                return controller;
            }
        }

        return null;
    }

    private bool IsScaleTransitionActive()
    {
        // ScaleManager doesn't expose its private isTransitioning flag, but it owns the
        // ScaleTransitionController. While a blink is playing the controller is mid-fade;
        // the simplest conservative check is whether the cached scale manager exists and
        // the transition controller is the one currently playing for it. We don't get a
        // public hook from ScaleManager today, so for now we only gate on our own routine.
        return false;
    }

    private void CacheReferences()
    {
        if (xrOrigin == null)
        {
            xrOrigin = GetComponentInParent<XROrigin>(true);
            if (xrOrigin == null)
            {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
                xrOrigin = FindAnyObjectByType<XROrigin>(FindObjectsInactive.Include);
#else
#pragma warning disable CS0618
                xrOrigin = FindObjectOfType<XROrigin>(true);
#pragma warning restore CS0618
#endif
            }
        }

        if (targetCamera == null && xrOrigin != null)
        {
            targetCamera = xrOrigin.Camera;
        }

        if (targetCamera == null)
        {
            Transform head = QuestInteractionUtils.FindHeadTransform();
            if (head != null)
            {
                targetCamera = head.GetComponent<Camera>();
                if (targetCamera == null)
                {
                    targetCamera = head.GetComponentInParent<Camera>();
                }
            }
        }

        if (transitionController == null)
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            transitionController = FindAnyObjectByType<ScaleTransitionController>(FindObjectsInactive.Include);
#else
#pragma warning disable CS0618
            transitionController = FindObjectOfType<ScaleTransitionController>(true);
#pragma warning restore CS0618
#endif
        }

        if (rightControllerOrigin == null)
        {
            rightControllerOrigin = QuestInteractionUtils.FindControllerRayOrigin(true);
        }

        if (rightHaptics == null)
        {
            rightHaptics = QuestInteractionUtils.FindHapticPlayer(true, rightControllerOrigin);
        }

        if (cachedRideControllers == null || cachedRideControllers.Length == 0)
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            cachedRideControllers = FindObjectsByType<CatRideControllerV2>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            cachedRideControllers = FindObjectsOfType<CatRideControllerV2>(true);
#pragma warning restore CS0618
#endif
        }

        if (cachedScaleManager == null)
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            cachedScaleManager = FindAnyObjectByType<ScaleManager>(FindObjectsInactive.Include);
#else
#pragma warning disable CS0618
            cachedScaleManager = FindObjectOfType<ScaleManager>(true);
#pragma warning restore CS0618
#endif
        }
    }

    private void AutoAssignReferences()
    {
        if (xrOrigin == null)
        {
            xrOrigin = GetComponentInParent<XROrigin>(true);
        }

        if (targetCamera == null && xrOrigin != null)
        {
            targetCamera = xrOrigin.Camera;
        }
    }
}
