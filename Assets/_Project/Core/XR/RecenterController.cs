using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public sealed class RecenterController : MonoBehaviour
{
    private const float ReferenceRefreshSeconds = 0.5f;

    [Header("Targets")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private ScaleTransitionController transitionController;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private GravityProvider gravityProvider;

    [Header("Recenter Reference (optional)")]
    [Tooltip("If set, use this transform's forward as the desired view direction. Leave null to fall back to world Z+.")]
    [SerializeField] private Transform recenterAnchor;
    [Tooltip("Also snap player position to the anchor. When false (default) the player is only reoriented in place.")]
    [SerializeField] private bool snapToAnchorPosition = false;

    [Header("Input - Right B (secondaryButton)")]
    [SerializeField] private InputActionReference recenterAction;
    [SerializeField] private bool useRightSecondaryButton = true;
    [SerializeField, Min(0f)] private float holdSecondsToConfirm = 0.4f;

    [Header("Ground Recovery")]
    [Tooltip("Non-riding recenter is orientation-only. This recovery only lifts the rig if the player capsule is already below the ground after recenter.")]
    [SerializeField] private bool recoverIfBelowGroundAfterRecenter = true;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField, Min(0.1f)] private float groundProbeHeight = 6f;
    [SerializeField, Min(0.1f)] private float groundProbeDistance = 30f;
    [SerializeField, Min(0f)] private float groundLift = 0.04f;

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
    private QuestSwingRideController[] cachedSwingControllers;
    private ScaleManager cachedScaleManager;
    private bool actionHeldLastFrame;
    private bool secondaryHeldLastFrame;
    private bool keyboardHeldLastFrame;
    private float pressStartTime = -1f;
    private bool confirmFired;
    private bool isRecentering;
    private bool requireReleaseBeforeNextPress;
    private float nextReferenceRefreshTime;
    private readonly RaycastHit[] groundHits = new RaycastHit[12];

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
        CacheReferences(force: true);

        if (isRecentering || !CanRecenterNow())
        {
            return;
        }

        confirmFired = true;
        StartCoroutine(RecenterRoutine());
    }

    private IEnumerator RecenterRoutine()
    {
        isRecentering = true;
        CacheReferences(force: true);

        CatRideControllerV2 activeRide = routeToMountWhileRiding ? GetActiveRide() : null;
        QuestSwingRideController activeSwing = routeToMountWhileRiding ? GetActiveSwing() : null;

        if (useBlink && transitionController != null)
        {
            yield return StartCoroutine(BlinkAndApply(activeRide, activeSwing));
        }
        else
        {
            ApplyRecenter(activeRide, activeSwing);
        }

        isRecentering = false;
        requireReleaseBeforeNextPress = true;
        ResetPressState();
    }

    private IEnumerator BlinkAndApply(CatRideControllerV2 activeRide, QuestSwingRideController activeSwing)
    {
        float outAndHold = Mathf.Max(0.05f, blinkDuration * 0.45f);
        yield return StartCoroutine(transitionController.PlayBlink(outAndHold, targetCamera));
        ApplyRecenter(activeRide, activeSwing);
    }

    private void ApplyRecenter(CatRideControllerV2 activeRide, QuestSwingRideController activeSwing)
    {
        // Swing takes precedence: while the player is on the swing, the cat-ride routing must
        // NOT fire ApplyRecenterPose — that path lifts the rig because the capsule-recovery
        // raycast sees the swing's frame collider as "ground" and treats the seated player as
        // sunk into geometry.
        if (activeSwing != null)
        {
            activeSwing.RecenterMountedView();
            if (logDebug)
            {
                Debug.Log($"[Recenter] Routed to swing: {activeSwing.name}", this);
            }
            return;
        }

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

        Vector3 desiredForward = ResolveDesiredNeutralForward();
        Vector3 cameraWorldPosition = targetCamera.transform.position;
        Vector3 targetCameraPosition = cameraWorldPosition;
        if (snapToAnchorPosition && recenterAnchor != null)
        {
            targetCameraPosition = recenterAnchor.position;
        }

        Vector3 currentNeutralForward = ResolveCurrentNeutralForward();
        Quaternion yawDelta = Quaternion.FromToRotation(currentNeutralForward, desiredForward);
        bool characterControllerWasEnabled = SetCharacterControllerEnabled(false);

        Transform originTransform = xrOrigin.transform;
        originTransform.SetPositionAndRotation(
            cameraWorldPosition + yawDelta * (originTransform.position - cameraWorldPosition),
            yawDelta * originTransform.rotation);

        xrOrigin.MoveCameraToWorldLocation(targetCameraPosition);
        Physics.SyncTransforms();

        RestoreCharacterControllerEnabled(characterControllerWasEnabled);
        RecoverOriginIfBelowGround();

        if (gravityProvider != null)
        {
            gravityProvider.ResetFallForce();
        }

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(Vector3.zero);
        }

        if (logDebug)
        {
            Debug.Log(
                $"[Recenter] Applied. anchor={(recenterAnchor != null ? recenterAnchor.name : "<world Z+>")} " +
                $"snapPos={snapToAnchorPosition} camBefore={cameraWorldPosition} camAfter={targetCamera.transform.position} camFwd={targetCamera.transform.forward}",
                this);
        }
    }

    private Vector3 ResolveDesiredNeutralForward()
    {
        Vector3 desiredForward = recenterAnchor != null ? recenterAnchor.forward : targetCamera.transform.forward;
        desiredForward.y = 0f;

        if (desiredForward.sqrMagnitude < 0.0001f)
        {
            desiredForward = ResolveCurrentNeutralForward();
        }

        return desiredForward.normalized;
    }

    private Vector3 ResolveCurrentNeutralForward()
    {
        Vector3 forward = xrOrigin != null ? xrOrigin.transform.forward : transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        return forward.normalized;
    }

    private bool SetCharacterControllerEnabled(bool enabled)
    {
        if (characterController == null)
        {
            return false;
        }

        bool wasEnabled = characterController.enabled;
        characterController.enabled = enabled;
        return wasEnabled;
    }

    private void RestoreCharacterControllerEnabled(bool wasEnabled)
    {
        if (characterController != null)
        {
            characterController.enabled = wasEnabled;
        }
    }

    private bool RecoverOriginIfBelowGround()
    {
        if (!recoverIfBelowGroundAfterRecenter || xrOrigin == null)
        {
            return false;
        }

        Vector3 probePosition = ResolveGroundProbePosition();
        if (!TryProjectToGround(probePosition, out Vector3 groundPoint))
        {
            return false;
        }

        float bottomY = ResolvePlayerBottomY();
        float liftAmount = groundPoint.y + groundLift - bottomY;
        if (liftAmount <= 0f)
        {
            return false;
        }

        bool characterControllerWasEnabled = SetCharacterControllerEnabled(false);
        xrOrigin.transform.position += Vector3.up * liftAmount;
        Physics.SyncTransforms();
        RestoreCharacterControllerEnabled(characterControllerWasEnabled);
        return true;
    }

    private Vector3 ResolveGroundProbePosition()
    {
        if (targetCamera != null)
        {
            return targetCamera.transform.position;
        }

        if (characterController != null)
        {
            return characterController.bounds.center;
        }

        return xrOrigin != null ? xrOrigin.transform.position : transform.position;
    }

    private float ResolvePlayerBottomY()
    {
        if (characterController != null)
        {
            return ResolveCharacterControllerBottomY();
        }

        if (targetCamera != null)
        {
            return targetCamera.transform.position.y - ResolveCameraHeightAboveGround();
        }

        return xrOrigin != null ? xrOrigin.transform.position.y : transform.position.y;
    }

    private bool TryProjectToGround(Vector3 probePosition, out Vector3 groundPoint)
    {
        Vector3 origin = probePosition + Vector3.up * groundProbeHeight;
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHits,
            groundProbeHeight + groundProbeDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.PositiveInfinity;
        bool foundGround = false;
        groundPoint = probePosition;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = groundHits[i];
            Collider hitCollider = hit.collider;
            if (hitCollider == null || IsSelfCollider(hitCollider))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                groundPoint = hit.point;
                foundGround = true;
            }
        }

        return foundGround;
    }

    private float ResolveCameraHeightAboveGround()
    {
        if (targetCamera == null)
        {
            return 1.4f;
        }

        if (characterController != null)
        {
            float bottomY = ResolveCharacterControllerBottomY();
            float height = targetCamera.transform.position.y - bottomY;
            if (height > 0.05f)
            {
                return height;
            }
        }

        if (xrOrigin != null && xrOrigin.CameraInOriginSpaceHeight > 0.05f)
        {
            float verticalScale = Mathf.Abs(xrOrigin.transform.lossyScale.y);
            return Mathf.Max(0.05f, xrOrigin.CameraInOriginSpaceHeight * Mathf.Max(0.0001f, verticalScale));
        }

        return 1.4f;
    }

    private float ResolveCharacterControllerBottomY()
    {
        if (characterController == null)
        {
            return targetCamera != null ? targetCamera.transform.position.y - 1.4f : transform.position.y;
        }

        if (characterController.enabled)
        {
            return characterController.bounds.min.y;
        }

        Vector3 worldCenter = characterController.transform.TransformPoint(characterController.center);
        float verticalScale = Mathf.Max(0.0001f, Mathf.Abs(characterController.transform.lossyScale.y));
        return worldCenter.y - characterController.height * verticalScale * 0.5f;
    }

    private bool IsSelfCollider(Collider candidate)
    {
        if (candidate == null)
        {
            return true;
        }

        if (characterController != null && candidate == characterController)
        {
            return true;
        }

        return xrOrigin != null && candidate.transform.IsChildOf(xrOrigin.transform);
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

    private QuestSwingRideController GetActiveSwing()
    {
        if (cachedSwingControllers == null || cachedSwingControllers.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < cachedSwingControllers.Length; i++)
        {
            QuestSwingRideController controller = cachedSwingControllers[i];
            if (controller != null && controller.IsMounted)
            {
                return controller;
            }
        }

        return null;
    }

    private bool IsScaleTransitionActive()
    {
        return cachedScaleManager != null && cachedScaleManager.IsTransitioning;
    }

    private void CacheReferences(bool force = false)
    {
        if (!force && !ShouldRefreshReferences())
        {
            return;
        }

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

        if (characterController == null)
        {
            if (xrOrigin != null)
            {
                characterController = xrOrigin.GetComponent<CharacterController>();
                if (characterController == null)
                {
                    characterController = xrOrigin.GetComponentInChildren<CharacterController>(true);
                }
            }

            if (characterController == null && targetCamera != null)
            {
                characterController = targetCamera.GetComponentInParent<CharacterController>();
            }
        }

        if (gravityProvider == null)
        {
            if (xrOrigin != null)
            {
                gravityProvider = xrOrigin.GetComponentInChildren<GravityProvider>(true);
            }

            if (gravityProvider == null)
            {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
                gravityProvider = FindAnyObjectByType<GravityProvider>(FindObjectsInactive.Include);
#else
#pragma warning disable CS0618
                gravityProvider = FindObjectOfType<GravityProvider>(true);
#pragma warning restore CS0618
#endif
            }
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

        if (cachedSwingControllers == null || cachedSwingControllers.Length == 0)
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            cachedSwingControllers = FindObjectsByType<QuestSwingRideController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            cachedSwingControllers = FindObjectsOfType<QuestSwingRideController>(true);
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

    private bool ShouldRefreshReferences()
    {
        bool needsRefresh =
            xrOrigin == null ||
            targetCamera == null ||
            transitionController == null ||
            characterController == null ||
            gravityProvider == null ||
            rightControllerOrigin == null ||
            rightHaptics == null ||
            cachedRideControllers == null ||
            cachedRideControllers.Length == 0 ||
            cachedSwingControllers == null ||
            cachedSwingControllers.Length == 0 ||
            cachedScaleManager == null;

        if (!needsRefresh)
        {
            return false;
        }

        if (!Application.isPlaying)
        {
            return true;
        }

        if (Time.unscaledTime < nextReferenceRefreshTime)
        {
            return false;
        }

        nextReferenceRefreshTime = Time.unscaledTime + ReferenceRefreshSeconds;
        return true;
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

        if (characterController == null && xrOrigin != null)
        {
            characterController = xrOrigin.GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = xrOrigin.GetComponentInChildren<CharacterController>(true);
            }
        }
    }
}
