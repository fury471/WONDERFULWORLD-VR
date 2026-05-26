using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
public sealed class QuestTeleportLandingClearanceFilter : MonoBehaviour, IXRSelectFilter
{
    private const int MaxOverlapCount = 32;

    [SerializeField] private CharacterController characterController;
    [SerializeField] private LayerMask blockingMask = ~0;
    [SerializeField, Min(0.05f)] private float minimumRadius = 0.24f;
    [SerializeField, Min(0.5f)] private float minimumHeight = 1.45f;
    [SerializeField, Min(0f)] private float groundSkin = 0.03f;
    [SerializeField] private Collider landingSurfaceCollider;
    [SerializeField] private bool logRejectedLandings;

    private readonly Collider[] overlapBuffer = new Collider[MaxOverlapCount];

    public bool canProcess => isActiveAndEnabled;

    public void Configure(
        CharacterController controller,
        LayerMask blockers,
        float radius,
        float height,
        float skin,
        Collider surfaceCollider,
        bool logRejected)
    {
        characterController = controller;
        blockingMask = blockers;
        minimumRadius = Mathf.Max(0.05f, radius);
        minimumHeight = Mathf.Max(0.5f, height);
        groundSkin = Mathf.Max(0f, skin);
        landingSurfaceCollider = surfaceCollider;
        logRejectedLandings = logRejected;
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        XRRayInteractor rayInteractor = interactor as XRRayInteractor;
        if (rayInteractor == null || !rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            return true;
        }

        if (hit.collider == null)
        {
            return false;
        }

        return HasLandingClearance(hit.point, hit.collider, interactable as Component);
    }

    private bool HasLandingClearance(Vector3 landingPoint, Collider hitCollider, Component interactableComponent)
    {
        float radius = minimumRadius;
        float height = minimumHeight;
        float skinWidth = groundSkin;

        if (characterController != null)
        {
            radius = Mathf.Max(radius, characterController.radius);
            height = Mathf.Max(height, characterController.height);
            skinWidth = Mathf.Max(skinWidth, characterController.skinWidth);
        }

        Vector3 bottomSphere = landingPoint + Vector3.up * (radius + skinWidth);
        Vector3 topSphere = landingPoint + Vector3.up * Mathf.Max(radius + skinWidth, height - radius + skinWidth);
        int overlapCount = Physics.OverlapCapsuleNonAlloc(
            bottomSphere,
            topSphere,
            radius,
            overlapBuffer,
            blockingMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider obstacle = overlapBuffer[i];
            overlapBuffer[i] = null;

            if (ShouldIgnoreObstacle(obstacle, hitCollider, interactableComponent))
            {
                continue;
            }

            if (logRejectedLandings)
            {
                Debug.Log(
                    $"[TeleportLanding] Rejected landing at {landingPoint} because {obstacle.name} blocks player clearance.",
                    this);
            }

            return false;
        }

        return true;
    }

    private bool ShouldIgnoreObstacle(Collider obstacle, Collider hitCollider, Component interactableComponent)
    {
        if (obstacle == null || !obstacle.enabled || obstacle.isTrigger)
        {
            return true;
        }

        if (obstacle == hitCollider || obstacle == landingSurfaceCollider || obstacle == characterController)
        {
            return true;
        }

        if (interactableComponent != null && obstacle.transform.IsChildOf(interactableComponent.transform))
        {
            return true;
        }

        if (characterController != null && obstacle.transform.IsChildOf(characterController.transform))
        {
            return true;
        }

        return false;
    }
}
