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
    [SerializeField] private float groundProbeDistance = 8f;
    [SerializeField] private float groundOffset = 0f;
    [SerializeField] private float maxStepUp = 1.5f;
    [SerializeField] private float maxStepDown = 3f;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private Transform currentTarget;
    private float waitTimer;
    private readonly RaycastHit[] groundHitBuffer = new RaycastHit[8];

    private void Start()
    {
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
                foundGround = true;
            }
        }

        if (!foundGround)
        {
            return desiredPosition;
        }

        float deltaY = groundPoint.y - transform.position.y;
        if (deltaY > Mathf.Max(0f, maxStepUp) || deltaY < -Mathf.Max(0f, maxStepDown))
        {
            return desiredPosition;
        }

        desiredPosition.y = groundPoint.y + groundOffset;
        return desiredPosition;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
