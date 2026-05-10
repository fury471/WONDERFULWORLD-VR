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

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private Transform currentTarget;
    private float waitTimer;

    private void Start()
    {
        currentTarget = pointA;
    }

    private void Update()
    {
        if (rideController != null && rideController.IsRideActive)
        {
            SetIdleAnimation();
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
        targetPosition.y = transform.position.y;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            paceSpeed * Time.deltaTime);

        Vector3 direction = targetPosition - transform.position;
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

        if (Vector3.Distance(transform.position, targetPosition) <= arriveDistance)
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
}
