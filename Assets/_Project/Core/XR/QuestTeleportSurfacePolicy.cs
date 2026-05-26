using UnityEngine;

[DisallowMultipleComponent]
public sealed class QuestTeleportSurfacePolicy : MonoBehaviour
{
    public enum TeleportPermission
    {
        Inherit,
        Allow,
        Block
    }

    [Header("Surface")]
    [SerializeField] private TeleportPermission permission = TeleportPermission.Inherit;

    [Header("Slope")]
    [SerializeField] private bool overrideNormalTolerance;
    [SerializeField, Range(5f, 75f)] private float normalToleranceDegrees = 52f;

    [Header("Landing clearance")]
    [SerializeField] private bool overrideLandingClearance;
    [SerializeField] private bool requireLandingClearance = true;

    public TeleportPermission Permission => permission;
    public bool OverrideNormalTolerance => overrideNormalTolerance;
    public float NormalToleranceDegrees => normalToleranceDegrees;
    public bool OverrideLandingClearance => overrideLandingClearance;
    public bool RequireLandingClearance => requireLandingClearance;

    public static QuestTeleportSurfacePolicy FindFor(Collider surfaceCollider)
    {
        if (surfaceCollider == null)
        {
            return null;
        }

        QuestTeleportSurfacePolicy policy = surfaceCollider.GetComponent<QuestTeleportSurfacePolicy>();
        if (policy != null)
        {
            return policy;
        }

        return surfaceCollider.GetComponentInParent<QuestTeleportSurfacePolicy>();
    }
}
