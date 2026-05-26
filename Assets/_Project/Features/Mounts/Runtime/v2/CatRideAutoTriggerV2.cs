using UnityEngine;

public class CatRideAutoTriggerV2 : MonoBehaviour
{
    [SerializeField] private CatRideControllerV2 controller;
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private bool logDebug = true;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        CatRideControllerV2 hitController = other.GetComponentInParent<CatRideControllerV2>();
        if (hitController == null)
        {
            return;
        }

        CatRideControllerV2 targetController = controller != null ? controller : hitController;
        if (hitController != targetController)
        {
            return;
        }

        bool started = targetController.BeginAutoRide();
        if (!started)
        {
            return;
        }

        hasTriggered = true;

        if (logDebug)
        {
            Debug.Log("[CatRideAutoTriggerV2] Auto ride trigger activated.");
        }
    }
}
