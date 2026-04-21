using UnityEngine;

public class ScaleShiftZoneTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScaleManager scaleManager;
    [SerializeField] private GameFlowManager gameFlowManager;

    [Header("Behavior")]
    [SerializeField] private ScaleState targetState = ScaleState.Small;
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private string attractionId = "FlowerField";
    [SerializeField] private bool logDebug = true;

    private bool hasTriggered;

    private void Awake()
    {
        AutoAssignReferences();
    }

    private void OnValidate()
    {
        AutoAssignReferences();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTrigger(other);
    }

    private void AutoAssignReferences()
    {
        if (scaleManager == null)
        {
            scaleManager = FindFirstObjectByType<ScaleManager>();
        }

        if (gameFlowManager == null)
        {
            gameFlowManager = FindFirstObjectByType<GameFlowManager>();
        }
    }

    private void TryTrigger(Collider other)
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        if (!IsPlayerCollider(other))
        {
            return;
        }

        scaleManager?.SetScale(targetState);
        gameFlowManager?.DiscoverAttraction(attractionId);
        gameFlowManager?.VisitAttraction(attractionId);

        hasTriggered = true;

        if (logDebug)
        {
            Debug.Log($"[ScaleShiftZoneTrigger] Triggered {targetState} for {attractionId} via {other.name}");
        }
    }

    private static bool IsPlayerCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.GetComponent<CharacterController>() != null)
        {
            return true;
        }

        if (other.GetComponentInParent<CharacterController>() != null)
        {
            return true;
        }

        Transform root = other.transform.root;
        return root != null && root.name.Contains("WonderlandXROrigin");
    }
}
