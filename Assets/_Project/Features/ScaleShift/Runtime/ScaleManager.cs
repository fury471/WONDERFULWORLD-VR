using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(ScaleTransitionController))]
public class ScaleManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Transform scaleRoot;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private ScaleTransitionController transitionController;
    [SerializeField] private ScaleSettings settings;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CatRideControllerV2 rideController;

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
    private bool baseControllerCaptured;
    private Vector3 baseCameraPivotLocalPosition;
    private bool baseCameraPivotCaptured;
    private Vector3 baseScaleRootLocalScale = Vector3.one;
    private bool baseScaleRootCaptured;
    private bool rightThumbstickWasPressed;
    private bool rightThumbstickLongPressConsumed;
    private float rightThumbstickPressStartTime;
    private float lastRightThumbstickClickTime = -999f;
    public ScaleState CurrentState => currentState;
    public bool IsSmallScale => currentState == ScaleState.Small;



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

        if (WasPressed(normalScaleAction, Key.Digit1))
            SetScale(ScaleState.Normal);

        if (WasPressed(smallScaleAction, Key.Digit2))
            SetScale(ScaleState.Small);

        if (WasPressed(largeScaleAction, Key.Digit3))
            SetScale(ScaleState.Large);
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
            TryApplyQuestScaleClick();
        }

        rightThumbstickWasPressed = pressed;
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
                if (Time.time <= lastRightThumbstickClickTime + rightThumbstickDoubleClickSeconds)
                {
                    lastRightThumbstickClickTime = -999f;
                    SetScale(ScaleState.Small);
                    return true;
                }

                lastRightThumbstickClickTime = Time.time;
                return false;
            case ScaleState.Large:
                lastRightThumbstickClickTime = -999f;
                SetScale(ScaleState.Normal);
                return true;
            default:
                lastRightThumbstickClickTime = -999f;
                return false;
        }
    }

    private bool TryApplyQuestScaleLongPress()
    {
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
                targetCamera = Camera.main;
            }
        }

        if (transitionController == null)
        {
            transitionController = GetComponent<ScaleTransitionController>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
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
    }

    public void SetScale(ScaleState newState)
    {
        if (isTransitioning || settings == null || scaleRoot == null)
            return;

        if (newState == currentState)
            return;

        if (Time.time < lastChangeTime + settings.cooldown)
            return;

        StartCoroutine(SetScaleRoutine(newState));
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

        isTransitioning = false;
    }

    private void ApplyScaleImmediate(ScaleState state)
    {
        ScalePoseAnchor preScaleAnchor = CaptureScalePoseAnchor();
        bool characterControllerWasEnabled = SetCharacterControllerEnabled(false);

        ScaleSettings.ScaleProfile profile = GetProfile(state);

        ApplyScaleRoot(profile.playerScale);

        if (targetCamera != null)
            targetCamera.nearClipPlane = profile.nearClip;

        ApplyEyeHeight(profile.eyeHeightMultiplier);
        ApplyMoveSpeed(profile.moveSpeedMultiplier);
        ApplyInteractionDistance(profile.interactionDistanceMultiplier);
        ApplyCharacterController(profile.controllerHeightMultiplier, profile.controllerRadiusMultiplier);
        RestoreCharacterControllerEnabled(characterControllerWasEnabled);
        Physics.SyncTransforms();
        RestoreScalePoseAnchor(preScaleAnchor);

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
            baseControllerHeight = characterController.height;
            baseControllerRadius = characterController.radius;
            baseControllerStepOffset = characterController.stepOffset;
            baseControllerCenter = characterController.center;
            baseControllerCaptured = true;
        }


        if (cameraPivot != null)
        {
            baseCameraPivotLocalPosition = cameraPivot.localPosition;
            baseCameraPivotCaptured = true;
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

    private void ApplyCharacterController(float heightMultiplier, float radiusMultiplier)
    {
        if (!baseControllerCaptured || characterController == null)
            return;

        float scaledHeight = baseControllerHeight * heightMultiplier;
        float scaledRadius = baseControllerRadius * radiusMultiplier;

        characterController.stepOffset = 0f;
        characterController.height = scaledHeight;
        characterController.radius = scaledRadius;

        float scaledStepOffset = baseControllerStepOffset * heightMultiplier * GetCharacterControllerVerticalScale();
        float maxAllowedStepOffset = GetMaxAllowedStepOffset(scaledHeight, scaledRadius);
        characterController.stepOffset = Mathf.Min(scaledStepOffset, maxAllowedStepOffset);

        Vector3 center = baseControllerCenter;
        center.y = baseControllerCenter.y * heightMultiplier;
        characterController.center = center;
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
        if (!baseCameraPivotCaptured || cameraPivot == null)
            return;

        Vector3 localPosition = baseCameraPivotLocalPosition;
        localPosition.y = baseCameraPivotLocalPosition.y * eyeHeightMultiplier;
        cameraPivot.localPosition = localPosition;
    }

    private ScalePoseAnchor CaptureScalePoseAnchor()
    {
        return new ScalePoseAnchor
        {
            cameraWorldPosition = targetCamera != null ? targetCamera.transform.position : Vector3.zero,
            rootY = scaleRoot != null ? scaleRoot.position.y : ResolveGroundY(),
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

    private void RestoreScalePoseAnchor(ScalePoseAnchor anchor)
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
            delta.y = anchor.rootY - scaleRoot.position.y;
        }

        bool characterControllerWasEnabled = SetCharacterControllerEnabled(false);
        scaleRoot.position += delta;
        RestoreCharacterControllerEnabled(characterControllerWasEnabled);
        Physics.SyncTransforms();
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
            ClampCharacterControllerStepOffset();
            characterController.enabled = wasEnabled;
        }
    }

    private void ClampCharacterControllerStepOffset()
    {
        if (characterController == null)
        {
            return;
        }

        float maxAllowedStepOffset = GetMaxAllowedStepOffset(characterController.height, characterController.radius);
        if (characterController.stepOffset > maxAllowedStepOffset)
        {
            characterController.stepOffset = maxAllowedStepOffset;
        }
    }

    private float GetMaxAllowedStepOffset(float controllerHeight, float controllerRadius)
    {
        float verticalScale = GetCharacterControllerVerticalScale();
        float radiusScale = GetCharacterControllerRadiusScale();
        float scaledHeight = controllerHeight * verticalScale;
        float scaledRadius = controllerRadius * radiusScale;
        return Mathf.Max(0f, scaledHeight + scaledRadius * 2f - 0.001f);
    }

    private float GetCharacterControllerVerticalScale()
    {
        return characterController != null
            ? Mathf.Max(0.0001f, Mathf.Abs(characterController.transform.lossyScale.y))
            : 1f;
    }

    private float GetCharacterControllerRadiusScale()
    {
        if (characterController == null)
        {
            return 1f;
        }

        Vector3 scale = characterController.transform.lossyScale;
        return Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));
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
        public float rootY;
        public bool hasCamera;
        public bool hasRoot;
    }
}
