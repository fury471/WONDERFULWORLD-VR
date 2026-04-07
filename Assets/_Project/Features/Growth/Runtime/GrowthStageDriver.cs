using UnityEngine;
using UnityEngine.InputSystem;

public class GrowthStageDriver : MonoBehaviour
{
    [Header("Interaction Origin")]
    [SerializeField] private Transform interactionOrigin;

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

    private void Update()
    {
        if (interactionOrigin == null)
        {
            Debug.Log("GrowthStageDriver: interactionOrigin is NULL");
            return;
        }

        bool singlePressedThisFrame = false;
        bool clusterPressedThisFrame = false;
        bool regressPressedThisFrame = false;

        if (enableSimulatorTrigger)
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                singlePressedThisFrame = true;
            }

            if (Keyboard.current != null && Keyboard.current.gKey.isPressed)
            {
                clusterPressedThisFrame = true;
            }
        }

        if (enableKeyboardFallback && Keyboard.current != null)
        {
            if (Keyboard.current.eKey.isPressed)
            {
                singlePressedThisFrame = true;
            }

            if (Keyboard.current.rKey.isPressed)
            {
                clusterPressedThisFrame = true;
            }

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
                singleGrowthController.TriggerGrowth();
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
                clusterGrowthController.TriggerGrowth();
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
                singleGrowthController.TriggerGrowthReverse();
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
}
