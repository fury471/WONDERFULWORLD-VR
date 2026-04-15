using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    public class FireworksZoneTrigger : MonoBehaviour
    {
        [SerializeField] private FireworkLaunchPad launchPad;
        [SerializeField] private GameFlowManager gameFlowManager;
        [SerializeField] private string attractionId = "FireworksClearing";
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

        private void TryTrigger(Collider other)
        {
            if (triggerOnlyOnce && hasTriggered)
            {
                return;
            }

            if (other == null)
            {
                return;
            }

            hasTriggered = true;
            launchPad?.TriggerLaunch();
            gameFlowManager?.DiscoverAttraction(attractionId);
            gameFlowManager?.VisitAttraction(attractionId);

            if (logDebug)
            {
                Debug.Log($"[Fireworks] Triggered by {other.name} on {name}");
            }
        }

        private void AutoAssignReferences()
        {
            if (launchPad == null)
            {
                launchPad = GetComponentInChildren<FireworkLaunchPad>(true);
                if (launchPad == null)
                {
                    launchPad = FindFirstObjectByType<FireworkLaunchPad>();
                }
            }

            if (gameFlowManager == null)
            {
                gameFlowManager = FindFirstObjectByType<GameFlowManager>();
            }
        }
    }
}
