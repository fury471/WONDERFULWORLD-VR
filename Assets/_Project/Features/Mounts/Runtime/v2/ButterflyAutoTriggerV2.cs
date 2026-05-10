using UnityEngine;

public class ButterflyAutoTriggerV2 : MonoBehaviour
{
    [SerializeField] private ButterflyFlightControllerV2 butterflyFlight;
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private bool logDebug = true;

    private bool hasTriggered;
    private bool isInside;

    private void OnTriggerEnter(Collider other)
    {
        if (isInside)
        {
            return;
        }

        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        CatRideControllerV2 catController = other.GetComponentInParent<CatRideControllerV2>();
        if (catController == null)
        {
            return;
        }

        if (butterflyFlight == null)
        {
            return;
        }

        butterflyFlight.BeginFlight();

        isInside = true;
        hasTriggered = true;

        if (logDebug)
        {
            Debug.Log("[ButterflyAutoTriggerV2] Butterfly flight triggered.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (triggerOnlyOnce)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        CatRideControllerV2 catController = other.GetComponentInParent<CatRideControllerV2>();
        if (catController == null)
        {
            return;
        }

        isInside = false;
        hasTriggered = false;

        if (logDebug)
        {
            Debug.Log("[ButterflyAutoTriggerV2] Trigger reset after exit.");
        }
    }
}
