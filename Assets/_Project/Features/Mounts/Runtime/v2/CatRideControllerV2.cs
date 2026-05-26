using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;

public class CatRideControllerV2 : MonoBehaviour
{
    public enum MountScaleRequirement
    {
        Any,
        SmallOnly,
        NormalOnly,
        LargeOnly
    }

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
    [SerializeField] private MountScaleRequirement mountScaleRequirement = MountScaleRequirement.SmallOnly;

    [Header("Quest Interaction")]
    [SerializeField] private bool enableQuestControllerInteraction = true;
    [SerializeField] private Transform questRayOrigin;
    [SerializeField] private LayerMask questRayMask = ~0;
    [SerializeField] private float questRayDistance = 7f;
    [SerializeField] private float questMountMaxDistance = 2.6f;
    [SerializeField] private bool allowQuestPrimaryButtonDismount = true;
    [SerializeField] private bool allowQuestTriggerDismount = false;
    [SerializeField] private Color questMountOutlineColor = new Color(1f, 0.66f, 0.28f, 0.64f);

    [Header("Mounted UI Safety")]
    [SerializeField] private bool ignoreMountCollidersForRaycastsWhileMounted = true;
    [SerializeField] private int mountedRaycastIgnoreLayer = 2;

    [Header("Manual Ride")]
    [SerializeField] private float manualMoveSpeed = 6.25f;
    [SerializeField] private float manualTurnSpeed = 120f;
    [SerializeField] private InputActionReference mountAction;
    [SerializeField] private InputActionReference dismountAction;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference turnAction;
    [SerializeField] private Key mountKey = Key.F;
    [SerializeField] private Key dismountKey = Key.F;

    [Header("Production Debug")]
    [SerializeField] private bool enableKeyboardDebugControls = false;
    [SerializeField] private bool lockXRDeviceSimulatorDuringRide = true;
    [SerializeField] private float debugSimulatorRestoreDelay = 0.15f;

    [Header("Ride Comfort")]
    [SerializeField] private bool syncRideVignetteWithComfortProfile = true;
    [SerializeField] private QuestLocomotionComfortProfile comfortProfile;
    [SerializeField] private bool enableRideComfortVignette = true;
    [SerializeField, Range(0.2f, 1f)] private float rideVignetteAperture = 0.58f;
    [SerializeField, Range(0f, 1f)] private float rideVignetteFeathering = 0.30f;
    [SerializeField, Min(0f)] private float rideVignetteEaseInTime = 0.10f;
    [SerializeField, Min(0f)] private float rideVignetteEaseOutTime = 0.20f;
    [SerializeField, Min(0f)] private float rideVignetteEaseOutDelayTime = 0.06f;
    [SerializeField, Min(0f)] private float rideVignetteInputDeadzone = 0.08f;

    [Header("Auto Ride")]
    [SerializeField] private List<Transform> autoRoutePoints = new List<Transform>();

    [Header("Blend")]
    [SerializeField] private float fallbackMountBlendTime = 0.25f;
    [SerializeField] private float fallbackDismountBlendTime = 0.25f;

    [Header("Dismount")]
    [SerializeField] private bool useAuthoredDismountPoint = false;
    [SerializeField] private bool dismountOnRightSide = true;
    [SerializeField] private float dismountSideDistance = 1.35f;
    [SerializeField] private float dismountRearDistance = 0.65f;
    [SerializeField] private float dismountSideClearance = 0.2f;
    [SerializeField] private LayerMask dismountCollisionMask = ~0;
    [SerializeField] private float dismountMaxSafetyNudgeDistance = 0.75f;
    [SerializeField] private float dismountClearanceRadiusStep = 0.35f;
    [SerializeField] private float dismountGroundProbeHeight = 2f;
    [SerializeField] private float dismountGroundProbeDistance = 6f;
    [SerializeField] private float dismountGroundLift = 0.05f;
    [SerializeField] private float dismountUnlockDelay = 0.08f;
    [SerializeField] private int dismountSettleFramesBeforeUnlock = 2;
    [SerializeField] private int postDismountPoseHoldFrames = 3;

    [Header("Terrain Motion")]
    [SerializeField] private bool projectRideMotionToGround = true;
    [SerializeField] private LayerMask rideGroundMask = ~0;
    [SerializeField] private float rideGroundProbeHeight = 3f;
    [SerializeField] private float rideGroundProbeDistance = 12f;
    [SerializeField] private float rideGroundOffset = 0f;
    [SerializeField] private float rideMaxStepUp = 1.5f;
    [SerializeField] private float rideMaxStepDown = 5f;
    [SerializeField] private bool alignRideToGroundNormal = true;
    [SerializeField] private Transform rideVisualTiltRoot;
    [SerializeField] private float rideGroundAlignSpeed = 240f;
    [SerializeField] private float rideMaxGroundTiltAngle = 32f;

    [Header("Mounted Rider Slope Pose")]
    [SerializeField] private bool tiltMountedRiderWithSlope = true;
    [SerializeField, Range(0f, 1f)] private float riderSlopePitchMultiplier = 0.18f;
    [SerializeField, Range(0f, 1f)] private float riderSlopeRollMultiplier = 0.08f;
    [SerializeField] private float riderMaxSlopePitch = 4f;
    [SerializeField] private float riderMaxSlopeRoll = 1.5f;
    [SerializeField] private float riderSlopeTiltSpeed = 6f;


    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    [SerializeField] private RideState currentState = RideState.Idle;

    private CharacterController playerCharacterController;
    private bool playerCharacterControllerWasEnabled;
    private bool playerCharacterControllerDetectCollisionsWasEnabled;
    private bool locomotionRootWasActive;
    private LocomotionProvider[] lockedLocomotionProviders;
    private bool[] lockedLocomotionProviderWasEnabled;
    private Transform rigOriginalParent;
    private int rigOriginalSiblingIndex = -1;
    private bool hasRigOriginalParent;

    private Transform trackedHeadTransform;

    private XROrigin xrOrigin;
    private XRDeviceSimulator xrDeviceSimulator;
    [SerializeField] private ScaleManager scaleManager;
    private bool simulatorKeyboardXWasEnabled;
    private bool simulatorKeyboardYWasEnabled;
    private bool simulatorKeyboardZWasEnabled;
    private readonly Collider[] dismountOverlapBuffer = new Collider[24];
    private readonly RaycastHit[] dismountGroundHitBuffer = new RaycastHit[8];
    private readonly RaycastHit[] rideGroundHitBuffer = new RaycastHit[8];
    private Vector3 lastRideGroundNormal = Vector3.up;
    private bool hasRideGroundNormal;
    private readonly RaycastHit[] questRayHits = new RaycastHit[16];
    private Collider[] questTargetColliders;
    private GameObject[] mountedRaycastIgnoredObjects;
    private int[] mountedRaycastOriginalLayers;
    private bool mountCollidersAreIgnoredForRaycasts;
    private QuestInteractableFeedback questFeedback;
    private HapticImpulsePlayer questRightHaptics;
    private bool questHovering;
    private bool questTriggerLastFrame;
    private bool questPrimaryLastFrame;
    private TunnelingVignetteController[] rideVignetteControllers;
    private RideVignetteProvider rideVignetteProvider;
    private bool rideVignetteActive;
    private Vector3 mountedRigNeutralLocalPosition;
    private Quaternion mountedRigNeutralLocalRotation = Quaternion.identity;
    private Quaternion mountedRiderSlopeOffset = Quaternion.identity;
    private bool hasMountedRigNeutralLocalRotation;

    public bool IsRideActive => currentState != RideState.Idle;
    public bool IsAutoRideActive => currentState == RideState.MountedAuto;

    public IReadOnlyList<Transform> AutoRoutePoints => autoRoutePoints;


    private int currentAutoIndex = 0;
    private Coroutine stateRoutine;



    private void Awake()
    {
        WonderfulWorld.Audio.WonderlandMountAudioAutoBinder.EnsureFootsteps(gameObject);
        CacheRigReferences();
        CacheQuestInteractionReferences();
        CacheScaleManagerReference();
        CacheRideVignetteReferences();
        AutoAssignRideVisualTiltRoot();
        SyncRideVignetteFromComfortProfile();
    }

    private void OnEnable()
    {
        QuestLocomotionComfortProfile.ComfortVignetteChanged += HandleComfortVignetteChanged;
        SyncRideVignetteFromComfortProfile();
    }

    private void OnDisable()
    {
        QuestLocomotionComfortProfile.ComfortVignetteChanged -= HandleComfortVignetteChanged;
        SetComfortProfileLocomotionLocked(false);
        SetMountCollidersIgnoredForRaycasts(false);
        SetRideVignetteActive(false);
    }

    private void Update()
    {
        if (UpdateQuestControllerInteraction())
        {
            return;
        }

        if (currentState == RideState.Idle)
        {
            if (WasPressed(mountAction, mountKey) && IsPlayerInsideMountZone() && CanMountInCurrentScale())

            {
                StartMount();
            }

            return;
        }

        if (currentState == RideState.MountedManual)
        {
            HandleManualRide();
            ApplyMountedRiderSlopePose(false);

            if (WasPressed(dismountAction, dismountKey))
            {
                StartDismount();
            }

            return;
        }

        if (currentState == RideState.MountedAuto)
        {
            HandleAutoRide();
            ApplyMountedRiderSlopePose(false);

            if (WasPressed(dismountAction, dismountKey))
            {
                StartDismount();
            }
        }
    }

    private bool UpdateQuestControllerInteraction()
    {
        if (!enableQuestControllerInteraction)
        {
            return false;
        }

        CacheQuestInteractionReferences();

        bool triggerPressed;
        QuestInteractionUtils.TryReadTriggerButton(true, out triggerPressed);
        bool triggerPressedThisFrame = triggerPressed && !questTriggerLastFrame;
        questTriggerLastFrame = triggerPressed;

        bool primaryPressed;
        QuestInteractionUtils.TryReadPrimaryButton(true, out primaryPressed);
        bool primaryPressedThisFrame = primaryPressed && !questPrimaryLastFrame;
        questPrimaryLastFrame = primaryPressed;

        if (currentState == RideState.Idle)
        {
            bool canMount = CanMountInCurrentScale() && IsPlayerCloseEnoughForQuestMount();
            bool hover = canMount && RayHitsMount();
            SetQuestHover(hover);
            if (triggerPressedThisFrame && hover)
            {
                questFeedback?.PulseSelect(questRightHaptics);
                StartMount();
                return true;
            }

            return triggerPressedThisFrame;
        }

        SetQuestHover(false);
        if ((allowQuestPrimaryButtonDismount && primaryPressedThisFrame) ||
            (allowQuestTriggerDismount && triggerPressedThisFrame))
        {
            if (currentState == RideState.MountedManual || currentState == RideState.MountedAuto)
            {
                StartDismount();
                return true;
            }
        }

        return false;
    }

    private void CacheQuestInteractionReferences()
    {
        if (questRayOrigin == null)
        {
            questRayOrigin = QuestInteractionUtils.FindControllerRayOrigin(true);
        }

        if (questRightHaptics == null)
        {
            questRightHaptics = QuestInteractionUtils.FindHapticPlayer(true, questRayOrigin);
        }

        if (questTargetColliders == null || questTargetColliders.Length == 0)
        {
            questTargetColliders = GetComponentsInChildren<Collider>(true);
        }

        if (questFeedback == null)
        {
            questFeedback = GetComponent<QuestInteractableFeedback>();
            if (questFeedback == null)
            {
                questFeedback = gameObject.AddComponent<QuestInteractableFeedback>();
            }

            questFeedback.Configure(questMountOutlineColor, 0.02f);
        }
    }

    private bool RayHitsMount()
    {
        if (questRayOrigin == null)
        {
            return false;
        }

        Ray ray = new Ray(questRayOrigin.position, questRayOrigin.forward);
        int hitCount = Physics.RaycastNonAlloc(
            ray,
            questRayHits,
            Mathf.Max(0.1f, questRayDistance),
            questRayMask,
            QueryTriggerInteraction.Collide);

        if (hitCount <= 0)
        {
            return false;
        }

        System.Array.Sort(questRayHits, 0, hitCount, RaycastHitDistanceComparer.Instance);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = questRayHits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (IsMountCollider(hitCollider))
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

    private bool IsMountCollider(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        if (hitCollider.transform.IsChildOf(transform))
        {
            return true;
        }

        if (questTargetColliders == null)
        {
            return false;
        }

        for (int i = 0; i < questTargetColliders.Length; i++)
        {
            if (questTargetColliders[i] == hitCollider)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPlayerCloseEnoughForQuestMount()
    {
        if (IsPlayerInsideMountZone())
        {
            return true;
        }

        Transform reference = trackedHeadTransform != null ? trackedHeadTransform : (playerRigRoot != null ? playerRigRoot.transform : null);
        if (reference == null)
        {
            CacheRigReferences();
            reference = trackedHeadTransform != null ? trackedHeadTransform : (playerRigRoot != null ? playerRigRoot.transform : null);
        }

        if (reference == null)
        {
            return false;
        }

        Vector3 playerPosition = reference.position;
        Vector3 mountPosition = seatAnchor != null ? seatAnchor.position : transform.position;
        playerPosition.y = 0f;
        mountPosition.y = 0f;
        return Vector3.Distance(playerPosition, mountPosition) <= Mathf.Max(remountDistance, questMountMaxDistance);
    }

    private void SetMountCollidersIgnoredForRaycasts(bool ignored)
    {
        if (ignored && !ignoreMountCollidersForRaycastsWhileMounted)
        {
            return;
        }

        if (ignored)
        {
            if (mountCollidersAreIgnoredForRaycasts)
            {
                return;
            }

            Collider[] mountColliders = GetComponentsInChildren<Collider>(true);
            var ignoredObjects = new List<GameObject>(mountColliders.Length);
            var originalLayers = new List<int>(mountColliders.Length);
            var seenObjects = new HashSet<GameObject>();

            int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreLayer < 0)
            {
                ignoreLayer = Mathf.Clamp(mountedRaycastIgnoreLayer, 0, 31);
            }

            Transform rigTransform = playerRigRoot != null ? playerRigRoot.transform : null;
            for (int i = 0; i < mountColliders.Length; i++)
            {
                Collider targetCollider = mountColliders[i];
                if (targetCollider == null)
                {
                    continue;
                }

                if (rigTransform != null && targetCollider.transform.IsChildOf(rigTransform))
                {
                    continue;
                }

                GameObject colliderObject = targetCollider.gameObject;
                if (colliderObject == null || !seenObjects.Add(colliderObject))
                {
                    continue;
                }

                ignoredObjects.Add(colliderObject);
                originalLayers.Add(colliderObject.layer);
                colliderObject.layer = ignoreLayer;
            }

            mountedRaycastIgnoredObjects = ignoredObjects.ToArray();
            mountedRaycastOriginalLayers = originalLayers.ToArray();
            mountCollidersAreIgnoredForRaycasts = true;
            return;
        }

        if (!mountCollidersAreIgnoredForRaycasts)
        {
            return;
        }

        if (mountedRaycastIgnoredObjects != null && mountedRaycastOriginalLayers != null)
        {
            int count = Mathf.Min(mountedRaycastIgnoredObjects.Length, mountedRaycastOriginalLayers.Length);
            for (int i = 0; i < count; i++)
            {
                GameObject targetObject = mountedRaycastIgnoredObjects[i];
                if (targetObject == null || mountedRaycastOriginalLayers[i] < 0)
                {
                    continue;
                }

                targetObject.layer = mountedRaycastOriginalLayers[i];
            }
        }

        mountedRaycastIgnoredObjects = null;
        mountedRaycastOriginalLayers = null;
        mountCollidersAreIgnoredForRaycasts = false;
        questTargetColliders = null;
    }

    private void SetQuestHover(bool hover)
    {
        if (questHovering == hover)
        {
            if (questFeedback != null)
            {
                questFeedback.SetHovered(hover, questRightHaptics);
            }

            return;
        }

        questHovering = hover;
        questFeedback?.SetInteractable(hover);
        questFeedback?.SetHovered(hover, questRightHaptics);
    }

    private bool WasPressed(InputActionReference actionReference, Key debugKey)
    {
        if (actionReference != null && actionReference.action != null && actionReference.action.WasPressedThisFrame())
        {
            return true;
        }

        return enableKeyboardDebugControls &&
               Keyboard.current != null &&
               Keyboard.current[debugKey].wasPressedThisFrame;
    }

    private static Vector2 ReadVector2(InputActionReference actionReference)
    {
        if (actionReference == null || actionReference.action == null)
        {
            return Vector2.zero;
        }

        return actionReference.action.ReadValue<Vector2>();
    }

    private static Vector2 ReadQuestAxisFallback(InputActionReference actionReference, bool rightHand)
    {
        Vector2 actionValue = ReadVector2(actionReference);
        if (actionValue.sqrMagnitude > 0.0001f)
        {
            return actionValue;
        }

        return QuestInteractionUtils.TryReadPrimary2DAxis(rightHand, out Vector2 questValue)
            ? questValue
            : Vector2.zero;
    }

    private void CacheRigReferences()
    {
        if (playerRigRoot != null)
        {
            if (xrOrigin == null)
            {
                xrOrigin = playerRigRoot.GetComponent<XROrigin>();
            }

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

    private void SyncRideVignetteFromComfortProfile()
    {
        if (!syncRideVignetteWithComfortProfile)
        {
            return;
        }

        CacheComfortProfileReference();

        if (comfortProfile == null)
        {
            return;
        }

        ApplyRideVignetteSettings(comfortProfile.ComfortVignetteEnabled, comfortProfile.ComfortVignetteAperture);
    }

    private void HandleComfortVignetteChanged(bool enabled, float comfort01, float aperture)
    {
        if (!syncRideVignetteWithComfortProfile)
        {
            return;
        }

        ApplyRideVignetteSettings(enabled, aperture);
    }

    private void ApplyRideVignetteSettings(bool enabled, float aperture)
    {
        enableRideComfortVignette = enabled;
        rideVignetteAperture = Mathf.Clamp(aperture, 0.2f, 1f);

        if (rideVignetteProvider != null)
        {
            rideVignetteProvider.SetParameters(CreateRideVignetteParameters());
        }

        if (!enableRideComfortVignette)
        {
            SetRideVignetteActive(false);
        }
    }

    private void CacheRideVignetteReferences()
    {
        if (rideVignetteProvider == null)
        {
            rideVignetteProvider = new RideVignetteProvider(CreateRideVignetteParameters());
        }
        else
        {
            rideVignetteProvider.SetParameters(CreateRideVignetteParameters());
        }

#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        rideVignetteControllers = FindObjectsByType<TunnelingVignetteController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        rideVignetteControllers = FindObjectsOfType<TunnelingVignetteController>(false);
#pragma warning restore CS0618
#endif
    }

    private VignetteParameters CreateRideVignetteParameters()
    {
        return new VignetteParameters
        {
            apertureSize = rideVignetteAperture,
            featheringEffect = rideVignetteFeathering,
            easeInTime = rideVignetteEaseInTime,
            easeOutTime = rideVignetteEaseOutTime,
            easeInTimeLock = false,
            easeOutDelayTime = rideVignetteEaseOutDelayTime,
            vignetteColor = Color.black,
            vignetteColorBlend = Color.black,
            apertureVerticalPosition = 0f,
        };
    }

    private void SetRideVignetteActive(bool active)
    {
        if (!enableRideComfortVignette)
        {
            active = false;
        }

        if (rideVignetteProvider == null || rideVignetteControllers == null || rideVignetteControllers.Length == 0)
        {
            CacheRideVignetteReferences();
        }

        if (rideVignetteProvider == null || rideVignetteControllers == null)
        {
            rideVignetteActive = false;
            return;
        }

        if (rideVignetteActive == active)
        {
            return;
        }

        for (int i = 0; i < rideVignetteControllers.Length; i++)
        {
            TunnelingVignetteController vignetteController = rideVignetteControllers[i];
            if (vignetteController == null)
            {
                continue;
            }

            if (active)
            {
                if (!vignetteController.isActiveAndEnabled)
                {
                    continue;
                }

                vignetteController.BeginTunnelingVignette(rideVignetteProvider);
            }
            else
            {
                // Always queue the End even if the controller is momentarily disabled
                // during a state transition — otherwise the provider's record stays
                // pinned in EasingIn and the vignette is stuck after dismount.
                vignetteController.EndTunnelingVignette(rideVignetteProvider);
            }
        }

        rideVignetteActive = active;
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

    private bool CanMountInCurrentScale()
    {
        CacheScaleManagerReference();

        if (scaleManager == null)
        {
            return false;
        }

        switch (mountScaleRequirement)
        {
            case MountScaleRequirement.Any:
                return true;

            case MountScaleRequirement.SmallOnly:
                return scaleManager.CurrentState == ScaleState.Small;

            case MountScaleRequirement.NormalOnly:
                return scaleManager.CurrentState == ScaleState.Normal;

            case MountScaleRequirement.LargeOnly:
                return scaleManager.CurrentState == ScaleState.Large;

            default:
                return false;
        }
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
                playerCharacterControllerDetectCollisionsWasEnabled = playerCharacterController.detectCollisions;
                playerCharacterController.detectCollisions = false;
                playerCharacterController.enabled = false;
            }

            if (locomotionRoot != null)
            {
                locomotionRootWasActive = locomotionRoot.activeSelf;
                SetLocomotionRootBehavioursLocked(true);
            }

            SetComfortProfileLocomotionLocked(true);

            if (xrDeviceSimulator != null)
            {
                DisableXRDeviceSimulatorInput();
            }
        }
        else
        {
            if (playerCharacterController != null)
            {
                playerCharacterController.enabled = false;
                ClampCharacterControllerStepOffsetForScale();

                bool shouldRestoreController =
                    playerCharacterControllerWasEnabled &&
                    playerCharacterController.gameObject.activeInHierarchy;

                if (shouldRestoreController)
                {
                    playerCharacterController.enabled = true;
                }

                playerCharacterController.detectCollisions = playerCharacterControllerDetectCollisionsWasEnabled;
            }

            if (locomotionRoot != null)
            {
                SetLocomotionRootBehavioursLocked(false);
                locomotionRoot.SetActive(locomotionRootWasActive);
            }

            SetComfortProfileLocomotionLocked(false);

            if (!lockXRDeviceSimulatorDuringRide)
            {
                RestoreXRDeviceSimulatorInput();
            }
        }
    }

    private void DisableXRDeviceSimulatorInput()
    {
        if (xrDeviceSimulator == null)
        {
            return;
        }

        simulatorKeyboardXWasEnabled = IsActionEnabled(xrDeviceSimulator.keyboardXTranslateAction);
        simulatorKeyboardYWasEnabled = IsActionEnabled(xrDeviceSimulator.keyboardYTranslateAction);
        simulatorKeyboardZWasEnabled = IsActionEnabled(xrDeviceSimulator.keyboardZTranslateAction);

        // Keep controller and hand action assets alive so XR ray/UI selection still works while mounted.
        SetActionEnabled(xrDeviceSimulator.keyboardXTranslateAction, false);
        SetActionEnabled(xrDeviceSimulator.keyboardYTranslateAction, false);
        SetActionEnabled(xrDeviceSimulator.keyboardZTranslateAction, false);
    }

    private void RestoreXRDeviceSimulatorInput()
    {
        if (xrDeviceSimulator == null)
        {
            return;
        }

        SetActionEnabled(xrDeviceSimulator.keyboardXTranslateAction, simulatorKeyboardXWasEnabled);
        SetActionEnabled(xrDeviceSimulator.keyboardYTranslateAction, simulatorKeyboardYWasEnabled);
        SetActionEnabled(xrDeviceSimulator.keyboardZTranslateAction, simulatorKeyboardZWasEnabled);
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

        WonderfulWorld.Audio.WonderlandAudioOneShotPlayer.PlayAt("WW_SFX_MountTransition", transform.position, volumeScale: 1f, maxVoices: 3);
        WonderfulWorld.Audio.WonderlandMountAudioAutoBinder.PlayVoice(gameObject, volumeScale: 0.85f, maxVoices: 2);
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
        SetMountCollidersIgnoredForRaycasts(true);

        currentState = RideState.Mounting;

        Transform rig = playerRigRoot.transform;
        rigOriginalParent = rig.parent;
        rigOriginalSiblingIndex = rig.GetSiblingIndex();
        hasRigOriginalParent = true;

        rig.SetParent(seatAnchor, true);

        Vector3 startLocalPosition = rig.localPosition;
        Quaternion startLocalRotation = rig.localRotation;

        AlignMountedViewToSeatForward(rig, false);
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

        SetMountedRigNeutralPose(rig);
        ApplyMountedRiderSlopePose(true);

        currentAutoIndex = 0;
        currentState = RideState.MountedManual;
        UpdateKittyAnimation(0f, false);
        stateRoutine = null;

        if (debugLogs)
        {
            Debug.Log("[CatRideControllerV2] Mounted. Manual control enabled.");
        }
    }

    private void AlignMountedViewToSeatForward(Transform rig, bool alignFromCurrentHeadYaw)
    {
        if (rig == null || seatAnchor == null)
        {
            return;
        }

        // During mount we align the neutral rig heading; during B-recenter we align the
        // current HMD yaw, which gives the player an explicit way to re-square the view.
        if (alignFromCurrentHeadYaw && trackedHeadTransform == null)
        {
            CacheRigReferences();
        }

        Vector3 currentRigForward = alignFromCurrentHeadYaw && trackedHeadTransform != null
            ? trackedHeadTransform.forward
            : rig.forward;
        currentRigForward.y = 0f;

        if (currentRigForward.sqrMagnitude < 0.0001f)
        {
            currentRigForward = Vector3.forward;
        }

        Transform targetAnchor = mountedViewAnchor != null ? mountedViewAnchor : seatAnchor;
        Vector3 targetForward = targetAnchor != null ? targetAnchor.forward : seatAnchor.forward;
        targetForward.y = 0f;

        if (targetForward.sqrMagnitude < 0.0001f)
        {
            targetForward = transform.forward;
            targetForward.y = 0f;
        }

        currentRigForward.Normalize();
        targetForward.Normalize();

        Quaternion yawDelta = Quaternion.FromToRotation(currentRigForward, targetForward);
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

    /// <summary>
    /// Re-aligns the rider's view to face along the mounted view anchor and re-snaps the head to the
    /// mounted view anchor. Safe to call only while a ride is active; no-op otherwise.
    /// Intended for the B-button "recenter" affordance during a ride.
    /// </summary>
    public bool RecenterMountedView()
    {
        if ((currentState != RideState.MountedManual && currentState != RideState.MountedAuto) || playerRigRoot == null)
        {
            return false;
        }

        Transform rig = playerRigRoot.transform;
        mountedRiderSlopeOffset = Quaternion.identity;
        AlignMountedViewToSeatForward(rig, true);
        SnapHeadToMountedViewAnchor(rig);
        SetMountedRigNeutralPose(rig);
        Physics.SyncTransforms();

        if (debugLogs)
        {
            Debug.Log("[CatRideControllerV2] Mounted view recentered.");
        }

        return true;
    }

    private void StartDismount()
    {
        if ((currentState != RideState.MountedManual && currentState != RideState.MountedAuto) || stateRoutine != null)
        {
            return;
        }

        SetRideVignetteActive(false);
        WonderfulWorld.Audio.WonderlandAudioOneShotPlayer.PlayAt("WW_SFX_MountTransition", transform.position, volumeScale: 0.9f, maxVoices: 3);
        stateRoutine = StartCoroutine(DismountSequence());
    }

    private IEnumerator DismountSequence()
    {
        if (playerRigRoot == null)
        {
            SetComfortProfileLocomotionLocked(false);
            SetMountCollidersIgnoredForRaycasts(false);
            stateRoutine = null;
            yield break;
        }

        currentState = RideState.Dismounting;

        Transform rig = playerRigRoot.transform;
        Vector3 startWorldPosition = rig.position;
        Quaternion startWorldRotation = rig.rotation;
        Vector3 startCameraWorldPosition = trackedHeadTransform != null ? trackedHeadTransform.position : startWorldPosition;
        Transform restoreParent = hasRigOriginalParent ? rigOriginalParent : null;
        int restoreSiblingIndex = rigOriginalSiblingIndex;

        rig.SetParent(restoreParent, true);

        Vector3 targetViewGroundPosition;
        Quaternion targetWorldRotation;
        ResolveDismountPose(startWorldRotation, out targetViewGroundPosition, out targetWorldRotation);
        targetViewGroundPosition = ResolveSafeDismountViewPosition(targetViewGroundPosition, targetWorldRotation);

        // Lift slightly before restoring the CharacterController to avoid ground or mount overlap.
        targetViewGroundPosition += Vector3.up * dismountGroundLift;
        Vector3 targetCameraWorldPosition = ResolveDismountCameraWorldPosition(targetViewGroundPosition);

        float duration = settings != null
            ? Mathf.Max(0f, settings.dismountBlendTime)
            : fallbackDismountBlendTime;

        if (duration <= 0f)
        {
            PlaceRigForDismountCameraPose(rig, targetCameraWorldPosition, targetWorldRotation);
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);

                PlaceRigForDismountCameraPose(
                    rig,
                    Vector3.Lerp(startCameraWorldPosition, targetCameraWorldPosition, t),
                    Quaternion.Slerp(startWorldRotation, targetWorldRotation, t));

                yield return null;
            }

            PlaceRigForDismountCameraPose(rig, targetCameraWorldPosition, targetWorldRotation);
        }

        if (restoreParent != null && restoreSiblingIndex >= 0)
        {
            rig.SetSiblingIndex(Mathf.Min(restoreSiblingIndex, restoreParent.childCount - 1));
        }

        Physics.SyncTransforms();

        yield return HoldRigPoseForFixedFrames(rig, targetCameraWorldPosition, targetWorldRotation, dismountSettleFramesBeforeUnlock);

        if (dismountUnlockDelay > 0f)
        {
            yield return HoldRigPoseForSeconds(rig, targetCameraWorldPosition, targetWorldRotation, dismountUnlockDelay);
        }
        else
        {
            yield return null;
        }

        SyncCharacterControllerHorizontalCenterToHead();
        SetPlayerLocomotionLocked(false);
        PlaceRigForDismountCameraPose(rig, targetCameraWorldPosition, targetWorldRotation);
        Physics.SyncTransforms();

        if (IsCharacterControllerReady())
        {
            playerCharacterController.Move(Vector3.zero);
            PlaceRigForDismountCameraPose(rig, targetCameraWorldPosition, targetWorldRotation);
            Physics.SyncTransforms();
        }

        yield return HoldRigPoseForFixedFrames(rig, targetCameraWorldPosition, targetWorldRotation, postDismountPoseHoldFrames);

        if (lockXRDeviceSimulatorDuringRide)
        {
            if (debugSimulatorRestoreDelay > 0f)
            {
                yield return HoldRigPoseForSeconds(rig, targetCameraWorldPosition, targetWorldRotation, debugSimulatorRestoreDelay);
            }

            RestoreXRDeviceSimulatorInput();
            PlaceRigForDismountCameraPose(rig, targetCameraWorldPosition, targetWorldRotation);
            Physics.SyncTransforms();
        }

        currentAutoIndex = 0;
        currentState = RideState.Idle;
        hasRigOriginalParent = false;
        hasMountedRigNeutralLocalRotation = false;
        mountedRiderSlopeOffset = Quaternion.identity;
        SetMountCollidersIgnoredForRaycasts(false);
        UpdateKittyAnimation(0f, false);
        stateRoutine = null;

        if (debugLogs)
        {
            Debug.Log("[CatRideControllerV2] Dismounted.");
        }
    }

    private IEnumerator HoldRigPoseForFixedFrames(Transform rig, Vector3 cameraWorldPosition, Quaternion rotation, int frameCount)
    {
        int frames = Mathf.Max(0, frameCount);
        for (int i = 0; i < frames; i++)
        {
            PlaceRigForDismountCameraPose(rig, cameraWorldPosition, rotation);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
        }

        PlaceRigForDismountCameraPose(rig, cameraWorldPosition, rotation);
        Physics.SyncTransforms();
    }

    private IEnumerator HoldRigPoseForSeconds(Transform rig, Vector3 cameraWorldPosition, Quaternion rotation, float seconds)
    {
        float remaining = Mathf.Max(0f, seconds);
        while (remaining > 0f)
        {
            PlaceRigForDismountCameraPose(rig, cameraWorldPosition, rotation);
            Physics.SyncTransforms();
            remaining -= Time.deltaTime;
            yield return null;
        }

        PlaceRigForDismountCameraPose(rig, cameraWorldPosition, rotation);
        Physics.SyncTransforms();
    }

    private void PlaceRigForDismountCameraPose(Transform rig, Vector3 cameraWorldPosition, Quaternion rotation)
    {
        if (rig == null)
        {
            return;
        }

        rig.rotation = rotation;
        rig.position = ResolveRigPositionForCameraTarget(rig, cameraWorldPosition, rotation);

    }

    private Vector3 ResolveRigPositionForCameraTarget(Transform rig, Vector3 cameraWorldPosition, Quaternion rotation)
    {
        if (rig == null)
        {
            return cameraWorldPosition;
        }

        if (trackedHeadTransform == null)
        {
            CacheRigReferences();
        }

        if (trackedHeadTransform == null)
        {
            return cameraWorldPosition;
        }

        Vector3 localHeadPosition = rig.InverseTransformPoint(trackedHeadTransform.position);
        Vector3 scaledHeadOffset = Vector3.Scale(localHeadPosition, rig.lossyScale);
        return cameraWorldPosition - rotation * scaledHeadOffset;
    }

    private Vector3 ResolveDismountCameraWorldPosition(Vector3 viewGroundPosition)
    {
        return viewGroundPosition + Vector3.up * ResolveCameraWorldHeightAboveGround();
    }

    private float ResolveCameraWorldHeightAboveGround()
    {
        Transform rig = playerRigRoot != null ? playerRigRoot.transform : null;

        if (xrOrigin == null)
        {
            CacheRigReferences();
        }

        if (xrOrigin != null && xrOrigin.CameraInOriginSpaceHeight > 0.05f)
        {
            float verticalScale = rig != null ? Mathf.Abs(rig.lossyScale.y) : 1f;
            return Mathf.Max(0.05f, xrOrigin.CameraInOriginSpaceHeight * verticalScale);
        }

        if (trackedHeadTransform != null && rig != null)
        {
            return Mathf.Max(0.05f, trackedHeadTransform.position.y - rig.position.y);
        }

        return 1.4f;
    }

    private void SyncCharacterControllerHorizontalCenterToHead()
    {
        if (playerCharacterController == null || trackedHeadTransform == null)
        {
            CacheRigReferences();
        }

        if (playerCharacterController == null || trackedHeadTransform == null)
        {
            return;
        }

        Vector3 headLocalPosition = trackedHeadTransform.localPosition;
        playerCharacterController.center = new Vector3(
            headLocalPosition.x,
            playerCharacterController.center.y,
            headLocalPosition.z);
    }

    private void ClampCharacterControllerStepOffsetForScale()
    {
        if (playerCharacterController == null)
        {
            return;
        }

        Transform controllerTransform = playerCharacterController.transform;
        Vector3 lossyScale = controllerTransform != null ? controllerTransform.lossyScale : Vector3.one;
        float verticalScale = Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y));
        float horizontalScale = Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.z)));
        float localHeight = Mathf.Max(0f, playerCharacterController.height);
        float localRadius = Mathf.Max(0f, playerCharacterController.radius);
        float scaledHeight = Mathf.Max(0f, localHeight * verticalScale);
        float scaledRadius = Mathf.Max(0f, localRadius * horizontalScale);
        float scaledMaxStepOffset = Mathf.Max(0f, scaledHeight + scaledRadius * 2f - 0.001f);
        float scaledStepOffset = playerCharacterController.stepOffset * verticalScale;
        float maxLocalStepOffset = Mathf.Min(
            Mathf.Max(0f, localHeight - 0.001f),
            Mathf.Max(0f, localHeight + localRadius * 2f - 0.001f),
            Mathf.Max(0f, scaledMaxStepOffset / verticalScale));

        if (scaledStepOffset <= scaledMaxStepOffset && playerCharacterController.stepOffset <= maxLocalStepOffset)
        {
            return;
        }

        playerCharacterController.stepOffset = maxLocalStepOffset;
    }

    private bool IsCharacterControllerReady()
    {
        return playerCharacterController != null &&
               playerCharacterController.enabled &&
               playerCharacterController.gameObject.activeInHierarchy;
    }

    private void ResolveDismountPose(Quaternion currentRigRotation, out Vector3 targetWorldPosition, out Quaternion targetWorldRotation)
    {
        targetWorldRotation = GetFlattenedRotation(currentRigRotation, ResolveMountForward());

        if (useAuthoredDismountPoint && dismountPoint != null)
        {
            targetWorldPosition = dismountPoint.position;
            targetWorldRotation = GetFlattenedRotation(dismountPoint.rotation, ResolveMountForward());
            return;
        }

        targetWorldPosition = ResolveWorldDismountPosition();
    }

    private static Quaternion GetFlattenedRotation(Quaternion rotation, Vector3 fallbackForward)
    {
        Vector3 forward = rotation * Vector3.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = fallbackForward;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private Vector3 ResolveWorldDismountPosition()
    {
        Vector3 forward = ResolveMountForward();
        Vector3 side = ResolveMountSide(forward);
        Vector3 center = ResolveMountWorldCenter();
        float sideDistance = ResolveDismountSideDistance(side);
        return center +
               side * sideDistance -
               forward * Mathf.Max(0f, dismountRearDistance);
    }

    private Vector3 ResolveMountWorldCenter()
    {
        if (mountTrigger != null)
        {
            Vector3 center = mountTrigger.bounds.center;
            center.y = transform.position.y;
            return center;
        }

        return transform.position;
    }

    private Vector3 ResolveMountForward()
    {
        Transform directionReference = mountedViewAnchor != null ? mountedViewAnchor : seatAnchor;
        Vector3 forward = directionReference != null ? directionReference.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        return forward.sqrMagnitude < 0.0001f ? Vector3.forward : forward.normalized;
    }

    private Vector3 ResolveMountSide(Vector3 forward)
    {
        Transform directionReference = seatAnchor != null ? seatAnchor : mountedViewAnchor;
        Vector3 side = directionReference != null ? directionReference.right : Vector3.Cross(Vector3.up, forward);
        side.y = 0f;
        if (side.sqrMagnitude < 0.0001f)
        {
            side = Vector3.Cross(Vector3.up, forward);
        }

        side = side.sqrMagnitude < 0.0001f ? Vector3.right : side.normalized;
        return dismountOnRightSide ? side : -side;
    }

    private float ResolveDismountSideDistance(Vector3 side)
    {
        float configuredDistance = Mathf.Max(0f, dismountSideDistance);
        if (mountTrigger == null)
        {
            return configuredDistance;
        }

        Bounds bounds = mountTrigger.bounds;
        Vector3 extents = bounds.extents;
        float mountHalfWidthAlongSide =
            Mathf.Abs(side.x) * extents.x +
            Mathf.Abs(side.y) * extents.y +
            Mathf.Abs(side.z) * extents.z;

        float playerRadius = ResolvePlayerHorizontalRadius();
        float boundsDistance = mountHalfWidthAlongSide + playerRadius + Mathf.Max(0f, dismountSideClearance);
        return Mathf.Max(configuredDistance, boundsDistance);
    }

    private float ResolvePlayerHorizontalRadius()
    {
        if (playerCharacterController == null)
        {
            return 0.3f;
        }

        Vector3 lossyScale = playerRigRoot != null ? playerRigRoot.transform.lossyScale : Vector3.one;
        float horizontalScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.z));
        return Mathf.Max(0.05f, playerCharacterController.radius * horizontalScale);
    }

    private Vector3 ResolveSafeDismountViewPosition(Vector3 preferredPosition, Quaternion targetRotation)
    {
        SyncCharacterControllerHorizontalCenterToHead();

        Vector3 projectedPreferred = ProjectDismountToGround(preferredPosition);
        if (IsDismountCapsuleClear(ResolveRigPositionForDismountViewGround(projectedPreferred, targetRotation), targetRotation))
        {
            return projectedPreferred;
        }

        Vector3 forward = ResolveMountForward();
        Vector3 side = ResolveMountSide(forward);
        Vector3 back = -forward;
        float step = Mathf.Max(0.05f, dismountClearanceRadiusStep);
        float maxNudge = Mathf.Max(step, dismountMaxSafetyNudgeDistance);
        int rings = Mathf.Max(1, Mathf.CeilToInt(maxNudge / step));

        for (int ring = 1; ring <= rings; ring++)
        {
            float distance = Mathf.Min(maxNudge, ring * step);
            if (TryResolveClearDismountViewCandidate(projectedPreferred + side * distance, targetRotation, out Vector3 candidate) ||
                TryResolveClearDismountViewCandidate(projectedPreferred + back * distance, targetRotation, out candidate) ||
                TryResolveClearDismountViewCandidate(projectedPreferred + side * distance + back * (distance * 0.5f), targetRotation, out candidate) ||
                TryResolveClearDismountViewCandidate(projectedPreferred - side * (distance * 0.35f), targetRotation, out candidate))
            {
                if (debugLogs)
                {
                    Debug.Log($"[CatRideControllerV2] Nudged dismount point by {Vector3.Distance(projectedPreferred, candidate):0.00}m.", this);
                }

                return candidate;
            }
        }

        if (debugLogs)
        {
            Debug.LogWarning("[CatRideControllerV2] Could not find a fully clear dismount point. Using the projected fallback point.", this);
        }

        return projectedPreferred;
    }

    private bool TryResolveClearDismountViewCandidate(Vector3 candidate, Quaternion targetRotation, out Vector3 resolved)
    {
        resolved = ProjectDismountToGround(candidate);
        return IsDismountCapsuleClear(ResolveRigPositionForDismountViewGround(resolved, targetRotation), targetRotation);
    }

    private Vector3 ResolveRigPositionForDismountViewGround(Vector3 viewGroundPosition, Quaternion targetRotation)
    {
        Transform rig = playerRigRoot != null ? playerRigRoot.transform : null;
        Vector3 cameraWorldPosition = ResolveDismountCameraWorldPosition(viewGroundPosition);
        return ResolveRigPositionForCameraTarget(rig, cameraWorldPosition, targetRotation);
    }

    private Vector3 ProjectDismountToGround(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * Mathf.Max(0.1f, dismountGroundProbeHeight);
        float distance = Mathf.Max(0.1f, dismountGroundProbeDistance);
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            dismountGroundHitBuffer,
            distance,
            dismountCollisionMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.PositiveInfinity;
        bool foundGround = false;
        Vector3 groundPoint = position;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = dismountGroundHitBuffer[i].collider;
            if (hitCollider == null || IsSelfCollider(hitCollider) || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (dismountGroundHitBuffer[i].distance < bestDistance)
            {
                bestDistance = dismountGroundHitBuffer[i].distance;
                groundPoint = dismountGroundHitBuffer[i].point;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            position.y = groundPoint.y;
        }

        return position;
    }

    private bool IsDismountCapsuleClear(Vector3 rigPosition, Quaternion rigRotation)
    {
        if (playerCharacterController == null)
        {
            return true;
        }

        GetCharacterControllerCapsule(rigPosition, rigRotation, out Vector3 capsuleBottom, out Vector3 capsuleTop, out float capsuleRadius);
        int overlapCount = Physics.OverlapCapsuleNonAlloc(
            capsuleBottom,
            capsuleTop,
            capsuleRadius,
            dismountOverlapBuffer,
            dismountCollisionMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider overlap = dismountOverlapBuffer[i];
            if (overlap == null || IsIgnoredDismountCollider(overlap))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void GetCharacterControllerCapsule(Vector3 rigPosition, Quaternion rigRotation, out Vector3 bottom, out Vector3 top, out float radius)
    {
        Vector3 lossyScale = playerRigRoot != null ? playerRigRoot.transform.lossyScale : Vector3.one;
        float horizontalScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.z));
        float verticalScale = Mathf.Abs(lossyScale.y);
        radius = Mathf.Max(0.05f, playerCharacterController.radius * horizontalScale);
        float height = Mathf.Max(radius * 2f, playerCharacterController.height * verticalScale);
        Vector3 scaledCenter = Vector3.Scale(playerCharacterController.center, lossyScale);
        Vector3 center = rigPosition + rigRotation * scaledCenter;
        Vector3 up = Vector3.up;
        float halfSegment = Mathf.Max(0f, (height * 0.5f) - radius);
        bottom = center - up * halfSegment;
        top = center + up * halfSegment;
    }

    private bool IsSelfCollider(Collider candidate)
    {
        return playerRigRoot != null && candidate.transform.IsChildOf(playerRigRoot.transform);
    }

    private bool IsIgnoredDismountCollider(Collider candidate)
    {
        if (candidate == null)
        {
            return true;
        }

        return IsSelfCollider(candidate) || candidate.isTrigger;
    }


    private void HandleManualRide()
    {
        Vector2 moveValue = ReadQuestAxisFallback(moveAction, false);
        Vector2 turnValue = ReadQuestAxisFallback(turnAction, true);

        float moveInput = moveValue.y;
        float turnInput = turnValue.x;

        if (enableKeyboardDebugControls && Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveInput += 1f;
            if (Keyboard.current.sKey.isPressed) moveInput -= 1f;
            if (Keyboard.current.aKey.isPressed) turnInput -= 1f;
            if (Keyboard.current.dKey.isPressed) turnInput += 1f;
        }

        bool rideMotionActive = Mathf.Abs(moveInput) > rideVignetteInputDeadzone ||
                                Mathf.Abs(turnInput) > rideVignetteInputDeadzone;
        SetRideVignetteActive(rideMotionActive);

        transform.Rotate(Vector3.up, turnInput * manualTurnSpeed * Time.deltaTime);

        Vector3 moveDirection = transform.forward;
        moveDirection.y = 0f;
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            MoveRideRoot(moveDirection.normalized * (moveInput * manualMoveSpeed * Time.deltaTime));
            AlignRideToGround();
        }

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
        SetRideVignetteActive(true);

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

        SetRideVignetteActive(true);

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

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Vector3 horizontalStep = direction.normalized * Mathf.Min(autoSpeed * Time.deltaTime, direction.magnitude);
            MoveRideRoot(horizontalStep);

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );

            AlignRideToGround();
        }

        if (HorizontalDistance(transform.position, target.position) <= reachDistance)
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
        SetRideVignetteActive(false);
        currentState = RideState.MountedManual;
        UpdateKittyAnimation(0f, false);


        if (debugLogs)
        {
            Debug.Log("[CatRideControllerV2] Auto ride finished. Manual control returned.");
        }
    }

    private void MoveRideRoot(Vector3 horizontalDelta)
    {
        Vector3 desiredPosition = transform.position + horizontalDelta;
        transform.position = ResolveRideGroundedPosition(desiredPosition);
    }

    private Vector3 ResolveRideGroundedPosition(Vector3 desiredPosition)
    {
        if (!projectRideMotionToGround)
        {
            hasRideGroundNormal = false;
            return desiredPosition;
        }

        Vector3 origin = desiredPosition + Vector3.up * Mathf.Max(0.1f, rideGroundProbeHeight);
        float distance = Mathf.Max(0.1f, rideGroundProbeDistance);
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            rideGroundHitBuffer,
            distance,
            rideGroundMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.PositiveInfinity;
        bool foundGround = false;
        Vector3 groundPoint = desiredPosition;
        Vector3 groundNormal = Vector3.up;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = rideGroundHitBuffer[i];
            Collider hitCollider = hit.collider;
            if (hitCollider == null || IsIgnoredRideGroundCollider(hitCollider))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                groundPoint = hit.point;
                groundNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
                foundGround = true;
            }
        }

        if (!foundGround)
        {
            hasRideGroundNormal = false;
            return desiredPosition;
        }

        float deltaY = groundPoint.y - transform.position.y;
        if (deltaY > Mathf.Max(0f, rideMaxStepUp) || deltaY < -Mathf.Max(0f, rideMaxStepDown))
        {
            hasRideGroundNormal = false;
            return desiredPosition;
        }

        desiredPosition.y = groundPoint.y + rideGroundOffset;
        lastRideGroundNormal = ClampGroundNormalTilt(groundNormal);
        hasRideGroundNormal = true;
        return desiredPosition;
    }

    private Vector3 ClampGroundNormalTilt(Vector3 normal)
    {
        if (normal.sqrMagnitude < 0.0001f)
        {
            return Vector3.up;
        }

        normal.Normalize();
        float angle = Vector3.Angle(Vector3.up, normal);
        float maxAngle = Mathf.Max(0f, rideMaxGroundTiltAngle);
        if (angle <= maxAngle || angle <= 0.001f)
        {
            return normal;
        }

        return Vector3.Slerp(Vector3.up, normal, maxAngle / angle).normalized;
    }

    private void AlignRideToGround()
    {
        if (!alignRideToGroundNormal || !hasRideGroundNormal)
        {
            return;
        }

        Transform tiltRoot = rideVisualTiltRoot != null ? rideVisualTiltRoot : transform;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, lastRideGroundNormal);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(tiltRoot.forward, lastRideGroundNormal);
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, lastRideGroundNormal);

        tiltRoot.rotation = Quaternion.RotateTowards(
            tiltRoot.rotation,
            targetRotation,
            Mathf.Max(0f, rideGroundAlignSpeed) * Time.deltaTime);
    }

    private void SetMountedRigNeutralPose(Transform rig)
    {
        if (rig == null)
        {
            return;
        }

        mountedRigNeutralLocalPosition = rig.localPosition;
        mountedRigNeutralLocalRotation = rig.localRotation;
        mountedRiderSlopeOffset = Quaternion.identity;
        hasMountedRigNeutralLocalRotation = true;
    }

    private void ApplyMountedRiderSlopePose(bool immediate)
    {
        if (!tiltMountedRiderWithSlope || playerRigRoot == null)
        {
            return;
        }

        Transform rig = playerRigRoot.transform;
        if (!hasMountedRigNeutralLocalRotation)
        {
            SetMountedRigNeutralPose(rig);
        }

        Quaternion targetOffset = Quaternion.identity;
        if (hasRideGroundNormal)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Vector3 slopeForward = Vector3.ProjectOnPlane(forward, lastRideGroundNormal);
            if (slopeForward.sqrMagnitude > 0.0001f)
            {
                slopeForward.Normalize();
                float slopePitch = Mathf.Asin(Mathf.Clamp(slopeForward.y, -1f, 1f)) * Mathf.Rad2Deg;
                float riderPitch = Mathf.Clamp(
                    slopePitch * Mathf.Clamp01(riderSlopePitchMultiplier),
                    -Mathf.Abs(riderMaxSlopePitch),
                    Mathf.Abs(riderMaxSlopePitch));

                Vector3 right = transform.right;
                right.y = 0f;
                if (right.sqrMagnitude < 0.0001f)
                {
                    right = Vector3.right;
                }

                right.Normalize();
                Vector3 slopeRight = Vector3.ProjectOnPlane(right, lastRideGroundNormal);
                float riderRoll = 0f;
                if (slopeRight.sqrMagnitude > 0.0001f)
                {
                    slopeRight.Normalize();
                    float sideSlope = Mathf.Asin(Mathf.Clamp(slopeRight.y, -1f, 1f)) * Mathf.Rad2Deg;
                    riderRoll = Mathf.Clamp(
                        -sideSlope * Mathf.Clamp01(riderSlopeRollMultiplier),
                        -Mathf.Abs(riderMaxSlopeRoll),
                        Mathf.Abs(riderMaxSlopeRoll));
                }

                targetOffset = Quaternion.Euler(riderPitch, 0f, riderRoll);
            }
        }

        if (immediate)
        {
            mountedRiderSlopeOffset = targetOffset;
        }
        else
        {
            float t = 1f - Mathf.Exp(-Mathf.Max(0f, riderSlopeTiltSpeed) * Time.deltaTime);
            mountedRiderSlopeOffset = Quaternion.Slerp(mountedRiderSlopeOffset, targetOffset, t);
        }

        ApplyMountedRigSlopePose(rig, mountedRigNeutralLocalRotation * mountedRiderSlopeOffset);
    }

    private void ApplyMountedRigSlopePose(Transform rig, Quaternion localRotation)
    {
        if (rig == null)
        {
            return;
        }

        Vector3 targetLocalPosition = mountedRigNeutralLocalPosition;
        if (TryResolveMountedViewSlopeDelta(out Vector3 slopeDeltaWorld))
        {
            targetLocalPosition += rig.parent != null
                ? rig.parent.InverseTransformVector(slopeDeltaWorld)
                : slopeDeltaWorld;
        }

        rig.localPosition = targetLocalPosition;
        rig.localRotation = localRotation;
    }

    private bool TryResolveMountedViewSlopeDelta(out Vector3 deltaWorld)
    {
        deltaWorld = Vector3.zero;

        Transform anchor = mountedViewAnchor != null ? mountedViewAnchor : seatAnchor;
        if (anchor == null || !hasRideGroundNormal || !TryResolveRideGroundRotation(out Quaternion slopeWorldRotation))
        {
            return false;
        }

        Quaternion localSlopeRotation = Quaternion.Inverse(transform.rotation) * slopeWorldRotation;
        Vector3 anchorLocalPosition = transform.InverseTransformPoint(anchor.position);
        Vector3 slopeAnchorWorldPosition = transform.TransformPoint(localSlopeRotation * anchorLocalPosition);
        deltaWorld = slopeAnchorWorldPosition - anchor.position;
        return deltaWorld.sqrMagnitude > 0.000001f;
    }

    private bool TryResolveRideGroundRotation(out Quaternion slopeWorldRotation)
    {
        slopeWorldRotation = transform.rotation;
        if (!hasRideGroundNormal)
        {
            return false;
        }

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, lastRideGroundNormal);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        slopeWorldRotation = Quaternion.LookRotation(forward.normalized, lastRideGroundNormal);
        return true;
    }

    private void AutoAssignRideVisualTiltRoot()
    {
        if (rideVisualTiltRoot != null)
        {
            return;
        }

        if (kittyAnimator != null && kittyAnimator.transform != transform)
        {
            rideVisualTiltRoot = kittyAnimator.transform;
            return;
        }

        Renderer renderer = GetComponentInChildren<Renderer>(true);
        if (renderer != null && renderer.transform != transform)
        {
            rideVisualTiltRoot = renderer.transform;
        }
    }

    private bool IsIgnoredRideGroundCollider(Collider candidate)
    {
        if (candidate == null || candidate.isTrigger)
        {
            return true;
        }

        if (candidate.transform.IsChildOf(transform))
        {
            return true;
        }

        return playerRigRoot != null && candidate.transform.IsChildOf(playerRigRoot.transform);
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private sealed class RideVignetteProvider : ITunnelingVignetteProvider
    {
        public RideVignetteProvider(VignetteParameters parameters)
        {
            vignetteParameters = parameters;
        }

        public VignetteParameters vignetteParameters { get; private set; }

        public void SetParameters(VignetteParameters parameters)
        {
            vignetteParameters = parameters;
        }
    }

    private void SetLocomotionRootBehavioursLocked(bool locked)
    {
        if (locomotionRoot == null || !locomotionRootWasActive)
        {
            return;
        }

        if (locked)
        {
            // Only disable LocomotionProvider components themselves. Touching every
            // Behaviour under locomotionRoot also disabled LocomotionMediator,
            // XRBodyTransformer and any UI / ray-interactor components a project
            // might park under the locomotion subtree, which broke menu interaction
            // and tunneling-vignette updates while mounted.
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
