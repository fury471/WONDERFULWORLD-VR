using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class QuestLocomotionComfortProfile : MonoBehaviour
{
    public enum MovementMode
    {
        Teleport,
        Smooth
    }

    public enum TurnMode
    {
        Snap,
        Smooth
    }

    private const int DefaultTeleportSurfaceMask = (1 << 0) | (1 << 3);
    private const int DefaultTeleportRaycastMask = unchecked((int)0x80000009);

    [Header("User Locomotion Preferences")]
    [SerializeField] private MovementMode movementMode = MovementMode.Teleport;
    [SerializeField] private TurnMode turnMode = TurnMode.Snap;
    [SerializeField, Min(0.1f)] private float smoothMoveSpeed = 1.6f;
    [SerializeField, Min(1f)] private float smoothTurnSpeed = 45f;

    [Header("Controller ownership")]
    [SerializeField] private ControllerInputActionManager leftController = null;
    [SerializeField] private ControllerInputActionManager rightController = null;

    [Header("Locomotion providers")]
    [SerializeField] private TeleportationProvider teleportationProvider = null;
    [SerializeField] private ContinuousMoveProvider continuousMove = null;
    [SerializeField] private ContinuousTurnProvider continuousTurn = null;
    [SerializeField] private SnapTurnProvider snapTurn = null;

    [Header("Teleport rays")]
    [SerializeField] private XRRayInteractor leftTeleportInteractor = null;
    [SerializeField] private XRRayInteractor rightTeleportInteractor = null;

    [Header("Input ownership")]
    [SerializeField] private InputActionReference leftTeleportModeAction = null;
    [SerializeField] private InputActionReference leftTeleportCancelAction = null;
    [SerializeField] private InputActionReference leftMoveAction = null;
    [SerializeField] private InputActionReference leftContinuousTurnAction = null;
    [SerializeField] private InputActionReference leftSnapTurnAction = null;
    [SerializeField] private InputActionReference rightTeleportModeAction = null;
    [SerializeField] private InputActionReference rightTeleportCancelAction = null;
    [SerializeField] private InputActionReference rightContinuousMoveAction = null;
    [SerializeField] private InputActionReference rightContinuousTurnAction = null;

    [Header("Teleport surface installation")]
    [SerializeField] private LayerMask teleportSurfaceMask = DefaultTeleportSurfaceMask;
    [SerializeField] private LayerMask teleportRaycastMask = DefaultTeleportRaycastMask;
    [SerializeField] private InteractionLayerMask teleportInteractionLayers = -1;
    [SerializeField] private bool autoInstallTeleportAreasAtRuntime = true;
    [SerializeField] private bool ignoreTriggerColliders = true;
    [SerializeField] private bool ignoreDynamicRigidbodies = true;
    [SerializeField, Min(0f)] private float minimumSurfaceFootprint = 0.45f;
    [SerializeField, Range(5f, 60f)] private float maxTeleportSlopeDegrees = 38f;

    [Header("Comfort timings")]
    [SerializeField, Min(0f)] private float teleportDelayTime = 0.08f;
    [SerializeField, Range(15f, 60f)] private float snapTurnAmount = 30f;
    [SerializeField, Min(0.1f)] private float snapTurnDebounceTime = 0.35f;
    [SerializeField, Min(0f)] private float snapTurnDelayTime = 0.05f;

    [Header("Tunneling vignette")]
    [SerializeField] private bool configureSceneVignettes = true;
    [SerializeField, Range(0.2f, 1f)] private float teleportAperture = 0.52f;
    [SerializeField, Range(0.2f, 1f)] private float turnAperture = 0.58f;
    [SerializeField, Range(0.2f, 1f)] private float smoothMoveAperture = 0.58f;
    [SerializeField, Range(0.2f, 1f)] private float smoothTurnAperture = 0.62f;
    [SerializeField] private bool comfortVignetteEnabled = true;
    [SerializeField, Range(0f, 1f)] private float feathering = 0.30f;
    [SerializeField, Min(0f)] private float easeInTime = 0.10f;
    [SerializeField, Min(0f)] private float easeOutTime = 0.20f;
    [SerializeField, Min(0f)] private float easeOutDelayTime = 0.06f;

    private LocomotionVignetteProvider teleportVignetteProvider;
    private LocomotionVignetteProvider snapTurnVignetteProvider;
    private LocomotionVignetteProvider continuousMoveVignetteProvider;
    private LocomotionVignetteProvider continuousTurnVignetteProvider;

    private int lastInstalledTeleportAreaCount;
    private float suppressRightHandTurnUntil;

    public int lastTeleportSurfaceInstallCount => lastInstalledTeleportAreaCount;

    public MovementMode CurrentMovementMode => movementMode;
    public TurnMode CurrentTurnMode => turnMode;
    public float SmoothMoveSpeed => smoothMoveSpeed;
    public float SmoothTurnSpeed => smoothTurnSpeed;
    public bool ComfortVignetteEnabled => comfortVignetteEnabled;

    public void SetMovementMode(MovementMode mode)
    {
        movementMode = mode;
        ApplyProfile();
    }

    public void SetTurnMode(TurnMode mode)
    {
        turnMode = mode;
        ApplyProfile();
    }

    public void SetSmoothMoveSpeed(float speed)
    {
        smoothMoveSpeed = Mathf.Max(0.1f, speed);
        ApplyProfile();
    }

    public void SetSmoothTurnSpeed(float degreesPerSecond)
    {
        smoothTurnSpeed = Mathf.Max(1f, degreesPerSecond);
        ApplyProfile();
    }

    public void SetComfortVignetteEnabled(bool enabled)
    {
        comfortVignetteEnabled = enabled;
        ApplyProfile();
    }

    public void SetVignetteComfort(float comfort01)
    {
        float aperture = Mathf.Lerp(0.85f, 0.45f, Mathf.Clamp01(comfort01));
        teleportAperture = aperture;
        turnAperture = aperture;
        smoothMoveAperture = aperture;
        smoothTurnAperture = aperture;
        ApplyProfile();
    }

    public void SuppressRightHandTurn(float seconds)
    {
        suppressRightHandTurnUntil = Mathf.Max(suppressRightHandTurnUntil, Time.time + Mathf.Max(0f, seconds));
        EnforceInputOwnership();
    }

    private void Reset()
    {
        AutoWireReferences();
    }

    private void OnValidate()
    {
        AutoWireReferences();
    }

    private void Awake()
    {
        ApplyProfile();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyProfile();
    }

    private void Start()
    {
        RefreshTeleportSurfaces();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void LateUpdate()
    {
        EnforceInputOwnership();
    }

    [ContextMenu("Apply Quest Locomotion Comfort Profile")]
    public void ApplyProfile()
    {
        AutoWireReferences();
        ValidateInputReferences();
        ConfigureControllerOwnership();
        ConfigureLocomotionProviders();
        ConfigureTeleportInteractors();

        if (configureSceneVignettes)
        {
            ConfigureTunnelingVignettes();
        }

        EnforceInputOwnership();
    }

    [ContextMenu("Refresh Teleport Surfaces")]
    public void RefreshTeleportSurfaces()
    {
        if (!autoInstallTeleportAreasAtRuntime)
        {
            return;
        }

        AutoWireReferences();

        if (teleportationProvider == null)
        {
            Debug.LogError("[Locomotion] Cannot install teleport surfaces without a TeleportationProvider.", this);
            return;
        }

        var installed = 0;
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var colliders = FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        var colliders = FindObjectsOfType<Collider>(false);
#pragma warning restore CS0618
#endif
        for (int i = 0; i < colliders.Length; i++)
        {
            if (TryInstallTeleportArea(colliders[i]))
            {
                installed++;
            }
        }

        lastInstalledTeleportAreaCount = installed;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshTeleportSurfaces();
    }

    private void AutoWireReferences()
    {
        var managers = GetComponentsInChildren<ControllerInputActionManager>(true);
        for (int i = 0; i < managers.Length; i++)
        {
            var manager = managers[i];
            if (manager == null)
            {
                continue;
            }

            var path = GetHierarchyPath(manager.transform);
            if (leftController == null && ContainsToken(path, "left"))
            {
                leftController = manager;
            }
            else if (rightController == null && ContainsToken(path, "right"))
            {
                rightController = manager;
            }
        }

        if (teleportationProvider == null)
        {
            teleportationProvider = GetComponentInChildren<TeleportationProvider>(true);
        }

        if (continuousMove == null)
        {
            continuousMove = GetComponentInChildren<ContinuousMoveProvider>(true);
        }

        if (continuousTurn == null)
        {
            continuousTurn = GetComponentInChildren<ContinuousTurnProvider>(true);
        }

        if (snapTurn == null)
        {
            snapTurn = GetComponentInChildren<SnapTurnProvider>(true);
        }

        if (leftTeleportInteractor == null || rightTeleportInteractor == null)
        {
            var rays = GetComponentsInChildren<XRRayInteractor>(true);
            for (int i = 0; i < rays.Length; i++)
            {
                var ray = rays[i];
                if (ray == null)
                {
                    continue;
                }

                var path = GetHierarchyPath(ray.transform);
                if (!ContainsToken(path, "teleport"))
                {
                    continue;
                }

                if (leftTeleportInteractor == null && ContainsToken(path, "left"))
                {
                    leftTeleportInteractor = ray;
                }
                else if (rightTeleportInteractor == null && ContainsToken(path, "right"))
                {
                    rightTeleportInteractor = ray;
                }
            }
        }
    }

    private void ConfigureControllerOwnership()
    {
        if (leftController != null)
        {
            leftController.smoothMotionEnabled = movementMode == MovementMode.Smooth;
            leftController.smoothTurnEnabled = false;
        }
        else
        {
            Debug.LogError("[Locomotion] Missing left ControllerInputActionManager.", this);
        }

        if (rightController != null)
        {
            rightController.smoothMotionEnabled = false;
            rightController.smoothTurnEnabled = turnMode == TurnMode.Smooth;
        }
        else
        {
            Debug.LogError("[Locomotion] Missing right ControllerInputActionManager.", this);
        }
    }

    private void ValidateInputReferences()
    {
        ValidateActionReference(leftTeleportModeAction, "left teleport mode");
        ValidateActionReference(leftTeleportCancelAction, "left teleport cancel");
        ValidateActionReference(leftMoveAction, "left continuous move guardrail");
        ValidateActionReference(leftContinuousTurnAction, "left continuous turn guardrail");
        ValidateActionReference(leftSnapTurnAction, "left snap turn guardrail");
        ValidateActionReference(rightTeleportModeAction, "right teleport mode guardrail");
        ValidateActionReference(rightTeleportCancelAction, "right teleport cancel guardrail");
        ValidateActionReference(rightContinuousMoveAction, "right continuous move guardrail");
        ValidateActionReference(rightContinuousTurnAction, "right continuous turn guardrail");
    }

    private void ConfigureLocomotionProviders()
    {
        if (teleportationProvider != null)
        {
            teleportationProvider.enabled = movementMode == MovementMode.Teleport;
            teleportationProvider.delayTime = teleportDelayTime;
        }
        else
        {
            Debug.LogError("[Locomotion] Missing TeleportationProvider.", this);
        }

        if (continuousMove != null)
        {
            continuousMove.enabled = movementMode == MovementMode.Smooth;
            continuousMove.moveSpeed = smoothMoveSpeed;
            continuousMove.enableStrafe = false;
            continuousMove.enableFly = false;
        }
        else
        {
            Debug.LogError("[Locomotion] Missing ContinuousMoveProvider.", this);
        }

        if (continuousTurn != null)
        {
            continuousTurn.enabled = turnMode == TurnMode.Smooth;
            continuousTurn.turnSpeed = smoothTurnSpeed;
            continuousTurn.enableTurnLeftRight = true;
            continuousTurn.enableTurnAround = false;
        }
        else
        {
            Debug.LogError("[Locomotion] Missing ContinuousTurnProvider.", this);
        }

        if (snapTurn != null)
        {
            snapTurn.enabled = turnMode == TurnMode.Snap;
            snapTurn.turnAmount = snapTurnAmount;
            snapTurn.debounceTime = snapTurnDebounceTime;
            snapTurn.delayTime = snapTurnDelayTime;
            snapTurn.enableTurnLeftRight = true;
            snapTurn.enableTurnAround = false;
        }

        EnforceInputOwnership();
    }

    private void ConfigureTeleportInteractors()
    {
        ConfigureTeleportInteractor(leftTeleportInteractor);
        ConfigureTeleportInteractor(rightTeleportInteractor);
    }

    private void ConfigureTeleportInteractor(XRRayInteractor teleportInteractor)
    {
        if (teleportInteractor == null)
        {
            return;
        }

        teleportInteractor.raycastMask = teleportRaycastMask;
        teleportInteractor.raycastTriggerInteraction = QueryTriggerInteraction.Ignore;
        teleportInteractor.hitDetectionType = XRRayInteractor.HitDetectionType.Raycast;
    }

    private void EnforceInputOwnership()
    {
        SetActionEnabled(leftTeleportModeAction, movementMode == MovementMode.Teleport);
        SetActionEnabled(leftTeleportCancelAction, movementMode == MovementMode.Teleport);
        SetActionEnabled(leftMoveAction, movementMode == MovementMode.Smooth);
        SetActionEnabled(leftContinuousTurnAction, false);
        SetActionEnabled(leftSnapTurnAction, false);
        SetActionEnabled(rightTeleportModeAction, false);
        SetActionEnabled(rightTeleportCancelAction, false);
        SetActionEnabled(rightContinuousMoveAction, false);
        bool rightTurnSuppressed = Time.time < suppressRightHandTurnUntil;
        SetActionEnabled(rightContinuousTurnAction, turnMode == TurnMode.Smooth && !rightTurnSuppressed);
    }

    private bool TryInstallTeleportArea(Collider surfaceCollider)
    {
        if (!IsTeleportSurfaceCandidate(surfaceCollider))
        {
            return false;
        }

        var existingTeleportInteractable = surfaceCollider.GetComponent<BaseTeleportationInteractable>();
        if (existingTeleportInteractable != null)
        {
            ConfigureTeleportInteractable(existingTeleportInteractable);
            return false;
        }

        if (HasOtherInteractableInHierarchy(surfaceCollider))
        {
            return false;
        }

        var area = surfaceCollider.gameObject.AddComponent<TeleportationArea>();
        ConfigureTeleportInteractable(area);
        return true;
    }

    private bool IsTeleportSurfaceCandidate(Collider surfaceCollider)
    {
        if (surfaceCollider == null || !surfaceCollider.enabled || !surfaceCollider.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (surfaceCollider.transform.IsChildOf(transform))
        {
            return false;
        }

        if (ignoreTriggerColliders && surfaceCollider.isTrigger)
        {
            return false;
        }

        var surfaceLayerMask = 1 << surfaceCollider.gameObject.layer;
        if ((teleportSurfaceMask.value & surfaceLayerMask) == 0)
        {
            return false;
        }

        if (ignoreDynamicRigidbodies)
        {
            var attachedBody = surfaceCollider.attachedRigidbody;
            if (attachedBody != null && !attachedBody.isKinematic)
            {
                return false;
            }
        }

        if (minimumSurfaceFootprint > 0f)
        {
            var size = surfaceCollider.bounds.size;
            if (size.x < minimumSurfaceFootprint && size.z < minimumSurfaceFootprint)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasOtherInteractableInHierarchy(Collider surfaceCollider)
    {
        if (surfaceCollider == null)
        {
            return false;
        }

        // GetComponentInParent walks self -> parents, so this also catches an interactable
        // sitting on the same GameObject as the collider, which would otherwise end up
        // sharing the collider with the TeleportationArea we are about to add.
        if (surfaceCollider.GetComponentInParent<XRBaseInteractable>() != null)
        {
            return true;
        }

        var childInteractables = surfaceCollider.GetComponentsInChildren<XRBaseInteractable>(true);
        for (int i = 0; i < childInteractables.Length; i++)
        {
            var childInteractable = childInteractables[i];
            if (childInteractable != null && childInteractable.gameObject != surfaceCollider.gameObject)
            {
                return true;
            }
        }

        return false;
    }

    private void ConfigureTeleportInteractable(BaseTeleportationInteractable teleportInteractable)
    {
        if (teleportInteractable == null)
        {
            return;
        }

        teleportInteractable.enabled = true;
        teleportInteractable.teleportationProvider = teleportationProvider;
        teleportInteractable.matchOrientation = MatchOrientation.WorldSpaceUp;
        teleportInteractable.matchDirectionalInput = false;
        teleportInteractable.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnSelectExited;
        teleportInteractable.filterSelectionByHitNormal = true;
        teleportInteractable.upNormalToleranceDegrees = maxTeleportSlopeDegrees;
        teleportInteractable.interactionLayers = teleportInteractionLayers;
    }

    private void ConfigureTunnelingVignettes()
    {
        // Reuse persistent provider instances. The XRI TunnelingVignetteController
        // keeps an internal record list keyed by provider reference, and that list is
        // not cleared when locomotionVignetteProviders is cleared. If we created new
        // instances each time, the controller would accumulate orphan records pinned
        // at the old apertureSize and the on-screen vignette would never go away.
        EnsureVignetteProvider(ref teleportVignetteProvider, teleportationProvider, teleportAperture, true);
        EnsureVignetteProvider(ref snapTurnVignetteProvider, snapTurn, turnAperture, true);
        EnsureVignetteProvider(ref continuousMoveVignetteProvider, continuousMove, smoothMoveAperture, false);
        EnsureVignetteProvider(ref continuousTurnVignetteProvider, continuousTurn, smoothTurnAperture, false);

#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var vignettes = FindObjectsByType<TunnelingVignetteController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        var vignettes = FindObjectsOfType<TunnelingVignetteController>(true);
#pragma warning restore CS0618
#endif
        for (int i = 0; i < vignettes.Length; i++)
        {
            var vignette = vignettes[i];
            if (vignette == null)
            {
                continue;
            }

            vignette.locomotionVignetteProviders.Clear();

            if (comfortVignetteEnabled)
            {
                AddIfNotNull(vignette, teleportVignetteProvider);
                AddIfNotNull(vignette, snapTurnVignetteProvider);
                AddIfNotNull(vignette, continuousMoveVignetteProvider);
                AddIfNotNull(vignette, continuousTurnVignetteProvider);
            }
            else
            {
                // Vignette turned off in settings. Removing providers from the list
                // alone is not enough — any record still in the controller stays in
                // EasingIn at its previous aperture. Explicitly end each provider so
                // the controller transitions the record to EasingOut and the visual
                // vignette actually fades back to no-effect.
                EndIfNotNull(vignette, teleportVignetteProvider);
                EndIfNotNull(vignette, snapTurnVignetteProvider);
                EndIfNotNull(vignette, continuousMoveVignetteProvider);
                EndIfNotNull(vignette, continuousTurnVignetteProvider);
            }
        }
    }

    private void EnsureVignetteProvider(
        ref LocomotionVignetteProvider vignetteProvider,
        LocomotionProvider locomotionProvider,
        float aperture,
        bool lockEaseIn)
    {
        if (locomotionProvider == null)
        {
            vignetteProvider = null;
            return;
        }

        if (vignetteProvider == null)
        {
            vignetteProvider = new LocomotionVignetteProvider
            {
                overrideParameters = new VignetteParameters(),
            };
        }

        vignetteProvider.locomotionProvider = locomotionProvider;
        vignetteProvider.enabled = true;
        vignetteProvider.overrideDefaultParameters = true;

        var parameters = vignetteProvider.overrideParameters;
        if (parameters == null)
        {
            parameters = new VignetteParameters();
            vignetteProvider.overrideParameters = parameters;
        }

        parameters.apertureSize = aperture;
        parameters.featheringEffect = feathering;
        parameters.easeInTime = easeInTime;
        parameters.easeOutTime = easeOutTime;
        parameters.easeInTimeLock = lockEaseIn;
        parameters.easeOutDelayTime = easeOutDelayTime;
        parameters.vignetteColor = Color.black;
        parameters.vignetteColorBlend = Color.black;
        parameters.apertureVerticalPosition = 0f;
    }

    private static void AddIfNotNull(TunnelingVignetteController vignette, LocomotionVignetteProvider provider)
    {
        if (provider != null)
        {
            vignette.locomotionVignetteProviders.Add(provider);
        }
    }

    private static void EndIfNotNull(TunnelingVignetteController vignette, LocomotionVignetteProvider provider)
    {
        if (provider != null)
        {
            vignette.EndTunnelingVignette(provider);
        }
    }

    private static bool ContainsToken(string value, string token)
    {
        return value != null && value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null)
        {
            return string.Empty;
        }

        var path = target.name;
        var parent = target.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private static void SetActionEnabled(InputActionReference actionReference, bool enabled)
    {
        var action = actionReference != null ? actionReference.action : null;
        if (action == null)
        {
            return;
        }

        if (enabled)
        {
            if (!action.enabled)
            {
                action.Enable();
            }
        }
        else if (action.enabled)
        {
            action.Disable();
        }
    }

    private void ValidateActionReference(InputActionReference actionReference, string actionName)
    {
        if (actionReference == null || actionReference.action == null)
        {
            Debug.LogError($"[Locomotion] Missing input action reference: {actionName}.", this);
        }
    }
}
