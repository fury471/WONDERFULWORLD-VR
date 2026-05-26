using UnityEngine;

public class CatIdlePaceV2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CatRideControllerV2 rideController;
    [SerializeField] private Animator kittyAnimator;
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Motion")]
    [SerializeField] private float paceSpeed = 1.2f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float arriveDistance = 0.1f;
    [SerializeField] private float waitAtPointSeconds = 1.0f;

    [Header("Terrain Motion")]
    [SerializeField] private bool projectMotionToGround = true;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundProbeHeight = 3f;
    [SerializeField] private float groundProbeDistance = 12f;
    [SerializeField] private float groundOffset = 0f;
    [SerializeField] private float maxStepUp = 1.5f;
    [SerializeField] private float maxStepDown = 5f;
    [SerializeField] private bool alignToGroundNormal = true;
    [SerializeField] private Transform visualTiltRoot;
    [SerializeField] private float groundAlignSpeed = 240f;
    [SerializeField] private float maxGroundTiltAngle = 32f;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private Transform currentTarget;
    private float waitTimer;
    private readonly RaycastHit[] groundHitBuffer = new RaycastHit[8];
    private Vector3 lastGroundNormal = Vector3.up;
    private bool hasGroundNormal;

    private void Start()
    {
        AutoAssignVisualTiltRoot();
        currentTarget = pointA;
    }

    private void Update()
    {
        if (rideController != null && rideController.IsRideActive)
        {
            
            return;
        }

        if (pointA == null || pointB == null)
        {
            SetIdleAnimation();
            return;
        }

        if (currentTarget == null)
        {
            currentTarget = pointA;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            SetIdleAnimation();
            return;
        }

        Vector3 targetPosition = currentTarget.position;

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Vector3 horizontalStep = direction.normalized * Mathf.Min(paceSpeed * Time.deltaTime, direction.magnitude);
            transform.position = ResolveGroundedPosition(transform.position + horizontalStep);
        }

        direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime);

            AlignToGround();
        }

        SetWalkAnimation();

        if (HorizontalDistance(transform.position, targetPosition) <= arriveDistance)
        {
            currentTarget = currentTarget == pointA ? pointB : pointA;
            waitTimer = waitAtPointSeconds;

            if (logDebug)
            {
                Debug.Log("[CatIdlePaceV2] Reached pace point, switching target.");
            }
        }
    }

    private void SetIdleAnimation()
    {
        if (kittyAnimator == null)
        {
            return;
        }

        kittyAnimator.SetFloat("Vert", 0f);
        kittyAnimator.SetFloat("State", 0f);
    }

    private void SetWalkAnimation()
    {
        if (kittyAnimator == null)
        {
            return;
        }

        kittyAnimator.SetFloat("Vert", 1f);
        kittyAnimator.SetFloat("State", 0f);
    }

    private Vector3 ResolveGroundedPosition(Vector3 desiredPosition)
    {
        if (!projectMotionToGround)
        {
            hasGroundNormal = false;
            return desiredPosition;
        }

        Vector3 origin = desiredPosition + Vector3.up * Mathf.Max(0.1f, groundProbeHeight);
        float distance = Mathf.Max(0.1f, groundProbeDistance);
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHitBuffer,
            distance,
            groundMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.PositiveInfinity;
        bool foundGround = false;
        Vector3 groundPoint = desiredPosition;
        Vector3 groundNormal = Vector3.up;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = groundHitBuffer[i];
            Collider hitCollider = hit.collider;
            if (hitCollider == null || hitCollider.isTrigger || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                groundPoint = hit.point;
                groundNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
                foundGround = true;
            }
        }

        if (!foundGround)
        {
            hasGroundNormal = false;
            return desiredPosition;
        }

        float deltaY = groundPoint.y - transform.position.y;
        if (deltaY > Mathf.Max(0f, maxStepUp) || deltaY < -Mathf.Max(0f, maxStepDown))
        {
            hasGroundNormal = false;
            return desiredPosition;
        }

        desiredPosition.y = groundPoint.y + groundOffset;
        lastGroundNormal = ClampGroundNormalTilt(groundNormal);
        hasGroundNormal = true;
        return desiredPosition;
    }

    private Vector3 ClampGroundNormalTilt(Vector3 normal)
    {
        if (normal.sqrMagnitude < 0.0001f)
        {
            return Vector3.up;
        }

        normal.Normalize();
        float angle = Vector3.Angle(Vector3.up, normal);
        float maxAngle = Mathf.Max(0f, maxGroundTiltAngle);
        if (angle <= maxAngle || angle <= 0.001f)
        {
            return normal;
        }

        return Vector3.Slerp(Vector3.up, normal, maxAngle / angle).normalized;
    }

    private void AlignToGround()
    {
        if (!alignToGroundNormal || !hasGroundNormal)
        {
            return;
        }

        Transform tiltRoot = visualTiltRoot != null ? visualTiltRoot : transform;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, lastGroundNormal);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(tiltRoot.forward, lastGroundNormal);
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, lastGroundNormal);
        tiltRoot.rotation = Quaternion.RotateTowards(
            tiltRoot.rotation,
            targetRotation,
            Mathf.Max(0f, groundAlignSpeed) * Time.deltaTime);
    }

    private void AutoAssignVisualTiltRoot()
    {
        if (visualTiltRoot != null)
        {
            return;
        }

        if (kittyAnimator != null && kittyAnimator.transform != transform)
        {
            visualTiltRoot = kittyAnimator.transform;
            return;
        }

        Renderer renderer = GetComponentInChildren<Renderer>(true);
        if (renderer != null && renderer.transform != transform)
        {
            visualTiltRoot = renderer.transform;
        }
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
