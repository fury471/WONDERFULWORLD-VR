using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class GrowthDriver : MonoBehaviour
{
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

    private bool singlePressedLastFrame;
    private bool clusterPressedLastFrame;
    private bool regressPressedLastFrame;

    [Header("Interaction Events")]
    [Tooltip("Events to trigger when growth is successfully activated")]
    public UnityEvent OnInteractionSuccess; 

    private void Awake()
    {
        AutoAssignReferences();
    }

    private void Update()
    {
        AutoAssignReferences();

        if (interactionOrigin == null)
        {
            Debug.Log("GrowthStageDriver: interactionOrigin is NULL");
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

            Debug.Log($"Single input pressed. Origin={interactionOrigin.name}, Target={(singleTarget != null ? singleTarget.name : "NULL")}, Distance={distance}, Range={singleRange}");

            if (singleGrowthController != null && singleTarget != null &&
                distance <= singleRange)
            {
                Debug.Log("Single growth TRIGGERED");
                singleGrowthController.TriggerSingleGrowth();
                OnInteractionSuccess?.Invoke();
            }
            else
            {
                Debug.Log("Single growth NOT triggered");
            }
        }

        if (clusterPressedThisFrame && !clusterPressedLastFrame)
        {
            float distance = clusterTarget != null
                ? Vector3.Distance(interactionOrigin.position, clusterTarget.position)
                : -1f;

            Debug.Log($"Cluster input pressed. Origin={interactionOrigin.name}, Target={(clusterTarget != null ? clusterTarget.name : "NULL")}, Distance={distance}, Range={clusterRange}");

            if (clusterGrowthController != null && clusterTarget != null &&
                distance <= clusterRange)
            {
                Debug.Log("Cluster growth TRIGGERED");
                clusterGrowthController.TriggerClusterGrowth();
                OnInteractionSuccess?.Invoke();
            }
            else
            {
                Debug.Log("Cluster growth NOT triggered");
            }
        }

        if (allowShrinkInDebug && regressPressedThisFrame && !regressPressedLastFrame)
        {
            float distance = singleTarget != null
                ? Vector3.Distance(interactionOrigin.position, singleTarget.position)
                : -1f;

            Debug.Log($"Reverse input pressed. Distance={distance}, Range={singleRange}");

            if (singleGrowthController != null && singleTarget != null &&
                distance <= singleRange)
            {
                Debug.Log("Reverse growth TRIGGERED");
                singleGrowthController.TriggerSingleGrowthReverse();
            }
            else
            {
                Debug.Log("Reverse growth NOT triggered");
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
            if (Camera.main != null)
            {
                interactionOrigin = Camera.main.transform;
            }
            else
            {
                interactionOrigin = FindInScene("Main Camera");
            }
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

    private static Transform FindInScene(string targetName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, targetName);
            if (found != null)
            {
                return found;
            }
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
