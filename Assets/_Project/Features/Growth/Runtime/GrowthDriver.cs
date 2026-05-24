using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class GrowthDriver : MonoBehaviour
{
    private const float ReferenceRefreshSeconds = 0.5f;

    [Header("Interaction Origin")]
    [SerializeField] private Transform interactionOrigin;
    [Header("XR Controllers")]
    [Header("Input Actions")]
    [SerializeField] private InputActionProperty leftTrigger; 
    [SerializeField] private InputActionProperty rightTrigger; 

    [Header("Single Growth")]
    [SerializeField] private GrowthController singleGrowthController;
    [SerializeField] private Transform singleTarget;
    [SerializeField] private float singleRange = 2.0f;

    [Header("Cluster Growth")]
    [SerializeField] private GrowthController clusterGrowthController;
    [SerializeField] private Transform clusterTarget;
    [SerializeField] private float clusterRange = 4.0f;

    [Header("Input")]
    [SerializeField] private bool enableSimulatorTrigger = true;
    [SerializeField] private bool enableKeyboardFallback = true;
    [SerializeField] private bool allowShrinkInDebug = false;
    [SerializeField] private bool logDebugMessages;

    private bool singlePressedLastFrame;
    private bool clusterPressedLastFrame;
    private bool regressPressedLastFrame;
    private float nextReferenceRefreshTime;

    [Header("Interaction Events")]
    [Tooltip("Events to trigger when growth is successfully activated")]
    public UnityEvent OnInteractionSuccess; 

    private void Awake()
    {
        AutoAssignReferences();
    }

    private void Update()
    {
        if (ShouldRefreshReferences())
        {
            AutoAssignReferences();
        }

        if (interactionOrigin == null)
        {
            if (logDebugMessages)
            {
                Debug.Log("GrowthDriver: interactionOrigin is missing.");
            }
            return;
        }

        bool singlePressedThisFrame = false;
        bool clusterPressedThisFrame = false;
        bool regressPressedThisFrame = false;

        // --- 1. XR Interaction (Trigger Buttons) ---
        if (rightTrigger.action != null && rightTrigger.action.WasPressedThisFrame())
        {
            singlePressedThisFrame = true;
        }

        if (leftTrigger.action != null && leftTrigger.action.WasPressedThisFrame())
        {
            clusterPressedThisFrame = true;
        }

        // --- 2. Simulator/Mouse Debug (Mouse Buttons) ---
        if (enableSimulatorTrigger && Mouse.current != null)
        {
            // Mouse Right Click -> Single Growth
            if (Mouse.current.rightButton.isPressed)
            {
                singlePressedThisFrame = true;
            }
            // Mouse Left Click -> Cluster Growth
            if (Mouse.current.leftButton.isPressed)
            {
                clusterPressedThisFrame = true;
            }
        }

        // --- 3. Keyboard Debug (R, T, Q keys) ---
        if (enableKeyboardFallback && Keyboard.current != null)
        {
            // T Key -> Single Growth
            if (Keyboard.current.tKey.isPressed)
            {
                singlePressedThisFrame = true;
            }
            // R Key -> Cluster Growth
            if (Keyboard.current.rKey.isPressed)
            {
                clusterPressedThisFrame = true;
            }
            // Q Key -> Shrink (Regress)
            if (allowShrinkInDebug && Keyboard.current.qKey.isPressed)
            {
                regressPressedThisFrame = true;
            }
        }

        if (singlePressedThisFrame && !singlePressedLastFrame)
        {
            float distance = singleTarget != null
                ? Vector3.Distance(interactionOrigin.position, singleTarget.position)
                : -1f;

            if (logDebugMessages)
            {
                Debug.Log($"GrowthDriver single input. Origin={interactionOrigin.name}, Target={(singleTarget != null ? singleTarget.name : "NULL")}, Distance={distance}, Range={singleRange}");
            }

            if (singleGrowthController != null && singleTarget != null &&
                distance <= singleRange)
            {
                singleGrowthController.TriggerSingleGrowth();
                OnInteractionSuccess?.Invoke();
            }
            else if (logDebugMessages)
            {
                Debug.Log("GrowthDriver single growth not triggered.");
            }
        }

        if (clusterPressedThisFrame && !clusterPressedLastFrame)
        {
            float distance = clusterTarget != null
                ? Vector3.Distance(interactionOrigin.position, clusterTarget.position)
                : -1f;

            if (logDebugMessages)
            {
                Debug.Log($"GrowthDriver cluster input. Origin={interactionOrigin.name}, Target={(clusterTarget != null ? clusterTarget.name : "NULL")}, Distance={distance}, Range={clusterRange}");
            }

            if (clusterGrowthController != null && clusterTarget != null &&
                distance <= clusterRange)
            {
                clusterGrowthController.TriggerClusterGrowth();
                OnInteractionSuccess?.Invoke();
            }
            else if (logDebugMessages)
            {
                Debug.Log("GrowthDriver cluster growth not triggered.");
            }
        }

        if (allowShrinkInDebug && regressPressedThisFrame && !regressPressedLastFrame)
        {
            float distance = singleTarget != null
                ? Vector3.Distance(interactionOrigin.position, singleTarget.position)
                : -1f;

            if (logDebugMessages)
            {
                Debug.Log($"GrowthDriver reverse input. Distance={distance}, Range={singleRange}");
            }

            if (singleGrowthController != null && singleTarget != null &&
                distance <= singleRange)
            {
                singleGrowthController.TriggerSingleGrowthReverse();
            }
            else if (logDebugMessages)
            {
                Debug.Log("GrowthDriver reverse growth not triggered.");
            }
        }

        singlePressedLastFrame = singlePressedThisFrame;
        clusterPressedLastFrame = clusterPressedThisFrame;
        regressPressedLastFrame = regressPressedThisFrame;
    }

    private void AutoAssignReferences()
    {
        if (interactionOrigin == null)
        {
            interactionOrigin = QuestInteractionUtils.FindHeadTransform();
        }

        if (singleGrowthController == null)
        {
            singleGrowthController = GetComponent<GrowthController>();
            if (singleGrowthController == null)
            {
                singleGrowthController = FindFirstObjectByType<GrowthController>();
            }
        }

        if (clusterGrowthController == null)
        {
            clusterGrowthController = singleGrowthController;
        }

        if (singleTarget == null)
        {
            if (singleGrowthController != null && singleGrowthController.TargetPlant != null)
            {
                singleTarget = singleGrowthController.TargetPlant.transform;
            }
            else
            {
                GrowthPlant plant = FindFirstObjectByType<GrowthPlant>();
                if (plant != null)
                {
                    singleTarget = plant.transform;
                }
            }
        }

        if (clusterTarget == null)
        {
            if (clusterGrowthController != null && clusterGrowthController.TargetCluster != null)
            {
                clusterTarget = clusterGrowthController.TargetCluster.transform;
            }
            else
            {
                GrowthCluster cluster = FindFirstObjectByType<GrowthCluster>();
                if (cluster != null)
                {
                    clusterTarget = cluster.transform;
                }
            }
        }
    }

    private bool ShouldRefreshReferences()
    {
        bool needsRefresh =
            interactionOrigin == null ||
            singleGrowthController == null ||
            clusterGrowthController == null ||
            singleTarget == null ||
            clusterTarget == null;

        if (!needsRefresh)
        {
            return false;
        }

        if (!Application.isPlaying)
        {
            return true;
        }

        if (Time.unscaledTime < nextReferenceRefreshTime)
        {
            return false;
        }

        nextReferenceRefreshTime = Time.unscaledTime + ReferenceRefreshSeconds;
        return true;
    }
}
