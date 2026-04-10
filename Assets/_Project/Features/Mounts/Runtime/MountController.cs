using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;

public enum MountState { Idle, Mounting, MountedAuto, Dismounting }

public class MountController : MonoBehaviour
{
    [SerializeField] private MountState currentState = MountState.Idle;

    [Header("References")]
    [SerializeField] private Transform seatAnchor;
    [SerializeField] private Transform dismountPoint;
    [SerializeField] private CatRideRouteController routeController;
    [SerializeField] private MountSettings_SO settings;
    [SerializeField] private GameObject playerRigRoot;
    [SerializeField] private Collider mountTrigger;

    [Header("Movement Lock Targets")]
    [SerializeField] private GameObject locomotionRoot;

    private CharacterController playerCharacterController;
    private bool playerCharacterControllerWasEnabled;
    private bool locomotionRootWasActive;
    private Coroutine stateRoutine;
    private float originalCameraOffsetY = 0f;
    private bool hasCachedCameraOffsetY = false;

    private void Awake()
    {
        CacheRigReferences();
    }

    private void CacheRigReferences()
    {
        if (playerRigRoot == null)
        {
            return;
        }

        playerCharacterController = playerRigRoot.GetComponent<CharacterController>();

        Transform cameraOffset = playerRigRoot.transform.Find("Camera Offset");
        if (cameraOffset != null)
        {
            originalCameraOffsetY = cameraOffset.localPosition.y;
            hasCachedCameraOffsetY = true;
        }
    }

    void Update()
    {
        if (currentState == MountState.Idle &&
            Keyboard.current != null &&
            Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (IsPlayerInsideMountZone())
            {
                StartMounting();
                return;
            }

            if (settings != null && settings.debugLogs)
            {
                Debug.Log("[MountController] C pressed but player is not inside mount zone.");
            }
        }

        if (currentState == MountState.MountedAuto && routeController != null && !routeController.isRunning)
        {
            StartDismounting();
        }
    }

    private bool IsPlayerInsideMountZone()
    {
        if (mountTrigger == null || playerRigRoot == null)
        {
            return false;
        }

        if (playerCharacterController == null)
        {
            CacheRigReferences();
        }

        if (playerCharacterController != null)
        {
            return playerCharacterController.bounds.Intersects(mountTrigger.bounds);
        }

        return mountTrigger.bounds.Contains(playerRigRoot.transform.position);
    }

    private void SetPlayerLocomotionLocked(bool locked)
    {
        if (playerCharacterController == null && playerRigRoot != null)
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
        }
    }

    private void StartMounting()
    {
        if (currentState != MountState.Idle)
        {
            return;
        }

        if (settings == null)
        {
            Debug.LogError("[MountController] Missing MountSettings_SO reference.");
            return;
        }

        if (seatAnchor == null || dismountPoint == null || routeController == null || playerRigRoot == null)
        {
            Debug.LogError("[MountController] Missing one or more required references.");
            return;
        }

        if (stateRoutine != null)
        {
            StopCoroutine(stateRoutine);
            stateRoutine = null;
        }

        stateRoutine = StartCoroutine(MountSequence());
    }

    private IEnumerator MountSequence()
    {
        if (settings.debugLogs) Debug.Log("[MountController] State: Mounting");
        currentState = MountState.Mounting;

        SetPlayerLocomotionLocked(true);

        Transform rig = playerRigRoot.transform;
        rig.SetParent(seatAnchor, true);

        Vector3 facingDirection = routeController.GetMountFacingDirection();
        facingDirection.y = 0f;

        Quaternion targetLocalRotation = Quaternion.identity;
        if (facingDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetWorldRotation = Quaternion.LookRotation(facingDirection.normalized, Vector3.up);
            targetLocalRotation = Quaternion.Inverse(seatAnchor.rotation) * targetWorldRotation;
        }

        Vector3 startLocalPosition = rig.localPosition;
        Quaternion startLocalRotation = rig.localRotation;

        float cameraOffsetY = hasCachedCameraOffsetY ? originalCameraOffsetY : 0f;
        Vector3 targetLocalPosition = new Vector3(0f, settings.seatHeightOffset - cameraOffsetY, 0f);

        float duration = Mathf.Max(0f, settings.mountBlendTime);

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
                rig.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, t);
                rig.localRotation = Quaternion.Slerp(startLocalRotation, targetLocalRotation, t);
                yield return null;
            }

            rig.localPosition = targetLocalPosition;
            rig.localRotation = targetLocalRotation;
        }

        currentState = MountState.MountedAuto;
        routeController.StartRoute();
        stateRoutine = null;
    }

    private void StartDismounting()
    {
        if (currentState != MountState.MountedAuto)
        {
            return;
        }

        if (settings == null)
        {
            Debug.LogError("[MountController] Missing MountSettings_SO reference.");
            return;
        }

        if (dismountPoint == null || playerRigRoot == null)
        {
            Debug.LogError("[MountController] Missing dismountPoint or playerRigRoot.");
            return;
        }

        if (stateRoutine != null)
        {
            StopCoroutine(stateRoutine);
            stateRoutine = null;
        }

        stateRoutine = StartCoroutine(DismountSequence());
    }

    private IEnumerator DismountSequence()
    {
        if (settings.debugLogs) Debug.Log("[MountController] State: Dismounting");
        currentState = MountState.Dismounting;

        Transform rig = playerRigRoot.transform;
        Vector3 startWorldPosition = rig.position;
        Quaternion startWorldRotation = rig.rotation;

        rig.SetParent(null, true);

        Vector3 targetWorldPosition = dismountPoint.position;
        Quaternion targetWorldRotation = dismountPoint.rotation;
        float duration = Mathf.Max(0f, settings.dismountBlendTime);

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
                rig.position = Vector3.Lerp(startWorldPosition, targetWorldPosition, t);
                rig.rotation = Quaternion.Slerp(startWorldRotation, targetWorldRotation, t);
                yield return null;
            }

            rig.position = targetWorldPosition;
            rig.rotation = targetWorldRotation;
        }

        SetPlayerLocomotionLocked(false);

        currentState = MountState.Idle;
        if (settings.debugLogs) Debug.Log("[MountController] State: Idle (Finished)");
        stateRoutine = null;
    }
}
