using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(ScaleTransitionController))]
public class ScaleManager : MonoBehaviour
{
    private const float MinCharacterControllerHeight = 0.01f;
    private const float CharacterControllerStepOffsetEpsilon = 0.001f;
    private const float CharacterControllerRadiusEpsilon = 0.001f;
    private const float MaxStepOffsetScaledHeightFraction = 0.45f;

    [Header("Core References")]
    [SerializeField] private Transform scaleRoot;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private ScaleTransitionController transitionController;
    [SerializeField] private ScaleSettings settings;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CatRideControllerV2 rideController;
    [SerializeField] private GravityProvider gravityProvider;

    [Header("XR Rig Scale")]
    [SerializeField] private bool keepXrRigShapeDuringScale = true;

    [Header("Optional Runtime Targets")]
    [SerializeField] private Component[] moveSpeedTargets;
    [SerializeField] private Component[] interactionDistanceTargets;

    [Header("Production Debug")]
    [SerializeField] private bool enableDebugKeyboardScaleShortcuts = false;
    [SerializeField] private InputActionReference normalScaleAction;
    [SerializeField] private InputActionReference smallScaleAction;
    [SerializeField] private InputActionReference largeScaleAction;
    [SerializeField] private bool logDebug = false;

    [Header("Quest Thumbstick Scale")]
    [SerializeField] private bool enableQuestThumbstickScale = true;
    [SerializeField] private InputActionReference rightThumbstickClickAction;
    [SerializeField, Min(0.1f)] private float rightThumbstickLongPressSeconds = 0.45f;
    [SerializeField, Min(0.05f)] private float rightThumbstickDoubleClickSeconds = 0.32f;
    [SerializeField, Min(0f)] private float thumbstickLocomotionSuppressSeconds = 0.15f;
    [SerializeField] private QuestLocomotionComfortProfile locomotionProfile;

    [Header("Scale Feedback")]
    [SerializeField] private bool useScaleShiftHaptics = true;
    [SerializeField, Range(0f, 1f)] private float scaleShiftHapticAmplitude = 0.45f;
    [SerializeField, Min(0f)] private float scaleShiftHapticDuration = 0.08f;

    [SerializeField] private ScaleState currentState = ScaleState.Normal;

    private bool isTransitioning;
    private float lastChangeTime;
    private float baseMoveSpeed = 1f;
    private bool baseMoveSpeedCaptured;
    private float[] baseInteractionDistances;
    private float baseControllerHeight;
    private float baseControllerRadius;
    private float baseControllerStepOffset;
    private Vector3 baseControllerCenter;
    private Vector3 baseControllerLossyScale = Vector3.one;
    private bool baseControllerCaptured;
    private Vector3 baseCameraPivotLocalPosition;
    private float baseCameraPivotParentScaleY = 1f;
    private bool baseCameraPivotCaptured;
    private float baseXrCameraYOffset;
    private float baseXrCameraOffsetParentScaleY = 1f;
    private bool baseXrCameraYOffsetCaptured;
    private Vector3 baseScaleRootLocalScale = Vector3.one;
    private bool baseScaleRootCaptured;
    private bool rightThumbstickWasPressed;
    private bool rightThumbstickLongPressConsumed;
    private float rightThumbstickPressStartTime;
    private float lastRightThumbstickClickTime = -999f;
    private Vector3 driftLastScaleRootPosition;
    private Quaternion driftLastScaleRootRotation = Quaternion.identity;
    private bool driftSamplingInitialized;
    private float lastThumbstickEventTime = -999f;
    private string lastThumbstickEventLabel = "none";
    private int driftStreakFrames;
    private HapticImpulsePlayer leftHaptics;
    private HapticImpulsePlayer rightHaptics;
    public ScaleState CurrentState => currentState;
    public bool IsSmallScale => currentState == ScaleState.Small;
    public bool IsTransitioning => isTransitioning;



    private void Awake()
    {
        AutoAssignReferences();
    }

    private void OnValidate()
    {
        AutoAssignReferences();
    }

    private void Start()
    {
        CacheBaseValues();
        ApplyScaleImmediate(currentState);
    }

    private void Update()
    {
        if (IsAnyRideActive())
        {
            ResetQuestScaleGesture();
            return;
        }

        UpdateQuestScaleGesture();
        SampleDriftDiagnostics();

        if (WasPressed(normalScaleAction, Key.Digit1))
            SetScale(ScaleState.Normal);

        if (WasPressed(smallScaleAction, Key.Digit2))
            SetScale(ScaleState.Small);

        if (WasPressed(largeScaleAction, Key.Digit3))
            SetScale(ScaleState.Large);
    }

    private void LateUpdate()
    {
        ClampGravityWhileGrounded();
    }


    private bool WasPressed(InputActionReference actionReference, Key debugKey)
    {
        if (actionReference != null && actionReference.action != null && actionReference.action.WasPressedThisFrame())
        {
            return true;
        }

        return enableDebugKeyboardScaleShortcuts &&
               Keyboard.current != null &&
               Keyboard.current[debugKey].wasPressedThisFrame;
    }

    private void UpdateQuestScaleGesture()
    {
        if (!enableQuestThumbstickScale)
        {
            ResetQuestScaleGesture();
            return;
        }

        bool pressed = IsRightThumbstickClickPressed();

        if (pressed && !rightThumbstickWasPressed)
        {
            rightThumbstickPressStartTime = Time.time;
            rightThumbstickLongPressConsumed = false;
            MarkThumbstickEvent("press-down");
        }

        if (pressed)
        {
            SuppressRightHandTurnInput();
        }

        if (pressed && !rightThumbstickLongPressConsumed &&
            Time.time >= rightThumbstickPressStartTime + rightThumbstickLongPressSeconds)
        {
            rightThumbstickLongPressConsumed = true;
            TryApplyQuestScaleLongPress();
        }

        if (!pressed && rightThumbstickWasPressed && !rightThumbstickLongPressConsumed)
        {
            SuppressRightHandTurnInput();
            MarkThumbstickEvent("release-click");
            TryApplyQuestScaleClick();
        }

        rightThumbstickWasPressed = pressed;
    }

    private void ClampGravityWhileGrounded()
    {
        if (gravityProvider == null || characterController == null || !characterController.enabled)
        {
            return;
        }

        if (characterController.isGrounded)
        {
            gravityProvider.ResetFallForce();
        }
    }

    private void MarkThumbstickEvent(string label)
    {
        lastThumbstickEventTime = Time.time;
        lastThumbstickEventLabel = label;
        if (logDebug)
        {
            Debug.Log($"[ScaleShift][Gesture] {label} | state={currentState} | t={Time.time:F3}");
        }
    }

    private void SampleDriftDiagnostics()
    {
        if (!logDebug || scaleRoot == null)
        {
            return;
        }

        Vector3 currentPosition = scaleRoot.position;
        Quaternion currentRotation = scaleRoot.rotation;

        if (!driftSamplingInitialized)
        {
            driftLastScaleRootPosition = currentPosition;
            driftLastScaleRootRotation = currentRotation;
            driftSamplingInitialized = true;
            return;
        }

        if (isTransitioning)
        {
            driftLastScaleRootPosition = currentPosition;
            driftLastScaleRootRotation = currentRotation;
            driftStreakFrames = 0;
            return;
        }

        Vector3 delta = currentPosition - driftLastScaleRootPosition;
        float deltaXZ = new Vector2(delta.x, delta.z).magnitude;
        float deltaY = delta.y;
        float yawDelta = Quaternion.Angle(driftLastScaleRootRotation, currentRotation);

        const float positionThreshold = 0.0005f;
        const float rotationThreshold = 0.05f;

        if (deltaXZ > positionThreshold || Mathf.Abs(deltaY) > positionThreshold || yawDelta > rotationThreshold)
        {
            driftStreakFrames++;
            float sinceEvent = Time.time - lastThumbstickEventTime;
            Vector3 ccVelocity = characterController != null ? characterController.velocity : Vector3.zero;
            bool ccGrounded = characterController != null && characterController.isGrounded;
            Debug.Log(
                $"[ScaleShift][Drift#{driftStreakFrames}] state={currentState} | " +
                $"dXZ={deltaXZ:F4}m dY={deltaY:F4}m yaw={yawDelta:F2}deg | " +
                $"ccVel={ccVelocity} ccGrounded={ccGrounded} | " +
                $"sinceEvent={sinceEvent:F2}s last={lastThumbstickEventLabel} | " +
                $"pos={currentPosition}");
        }
        else if (driftStreakFrames > 0)
        {
            Debug.Log($"[ScaleShift][Drift] streak ended after {driftStreakFrames} frames");
            driftStreakFrames = 0;
        }

        driftLastScaleRootPosition = currentPosition;
        driftLastScaleRootRotation = currentRotation;
    }

    private bool IsRightThumbstickClickPressed()
    {
        if (rightThumbstickClickAction != null && rightThumbstickClickAction.action != null)
        {
            return rightThumbstickClickAction.action.IsPressed();
        }

        return QuestInteractionUtils.TryReadPrimary2DAxisClick(true, out bool pressed) && pressed;
    }

    private bool TryApplyQuestScaleClick()
    {
        switch (currentState)
        {
            case ScaleState.Normal:
                // Normal -> Small requires double click.
                if (Time.time <= lastRightThumbstickClickTime + rightThumbstickDoubleClickSeconds)
                {
                    lastRightThumbstickClickTime = -999f;
                    SetScale(ScaleState.Small);
                    return true;
                }

                // First click: wait for possible second click.
                lastRightThumbstickClickTime = Time.time;
                return false;

            case ScaleState.Small:
                // Double click / short click is invalid while small.
                lastRightThumbstickClickTime = -999f;
                return false;

            case ScaleState.Large:
                // Large -> Normal also requires double click.
                if (Time.time <= lastRightThumbstickClickTime + rightThumbstickDoubleClickSeconds)
                {
                    lastRightThumbstickClickTime = -999f;
                    SetScale(ScaleState.Normal);
                    return true;
                }

                // First click: wait for possible second click.
                lastRightThumbstickClickTime = Time.time;
                return false;

            default:
                lastRightThumbstickClickTime = -999f;
                return false;
        }
    }



    private bool TryApplyQuestScaleLongPress()
    {
        MarkThumbstickEvent($"long-press@{currentState}");
        switch (currentState)
        {
            case ScaleState.Normal:
                lastRightThumbstickClickTime = -999f;
                SetScale(ScaleState.Large);
                return true;
            case ScaleState.Small:
                lastRightThumbstickClickTime = -999f;
                SetScale(ScaleState.Normal);
                return true;
            default:
                lastRightThumbstickClickTime = -999f;
                return false;
        }
    }

    private void ResetQuestScaleGesture()
    {
        rightThumbstickWasPressed = false;
        rightThumbstickLongPressConsumed = false;
        lastRightThumbstickClickTime = -999f;
    }

    private bool IsAnyRideActive()
    {
        if (rideController != null && rideController.IsRideActive)
        {
            return true;
        }

#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        CatRideControllerV2[] rideControllers = FindObjectsByType<CatRideControllerV2>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        CatRideControllerV2[] rideControllers = FindObjectsOfType<CatRideControllerV2>(true);
#pragma warning restore CS0618
#endif
        for (int i = 0; i < rideControllers.Length; i++)
        {
            if (rideControllers[i] != null && rideControllers[i].IsRideActive)
            {
                return true;
            }
        }

        // Treat an active swing the same as a cat ride for scale purposes: no thumbstick gesture,
        // no SetScale call slips through, no half-scaled body locked to a swing.
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        QuestSwingRideController[] swingControllers = FindObjectsByType<QuestSwingRideController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        QuestSwingRideController[] swingControllers = FindObjectsOfType<QuestSwingRideController>(true);
#pragma warning restore CS0618
#endif
        for (int i = 0; i < swingControllers.Length; i++)
        {
            if (swingControllers[i] != null && swingControllers[i].IsMounted)
            {
                return true;
            }
        }

        return false;
    }

    private void AutoAssignReferences()
    {
        if (scaleRoot == null)
        {
            scaleRoot = transform;
        }

        if (cameraPivot == null)
        {
            Transform foundPivot = transform.Find("Camera Offset");
            if (foundPivot != null)
            {
                cameraPivot = foundPivot;
            }
        }

        if (targetCamera == null)
        {
            targetCamera = GetComponentInChildren<Camera>(includeInactive: true);
            if (targetCamera == null)
            {
                targetCamera = QuestInteractionUtils.FindHeadCamera();
            }
        }

        if (transitionController == null)
        {
            transitionController = GetComponent<ScaleTransitionController>();
        }

        if (xrOrigin == null)
        {
            if (scaleRoot != null)
            {
                xrOrigin = scaleRoot.GetComponent<XROrigin>();
            }

            if (xrOrigin == null)
            {
                xrOrigin = GetComponentInParent<XROrigin>();
            }

            if (xrOrigin == null && targetCamera != null)
            {
                xrOrigin = targetCamera.GetComponentInParent<XROrigin>();
            }
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (gravityProvider == null)
        {
            if (xrOrigin != null)
            {
                gravityProvider = xrOrigin.GetComponentInChildren<GravityProvider>(true);
            }

            if (gravityProvider == null)
            {
                gravityProvider = GetComponentInChildren<GravityProvider>(true);
            }
        }

        if (settings == null)
        {
#if UNITY_EDITOR
            settings = AssetDatabase.LoadAssetAtPath<ScaleSettings>(
                "Assets/_Project/Features/ScaleShift/ScriptableObjects/ScaleSettings_SO.asset");
#endif
        }

        if (locomotionProfile == null)
        {
            locomotionProfile = GetComponent<QuestLocomotionComfortProfile>();
        }

        CacheHapticPlayers();
    }

    public void SetScale(ScaleState newState)
    {
        if (isTransitioning || settings == null || scaleRoot == null)
            return;

        if (newState == currentState)
            return;

        if (Time.time < lastChangeTime + settings.cooldown)
            return;

        // Hard guard: no scale changes while the player is mounted on a cat or seated on a swing.
        // The Update() loop already early-outs on the thumbstick path, but this also blocks
        // external scripts (UI menus, debug actions, etc.) from sneaking a SetScale through.
        if (IsAnyRideActive())
        {
            if (logDebug)
            {
                Debug.Log($"[ScaleShift] Rejected SetScale({newState}) — player is on a mount/swing.", this);
            }
            return;
        }

        WonderfulWorld.Audio.WonderlandAudioOneShotPlayer.Play2D("WW_SFX_ScaleShift", volumeScale: 1f, maxVoices: 3);
        StartCoroutine(SetScaleRoutine(newState));
    }

    public void SetThumbstickScaleEnabled(bool enabled)
    {
        enableQuestThumbstickScale = enabled;
        if (!enabled)
        {
            ResetQuestScaleGesture();
        }
    }

    public void SetNormalScale()
    {
        SetScale(ScaleState.Normal);
    }

    public void SetSmallScale()
    {
        SetScale(ScaleState.Small);
    }

    public void SetLargeScale()
    {
        SetScale(ScaleState.Large);
    }

    private IEnumerator SetScaleRoutine(ScaleState newState)
    {
        isTransitioning = true;
        PlayScaleShiftHaptics();

        if (transitionController != null)
        {
            transitionController.ConfigureTimings(
                settings.fadeOutDuration,
                settings.blackHoldDuration,
                settings.fadeInDuration);
            yield return transitionController.PlayBlink(settings.blinkDuration, targetCamera);
        }

        currentState = newState;
        ApplyScaleImmediate(currentState);
        lastChangeTime = Time.time;
        driftSamplingInitialized = false;

        isTransitioning = false;
    }

    private void CacheHapticPlayers()
    {
        if (leftHaptics == null)
        {
            Transform leftOrigin = QuestInteractionUtils.FindControllerRayOrigin(false);
            leftHaptics = QuestInteractionUtils.FindHapticPlayer(false, leftOrigin);
        }

        if (rightHaptics == null)
        {
            Transform rightOrigin = QuestInteractionUtils.FindControllerRayOrigin(true);
            rightHaptics = QuestInteractionUtils.FindHapticPlayer(true, rightOrigin);
        }
    }

    private void PlayScaleShiftHaptics()
    {
        if (!useScaleShiftHaptics)
        {
            return;
        }

        CacheHapticPlayers();
        QuestInteractionUtils.SendHaptic(leftHaptics, scaleShiftHapticAmplitude, scaleShiftHapticDuration);
        QuestInteractionUtils.SendHaptic(rightHaptics, scaleShiftHapticAmplitude, scaleShiftHapticDuration);
    }

    private void ApplyScaleImmediate(ScaleState state)
    {
        ScalePoseAnchor preScaleAnchor = CaptureScalePoseAnchor();
        CharacterControllerMutation characterControllerMutation = BeginCharacterControllerMutation();

        ScaleSettings.ScaleProfile profile = GetProfile(state);

        ApplyScaleRoot(profile.playerScale);

        if (targetCamera != null)
            targetCamera.nearClipPlane = profile.nearClip;

        bool useUnifiedXrRigScale = ShouldUseUnifiedXrRigScale();
        float eyeHeightMultiplier = useUnifiedXrRigScale ? profile.playerScale : profile.eyeHeightMultiplier;
        float controllerHeightMultiplier = useUnifiedXrRigScale ? profile.playerScale : profile.controllerHeightMultiplier;
        float controllerRadiusMultiplier = useUnifiedXrRigScale ? profile.playerScale : profile.controllerRadiusMultiplier;

        ApplyEyeHeight(eyeHeightMultiplier);
        ApplyMoveSpeed(profile.moveSpeedMultiplier);
        ApplyInteractionDistance(profile.interactionDistanceMultiplier);
        float targetStepOffset = ApplyCharacterController(controllerHeightMultiplier, controllerRadiusMultiplier);
        Physics.SyncTransforms();
        RestoreScalePoseAnchorWhileControllerDisabled(preScaleAnchor);
        Physics.SyncTransforms();
        CompleteCharacterControllerMutation(characterControllerMutation, targetStepOffset);

        if (logDebug)
        {
            Debug.Log(
                $"[ScaleShift] Applied {state} | " +
                $"playerScale={profile.playerScale}, " +
                $"moveSpeedMultiplier={profile.moveSpeedMultiplier}, " +
                $"interactionDistanceMultiplier={profile.interactionDistanceMultiplier}, " +
                $"nearClip={profile.nearClip}");
        }
    }

    private void CacheBaseValues()
    {
        if (moveSpeedTargets != null)
        {
            foreach (Component target in moveSpeedTargets)
            {
                if (TryGetFloatMemberValue(target, out float value, "moveSpeed", "m_MoveSpeed", "speed"))
                {
                    baseMoveSpeed = value;
                    baseMoveSpeedCaptured = true;
                    break;
                }
            }
        }

        if (interactionDistanceTargets != null)
        {
            baseInteractionDistances = new float[interactionDistanceTargets.Length];

            for (int i = 0; i < interactionDistanceTargets.Length; i++)
            {
                if (!TryGetFloatMemberValue(
                        interactionDistanceTargets[i],
                        out baseInteractionDistances[i],
                        "maxRaycastDistance",
                        "m_MaxRaycastDistance",
                        "maxDistance",
                        "m_MaxDistance"))
                {
                    baseInteractionDistances[i] = 0f;
                }
            }
        }

        if (characterController != null)
        {
            PrepareCharacterControllerForStepOffsetWrite();
            baseControllerHeight = characterController.height;
            baseControllerRadius = characterController.radius;
            baseControllerStepOffset = Mathf.Clamp(
                characterController.stepOffset,
                0f,
                GetMaxAllowedStepOffset(
                    characterController.height,
                    characterController.radius,
                    characterController.transform.lossyScale));
            baseControllerCenter = characterController.center;
            baseControllerLossyScale = characterController.transform.lossyScale;
            baseControllerCaptured = true;
        }


        if (cameraPivot != null)
        {
            baseCameraPivotLocalPosition = cameraPivot.localPosition;
            baseCameraPivotParentScaleY = GetParentLossyScaleY(cameraPivot);
            baseCameraPivotCaptured = true;
        }

        if (xrOrigin != null)
        {
            baseXrCameraYOffset = xrOrigin.CameraYOffset;
            Transform offsetTransform = ResolveXrCameraOffsetTransform();
            baseXrCameraOffsetParentScaleY = offsetTransform != null
                ? GetParentLossyScaleY(offsetTransform)
                : GetParentLossyScaleY(cameraPivot);
            baseXrCameraYOffsetCaptured = true;
        }

        if (scaleRoot != null)
        {
            baseScaleRootLocalScale = scaleRoot.localScale;
            baseScaleRootCaptured = true;
        }
    }

    private ScaleSettings.ScaleProfile GetProfile(ScaleState state)
    {
        switch (state)
        {
            case ScaleState.Small:
                return settings.small;
            case ScaleState.Large:
                return settings.large;
            default:
                return settings.normal;
        }
    }

    private void ApplyMoveSpeed(float multiplier)
    {
        if (!baseMoveSpeedCaptured || moveSpeedTargets == null)
            return;

        float value = baseMoveSpeed * multiplier;

        foreach (Component target in moveSpeedTargets)
        {
            TrySetFloatMemberValue(target, value, "moveSpeed", "m_MoveSpeed", "speed");
        }
    }

    private void ApplyInteractionDistance(float multiplier)
    {
        if (interactionDistanceTargets == null || baseInteractionDistances == null)
            return;

        for (int i = 0; i < interactionDistanceTargets.Length; i++)
        {
            if (baseInteractionDistances[i] <= 0f)
                continue;

            float value = baseInteractionDistances[i] * multiplier;
            TrySetFloatMemberValue(
                interactionDistanceTargets[i],
                value,
                "maxRaycastDistance",
                "m_MaxRaycastDistance",
                "maxDistance",
                "m_MaxDistance");
        }
    }

    private float ApplyCharacterController(float heightMultiplier, float radiusMultiplier)
    {
        if (!baseControllerCaptured || characterController == null)
            return 0f;

        Vector3 currentLossyScale = characterController.transform.lossyScale;
        float verticalScaleRatio = GetSafeAxisScaleRatio(currentLossyScale.y, baseControllerLossyScale.y);
        float radiusScaleRatio = GetSafeScaleRatio(
            GetHorizontalScale(currentLossyScale),
            GetHorizontalScale(baseControllerLossyScale));
        float localHeightMultiplier = heightMultiplier / verticalScaleRatio;
        float localRadiusMultiplier = radiusMultiplier / radiusScaleRatio;
        float localHeight = Mathf.Max(MinCharacterControllerHeight, baseControllerHeight * localHeightMultiplier);
        float localRadius = Mathf.Max(0f, baseControllerRadius * localRadiusMultiplier);
        localHeight = Mathf.Max(localHeight, localRadius * 2f + CharacterControllerRadiusEpsilon);
        localRadius = Mathf.Clamp(localRadius, 0f, Mathf.Max(0f, localHeight * 0.5f - CharacterControllerRadiusEpsilon));

        PrepareCharacterControllerForStepOffsetWrite();
        ForceCharacterControllerStepOffsetZero();

        if (characterController.height < localHeight)
        {
            characterController.height = localHeight;
        }

        characterController.radius = localRadius;
        characterController.height = localHeight;

        Vector3 center = baseControllerCenter;
        center.y = baseControllerCenter.y * localHeightMultiplier;
        characterController.center = center;

        return baseControllerStepOffset * Mathf.Max(0f, heightMultiplier);
    }

    private void ApplyScaleRoot(float playerScale)
    {
        if (scaleRoot == null)
            return;

        Vector3 rootBaseScale = baseScaleRootCaptured ? baseScaleRootLocalScale : Vector3.one;
        scaleRoot.localScale = rootBaseScale * playerScale;
    }

    private void ApplyEyeHeight(float eyeHeightMultiplier)
    {
        if ((!baseCameraPivotCaptured || cameraPivot == null) && (!baseXrCameraYOffsetCaptured || xrOrigin == null))
            return;

        bool appliedThroughXrOrigin = false;
        if (xrOrigin != null && baseXrCameraYOffsetCaptured)
        {
            Transform offsetTransform = ResolveXrCameraOffsetTransform();
            float currentParentScaleY = offsetTransform != null
                ? GetParentLossyScaleY(offsetTransform)
                : GetParentLossyScaleY(cameraPivot);
            xrOrigin.CameraYOffset = ResolveCompensatedLocalY(
                baseXrCameraYOffset,
                baseXrCameraOffsetParentScaleY,
                currentParentScaleY,
                eyeHeightMultiplier);
            appliedThroughXrOrigin = IsXrManagedCameraPivot(cameraPivot);
        }

        if (!appliedThroughXrOrigin && cameraPivot != null && baseCameraPivotCaptured)
        {
            Vector3 localPosition = baseCameraPivotLocalPosition;
            localPosition.y = ResolveCompensatedLocalY(
                baseCameraPivotLocalPosition.y,
                baseCameraPivotParentScaleY,
                GetParentLossyScaleY(cameraPivot),
                eyeHeightMultiplier);
            cameraPivot.localPosition = localPosition;
        }
    }

    private bool ShouldUseUnifiedXrRigScale()
    {
        return keepXrRigShapeDuringScale && xrOrigin != null;
    }

    private ScalePoseAnchor CaptureScalePoseAnchor()
    {
        return new ScalePoseAnchor
        {
            cameraWorldPosition = targetCamera != null ? targetCamera.transform.position : Vector3.zero,
            groundY = ResolveGroundY(),
            hasCamera = targetCamera != null,
            hasRoot = scaleRoot != null,
        };
    }

    private float ResolveGroundY()
    {
        if (characterController != null && characterController.enabled)
        {
            return characterController.bounds.min.y;
        }

        if (characterController != null)
        {
            Vector3 worldCenter = characterController.transform.TransformPoint(characterController.center);
            float verticalScale = Mathf.Max(0.0001f, Mathf.Abs(characterController.transform.lossyScale.y));
            return worldCenter.y - characterController.height * verticalScale * 0.5f;
        }

        if (scaleRoot != null)
        {
            return scaleRoot.position.y;
        }

        return transform.position.y;
    }

    private void RestoreScalePoseAnchorWhileControllerDisabled(ScalePoseAnchor anchor)
    {
        if (scaleRoot == null)
        {
            return;
        }

        Vector3 delta = Vector3.zero;

        if (anchor.hasCamera && targetCamera != null)
        {
            Vector3 currentCameraPosition = targetCamera.transform.position;
            delta.x = anchor.cameraWorldPosition.x - currentCameraPosition.x;
            delta.z = anchor.cameraWorldPosition.z - currentCameraPosition.z;
        }

        if (anchor.hasRoot)
        {
            delta.y = anchor.groundY - ResolveGroundY();
        }

        scaleRoot.position += delta;
    }

    private CharacterControllerMutation BeginCharacterControllerMutation()
    {
        if (characterController == null)
        {
            return default;
        }

        CharacterControllerMutation mutation = new CharacterControllerMutation
        {
            hasController = true,
            wasEnabled = characterController.enabled
        };

        PrepareCharacterControllerForStepOffsetWrite();
        ForceCharacterControllerStepOffsetZero();
        if (characterController.enabled)
        {
            characterController.enabled = false;
        }

        return mutation;
    }

    private void CompleteCharacterControllerMutation(CharacterControllerMutation mutation, float targetStepOffset)
    {
        if (!mutation.hasController || characterController == null)
        {
            return;
        }

        PrepareCharacterControllerForStepOffsetWrite();
        ForceCharacterControllerStepOffsetZero();

        if (mutation.wasEnabled)
        {
            characterController.enabled = true;
            Physics.SyncTransforms();
            ApplySafeCharacterControllerStepOffset(targetStepOffset);
        }
    }

    private void ForceCharacterControllerStepOffsetZero()
    {
        if (characterController == null)
        {
            return;
        }

        if (characterController.stepOffset != 0f)
        {
            characterController.stepOffset = 0f;
        }
    }

    private void PrepareCharacterControllerForStepOffsetWrite()
    {
        if (characterController == null)
        {
            return;
        }

        float currentStepOffset = Mathf.Max(0f, characterController.stepOffset);
        float currentRadius = Mathf.Max(0f, characterController.radius);
        Vector3 lossyScale = characterController.transform.lossyScale;
        float scaleY = Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y));
        float scaleH = GetHorizontalScale(lossyScale);
        float scaledRadius = currentRadius * scaleH;
        float requiredHeightForScaledStep = Mathf.Max(
            0f,
            (currentStepOffset + CharacterControllerStepOffsetEpsilon * 2f - scaledRadius * 2f) / scaleY);
        float requiredHeight = Mathf.Max(
            MinCharacterControllerHeight,
            characterController.height,
            currentStepOffset + CharacterControllerStepOffsetEpsilon * 2f,
            currentRadius * 2f + CharacterControllerRadiusEpsilon * 2f,
            requiredHeightForScaledStep);

        if (characterController.height < requiredHeight)
        {
            characterController.height = requiredHeight;
        }

        float maxRadius = Mathf.Max(0f, requiredHeight * 0.5f - CharacterControllerRadiusEpsilon);
        if (characterController.radius > maxRadius)
        {
            characterController.radius = maxRadius;
        }
    }

    private void ApplySafeCharacterControllerStepOffset(float targetStepOffset)
    {
        if (characterController == null)
        {
            return;
        }

        float maxAllowedStepOffset = GetMaxAllowedStepOffset(
            characterController.height,
            characterController.radius,
            characterController.transform.lossyScale);
        float safeStepOffset = Mathf.Clamp(targetStepOffset, 0f, maxAllowedStepOffset);
        if (safeStepOffset > CharacterControllerStepOffsetEpsilon)
        {
            characterController.stepOffset = safeStepOffset;
        }
    }

    private static float GetMaxAllowedStepOffset(float controllerHeight, float controllerRadius, Vector3 controllerLossyScale)
    {
        float scaleY = Mathf.Max(0.0001f, Mathf.Abs(controllerLossyScale.y));
        float scaleH = GetHorizontalScale(controllerLossyScale);
        float scaledHeight = controllerHeight * scaleY;
        float scaledRadius = controllerRadius * scaleH;
        float scaledExtentLimit = Mathf.Max(0f, scaledHeight + 2f * scaledRadius - CharacterControllerStepOffsetEpsilon);
        float scaledComfortLimit = Mathf.Max(0f, scaledHeight * MaxStepOffsetScaledHeightFraction);
        float safeLocalLimit = Mathf.Max(0f, controllerHeight - CharacterControllerStepOffsetEpsilon);
        return Mathf.Max(0f, Mathf.Min(scaledExtentLimit, scaledComfortLimit, safeLocalLimit));
    }

    private Transform ResolveXrCameraOffsetTransform()
    {
        if (xrOrigin == null || xrOrigin.CameraFloorOffsetObject == null)
        {
            return null;
        }

        return xrOrigin.CameraFloorOffsetObject.transform;
    }

    private bool IsXrManagedCameraPivot(Transform candidate)
    {
        Transform offsetTransform = ResolveXrCameraOffsetTransform();
        return candidate != null && offsetTransform != null && candidate == offsetTransform;
    }

    private static float ResolveCompensatedLocalY(
        float baseLocalY,
        float baseParentScaleY,
        float currentParentScaleY,
        float targetWorldMultiplier)
    {
        float baseWorldY = baseLocalY * Mathf.Max(0.0001f, Mathf.Abs(baseParentScaleY));
        float targetWorldY = baseWorldY * targetWorldMultiplier;
        return targetWorldY / Mathf.Max(0.0001f, Mathf.Abs(currentParentScaleY));
    }

    private static float GetParentLossyScaleY(Transform child)
    {
        if (child == null || child.parent == null)
        {
            return 1f;
        }

        return Mathf.Max(0.0001f, Mathf.Abs(child.parent.lossyScale.y));
    }

    private static float GetHorizontalScale(Vector3 scale)
    {
        return Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));
    }

    private static float GetSafeAxisScaleRatio(float current, float baseline)
    {
        return GetSafeScaleRatio(Mathf.Abs(current), Mathf.Abs(baseline));
    }

    private static float GetSafeScaleRatio(float current, float baseline)
    {
        return Mathf.Max(0.0001f, current) / Mathf.Max(0.0001f, baseline);
    }

    private void SuppressRightHandTurnInput()
    {
        if (locomotionProfile != null && thumbstickLocomotionSuppressSeconds > 0f)
        {
            locomotionProfile.SuppressRightHandTurn(thumbstickLocomotionSuppressSeconds);
        }
    }

    private static bool TryGetFloatMemberValue(Component target, out float value, params string[] memberNames)
    {
        value = 0f;

        if (target == null)
            return false;

        foreach (string memberName in memberNames)
        {
            FieldInfo field = target.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(float))
            {
                value = (float)field.GetValue(target);
                return true;
            }

            PropertyInfo property = target.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanRead && property.PropertyType == typeof(float))
            {
                value = (float)property.GetValue(target);
                return true;
            }
        }

        return false;
    }

    private static bool TrySetFloatMemberValue(Component target, float value, params string[] memberNames)
    {
        if (target == null)
            return false;

        foreach (string memberName in memberNames)
        {
            FieldInfo field = target.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(target, value);
                return true;
            }

            PropertyInfo property = target.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite && property.PropertyType == typeof(float))
            {
                property.SetValue(target, value);
                return true;
            }
        }

        return false;
    }

    private struct ScalePoseAnchor
    {
        public Vector3 cameraWorldPosition;
        public float groundY;
        public bool hasCamera;
        public bool hasRoot;
    }

    private struct CharacterControllerMutation
    {
        public bool hasController;
        public bool wasEnabled;
    }
}
