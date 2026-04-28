using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class CatRideControllerV2 : MonoBehaviour
{
    public enum RideState
    {
        Idle,
        Mounting,
        MountedManual,
        MountedAuto,
        Dismounting
    }

    [Header("References")]
    [SerializeField] private Transform seatAnchor;
    [SerializeField] private Transform mountedViewAnchor;
    [SerializeField] private Transform dismountPoint;
    [SerializeField] private Collider mountTrigger;
    [SerializeField] private GameObject playerRigRoot;
    [SerializeField] private GameObject locomotionRoot;
    [SerializeField] private GameObject xrDeviceSimulatorRoot;
    [SerializeField] private MountSettings_SO settings;
    [SerializeField] private Animator kittyAnimator;


    [Header("Mount Access")]
    [SerializeField] private float remountDistance = 1.25f;

    [Header("Manual Ride")]
    [SerializeField] private float manualMoveSpeed = 4f;
    [SerializeField] private float manualTurnSpeed = 120f;
    [SerializeField] private Key mountKey = Key.F;
    [SerializeField] private Key dismountKey = Key.F;

    [Header("Auto Ride")]
    [SerializeField] private List<Transform> autoRoutePoints = new List<Transform>();

    [Header("Fallback Blend")]
    [SerializeField] private float fallbackMountBlendTime = 0.25f;
    [SerializeField] private float fallbackDismountBlendTime = 0.25f;
    [SerializeField] private float dismountGroundLift = 0.05f;
    [SerializeField] private float dismountUnlockDelay = 0.08f;


    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    [SerializeField] private RideState currentState = RideState.Idle;

    private CharacterController playerCharacterController;
    private bool playerCharacterControllerWasEnabled;
    private bool locomotionRootWasActive;

    private Transform trackedHeadTransform;

    private XRDeviceSimulator xrDeviceSimulator;
    private bool simulatorKeyboardXWasEnabled;
    private bool simulatorKeyboardYWasEnabled;
    private bool simulatorKeyboardZWasEnabled;

    private int currentAutoIndex = 0;
    private Coroutine stateRoutine;

    private void Awake()
    {
        CacheRigReferences();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (currentState == RideState.Idle)
        {
            if (Keyboard.current[mountKey].wasPressedThisFrame && IsPlayerInsideMountZone())
            {
                StartMount();
            }

            return;
        }

        if (currentState == RideState.MountedManual)
        {
            HandleManualRide();

            if (Keyboard.current[dismountKey].wasPressedThisFrame)
            {
                StartDismount();
            }

            return;
        }

        if (currentState == RideState.MountedAuto)
        {
            HandleAutoRide();

            if (Keyboard.current[dismountKey].wasPressedThisFrame)
            {
                StartDismount();
            }
        }
    }

    private void CacheRigReferences()
    {
        if (playerRigRoot != null)
        {
            playerCharacterController = playerRigRoot.GetComponent<CharacterController>();

            Transform cameraOffset = playerRigRoot.transform.Find("Camera Offset");
            if (cameraOffset != null)
            {
                Transform mainCamera = cameraOffset.Find("Main Camera");
                if (mainCamera != null)
                {
                    trackedHeadTransform = mainCamera;
                }
            }
        }

        if (xrDeviceSimulatorRoot != null && xrDeviceSimulator == null)
        {
            xrDeviceSimulator = xrDeviceSimulatorRoot.GetComponent<XRDeviceSimulator>();
        }
    }

    private bool IsPlayerInsideMountZone()
    {
        if (playerRigRoot == null)
        {
            return false;
        }

        if (playerCharacterController == null || trackedHeadTransform == null)
        {
            CacheRigReferences();
        }

        if (mountTrigger != null && playerCharacterController != null)
        {
            if (playerCharacterController.bounds.Intersects(mountTrigger.bounds))
            {
                return true;
            }
        }

        Vector3 playerPosition = trackedHeadTransform != null
            ? trackedHeadTransform.position
            : playerRigRoot.transform.position;

        Vector3 mountPosition = seatAnchor != null ? seatAnchor.position : transform.position;

        playerPosition.y = 0f;
        mountPosition.y = 0f;

        return Vector3.Distance(playerPosition, mountPosition) <= remountDistance;
    }

    private bool IsActionEnabled(InputActionReference actionReference)
    {
        return actionReference != null &&
               actionReference.action != null &&
               actionReference.action.enabled;
    }

    private void SetActionEnabled(InputActionReference actionReference, bool enabled)
    {
        if (actionReference == null || actionReference.action == null)
        {
            return;
        }

        if (enabled)
        {
            actionReference.action.Enable();
        }
        else
        {
            actionReference.action.Disable();
        }
    }

    private void SetPlayerLocomotionLocked(bool locked)
    {
        if (playerCharacterController == null || trackedHeadTransform == null || xrDeviceSimulator == null)
        {
            CacheRigReferences();
        }

        if (locked)
        {
            if (playerCharacterController != null)
            {
                playerCharacterControllerWasEnabled = playerCharacterController.enabled;
                playerCharacterController.enabled = false;
            }

            if (locomotionRoot != null)
            {
                locomotionRootWasActive = locomotionRoot.activeSelf;
                locomotionRoot.SetActive(false);
            }

            if (xrDeviceSimulator != null)
            {
                simulatorKeyboardXWasEnabled = IsActionEnabled(xrDeviceSimulator.keyboardXTranslateAction);
                simulatorKeyboardYWasEnabled = IsActionEnabled(xrDeviceSimulator.keyboardYTranslateAction);
                simulatorKeyboardZWasEnabled = IsActionEnabled(xrDeviceSimulator.keyboardZTranslateAction);

                SetActionEnabled(xrDeviceSimulator.keyboardXTranslateAction, false);
                SetActionEnabled(xrDeviceSimulator.keyboardYTranslateAction, false);
                SetActionEnabled(xrDeviceSimulator.keyboardZTranslateAction, false);
            }
        }
        else
        {
            if (playerCharacterController != null)
            {
                playerCharacterController.enabled = playerCharacterControllerWasEnabled;
            }

            if (locomotionRoot != null)
            {
                locomotionRoot.SetActive(locomotionRootWasActive);
            }

            if (xrDeviceSimulator != null)
            {
                SetActionEnabled(xrDeviceSimulator.keyboardXTranslateAction, simulatorKeyboardXWasEnabled);
                SetActionEnabled(xrDeviceSimulator.keyboardYTranslateAction, simulatorKeyboardYWasEnabled);
                SetActionEnabled(xrDeviceSimulator.keyboardZTranslateAction, simulatorKeyboardZWasEnabled);
            }
        }
    }


    private void UpdateKittyAnimation(float moveAmount, bool isAutoRiding)
    {
        if (kittyAnimator == null)
        {
            return;
        }

        if (isAutoRiding)
        {
            kittyAnimator.SetFloat("Vert", 1f);
            kittyAnimator.SetFloat("State", 1f);
            return;
        }

        if (moveAmount <= 0.01f)
        {
            kittyAnimator.SetFloat("Vert", 0f);
            kittyAnimator.SetFloat("State", 0f);
            return;
        }

        kittyAnimator.SetFloat("Vert", 1f);

        if (moveAmount < 0.75f)
        {
            kittyAnimator.SetFloat("State", 0f);
        }
        else
        {
            kittyAnimator.SetFloat("State", 1f);
        }
    }


    private void StartMount()
    {
        if (currentState != RideState.Idle || stateRoutine != null)
        {
            return;
        }

        stateRoutine = StartCoroutine(MountSequence());
    }

    private IEnumerator MountSequence()
    {
        if (seatAnchor == null || playerRigRoot == null)
        {
            Debug.LogError("[CatRideControllerV2] Missing seatAnchor or playerRigRoot.");
            stateRoutine = null;
            yield break;
        }

        CacheRigReferences();
        SetPlayerLocomotionLocked(true);

        currentState = RideState.Mounting;

        Transform rig = playerRigRoot.transform;
        rig.SetParent(seatAnchor, true);

        Vector3 startLocalPosition = rig.localPosition;
        Quaternion startLocalRotation = rig.localRotation;

        AlignMountedViewToSeatForward(rig);
        SnapHeadToMountedViewAnchor(rig);

        Vector3 targetLocalPosition = rig.localPosition;
        Quaternion targetLocalRotation = rig.localRotation;

        rig.localPosition = startLocalPosition;
        rig.localRotation = startLocalRotation;

        float duration = settings != null
            ? Mathf.Max(0f, settings.mountBlendTime)
            : fallbackMountBlendTime;

        if (duration <= 0f)
        {
            rig.localPosition = targetLocalPosition;
            rig.localRotation = targetLocalRotation;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);

                rig.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, t);
                rig.localRotation = Quaternion.Slerp(startLocalRotation, targetLocalRotation, t);

                yield return null;
            }

            rig.localPosition = targetLocalPosition;
            rig.localRotation = targetLocalRotation;
        }

        currentAutoIndex = 0;
        currentState = RideState.MountedManual;
        UpdateKittyAnimation(0f, false);
        stateRoutine = null;

        if (debugLogs)
        {
            Debug.Log("[CatRideControllerV2] Mounted. Manual control enabled.");
        }
    }

    private void AlignMountedViewToSeatForward(Transform rig)
    {
        if (rig == null || seatAnchor == null)
        {
            return;
        }

        if (trackedHeadTransform == null)
        {
            CacheRigReferences();
        }

        Vector3 currentHeadForward = trackedHeadTransform != null ? trackedHeadTransform.forward : rig.forward;
        currentHeadForward.y = 0f;

        if (currentHeadForward.sqrMagnitude < 0.0001f)
        {
            currentHeadForward = rig.forward;
            currentHeadForward.y = 0f;
        }

        Vector3 targetForward = seatAnchor.forward;
        targetForward.y = 0f;

        if (targetForward.sqrMagnitude < 0.0001f)
        {
            targetForward = transform.forward;
            targetForward.y = 0f;
        }

        currentHeadForward.Normalize();
        targetForward.Normalize();

        Quaternion yawDelta = Quaternion.FromToRotation(currentHeadForward, targetForward);
        rig.rotation = yawDelta * rig.rotation;
    }

    private void SnapHeadToMountedViewAnchor(Transform rig)
    {
        if (rig == null)
        {
            return;
        }

        if (trackedHeadTransform == null)
        {
            CacheRigReferences();
        }

        Transform targetAnchor = mountedViewAnchor != null ? mountedViewAnchor : seatAnchor;
        if (trackedHeadTransform == null || targetAnchor == null)
        {
            return;
        }

        Vector3 worldDelta = targetAnchor.position - trackedHeadTransform.position;
        rig.position += worldDelta;
    }

    private void StartDismount()
    {
        if ((currentState != RideState.MountedManual && currentState != RideState.MountedAuto) || stateRoutine != null)
        {
            return;
        }

        stateRoutine = StartCoroutine(DismountSequence());
    }

    private IEnumerator DismountSequence()
    {
        if (playerRigRoot == null)
        {
            stateRoutine = null;
            yield break;
        }

        currentState = RideState.Dismounting;

        Transform rig = playerRigRoot.transform;
        Vector3 startWorldPosition = rig.position;
        Quaternion startWorldRotation = rig.rotation;

        rig.SetParent(null, true);

        Vector3 targetWorldPosition;
        Quaternion targetWorldRotation;

        if (dismountPoint != null)
        {
            targetWorldPosition = dismountPoint.position;
            targetWorldRotation = dismountPoint.rotation;
        }
        else
        {
            targetWorldPosition = transform.position + transform.right * 0.8f;
            targetWorldRotation = Quaternion.LookRotation(transform.forward, Vector3.up);
        }

        // 稍微抬高一点，避免 CharacterController 恢复时和地面或猫体积发生挤压
        targetWorldPosition += Vector3.up * dismountGroundLift;

        float duration = settings != null
            ? Mathf.Max(0f, settings.dismountBlendTime)
            : fallbackDismountBlendTime;

        if (duration <= 0f)
        {
            rig.position = targetWorldPosition;
            rig.rotation = targetWorldRotation;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);

                rig.position = Vector3.Lerp(startWorldPosition, targetWorldPosition, t);
                rig.rotation = Quaternion.Slerp(startWorldRotation, targetWorldRotation, t);

                yield return null;
            }

            rig.position = targetWorldPosition;
            rig.rotation = targetWorldRotation;
        }

        Physics.SyncTransforms();

        if (dismountUnlockDelay > 0f)
        {
            yield return new WaitForSeconds(dismountUnlockDelay);
        }
        else
        {
            yield return null;
        }

        SetPlayerLocomotionLocked(false);

        currentAutoIndex = 0;
        currentState = RideState.Idle;
        UpdateKittyAnimation(0f, false);
        stateRoutine = null;

        if (debugLogs)
        {
            Debug.Log("[CatRideControllerV2] Dismounted.");
        }
    }


    private void HandleManualRide()
    {
        float moveInput = 0f;
        float turnInput = 0f;

        if (Keyboard.current.wKey.isPressed) moveInput += 1f;
        if (Keyboard.current.sKey.isPressed) moveInput -= 1f;
        if (Keyboard.current.aKey.isPressed) turnInput -= 1f;
        if (Keyboard.current.dKey.isPressed) turnInput += 1f;

        transform.Rotate(Vector3.up, turnInput * manualTurnSpeed * Time.deltaTime);
        transform.position += transform.forward * moveInput * manualMoveSpeed * Time.deltaTime;

        UpdateKittyAnimation(Mathf.Abs(moveInput), false);

    }

    public bool BeginAutoRide()
    {
        if (currentState != RideState.MountedManual || stateRoutine != null)
        {
            return false;
        }

        if (autoRoutePoints == null || autoRoutePoints.Count == 0)
        {
            Debug.LogWarning("[CatRideControllerV2] autoRoutePoints is empty.");
            return false;
        }

        for (int i = 0; i < autoRoutePoints.Count; i++)
        {
            if (autoRoutePoints[i] == null)
            {
                Debug.LogWarning($"[CatRideControllerV2] autoRoutePoints[{i}] is null.");
                return false;
            }
        }

        currentAutoIndex = 0;
        currentState = RideState.MountedAuto;

        if (debugLogs)
        {
            Debug.Log("[CatRideControllerV2] Auto ride started.");
        }

        return true;
    }

    private void HandleAutoRide()
    {
        if (autoRoutePoints == null || currentAutoIndex >= autoRoutePoints.Count)
        {
            FinishAutoRide();
            return;
        }

        Transform target = autoRoutePoints[currentAutoIndex];
        if (target == null)
        {
            FinishAutoRide();
            return;
        }


        UpdateKittyAnimation(1f, true);


        float autoSpeed = settings != null ? settings.autoRideSpeed : 2f;
        float rotateSpeed = settings != null ? settings.rotateSpeed : 180f;
        float reachDistance = settings != null ? settings.reachDistance : 0.25f;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            autoSpeed * Time.deltaTime
        );

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
        }

        if (Vector3.Distance(transform.position, target.position) <= reachDistance)
        {
            currentAutoIndex++;

            if (currentAutoIndex >= autoRoutePoints.Count)
            {
                FinishAutoRide();
            }
        }
    }

    private void FinishAutoRide()
    {
        currentState = RideState.MountedManual;
        UpdateKittyAnimation(0f, false);


        if (debugLogs)
        {
            Debug.Log("[CatRideControllerV2] Auto ride finished. Manual control returned.");
        }
    }
}
