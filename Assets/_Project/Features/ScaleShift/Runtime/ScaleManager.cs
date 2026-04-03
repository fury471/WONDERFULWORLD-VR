using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScaleManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Transform scaleRoot;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private ScaleTransitionController transitionController;
    [SerializeField] private ScaleSettings settings;
    [SerializeField] private CharacterController characterController;

    [Header("Optional Runtime Targets")]
    [SerializeField] private Component[] moveSpeedTargets;
    [SerializeField] private Component[] interactionDistanceTargets;

    [SerializeField] private ScaleState currentState = ScaleState.Normal;

    private bool isTransitioning;
    private float lastChangeTime;
    private float baseMoveSpeed = 1f;
    private bool baseMoveSpeedCaptured;
    private float[] baseInteractionDistances;
    private float baseControllerHeight;
    private float baseControllerRadius;
    private Vector3 baseControllerCenter;
    private bool baseControllerCaptured;
    private Vector3 baseCameraPivotLocalPosition;
    private bool baseCameraPivotCaptured;

    private void Start()
    {
        CacheBaseValues();
        ApplyScaleImmediate(currentState);
    }

    private void Update()
    {
        // Temporary debug input for sandbox only
        var keyboard = Keyboard.current;
        
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            SetScale(ScaleState.Normal);

        if (keyboard.digit2Key.wasPressedThisFrame)
            SetScale(ScaleState.Small);

        if (keyboard.digit3Key.wasPressedThisFrame)
            SetScale(ScaleState.Large);
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
        ScaleSettings.ScaleProfile profile = GetProfile(state);

        if (scaleRoot != null)
            scaleRoot.localScale = Vector3.one * profile.playerScale;

        if (targetCamera != null)
            targetCamera.nearClipPlane = profile.nearClip;

        ApplyEyeHeight(profile.eyeHeightMultiplier);
        ApplyMoveSpeed(profile.moveSpeedMultiplier);
        ApplyInteractionDistance(profile.interactionDistanceMultiplier);
        ApplyCharacterController(profile.controllerHeightMultiplier, profile.controllerRadiusMultiplier);

        Debug.Log(
            $"[ScaleShift] Applied {state} | " +
            $"playerScale={profile.playerScale}, " +
            $"moveSpeedMultiplier={profile.moveSpeedMultiplier}, " +
            $"interactionDistanceMultiplier={profile.interactionDistanceMultiplier}, " +
            $"nearClip={profile.nearClip}");
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
            baseControllerCenter = characterController.center;
            baseControllerCaptured = true;
        }

        if (cameraPivot != null)
        {
            baseCameraPivotLocalPosition = cameraPivot.localPosition;
            baseCameraPivotCaptured = true;
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

        characterController.height = baseControllerHeight * heightMultiplier;
        characterController.radius = baseControllerRadius * radiusMultiplier;

        Vector3 center = baseControllerCenter;
        center.y = (baseControllerCenter.y * heightMultiplier);
        characterController.center = center;
    }

    private void ApplyEyeHeight(float eyeHeightMultiplier)
    {
        if (!baseCameraPivotCaptured || cameraPivot == null)
            return;

        Vector3 localPosition = baseCameraPivotLocalPosition;
        localPosition.y = baseCameraPivotLocalPosition.y * eyeHeightMultiplier;
        cameraPivot.localPosition = localPosition;
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
}
