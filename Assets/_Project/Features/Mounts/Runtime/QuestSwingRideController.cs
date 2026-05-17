using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

[DisallowMultipleComponent]
public sealed class QuestSwingRideController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform seatAnchor;
    [SerializeField] private Transform playerRigRoot;
    [SerializeField] private Transform playerHead;
    [SerializeField] private GameObject locomotionRoot;
    [SerializeField] private Transform rightRayOrigin;

    [Header("Interaction")]
    [SerializeField] private LayerMask interactionMask = ~0;
    [SerializeField] private float rayDistance = 7f;
    [SerializeField] private float mountDistance = 2.4f;
    [SerializeField] private Color hoverOutlineColor = new Color(0.78f, 0.94f, 1f, 0.62f);

    [Header("Comfort Swing")]
    [SerializeField] private float swingLength = 2.15f;
    [SerializeField] private float mountedEyeHeight = 1.15f;
    [SerializeField] private float maxAngleDegrees = 16f;
    [SerializeField] private float pumpAcceleration = 34f;
    [SerializeField] private float gravityAcceleration = 9.5f;
    [SerializeField] private float angularDamping = 1.15f;
    [SerializeField] private float autoSettleDamping = 0.35f;
    [SerializeField] private bool keepViewUpright = true;

    [Header("Dismount")]
    [SerializeField] private float dismountSideDistance = 1.15f;
    [SerializeField] private float dismountBackDistance = 0.45f;
    [SerializeField] private float groundProbeHeight = 2f;
    [SerializeField] private float groundProbeDistance = 5f;
    [SerializeField] private LayerMask groundMask = ~0;

    private readonly RaycastHit[] rayHits = new RaycastHit[16];
    private readonly RaycastHit[] groundHits = new RaycastHit[8];
    private QuestInteractableFeedback feedback;
    private HapticImpulsePlayer rightHaptics;
    private Transform rideAnchor;
    private CharacterController characterController;
    private bool characterControllerWasEnabled;
    private bool locomotionRootWasActive;
    private Transform originalParent;
    private int originalSiblingIndex;
    private bool mounted;
    private bool hover;
    private bool triggerLastFrame;
    private bool primaryLastFrame;
    private float angleDegrees;
    private float angularVelocity;
    private Vector3 restSeatPosition;
    private Vector3 pivotPosition;
    private Vector3 swingForward;
    private Vector3 swingRight;

    private void Awake()
    {
        AutoAssignReferences();
        EnsureFeedback();
    }

    private void Update()
    {
        AutoAssignReferences();

        if (mounted)
        {
            UpdateMountedSwing();
            return;
        }

        UpdateHoverAndMount();
    }

    private void UpdateHoverAndMount()
    {
        bool triggerPressed;
        QuestInteractionUtils.TryReadTriggerButton(true, out triggerPressed);
        bool triggerPressedThisFrame = triggerPressed && !triggerLastFrame;
        triggerLastFrame = triggerPressed;

        bool canHover = IsPlayerNearSeat() && RayHitsSwing();
        if (canHover != hover)
        {
            hover = canHover;
            feedback?.SetInteractable(canHover);
            feedback?.SetHovered(canHover, rightHaptics);
        }
        else if (canHover)
        {
            feedback?.SetHovered(true, rightHaptics);
        }

        if (triggerPressedThisFrame && canHover)
        {
            feedback?.PulseSelect(rightHaptics);
            Mount();
        }
    }

    private void UpdateMountedSwing()
    {
        bool primaryPressed;
        QuestInteractionUtils.TryReadPrimaryButton(true, out primaryPressed);
        bool primaryPressedThisFrame = primaryPressed && !primaryLastFrame;
        primaryLastFrame = primaryPressed;
        if (primaryPressedThisFrame)
        {
            Dismount();
            return;
        }

        QuestInteractionUtils.TryReadPrimary2DAxis(false, out Vector2 leftAxis);
        float pump = Mathf.Abs(leftAxis.y) > 0.08f ? leftAxis.y : 0f;
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        float acceleration = pump * pumpAcceleration
            - Mathf.Sin(angleRadians) * gravityAcceleration
            - angularVelocity * angularDamping;

        if (Mathf.Abs(pump) < 0.01f)
        {
            acceleration -= angularVelocity * autoSettleDamping;
        }

        angularVelocity += acceleration * Time.deltaTime;
        angleDegrees += angularVelocity * Time.deltaTime;

        float maxAngle = Mathf.Max(1f, maxAngleDegrees);
        if (Mathf.Abs(angleDegrees) > maxAngle)
        {
            angleDegrees = Mathf.Clamp(angleDegrees, -maxAngle, maxAngle);
            angularVelocity *= -0.12f;
        }

        ApplySwingPose();
    }

    private void Mount()
    {
        if (mounted || playerRigRoot == null)
        {
            return;
        }

        CacheRigState();
        ResolveSwingFrame();
        EnsureRideAnchor();

        Transform rig = playerRigRoot;
        originalParent = rig.parent;
        originalSiblingIndex = rig.GetSiblingIndex();

        if (characterController != null)
        {
            characterControllerWasEnabled = characterController.enabled;
            characterController.enabled = false;
        }

        if (locomotionRoot != null)
        {
            locomotionRootWasActive = locomotionRoot.activeSelf;
            locomotionRoot.SetActive(false);
        }

        angleDegrees = 0f;
        angularVelocity = 0f;
        ApplySwingPose();

        rig.SetParent(rideAnchor, true);
        AlignRigCameraTo(rideAnchor.position + Vector3.up * mountedEyeHeight, Quaternion.LookRotation(swingForward, Vector3.up));
        mounted = true;
        hover = false;
        feedback?.SetHovered(false, rightHaptics, false);
        feedback?.SetInteractable(false);
    }

    private void Dismount()
    {
        if (!mounted || playerRigRoot == null)
        {
            return;
        }

        Transform rig = playerRigRoot;
        rig.SetParent(originalParent, true);
        if (originalParent != null && originalSiblingIndex >= 0)
        {
            rig.SetSiblingIndex(Mathf.Min(originalSiblingIndex, originalParent.childCount - 1));
        }

        Vector3 dismountView = ResolveDismountViewPosition();
        Quaternion dismountRotation = Quaternion.LookRotation(swingForward, Vector3.up);
        AlignRigCameraTo(dismountView, dismountRotation);

        if (characterController != null)
        {
            characterController.enabled = characterControllerWasEnabled;
        }

        if (locomotionRoot != null)
        {
            locomotionRoot.SetActive(locomotionRootWasActive);
        }

        mounted = false;
        angleDegrees = 0f;
        angularVelocity = 0f;
        ApplySwingPose();
    }

    private void ApplySwingPose()
    {
        if (rideAnchor == null)
        {
            return;
        }

        Quaternion swingRotation = Quaternion.AngleAxis(angleDegrees, swingRight);
        Vector3 seatPosition = pivotPosition + swingRotation * (restSeatPosition - pivotPosition);
        Quaternion anchorRotation = keepViewUpright
            ? Quaternion.LookRotation(swingForward, Vector3.up)
            : Quaternion.LookRotation(swingForward, swingRotation * Vector3.up);

        rideAnchor.SetPositionAndRotation(seatPosition, anchorRotation);
    }

    private void ResolveSwingFrame()
    {
        Transform seat = seatAnchor != null ? seatAnchor : transform;
        restSeatPosition = seat.position;

        swingForward = Vector3.ProjectOnPlane(seat.forward, Vector3.up);
        if (swingForward.sqrMagnitude < 0.001f)
        {
            swingForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        }

        if (swingForward.sqrMagnitude < 0.001f)
        {
            swingForward = Vector3.forward;
        }

        swingForward.Normalize();
        swingRight = Vector3.Cross(Vector3.up, swingForward).normalized;
        pivotPosition = restSeatPosition + Vector3.up * Mathf.Max(0.5f, swingLength);
    }

    private Vector3 ResolveDismountViewPosition()
    {
        Vector3 side = swingRight * Mathf.Max(0.1f, dismountSideDistance);
        Vector3 back = -swingForward * Mathf.Max(0f, dismountBackDistance);
        Vector3 groundPosition = ProjectToGround(restSeatPosition + side + back);
        return groundPosition + Vector3.up * ResolveCurrentEyeHeight();
    }

    private Vector3 ProjectToGround(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * Mathf.Max(0.1f, groundProbeHeight);
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHits,
            Mathf.Max(0.1f, groundProbeDistance),
            groundMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount <= 0)
        {
            return position;
        }

        System.Array.Sort(groundHits, 0, hitCount, RaycastHitDistanceComparer.Instance);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = groundHits[i].collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(transform) ||
                (playerRigRoot != null && hitCollider.transform.IsChildOf(playerRigRoot)))
            {
                continue;
            }

            position.y = groundHits[i].point.y;
            return position;
        }

        return position;
    }

    private float ResolveCurrentEyeHeight()
    {
        if (playerHead != null && playerRigRoot != null)
        {
            return Mathf.Max(0.4f, playerHead.position.y - playerRigRoot.position.y);
        }

        return mountedEyeHeight;
    }

    private void AlignRigCameraTo(Vector3 cameraWorldPosition, Quaternion rigRotation)
    {
        if (playerRigRoot == null)
        {
            return;
        }

        Transform rig = playerRigRoot;
        Transform head = playerHead != null ? playerHead : rig;
        Vector3 localHeadBefore = rig.InverseTransformPoint(head.position);
        rig.rotation = rigRotation;
        Vector3 scaledHeadOffset = Vector3.Scale(localHeadBefore, rig.lossyScale);
        rig.position = cameraWorldPosition - rig.rotation * scaledHeadOffset;
    }

    private bool RayHitsSwing()
    {
        if (rightRayOrigin == null)
        {
            return false;
        }

        Ray ray = new Ray(rightRayOrigin.position, rightRayOrigin.forward);
        int hitCount = Physics.RaycastNonAlloc(ray, rayHits, rayDistance, interactionMask, QueryTriggerInteraction.Collide);
        if (hitCount <= 0)
        {
            return false;
        }

        System.Array.Sort(rayHits, 0, hitCount, RaycastHitDistanceComparer.Instance);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = rayHits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.transform.IsChildOf(transform))
            {
                return true;
            }

            if (!hitCollider.isTrigger)
            {
                return false;
            }
        }

        return false;
    }

    private bool IsPlayerNearSeat()
    {
        Transform reference = playerHead != null ? playerHead : playerRigRoot;
        Transform seat = seatAnchor != null ? seatAnchor : transform;
        if (reference == null || seat == null)
        {
            return false;
        }

        Vector3 a = reference.position;
        Vector3 b = seat.position;
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b) <= Mathf.Max(0.1f, mountDistance);
    }

    private void AutoAssignReferences()
    {
        if (rightRayOrigin == null)
        {
            rightRayOrigin = QuestInteractionUtils.FindControllerRayOrigin(true);
        }

        if (rightHaptics == null)
        {
            rightHaptics = QuestInteractionUtils.FindHapticPlayer(true, rightRayOrigin);
        }

        if (playerRigRoot == null)
        {
            GameObject xrOrigin = GameObject.Find("WonderlandXROrigin");
            if (xrOrigin != null)
            {
                playerRigRoot = xrOrigin.transform;
            }
        }

        if (playerHead == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                playerHead = mainCamera.transform;
            }
        }

        if (locomotionRoot == null && playerRigRoot != null)
        {
            Transform locomotion = QuestInteractionUtils.FindChildRecursive(playerRigRoot, "Locomotion");
            if (locomotion != null)
            {
                locomotionRoot = locomotion.gameObject;
            }
        }

        if (seatAnchor == null)
        {
            seatAnchor = FindLikelySeatAnchor();
        }

        if (characterController == null && playerRigRoot != null)
        {
            characterController = playerRigRoot.GetComponent<CharacterController>();
        }
    }

    private Transform FindLikelySeatAnchor()
    {
        Transform namedSeat = FindChildNameContains(transform, "Seat");
        if (namedSeat != null)
        {
            return namedSeat;
        }

        Transform board = FindChildNameContains(transform, "Board");
        if (board != null)
        {
            return board;
        }

        Bounds bounds = ResolveRendererBounds();
        GameObject anchorObject = new GameObject("RuntimeSwingSeatAnchor");
        anchorObject.transform.SetParent(transform, false);
        anchorObject.transform.position = bounds.center + Vector3.down * Mathf.Max(0f, bounds.extents.y * 0.45f);
        anchorObject.transform.rotation = transform.rotation;
        return anchorObject.transform;
    }

    private static Transform FindChildNameContains(Transform root, string token)
    {
        if (root == null || string.IsNullOrEmpty(token))
        {
            return null;
        }

        if (root.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildNameContains(root.GetChild(i), token);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private Bounds ResolveRendererBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(transform.position, Vector3.one);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    private void EnsureRideAnchor()
    {
        if (rideAnchor != null)
        {
            return;
        }

        GameObject anchorObject = new GameObject("QuestSwingRideAnchor");
        anchorObject.transform.SetParent(transform, true);
        rideAnchor = anchorObject.transform;
    }

    private void EnsureFeedback()
    {
        if (feedback == null)
        {
            feedback = GetComponent<QuestInteractableFeedback>();
            if (feedback == null)
            {
                feedback = gameObject.AddComponent<QuestInteractableFeedback>();
            }
        }

        feedback.Configure(hoverOutlineColor, 0.018f);
        feedback.SetInteractable(false);
    }

    private void CacheRigState()
    {
        if (playerRigRoot != null && characterController == null)
        {
            characterController = playerRigRoot.GetComponent<CharacterController>();
        }
    }

    private sealed class RaycastHitDistanceComparer : System.Collections.IComparer
    {
        public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

        public int Compare(object x, object y)
        {
            RaycastHit a = (RaycastHit)x;
            RaycastHit b = (RaycastHit)y;
            return a.distance.CompareTo(b.distance);
        }
    }
}
