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

    [Header("Auto Dismount")]
    [SerializeField] private bool autoDismountBesideMount = true;
    [SerializeField] private float dismountSideOffset = 0.7f;
    [SerializeField] private float dismountForwardOffset = 0.1f;
    [SerializeField] private float dismountGroundProbeHeight = 3f;
    [SerializeField] private float dismountGroundProbeDistance = 8f;
    [SerializeField] private LayerMask dismountGroundMask = Physics.DefaultRaycastLayers;
    [SerializeField] private bool keepCurrentFacingOnDismount = true;

    private CharacterController playerCharacterController;
    private bool playerCharacterControllerWasEnabled;
    private bool locomotionRootWasActive;
    private Coroutine stateRoutine;
    private float originalCameraOffsetY = 0f;
    private bool hasCachedCameraOffsetY = false;
    private Transform trackedHeadTransform;

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

            Transform mainCamera = cameraOffset.Find("Main Camera");
            if (mainCamera != null)
            {
                trackedHeadTransform = mainCamera;
            }
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

        if (seatAnchor == null || routeController == null || playerRigRoot == null)
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

        Vector3 trackedHeadLocalPosition = GetTrackedHeadLocalPosition(rig);
        Vector3 targetLocalPosition = new Vector3(
            -trackedHeadLocalPosition.x,
            settings.seatHeightOffset - trackedHeadLocalPosition.y,
            -trackedHeadLocalPosition.z);

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

        if (playerRigRoot == null)
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

        if (!ResolveDismountPose(startWorldRotation, out Vector3 targetWorldPosition, out Quaternion targetWorldRotation))
        {
            Debug.LogError("[MountController] Could not resolve a dismount point.");
            yield break;
        }
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

    private Vector3 GetTrackedHeadLocalPosition(Transform rig)
    {
        if (rig == null)
        {
            return Vector3.zero;
        }

        if (trackedHeadTransform == null)
        {
            CacheRigReferences();
        }

        if (trackedHeadTransform != null)
        {
            return rig.InverseTransformPoint(trackedHeadTransform.position);
        }

        return new Vector3(0f, hasCachedCameraOffsetY ? originalCameraOffsetY : 0f, 0f);
    }

    private bool ResolveDismountPose(Quaternion currentWorldRotation, out Vector3 targetWorldPosition, out Quaternion targetWorldRotation)
    {
        Vector3 flattenedForward = transform.forward;
        flattenedForward.y = 0f;
        if (flattenedForward.sqrMagnitude < 0.0001f)
        {
            flattenedForward = Vector3.forward;
        }

        flattenedForward.Normalize();
        targetWorldRotation = keepCurrentFacingOnDismount
            ? GetFlattenedRotation(currentWorldRotation, flattenedForward)
            : Quaternion.LookRotation(flattenedForward, Vector3.up);

        if (autoDismountBesideMount)
        {
            Vector3 basePosition = seatAnchor != null ? seatAnchor.position : transform.position;
            Vector3 sampleBasePosition =
                basePosition +
                transform.right * dismountSideOffset +
                flattenedForward * dismountForwardOffset;

            Vector3 sampleOrigin = sampleBasePosition + Vector3.up * dismountGroundProbeHeight;

            if (Physics.Raycast(
                    sampleOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    dismountGroundProbeDistance,
                    dismountGroundMask,
                    QueryTriggerInteraction.Ignore))
            {
                targetWorldPosition = hit.point;
                return true;
            }

            targetWorldPosition = sampleBasePosition;
            return true;
        }

        if (dismountPoint != null)
        {
            targetWorldPosition = dismountPoint.position;
            targetWorldRotation = dismountPoint.rotation;
            return true;
        }

        Transform routeOut = FindNearbyTransform("RouteOut");
        if (routeOut != null)
        {
            targetWorldPosition = routeOut.position;
            targetWorldRotation = routeOut.rotation;
            return true;
        }

        Transform mountEndAnchor = FindNearbyTransform("MountEndAnchor");
        if (mountEndAnchor != null)
        {
            targetWorldPosition = mountEndAnchor.position;
            targetWorldRotation = mountEndAnchor.rotation;
            return true;
        }

        targetWorldPosition = transform.position;
        return false;
    }

    private static Quaternion GetFlattenedRotation(Quaternion currentWorldRotation, Vector3 fallbackForward)
    {
        Vector3 forward = currentWorldRotation * Vector3.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = fallbackForward;
        }

        forward.Normalize();
        return Quaternion.LookRotation(forward, Vector3.up);
    }

    private Transform FindNearbyTransform(string targetName)
    {
        Transform searchRoot = transform;

        while (searchRoot != null)
        {
            Transform found = FindChildRecursive(searchRoot, targetName);
            if (found != null)
            {
                return found;
            }

            searchRoot = searchRoot.parent;
        }

        return null;
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
}
