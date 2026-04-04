using UnityEngine;

public class ParticlePreviewAnchor : MonoBehaviour
{
    [Header("Left Hand Target")]
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Vector3 localOffset = new(0f, 0f, 0.2f);
    [SerializeField] private Vector3 shapeDisplayOffset = new(0f, 0f, 0.45f);
    [SerializeField] private bool alignWithTargetRotation = true;

    public Transform LeftHandTransform => leftHandTransform;
    public Vector3 LocalOffset => localOffset;
    public Vector3 ShapeDisplayOffset => shapeDisplayOffset;

    public void SetLeftHandTransform(Transform target)
    {
        leftHandTransform = target;
    }

    public Vector3 GetHoldWorldPosition(Vector3 localPosition)
    {
        if (leftHandTransform == null)
        {
            return transform.TransformPoint(localPosition);
        }

        return leftHandTransform.TransformPoint(localPosition);
    }

    public Vector3 GetShapeWorldPosition(Vector3 localPosition)
    {
        if (leftHandTransform == null)
        {
            return transform.TransformPoint(localPosition);
        }

        return leftHandTransform.TransformPoint(shapeDisplayOffset + localPosition);
    }

    private void LateUpdate()
    {
        if (leftHandTransform == null)
        {
            return;
        }

        transform.position = leftHandTransform.TransformPoint(localOffset);

        if (alignWithTargetRotation)
        {
            transform.rotation = leftHandTransform.rotation;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.08f);

        if (leftHandTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(leftHandTransform.TransformPoint(shapeDisplayOffset), 0.08f);
        }
    }
}
