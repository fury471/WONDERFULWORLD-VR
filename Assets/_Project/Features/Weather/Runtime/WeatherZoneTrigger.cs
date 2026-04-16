using UnityEngine;

public class WeatherZoneTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private GameFlowManager gameFlowManager;

    [Header("Behavior")]
    [SerializeField] private WeatherType targetWeather = WeatherType.Overcast;
    [SerializeField] private string attractionId = "FlowerField";
    [SerializeField] private bool triggerOnlyOnce = true;
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
        if (weatherManager == null)
        {
            weatherManager = FindFirstObjectByType<WeatherManager>();
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

        weatherManager?.SetWeather(targetWeather);
        gameFlowManager?.DiscoverAttraction(attractionId);
        gameFlowManager?.VisitAttraction(attractionId);
        hasTriggered = true;

        if (logDebug)
        {
            Debug.Log($"[WeatherZoneTrigger] Triggered {targetWeather} for {attractionId} via {other.name}");
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
