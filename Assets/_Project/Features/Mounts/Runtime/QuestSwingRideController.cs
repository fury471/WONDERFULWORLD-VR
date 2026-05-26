using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;

[DisallowMultipleComponent]
public sealed class QuestSwingRideController : MonoBehaviour
{
    public enum SwingScaleRequirement
    {
        Any,
        SmallOnly,
        NormalOnly,
        LargeOnly,
    }

    [Header("Mount Access")]
    [Tooltip("Which player scale states can mount the swing. NormalOnly by default — small/large players can't sit on it.")]
    [SerializeField] private SwingScaleRequirement mountScaleRequirement = SwingScaleRequirement.NormalOnly;
    [SerializeField] private ScaleManager scaleManager;

    [Header("References")]
    [Tooltip("The rope attachment / pivot point at the TOP of the swing where ropes meet the crossbar. " +
             "On TFF_Wooden_Swing_01A this is the TFF_Wooden_Swing_Seat_01A transform — the rider sits BELOW it by swingLength.")]
    [SerializeField] private Transform seatAnchor;
    [Tooltip("(Deprecated) Was previously rotated as a whole. The frame now stays still; only the seatAnchor swings.")]
    [SerializeField] private Transform swingVisualRoot;
    [SerializeField] private Transform playerRigRoot;
    [SerializeField] private Transform playerHead;
    [SerializeField] private GameObject locomotionRoot;
    [SerializeField] private Transform rightRayOrigin;

    [Header("Interaction")]
    [SerializeField] private LayerMask interactionMask = ~0;
    [SerializeField] private float rayDistance = 7f;
    [SerializeField] private float mountDistance = 2.4f;
    [SerializeField] private Color hoverOutlineColor = new Color(0.78f, 0.94f, 1f, 0.62f);
    [Tooltip("When true, only colliders on the seatAnchor (and its descendants) trigger the hover/mount. " +
             "Prevents pointing at the frame or ropes from mounting.")]
    [SerializeField] private bool restrictRayToSeatOnly = true;

    [Header("Comfort Swing")]
    [Tooltip("Distance from the pivot (seatAnchor) DOWN (in seat's LOCAL -Y direction) to the rider's seated position. " +
             "Effectively the rope length. Smaller = shorter rope, smaller arc.")]
    [SerializeField] private float swingLength = 1.1f;
    [Tooltip("Sitting eye height above the rider's seated position (where the rider's butt is). " +
             "~0.65 for adults seated.")]
    [SerializeField] private float mountedEyeHeight = 0.62f;
    [Tooltip("Maximum swing angle (degrees) on the seat's LOCAL Z axis — clamp keeps motion comfortable for VR.")]
    [SerializeField] private float maxAngleDegrees = 22f;
    [Tooltip("How hard the left-stick pump impulse pushes the swing. Lower = gentler response.")]
    [SerializeField] private float pumpAcceleration = 26f;
    [SerializeField] private float gravityAcceleration = 9.5f;
    [SerializeField] private float angularDamping = 1.15f;
    [SerializeField] private float autoSettleDamping = 0.5f;

    [Header("Comfort Vignette (visual params match the mount vignette; logic is swing-specific)")]
    [Tooltip("Sync ON/OFF + min-aperture with the global comfort profile so swing matches the mount visually. " +
             "The swing's DYNAMIC speed-driven behavior stays in this script — it doesn't leak to the mount.")]
    [SerializeField] private bool syncSwingVignetteWithComfortProfile = true;
    [SerializeField] private QuestLocomotionComfortProfile comfortProfile;
    [SerializeField] private bool enableSwingComfortVignette = true;
    [Tooltip("Smallest aperture value when the rider is at full backward speed. Matches mount's rideVignetteAperture for an identical look at full close.")]
    [SerializeField, Range(0.2f, 1f)] private float swingVignetteAperture = 0.58f;
    [SerializeField, Range(0f, 1f)] private float swingVignetteFeathering = 0.30f;
    [Tooltip("MUST be 0 — the swing script does its own smoothing each frame. Any non-zero easeIn fights us and the ring appears stuck at a single fixed aperture.")]
    [SerializeField, Min(0f)] private float swingVignetteEaseInTime = 0f;
    [Tooltip("Smooth ring fade-out at dismount.")]
    [SerializeField, Min(0f)] private float swingVignetteEaseOutTime = 0.20f;
    [SerializeField, Min(0f)] private float swingVignetteEaseOutDelayTime = 0f;
    [Tooltip("Backward horizontal speed (m/s) at which the ring is fully closed to swingVignetteAperture. " +
             "Should match the real max horizontal speed of THIS swing (≈ swingLength × maxAngular). " +
             "If set higher than reachable speed, the ring never fully closes and feels 'stuck'.")]
    [SerializeField, Min(0.01f)] private float swingVignetteFullSpeed = 0.55f;
    [Tooltip("Backward speed (m/s) below which the ring is fully open. Prevents flicker at micro-motions.")]
    [SerializeField, Min(0f)] private float swingVignetteSpeedDeadzone = 0.08f;
    [Tooltip("How fast our internal aperture eases toward its target each frame (higher = snappier zoom).")]
    [SerializeField, Min(0.1f)] private float swingVignetteResponseSpeed = 6f;

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
    private Animator swingAnimator;
    private bool swingAnimatorWasEnabled;
    private CharacterController characterController;
    private bool characterControllerWasEnabled;
    private bool locomotionRootWasActive;
    private LocomotionProvider[] lockedLocomotionProviders;
    private bool[] lockedLocomotionProviderWasEnabled;
    private Transform originalParent;
    private int originalSiblingIndex;
    private bool mounted;
    private bool hover;
    private bool triggerLastFrame;
    private bool primaryLastFrame;
    private float angleDegrees;
    private float angularVelocity;
    // Seat's authored local rotation at mount time. Swing rotation is applied as a delta around
    // the seat's LOCAL Z (the same channel the Idle clip drives).
    private Quaternion restSeatLocalRotation = Quaternion.identity;
    // Rider's seated offset expressed in the seat's LOCAL frame (just below the pivot in -Y).
    // When seatAnchor rotates around its local Z, TransformPoint(restRiderLocalPosition) traces
    // the pendulum arc in world space automatically.
    private Vector3 restRiderLocalPosition = Vector3.down;
    // View forward and right captured at mount time, projected to horizontal (world XZ plane).
    // The view is locked to this initial direction so the player feels translation only, not roll.
    private Vector3 restRiderViewForward = Vector3.right;
    private Vector3 restRiderViewRight = Vector3.forward;
    private bool restSeatStateCached;
    private TunnelingVignetteController[] swingVignetteControllers;
    private SwingVignetteProvider swingVignetteProvider;
    private bool swingVignetteActive;
    // Aperture we're currently driving the provider at. Smoothly approaches the target each frame.
    private float currentSwingVignetteAperture = 1f;

    /// <summary>True while the player is seated on the swing. Mirrors CatRideControllerV2.IsRideActive.</summary>
    public bool IsMounted => mounted;

    private void Awake()
    {
        AutoAssignReferences();
        EnsureFeedback();
        CacheSwingVignetteReferences();
        SyncSwingVignetteFromComfortProfile();
    }

    private void OnEnable()
    {
        QuestLocomotionComfortProfile.ComfortVignetteChanged += HandleComfortVignetteChanged;
        SyncSwingVignetteFromComfortProfile();
    }

    private void OnDisable()
    {
        QuestLocomotionComfortProfile.ComfortVignetteChanged -= HandleComfortVignetteChanged;
        if (mounted || lockedLocomotionProviders != null)
        {
            SetPlayerLocomotionLocked(false);
        }

        if (mounted && swingAnimator != null)
        {
            swingAnimator.enabled = swingAnimatorWasEnabled;
        }

        SetComfortProfileLocomotionLocked(false);
        StopSwingVignette();
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

        // Scale gate first: if the player isn't in an allowed scale, no highlight and no mount.
        // This mirrors how the cat mount gates by scale (NormalOnly for the swing).
        bool canMountThisScale = CanMountInCurrentScale();
        bool canHover = canMountThisScale && IsPlayerNearSeat() && RayHitsSwing();
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

    private bool CanMountInCurrentScale()
    {
        if (mountScaleRequirement == SwingScaleRequirement.Any)
        {
            return true;
        }

        CacheScaleManagerReference();

        // No ScaleManager in the scene → fall back to "Any" semantics so the swing still works in
        // worlds where scaling isn't a thing (rather than getting silently un-mountable).
        if (scaleManager == null)
        {
            return mountScaleRequirement == SwingScaleRequirement.NormalOnly
                || mountScaleRequirement == SwingScaleRequirement.Any;
        }

        // Mid-transition the player's scale is in flux — disallow mount so we don't end up locking
        // the rig in a half-scaled state.
        if (scaleManager.IsTransitioning)
        {
            return false;
        }

        switch (mountScaleRequirement)
        {
            case SwingScaleRequirement.SmallOnly:
                return scaleManager.CurrentState == ScaleState.Small;
            case SwingScaleRequirement.NormalOnly:
                return scaleManager.CurrentState == ScaleState.Normal;
            case SwingScaleRequirement.LargeOnly:
                return scaleManager.CurrentState == ScaleState.Large;
            default:
                return true;
        }
    }

    private void CacheScaleManagerReference()
    {
        if (scaleManager != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        scaleManager = FindAnyObjectByType<ScaleManager>(FindObjectsInactive.Include);
#else
#pragma warning disable CS0618
        scaleManager = FindObjectOfType<ScaleManager>(true);
#pragma warning restore CS0618
#endif
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

        // Simplified pendulum: angular acceleration = pump input - gravity restoring force - air damping.
        // After release the gravity term keeps it oscillating; angularDamping bleeds energy slowly
        // so the swing decays over many seconds, like a real playground swing.
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
        UpdateSwingVignetteIntensity(Time.deltaTime);
    }

    private void Mount()
    {
        if (mounted || playerRigRoot == null)
        {
            return;
        }

        CacheRigState();
        EnsureRideAnchor();
        CacheSwingAnimator();

        // Disable the idle animator BEFORE sampling rest pose, otherwise we'd cache a mid-sway
        // rotation as "rest" and the swing would drift each remount.
        if (swingAnimator != null)
        {
            swingAnimatorWasEnabled = swingAnimator.enabled;
            swingAnimator.enabled = false;
        }

        ResetSeatToRest();   // snap to previously cached rest if we mounted before
        CacheSeatRest();     // capture current authored pose as the new rest
        ResolveSwingFrame(); // compute restRiderLocalPosition from swingLength

        Transform rig = playerRigRoot;
        originalParent = rig.parent;
        originalSiblingIndex = rig.GetSiblingIndex();

        // Locks the CharacterController, every LocomotionProvider under the rig's Locomotion
        // subtree (continuous-move AND turn providers), and the ComfortProfile — which together
        // disable BOTH stick translation and right-stick turn while mounted on the swing.
        SetPlayerLocomotionLocked(true);

        angleDegrees = 0f;
        angularVelocity = 0f;
        ApplySwingPose();

        rig.SetParent(rideAnchor, true);
        AlignRigCameraTo(
            rideAnchor.position + Vector3.up * mountedEyeHeight,
            Quaternion.LookRotation(restRiderViewForward, Vector3.up));
        mounted = true;
        hover = false;
        feedback?.SetHovered(false, rightHaptics, false);
        feedback?.SetInteractable(false);

        // Begin the vignette ONCE — visibility is then controlled by dynamic apertureSize each
        // frame. Aperture=1 hides it; aperture < 1 closes the black ring smoothly.
        currentSwingVignetteAperture = 1f;
        StartSwingVignette();
    }

    private void Dismount()
    {
        if (!mounted || playerRigRoot == null)
        {
            return;
        }

        StopSwingVignette();

        Transform rig = playerRigRoot;
        rig.SetParent(originalParent, true);
        if (originalParent != null && originalSiblingIndex >= 0)
        {
            rig.SetSiblingIndex(Mathf.Min(originalSiblingIndex, originalParent.childCount - 1));
        }

        // Snap the seat back to its authored rest pose FIRST so the dismount candidate is
        // computed from the rest rider position (not from wherever the seat was swung to when
        // the player pressed A). This also lets the player's ground projection start from a
        // predictable point below the pivot.
        angleDegrees = 0f;
        angularVelocity = 0f;
        ResetSeatToRest();

        Vector3 dismountView = ResolveDismountViewPosition();
        Quaternion dismountRotation = Quaternion.LookRotation(restRiderViewForward, Vector3.up);
        AlignRigCameraTo(dismountView, dismountRotation);

        SetPlayerLocomotionLocked(false);

        mounted = false;

        if (swingAnimator != null)
        {
            swingAnimator.enabled = swingAnimatorWasEnabled;
        }
    }

    private void ResetSeatToRest()
    {
        if (!restSeatStateCached || seatAnchor == null)
        {
            return;
        }

        // Only restore local rotation — position is owned by the parent hierarchy.
        seatAnchor.localRotation = restSeatLocalRotation;
    }

    /// <summary>
    /// Public hook for RecenterController to recenter the rider while seated on the swing.
    /// Same intent as CatRideControllerV2.RecenterMountedView: take the HMD's current yaw and
    /// rotate the rig so the head now faces the seat's INITIAL local +X axis. The world Y of
    /// the head is preserved — only yaw plus a small XZ snap to the seat center.
    /// </summary>
    public bool RecenterMountedView()
    {
        if (!mounted || playerRigRoot == null || rideAnchor == null)
        {
            return false;
        }

        AutoAssignReferences();

        Transform head = playerHead != null ? playerHead : playerRigRoot;
        Transform rig = playerRigRoot;

        Vector3 targetForward = restRiderViewForward;
        if (targetForward.sqrMagnitude < 0.0001f)
        {
            targetForward = Vector3.right;
        }

        // 1) Compute yaw delta between the HMD's current horizontal forward and the seat's
        //    local +X. Doing this in pure-yaw (Y axis) avoids tipping the horizon.
        Vector3 currentForward = head.forward;
        currentForward.y = 0f;
        if (currentForward.sqrMagnitude < 0.0001f)
        {
            currentForward = rig.forward;
            currentForward.y = 0f;
        }

        if (currentForward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        currentForward.Normalize();
        Quaternion yawDelta = Quaternion.FromToRotation(currentForward, targetForward.normalized);

        // 2) Rotate the rig in place — rotate AROUND the head's world position so the head's
        //    XZ stays where it is and only its facing changes. (The rotation moves all rig
        //    children including the head, so compensate after.)
        Vector3 headWorldBefore = head.position;
        rig.rotation = yawDelta * rig.rotation;
        Vector3 headWorldAfter = head.position;
        rig.position += headWorldBefore - headWorldAfter;

        // 3) "Sit straight": snap the head's XZ to the swing seat center. Keep Y unchanged so
        //    the view height doesn't jump — that was the bug the user reported.
        Vector3 headNow = head.position;
        Vector3 targetHead = new Vector3(rideAnchor.position.x, headNow.y, rideAnchor.position.z);
        rig.position += targetHead - headNow;

        return true;
    }

    private void ApplySwingPose()
    {
        if (seatAnchor == null || !restSeatStateCached)
        {
            return;
        }

        // Drive the seat's LOCAL Z rotation directly. This is exactly the channel the authored
        // Idle clip animates, so the visible seat board pivots the same way it would naturally.
        // Quaternion delta around the local (0,0,1) axis is applied AFTER the rest rotation.
        Quaternion swingDelta = Quaternion.AngleAxis(angleDegrees, Vector3.forward);
        seatAnchor.localRotation = restSeatLocalRotation * swingDelta;

        if (rideAnchor == null)
        {
            return;
        }

        // Rider position: a fixed point in the seat's LOCAL frame (just below the pivot).
        // TransformPoint converts to world automatically, so as the seat rotates the rider
        // traces a true pendulum arc with no extra math needed.
        Vector3 riderWorldPosition = seatAnchor.TransformPoint(restRiderLocalPosition);

        // View stays at the INITIAL seat-local +X direction (captured at mount time, projected
        // horizontally) with world up — this means the player feels position translation only,
        // never roll/pitch. Standard VR pendulum-comfort trick.
        rideAnchor.SetPositionAndRotation(
            riderWorldPosition,
            Quaternion.LookRotation(restRiderViewForward, Vector3.up));
    }

    private void CacheSeatRest()
    {
        if (seatAnchor == null)
        {
            restSeatStateCached = false;
            return;
        }

        restSeatLocalRotation = seatAnchor.localRotation;

        // Capture the seat's local +X as the view forward, projected to horizontal so the
        // horizon stays level even if the swing is tilted in the scene.
        Vector3 viewForward = Vector3.ProjectOnPlane(seatAnchor.right, Vector3.up);
        if (viewForward.sqrMagnitude < 0.0001f)
        {
            // Fallback: use the seat's local +Z if +X projected to nothing (e.g. seat pointing straight up).
            viewForward = Vector3.ProjectOnPlane(seatAnchor.forward, Vector3.up);
        }

        if (viewForward.sqrMagnitude < 0.0001f)
        {
            viewForward = Vector3.right;
        }

        restRiderViewForward = viewForward.normalized;
        restRiderViewRight = Vector3.Cross(Vector3.up, restRiderViewForward);
        if (restRiderViewRight.sqrMagnitude < 0.0001f)
        {
            restRiderViewRight = Vector3.forward;
        }
        restRiderViewRight.Normalize();

        restSeatStateCached = true;
    }

    private void ResolveSwingFrame()
    {
        // Rider hangs straight DOWN from the seat anchor in the seat's LOCAL frame. As the seat
        // rotates around its local Z, this offset orbits through 3D space via TransformPoint.
        restRiderLocalPosition = Vector3.down * Mathf.Max(0.05f, swingLength);
    }

    private Vector3 ResolveDismountViewPosition()
    {
        Vector3 side = restRiderViewRight * Mathf.Max(0.1f, dismountSideDistance);
        Vector3 back = -restRiderViewForward * Mathf.Max(0f, dismountBackDistance);

        // Step off to the side of where the rider was sitting (not the high pivot).
        Vector3 riderRestWorld = seatAnchor != null && restSeatStateCached
            ? seatAnchor.TransformPoint(restRiderLocalPosition)
            : transform.position;
        Vector3 groundPosition = ProjectToGround(riderRestWorld + side + back);
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
            if (hitCollider == null || IsSwingOwnedCollider(hitCollider) ||
                (playerRigRoot != null && hitCollider.transform.IsChildOf(playerRigRoot)))
            {
                continue;
            }

            position.y = groundHits[i].point.y;
            return position;
        }

        return position;
    }

    private bool IsSwingOwnedCollider(Collider candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        Transform hitTransform = candidate.transform;
        // This component lives on the SEAT GameObject (TFF_Wooden_Swing_Seat_01A), so
        // hitTransform.IsChildOf(transform) only catches seat colliders. The frame's
        // MeshCollider sits on the PARENT (TFF_Wooden_Swing_01A) and would otherwise be
        // hit by the dismount raycast, dropping the player onto the swing's frame.
        if (hitTransform.IsChildOf(transform))
        {
            return true;
        }

        if (swingVisualRoot != null && hitTransform.IsChildOf(swingVisualRoot))
        {
            return true;
        }

        if (transform.parent != null && hitTransform.IsChildOf(transform.parent))
        {
            return true;
        }

        return false;
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

            if (IsSwingCollider(hitCollider))
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

    private bool IsSwingCollider(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        // Only treat hits on the seat (and its descendants) as the mountable target.
        // Pointing at the frame, ropes, or posts should NOT trigger the mount.
        if (restrictRayToSeatOnly && seatAnchor != null)
        {
            Transform hitTransform = hitCollider.transform;
            return hitTransform == seatAnchor || hitTransform.IsChildOf(seatAnchor);
        }

        return hitCollider.transform.IsChildOf(transform);
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
            playerRigRoot = QuestInteractionUtils.FindInScene("WonderlandXROrigin");
        }

        if (playerHead == null)
        {
            playerHead = QuestInteractionUtils.FindHeadTransform();
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

        if (swingAnimator == null)
        {
            CacheSwingAnimator();
        }

        CacheComfortProfileReference();
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
        // Parent the rideAnchor under a stable transform that does NOT move during the swing
        // (we drive the rider's world position directly in ApplySwingPose). The swing root
        // component holder is ideal — it stays put even as the seatAnchor child rotates.
        Transform parent = ResolveAnchorParent();
        if (parent == null)
        {
            parent = transform;
        }

        if (rideAnchor == null)
        {
            GameObject anchorObject = new GameObject("QuestSwingRideAnchor");
            anchorObject.transform.SetParent(parent, true);
            rideAnchor = anchorObject.transform;
        }
        else if (rideAnchor.parent != parent)
        {
            rideAnchor.SetParent(parent, true);
        }
    }

    private Transform ResolveAnchorParent()
    {
        if (swingVisualRoot != null)
        {
            return swingVisualRoot;
        }

        return transform;
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

    private void CacheSwingAnimator()
    {
        // The swing's Animator runs the authored idle clip that rotates the seat. We disable
        // it while mounted so our manual pose drives the seat instead. Search where the
        // controller is most likely sitting: the swing root, the legacy visual root reference,
        // or the seat's parent.
        Transform[] candidates =
        {
            transform,
            swingVisualRoot,
            seatAnchor != null ? seatAnchor.parent : null,
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            Transform candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            swingAnimator = candidate.GetComponent<Animator>();
            if (swingAnimator != null)
            {
                return;
            }
        }
    }

    private void SetPlayerLocomotionLocked(bool locked)
    {
        AutoAssignReferences();

        if (locked)
        {
            if (characterController != null)
            {
                characterControllerWasEnabled = characterController.enabled;
                characterController.enabled = false;
            }

            if (locomotionRoot != null)
            {
                locomotionRootWasActive = locomotionRoot.activeSelf;
                SetLocomotionRootProvidersLocked(true);
            }

            SetComfortProfileLocomotionLocked(true);
            return;
        }

        if (characterController != null)
        {
            characterController.enabled = false;
            ClampCharacterControllerStepOffsetForScale();
            if (characterControllerWasEnabled && characterController.gameObject.activeInHierarchy)
            {
                characterController.enabled = true;
            }
        }

        SetLocomotionRootProvidersLocked(false);
        SetComfortProfileLocomotionLocked(false);
    }

    private void SetLocomotionRootProvidersLocked(bool locked)
    {
        if (locomotionRoot == null || !locomotionRootWasActive)
        {
            return;
        }

        if (locked)
        {
            lockedLocomotionProviders = locomotionRoot.GetComponentsInChildren<LocomotionProvider>(true);
            lockedLocomotionProviderWasEnabled = new bool[lockedLocomotionProviders.Length];
            for (int i = 0; i < lockedLocomotionProviders.Length; i++)
            {
                LocomotionProvider provider = lockedLocomotionProviders[i];
                if (provider == null)
                {
                    continue;
                }

                lockedLocomotionProviderWasEnabled[i] = provider.enabled;
                provider.enabled = false;
            }

            return;
        }

        if (lockedLocomotionProviders == null || lockedLocomotionProviderWasEnabled == null)
        {
            return;
        }

        int count = Mathf.Min(lockedLocomotionProviders.Length, lockedLocomotionProviderWasEnabled.Length);
        for (int i = 0; i < count; i++)
        {
            LocomotionProvider provider = lockedLocomotionProviders[i];
            if (provider != null)
            {
                provider.enabled = lockedLocomotionProviderWasEnabled[i];
            }
        }

        lockedLocomotionProviders = null;
        lockedLocomotionProviderWasEnabled = null;
    }

    private void ClampCharacterControllerStepOffsetForScale()
    {
        if (characterController == null)
        {
            return;
        }

        Vector3 scale = characterController.transform != null ? characterController.transform.lossyScale : Vector3.one;
        float verticalScale = Mathf.Max(0.0001f, Mathf.Abs(scale.y));
        float horizontalScale = Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));
        float localHeight = Mathf.Max(0f, characterController.height);
        float localRadius = Mathf.Max(0f, characterController.radius);
        float scaledHeight = localHeight * verticalScale;
        float scaledRadius = localRadius * horizontalScale;
        float scaledMaxStepOffset = Mathf.Max(0f, scaledHeight + scaledRadius * 2f - 0.001f);
        float maxLocalStepOffset = Mathf.Min(
            Mathf.Max(0f, localHeight - 0.001f),
            Mathf.Max(0f, scaledMaxStepOffset / verticalScale));

        if (characterController.stepOffset > maxLocalStepOffset)
        {
            characterController.stepOffset = maxLocalStepOffset;
        }
    }

    private void CacheComfortProfileReference()
    {
        if (comfortProfile != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        comfortProfile = FindAnyObjectByType<QuestLocomotionComfortProfile>(FindObjectsInactive.Include);
#else
#pragma warning disable CS0618
        comfortProfile = FindObjectOfType<QuestLocomotionComfortProfile>(true);
#pragma warning restore CS0618
#endif
    }

    private void SetComfortProfileLocomotionLocked(bool locked)
    {
        CacheComfortProfileReference();
        if (comfortProfile != null)
        {
            comfortProfile.SetRuntimeLocomotionLocked(locked);
        }
    }

    private void SyncSwingVignetteFromComfortProfile()
    {
        if (!syncSwingVignetteWithComfortProfile)
        {
            return;
        }

        CacheComfortProfileReference();
        if (comfortProfile == null)
        {
            return;
        }

        ApplySwingVignetteSettings(comfortProfile.ComfortVignetteEnabled, comfortProfile.ComfortVignetteAperture);
    }

    private void HandleComfortVignetteChanged(bool enabled, float comfort01, float aperture)
    {
        if (!syncSwingVignetteWithComfortProfile)
        {
            return;
        }

        ApplySwingVignetteSettings(enabled, aperture);
    }

    private void ApplySwingVignetteSettings(bool enabled, float aperture)
    {
        enableSwingComfortVignette = enabled;
        swingVignetteAperture = Mathf.Clamp(aperture, 0.2f, 1f);

        if (swingVignetteProvider != null)
        {
            swingVignetteProvider.SetParameters(CreateSwingVignetteParameters());

            // The line above just rebuilt the parameters with apertureSize = swingVignetteAperture
            // (the full-close target). If we're mid-ride that would overwrite our dynamic value
            // for one frame and make the ring look "snapped". Stamp the live value back in.
            if (mounted && swingVignetteActive)
            {
                WriteApertureToProvider(currentSwingVignetteAperture);
            }
        }

        if (!enableSwingComfortVignette)
        {
            StopSwingVignette();
        }
    }

    private void CacheSwingVignetteReferences()
    {
        if (swingVignetteProvider == null)
        {
            swingVignetteProvider = new SwingVignetteProvider(CreateSwingVignetteParameters());
        }
        else
        {
            swingVignetteProvider.SetParameters(CreateSwingVignetteParameters());
        }

#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        swingVignetteControllers = FindObjectsByType<TunnelingVignetteController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        swingVignetteControllers = FindObjectsOfType<TunnelingVignetteController>(false);
#pragma warning restore CS0618
#endif
    }

    private VignetteParameters CreateSwingVignetteParameters()
    {
        return new VignetteParameters
        {
            apertureSize = swingVignetteAperture,
            featheringEffect = swingVignetteFeathering,
            easeInTime = swingVignetteEaseInTime,
            easeOutTime = swingVignetteEaseOutTime,
            easeInTimeLock = false,
            easeOutDelayTime = swingVignetteEaseOutDelayTime,
            vignetteColor = Color.black,
            vignetteColorBlend = Color.black,
            apertureVerticalPosition = 0f,
        };
    }

    private void StartSwingVignette()
    {
        if (!enableSwingComfortVignette)
        {
            return;
        }

        if (swingVignetteProvider == null || swingVignetteControllers == null || swingVignetteControllers.Length == 0)
        {
            CacheSwingVignetteReferences();
        }

        if (swingVignetteProvider == null || swingVignetteControllers == null)
        {
            return;
        }

        if (swingVignetteActive)
        {
            return;
        }

        // Begin the vignette fully OPEN (aperture=1, invisible). UpdateSwingVignetteIntensity
        // then closes the ring smoothly as backward speed grows.
        currentSwingVignetteAperture = 1f;
        WriteApertureToProvider(currentSwingVignetteAperture);

        for (int i = 0; i < swingVignetteControllers.Length; i++)
        {
            TunnelingVignetteController controller = swingVignetteControllers[i];
            if (controller == null || !controller.isActiveAndEnabled)
            {
                continue;
            }

            controller.BeginTunnelingVignette(swingVignetteProvider);
        }

        swingVignetteActive = true;
    }

    private void StopSwingVignette()
    {
        if (!swingVignetteActive)
        {
            return;
        }

        if (swingVignetteControllers != null)
        {
            for (int i = 0; i < swingVignetteControllers.Length; i++)
            {
                TunnelingVignetteController controller = swingVignetteControllers[i];
                if (controller == null)
                {
                    continue;
                }

                // Always queue the End even if the controller is momentarily disabled, otherwise
                // the provider's record stays pinned and the ring sticks on after dismount.
                controller.EndTunnelingVignette(swingVignetteProvider);
            }
        }

        swingVignetteActive = false;
        currentSwingVignetteAperture = 1f;
    }

    private void UpdateSwingVignetteIntensity(float deltaTime)
    {
        if (!swingVignetteActive || !enableSwingComfortVignette || swingVignetteProvider == null)
        {
            return;
        }

        // Convert the angular state into a HORIZONTAL world speed along the seat's local +X
        // direction. cos(angle) handles the projection; the sign of angularVelocity tells us
        // whether the rider is moving forward (+X) or backward (-X).
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        float horizontalSpeedDegPerSec = angularVelocity * Mathf.Cos(angleRadians);

        // Convert deg/sec to meters/sec for an intuitive m/s parameter (small-angle approx for
        // a pendulum of length swingLength: v ≈ L * dθ/dt).
        float horizontalSpeedMPerSec = horizontalSpeedDegPerSec * Mathf.Deg2Rad * Mathf.Max(0.05f, swingLength);

        // Only NEGATIVE (backward) speed triggers the vignette.
        float backwardSpeed = Mathf.Max(0f, -horizontalSpeedMPerSec);

        float targetAperture;
        if (backwardSpeed <= swingVignetteSpeedDeadzone)
        {
            targetAperture = 1f;
        }
        else
        {
            float range = Mathf.Max(0.001f, swingVignetteFullSpeed - swingVignetteSpeedDeadzone);
            float t = Mathf.Clamp01((backwardSpeed - swingVignetteSpeedDeadzone) / range);
            // smoothstep gives a softer "ring zoom" feel than linear
            t = t * t * (3f - 2f * t);
            targetAperture = Mathf.Lerp(1f, swingVignetteAperture, t);
        }

        // Exponential easing toward the target — smoothing rate is deltaTime-independent.
        float alpha = 1f - Mathf.Exp(-swingVignetteResponseSpeed * deltaTime);
        currentSwingVignetteAperture = Mathf.Lerp(currentSwingVignetteAperture, targetAperture, alpha);
        WriteApertureToProvider(currentSwingVignetteAperture);
    }

    private void WriteApertureToProvider(float aperture)
    {
        if (swingVignetteProvider == null)
        {
            return;
        }

        VignetteParameters parameters = swingVignetteProvider.vignetteParameters;
        parameters.apertureSize = Mathf.Clamp(aperture, swingVignetteAperture, 1f);
        swingVignetteProvider.SetParameters(parameters);
    }

    private sealed class SwingVignetteProvider : ITunnelingVignetteProvider
    {
        public SwingVignetteProvider(VignetteParameters parameters)
        {
            vignetteParameters = parameters;
        }

        public VignetteParameters vignetteParameters { get; private set; }

        public void SetParameters(VignetteParameters parameters)
        {
            vignetteParameters = parameters;
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
