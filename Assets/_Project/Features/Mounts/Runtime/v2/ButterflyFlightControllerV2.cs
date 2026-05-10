using System.Collections.Generic;
using UnityEngine;

public class ButterflyFlightControllerV2 : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private List<Transform> flightPoints = new List<Transform>();

    [Header("Motion")]
    [SerializeField] private float flightSpeed = 2.5f;
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private float reachDistance = 0.08f;
    [SerializeField] private bool resetToStartOnBegin = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool initialPoseCached;
    private int currentPointIndex;
    private bool isFlying;

    private void Awake()
    {
        CacheInitialPose();
    }

    private void Update()
    {
        if (!isFlying)
        {
            return;
        }

        Transform targetPoint = GetCurrentPoint();
        if (targetPoint == null)
        {
            isFlying = false;

            if (debugLogs)
            {
                Debug.Log("[ButterflyFlightControllerV2] Flight complete.");
            }

            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPoint.position,
            flightSpeed * Time.deltaTime);

        Vector3 direction = targetPoint.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetPoint.position) <= reachDistance)
        {
            currentPointIndex++;
        }
    }

    public void BeginFlight()
    {
        CacheInitialPose();

        if (resetToStartOnBegin && initialPoseCached)
        {
            transform.SetPositionAndRotation(initialPosition, initialRotation);
        }

        currentPointIndex = 0;
        isFlying = true;

        if (debugLogs)
        {
            Debug.Log("[ButterflyFlightControllerV2] Flight started.");
        }
    }

    private Transform GetCurrentPoint()
    {
        while (currentPointIndex < flightPoints.Count)
        {
            Transform point = flightPoints[currentPointIndex];
            if (point != null)
            {
                return point;
            }

            currentPointIndex++;
        }

        return null;
    }

    private void CacheInitialPose()
    {
        if (initialPoseCached)
        {
            return;
        }

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialPoseCached = true;
    }
}
